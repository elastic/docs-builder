// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Base class for stream transformers that handles common streaming logic
/// </summary>
public abstract class StreamTransformerBase(ILogger logger) : IStreamTransformer
{
	protected ILogger Logger { get; } = logger;

	// ActivitySource for tracing streaming operations
	private static readonly ActivitySource StreamTransformerActivitySource = new(TelemetryConstants.StreamTransformerSourceName);

	/// <summary>
	/// Get the agent ID for this transformer
	/// </summary>
	protected abstract string GetAgentId();

	/// <summary>
	/// Get the agent provider/platform for this transformer
	/// </summary>
	protected abstract string GetAgentProvider();

	/// <summary>
	/// Public property to expose agent ID (implements IStreamTransformer)
	/// </summary>
	public string AgentId => GetAgentId();

	/// <summary>
	/// Public property to expose agent provider (implements IStreamTransformer)
	/// </summary>
	public string AgentProvider => GetAgentProvider();

	public Task<Stream> TransformAsync(Stream rawStream, string? conversationId, Activity? parentActivity, Cancel cancellationToken = default)
	{
		// Configure pipe for low-latency streaming
		var pipeOptions = new PipeOptions(
			minimumSegmentSize: 1024, // Smaller segments for faster processing
			pauseWriterThreshold: 64 * 1024, // 64KB high water mark
			resumeWriterThreshold: 32 * 1024, // 32KB low water mark
			readerScheduler: PipeScheduler.Inline,
			writerScheduler: PipeScheduler.Inline,
			useSynchronizationContext: false
		);

		var pipe = new Pipe(pipeOptions);
		var reader = PipeReader.Create(rawStream);

		// Start processing task to transform and write events to pipe
		// Note: We intentionally don't await this task as we need to return the stream immediately
		// The pipe handles synchronization and backpressure between producer and consumer
		// Pass parent activity - it will be disposed when streaming completes
		_ = ProcessPipeAsync(reader, pipe.Writer, conversationId, parentActivity, cancellationToken);

		// Return the read side of the pipe as a stream
		return Task.FromResult(pipe.Reader.AsStream());
	}

	/// <summary>
	/// Process the pipe reader and write transformed events to the pipe writer.
	/// This runs concurrently with the consumer reading from the output stream.
	/// </summary>
	private async Task ProcessPipeAsync(PipeReader reader, PipeWriter writer, string? conversationId, Activity? parentActivity, CancellationToken cancellationToken)
	{
		try
		{
			try
			{
				await ProcessStreamAsync(reader, writer, conversationId, parentActivity, cancellationToken);
			}
			catch (OperationCanceledException ex)
			{
				Logger.LogDebug(ex, "Stream processing was cancelled for transformer {TransformerType}", GetType().Name);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error transforming stream for transformer {TransformerType}. Stream processing will be terminated.", GetType().Name);
				_ = parentActivity?.SetTag("error.type", ex.GetType().Name);
				try
				{
					// Complete writer first, then reader - but don't try to complete reader
					// if the exception came from reading (would cause "read operation pending" error)
					await writer.CompleteAsync(ex);
				}
				catch (Exception completeEx)
				{
					Logger.LogError(completeEx, "Error completing pipe after transformation error for transformer {TransformerType}", GetType().Name);
				}
				return;
			}

			// Normal completion - ensure cleanup happens
			try
			{
				await writer.CompleteAsync();
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Error completing pipe after successful transformation");
			}
		}
		finally
		{
			parentActivity?.Dispose();
		}
	}


	/// <summary>
	/// Process the raw stream and write transformed events to the pipe writer.
	/// Default implementation parses SSE events and JSON, then calls TransformJsonEvent.
	/// </summary>
	/// <returns>Stream processing result with metrics and captured output</returns>
	protected virtual async Task ProcessStreamAsync(PipeReader reader, PipeWriter writer, string? conversationId, Activity? parentActivity, CancellationToken cancellationToken)
	{
		using var activity = StreamTransformerActivitySource.StartActivity("process ask_ai stream", ActivityKind.Internal);

		if (parentActivity?.Id != null)
			_ = activity?.SetParentId(parentActivity.Id);

		List<MessagePart> outputMessageParts = [];
		await foreach (var sseEvent in SseParser.ParseAsync(reader, cancellationToken))
		{
			AskAiEvent? transformedEvent;
			try
			{
				// Parse JSON once in base class
				using var doc = JsonDocument.Parse(sseEvent.Data);
				var root = doc.RootElement;

				// Subclass transforms JsonElement to AskAiEvent
				transformedEvent = TransformJsonEvent(sseEvent.EventType, root);
			}
			catch (JsonException ex)
			{
				Logger.LogError(ex, "Failed to parse JSON from SSE event for transformer {TransformerType}. EventType: {EventType}, Data: {Data}",
					GetType().Name, sseEvent.EventType, sseEvent.Data);
				throw;
			}

			if (transformedEvent == null)
			{
				Logger.LogWarning("Transformed event is null for transformer {TransformerType}. Skipping event. EventType: {EventType}",
					GetType().Name, sseEvent.EventType);
				Logger.LogWarning("Original event: {event}", JsonSerializer.Serialize(sseEvent, SseSerializerContext.Default.SseEvent));
				continue;
			}

			var askAiEventType = transformedEvent.GetType().Name;
			using var parseActivity = StreamTransformerActivitySource.StartActivity($"parse ask_ai event {askAiEventType}");

			// Set event type tag on parse_event activity
			_ = parseActivity?.SetTag("ask_ai.event.type", askAiEventType);
			_ = parseActivity?.SetTag("gen_ai.response.id", transformedEvent.Id);

			switch (transformedEvent)
			{
				case AskAiEvent.ConversationStart conversationStart:
					{
						_ = parentActivity?.SetTag("gen_ai.conversation.id", conversationStart.ConversationId);
						_ = activity?.SetTag("gen_ai.conversation.id", conversationStart.ConversationId);
						Logger.LogDebug("AskAI conversation started: {ConversationId}", conversationStart.ConversationId);
						break;
					}
				case AskAiEvent.Reasoning reasoning:
					{
						Logger.LogDebug("AskAI reasoning: {ReasoningMessage}", reasoning.Message);
						outputMessageParts.Add(new MessagePart("reasoning", reasoning.Message ?? string.Empty));
						break;
					}
				case AskAiEvent.MessageChunk messageChunk:
					{
						Logger.LogDebug("AskAI message chunk: {ChunkContent}", messageChunk.Content);
						// Event type already tagged above
						break;
					}

				case AskAiEvent.ErrorEvent errorEvent:
					{
						_ = activity?.SetStatus(ActivityStatusCode.Error, "AI provider error event");
						_ = activity?.SetTag("error.type", "AIProviderError");
						_ = activity?.SetTag("error.message", errorEvent.Message);
						_ = parseActivity?.SetStatus(ActivityStatusCode.Error, errorEvent.Message);
						Logger.LogError("AskAI error event: {Message}", errorEvent.Message);
						break;
					}
				case AskAiEvent.ToolCall toolCall:
					{
						// Event type already tagged above
						Logger.LogDebug("AskAI tool call: {ToolCall}", toolCall.ToolName);
						break;
					}
				case AskAiEvent.SearchToolCall searchToolCall:
					{
						_ = parseActivity?.SetTag("search.query", searchToolCall.SearchQuery);
						Logger.LogDebug("AskAI search tool call: {SearchQuery}", searchToolCall.SearchQuery);
						break;
					}
				case AskAiEvent.ToolResult toolResult:
					{
						Logger.LogDebug("AskAI tool result: {ToolResult}", toolResult.Result);
						break;
					}
				case AskAiEvent.MessageComplete messageComplete:
					{
						outputMessageParts.Add(new MessagePart("text", messageComplete.FullContent));
						Logger.LogInformation("AskAI output message: {ask_ai.output.message}", messageComplete.FullContent);
						break;
					}
				case AskAiEvent.ConversationEnd conversationEnd:
					{
						Logger.LogDebug("AskAI conversation end: {ConversationId}", conversationEnd.Id);
						break;
					}
			}
			await WriteEventAsync(transformedEvent, writer, cancellationToken);
		}

		// Set output messages tag once after all events are processed
		if (outputMessageParts.Count > 0)
		{
			var outputMessage = new OutputMessage("assistant", outputMessageParts.ToArray(), "stop");
			var outputMessages = new[] { outputMessage };
			var outputMessagesJson = JsonSerializer.Serialize(outputMessages, ApiJsonContext.Default.OutputMessageArray);
			_ = parentActivity?.SetTag("gen_ai.output.messages", outputMessagesJson);
		}
	}

	/// <summary>
	/// Transform a parsed JSON event into an AskAiEvent.
	/// Subclasses implement provider-specific transformation logic.
	/// </summary>
	/// <param name="eventType">The SSE event type (from "event:" field), or null if not present</param>
	/// <param name="json">The parsed JSON data from the "data:" field</param>
	/// <returns>The transformed AskAiEvent, or null to skip this event</returns>
	protected abstract AskAiEvent? TransformJsonEvent(string? eventType, JsonElement json);

	/// <summary>
	/// Write a transformed event to the output stream
	/// </summary>
	protected async Task WriteEventAsync(AskAiEvent? transformedEvent, PipeWriter writer, CancellationToken cancellationToken)
	{
		if (transformedEvent == null)
			return;
		try
		{
			// Serialize as base AskAiEvent type to include the type discriminator
			var json = JsonSerializer.Serialize<AskAiEvent>(transformedEvent, AskAiEventJsonContext.Default.AskAiEvent);
			var sseData = $"data: {json}\n\n";
			var bytes = Encoding.UTF8.GetBytes(sseData);

			// Write to pipe and flush immediately for real-time streaming
			_ = await writer.WriteAsync(bytes, cancellationToken);
			_ = await writer.FlushAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error writing event to stream for transformer {TransformerType}. EventType: {EventType}",
				GetType().Name, transformedEvent.GetType().Name);
			throw; // Re-throw to be handled by caller
		}
	}
}
