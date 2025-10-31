// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Represents a parsed Server-Sent Event (SSE)
/// </summary>
/// <param name="EventType">The event type from the "event:" field, or null if not specified</param>
/// <param name="Data">The accumulated data from all "data:" fields</param>
public record SseEvent(string? EventType, string Data);

/// <summary>
/// Base class for stream transformers that handles common streaming logic
/// </summary>
public abstract class StreamTransformerBase(ILogger logger) : IStreamTransformer
{
	protected ILogger Logger { get; } = logger;

	// ActivitySource for tracing streaming operations
	private static readonly ActivitySource StreamTransformerActivitySource = new("Elastic.Documentation.Api.StreamTransformer");

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

	public Task<Stream> TransformAsync(Stream rawStream, Activity? parentActivity, Cancel cancellationToken = default)
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
		_ = ProcessPipeAsync(reader, pipe.Writer, parentActivity, cancellationToken);

		// Return the read side of the pipe as a stream
		return Task.FromResult(pipe.Reader.AsStream());
	}

	/// <summary>
	/// Process the pipe reader and write transformed events to the pipe writer.
	/// This runs concurrently with the consumer reading from the output stream.
	/// </summary>
	private async Task ProcessPipeAsync(PipeReader reader, PipeWriter writer, Activity? parentActivity, CancellationToken cancellationToken)
	{
		try
		{
			try
			{
				await ProcessStreamAsync(reader, writer, parentActivity, cancellationToken);
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
					await writer.CompleteAsync(ex);
					await reader.CompleteAsync(ex);
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
				await reader.CompleteAsync();
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
	private async Task ProcessStreamAsync(PipeReader reader, PipeWriter writer, Activity? parentActivity, CancellationToken cancellationToken)
	{
		using var activity = StreamTransformerActivitySource.StartActivity("transform_stream");

		if (parentActivity?.Id != null)
			_ = activity?.SetParentId(parentActivity.Id);

		List<MessagePart> outputMessageParts = [];
		await foreach (var sseEvent in ParseSseEventsAsync(reader, cancellationToken))
		{
			using var parseActivity = StreamTransformerActivitySource.StartActivity("parse_event");
			// parseActivity automatically inherits from Activity.Current (transform_stream)

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
				continue;

			// Set event type tag on parse_event activity
			_ = parseActivity?.SetTag("ask_ai.event", transformedEvent.GetType().Name);

			switch (transformedEvent)
			{
				case AskAiEvent.ConversationStart conversationStart:
					{
						_ = parentActivity?.SetTag("gen_ai.conversation.id", conversationStart.ConversationId);
						_ = activity?.SetTag("gen_ai.conversation.id", conversationStart.ConversationId);
						break;
					}
				case AskAiEvent.Reasoning reasoning:
					{
						outputMessageParts.Add(new MessagePart("reasoning", reasoning.Message ?? string.Empty));
						break;
					}
				case AskAiEvent.MessageChunk:
					{
						// Event type already tagged above
						break;
					}

				case AskAiEvent.ErrorEvent errorEvent:
					{
						_ = activity?.SetStatus(ActivityStatusCode.Error, "AI provider error event");
						_ = activity?.SetTag("error.type", "AIProviderError");
						_ = activity?.SetTag("error.message", errorEvent.Message);
						_ = parseActivity?.SetStatus(ActivityStatusCode.Error, errorEvent.Message);
						break;
					}
				case AskAiEvent.ToolCall:
					{
						// Event type already tagged above
						break;
					}
				case AskAiEvent.SearchToolCall searchToolCall:
					{
						_ = parseActivity?.SetTag("search.query", searchToolCall.SearchQuery);
						break;
					}
				case AskAiEvent.ToolResult toolResult:
					{
						_ = parseActivity?.SetTag("tool.result_summary", toolResult.Result);
						break;
					}
				case AskAiEvent.MessageComplete chunkComplete:
					{
						outputMessageParts.Add(new MessagePart("text", chunkComplete.FullContent));
						Logger.LogInformation("AskAI output message: {OutputMessage}", chunkComplete.FullContent);
						break;
					}
				case AskAiEvent.ConversationEnd:
					{
						// Event type already tagged above
						break;
					}
			}
			await WriteEventAsync(transformedEvent, writer, cancellationToken);
		}

		// Set output messages tag once after all events are processed
		if (outputMessageParts.Count > 0)
		{
			var outputMessages = new OutputMessage("assistant", outputMessageParts.ToArray(), "stop");
			var outputMessagesJson = JsonSerializer.Serialize(outputMessages, ApiJsonContext.Default.OutputMessage);
			_ = parentActivity?.SetTag("gen_ai.output.messages", outputMessagesJson);
			_ = activity?.SetTag("gen_ai.output.messages", outputMessagesJson);
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
	private async Task WriteEventAsync(AskAiEvent? transformedEvent, PipeWriter writer, CancellationToken cancellationToken)
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

	/// <summary>
	/// Parse Server-Sent Events (SSE) from a PipeReader following the W3C SSE specification.
	/// This method handles the standard SSE format with event:, data:, and comment lines.
	/// </summary>
	private static async IAsyncEnumerable<SseEvent> ParseSseEventsAsync(
		PipeReader reader,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		string? currentEvent = null;
		var dataBuilder = new StringBuilder();

		while (!cancellationToken.IsCancellationRequested)
		{
			var result = await reader.ReadAsync(cancellationToken);
			var buffer = result.Buffer;

			// Process all complete lines in the buffer
			while (TryReadLine(ref buffer, out var line))
			{
				// SSE comment line - skip
				if (line.Length > 0 && line[0] == ':')
					continue;

				// Event type line
				if (line.StartsWith("event:", StringComparison.Ordinal))
					currentEvent = line[6..].Trim();
				// Data line
				else if (line.StartsWith("data:", StringComparison.Ordinal))
					_ = dataBuilder.Append(line[5..].Trim());
				// Empty line - marks end of event
				else if (string.IsNullOrEmpty(line))
				{
					if (dataBuilder.Length <= 0)
						continue;
					yield return new SseEvent(currentEvent, dataBuilder.ToString());
					currentEvent = null;
					_ = dataBuilder.Clear();
				}
			}

			// Tell the PipeReader how much of the buffer we consumed
			reader.AdvanceTo(buffer.Start, buffer.End);

			// Stop reading if there's no more data coming
			if (!result.IsCompleted)
				continue;

			// Yield any remaining event that hasn't been terminated with an empty line
			if (dataBuilder.Length > 0)
				yield return new SseEvent(currentEvent, dataBuilder.ToString());
			break;
		}
	}

	/// <summary>
	/// Try to read a single line from the buffer
	/// </summary>
	private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out string line)
	{
		// Look for a line ending
		var position = buffer.PositionOf((byte)'\n');

		if (position == null)
		{
			line = string.Empty;
			return false;
		}

		// Extract the line (excluding the \n)
		var lineSlice = buffer.Slice(0, position.Value);
		line = Encoding.UTF8.GetString(lineSlice).TrimEnd('\r');

		// Skip past the line + \n
		buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
		return true;
	}
}
