// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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

	public Task<Stream> TransformAsync(Stream rawStream, CancellationToken cancellationToken = default)
	{
		using var activity = StreamTransformerActivitySource.StartActivity($"chat {GetAgentId()}", ActivityKind.Client);
		_ = (activity?.SetTag("gen_ai.operation.name", "chat"));
		_ = (activity?.SetTag("gen_ai.request.model", GetAgentId()));
		_ = (activity?.SetTag("gen_ai.agent.name", GetAgentId()));
		_ = (activity?.SetTag("gen_ai.provider.name", GetAgentProvider()));

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
		_ = ProcessPipeAsync(reader, pipe.Writer, activity, cancellationToken);

		// Return the read side of the pipe as a stream
		return Task.FromResult(pipe.Reader.AsStream());
	}

	/// <summary>
	/// Process the pipe reader and write transformed events to the pipe writer.
	/// This runs concurrently with the consumer reading from the output stream.
	/// </summary>
	private async Task ProcessPipeAsync(PipeReader reader, PipeWriter writer, Activity? parentActivity, CancellationToken cancellationToken)
	{
		using var activity = StreamTransformerActivitySource.StartActivity("gen_ai.agent.pipe");
		_ = (activity?.SetTag("transformer.type", GetType().Name));

		try
		{
			await ProcessStreamAsync(reader, writer, parentActivity, cancellationToken);
		}
		catch (OperationCanceledException ex)
		{
			// Cancellation is expected and not an error - log as debug
			Logger.LogDebug(ex, "Stream processing was cancelled for transformer {TransformerType}", GetType().Name);
			_ = (activity?.SetTag("gen_ai.response.error", true));
			_ = (activity?.SetTag("gen_ai.response.error_type", "OperationCanceledException"));

			// Add error event to activity
			_ = (activity?.AddEvent(new ActivityEvent("gen_ai.error",
				timestamp: DateTimeOffset.UtcNow,
				tags:
				[
					new KeyValuePair<string, object?>("gen_ai.error.type", "OperationCanceledException"),
					new KeyValuePair<string, object?>("gen_ai.error.message", "Stream processing was cancelled"),
					new KeyValuePair<string, object?>("gen_ai.transformer.type", GetType().Name)
				])));

			try
			{
				await writer.CompleteAsync(ex);
				await reader.CompleteAsync(ex);
			}
			catch (Exception completeEx)
			{
				Logger.LogError(completeEx, "Error completing pipe after cancellation for transformer {TransformerType}", GetType().Name);
			}
			return;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error transforming stream for transformer {TransformerType}. Stream processing will be terminated.", GetType().Name);
			_ = (activity?.SetTag("gen_ai.response.error", true));
			_ = (activity?.SetTag("gen_ai.response.error_type", ex.GetType().Name));
			_ = (activity?.SetTag("gen_ai.response.error_message", ex.Message));

			// Add error event to activity
			_ = (activity?.AddEvent(new ActivityEvent("gen_ai.error",
				timestamp: DateTimeOffset.UtcNow,
				tags:
				[
					new KeyValuePair<string, object?>("gen_ai.error.type", ex.GetType().Name),
					new KeyValuePair<string, object?>("gen_ai.error.message", ex.Message),
					new KeyValuePair<string, object?>("gen_ai.transformer.type", GetType().Name),
					new KeyValuePair<string, object?>("gen_ai.error.stack_trace", ex.StackTrace ?? "")
				])));

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

	/// <summary>
	/// Process the raw stream and write transformed events to the pipe writer.
	/// Default implementation parses SSE events and JSON, then calls TransformJsonEvent.
	/// </summary>
	protected virtual async Task ProcessStreamAsync(PipeReader reader, PipeWriter writer, Activity? parentActivity, CancellationToken cancellationToken)
	{
		using var activity = StreamTransformerActivitySource.StartActivity("gen_ai.agent.stream");
		_ = (activity?.SetTag("gen_ai.agent.name", GetAgentId()));
		_ = (activity?.SetTag("gen_ai.provider.name", GetAgentProvider()));

		var eventCount = 0;
		var jsonParseErrors = 0;

		await foreach (var sseEvent in ParseSseEventsAsync(reader, cancellationToken))
		{
			eventCount++;
			AskAiEvent? transformedEvent = null;

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
				jsonParseErrors++;
				Logger.LogError(ex, "Failed to parse JSON from SSE event for transformer {TransformerType}. EventType: {EventType}, Data: {Data}",
					GetType().Name, sseEvent.EventType, sseEvent.Data);

				// Add error event to activity for JSON parsing failures
				_ = (activity?.AddEvent(new ActivityEvent("gen_ai.error",
					timestamp: DateTimeOffset.UtcNow,
					tags:
					[
						new KeyValuePair<string, object?>("gen_ai.error.type", "JsonException"),
						new KeyValuePair<string, object?>("gen_ai.error.message", ex.Message),
						new KeyValuePair<string, object?>("gen_ai.transformer.type", GetType().Name),
						new KeyValuePair<string, object?>("gen_ai.sse.event_type", sseEvent.EventType ?? "unknown"),
						new KeyValuePair<string, object?>("gen_ai.sse.data", sseEvent.Data)
					])));
			}

			if (transformedEvent != null)
			{
				// Update parent activity with conversation ID and model info when we receive ConversationStart events
				if (transformedEvent is AskAiEvent.ConversationStart conversationStart)
				{
					_ = (parentActivity?.SetTag("gen_ai.conversation.id", conversationStart.ConversationId));
					_ = (parentActivity?.SetTag("gen_ai.request.model", GetAgentId()));
					_ = (parentActivity?.SetTag("gen_ai.agent.name", GetAgentId()));
					_ = (parentActivity?.SetTag("gen_ai.provider.name", GetAgentProvider()));
					_ = (activity?.SetTag("gen_ai.conversation.id", conversationStart.ConversationId));
				}

				await WriteEventAsync(transformedEvent, writer, cancellationToken);
			}
		}

		// Set metrics on the activity using GenAI conventions
		_ = (activity?.SetTag("gen_ai.response.token_count", eventCount));
		_ = (activity?.SetTag("gen_ai.response.error_count", jsonParseErrors));
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

		using var activity = StreamTransformerActivitySource.StartActivity("gen_ai.agent.token");
		_ = (activity?.SetTag("gen_ai.agent.name", GetAgentId()));
		_ = (activity?.SetTag("gen_ai.provider.name", GetAgentProvider()));
		_ = (activity?.SetTag("gen_ai.response.token_type", transformedEvent.GetType().Name));

		try
		{
			// Add GenAI completion event for each token/chunk
			_ = (activity?.AddEvent(new ActivityEvent("gen_ai.content.completion",
				timestamp: DateTimeOffset.UtcNow,
				tags:
				[
					new KeyValuePair<string, object?>("gen_ai.completion", JsonSerializer.Serialize(transformedEvent, AskAiEventJsonContext.Default.AskAiEvent))
				])));

			// Serialize as base AskAiEvent type to include the type discriminator
			var json = JsonSerializer.Serialize<AskAiEvent>(transformedEvent, AskAiEventJsonContext.Default.AskAiEvent);
			var sseData = $"data: {json}\n\n";
			var bytes = Encoding.UTF8.GetBytes(sseData);

			_ = (activity?.SetTag("gen_ai.response.token_size", bytes.Length));

			// Write to pipe and flush immediately for real-time streaming
			_ = await writer.WriteAsync(bytes, cancellationToken);
			_ = await writer.FlushAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error writing event to stream for transformer {TransformerType}. EventType: {EventType}",
				GetType().Name, transformedEvent.GetType().Name);

			// Add error event to activity
			_ = (activity?.AddEvent(new ActivityEvent("gen_ai.error",
				timestamp: DateTimeOffset.UtcNow,
				tags:
				[
					new KeyValuePair<string, object?>("gen_ai.error.type", ex.GetType().Name),
					new KeyValuePair<string, object?>("gen_ai.error.message", ex.Message),
					new KeyValuePair<string, object?>("gen_ai.transformer.type", GetType().Name),
					new KeyValuePair<string, object?>("gen_ai.event.type", transformedEvent.GetType().Name)
				])));

			throw; // Re-throw to be handled by caller
		}
	}

	/// <summary>
	/// Parse Server-Sent Events (SSE) from a PipeReader following the W3C SSE specification.
	/// This method handles the standard SSE format with event:, data:, and comment lines.
	/// </summary>
	protected async IAsyncEnumerable<SseEvent> ParseSseEventsAsync(
		PipeReader reader,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		using var activity = StreamTransformerActivitySource.StartActivity("gen_ai.agent.parse");
		_ = (activity?.SetTag("gen_ai.agent.name", GetAgentId()));
		_ = (activity?.SetTag("gen_ai.provider.name", GetAgentProvider()));

		string? currentEvent = null;
		var dataBuilder = new StringBuilder();
		var eventsParsed = 0;
		var readOperations = 0;
		var totalBytesRead = 0L;

		while (!cancellationToken.IsCancellationRequested)
		{
			readOperations++;
			var result = await reader.ReadAsync(cancellationToken);
			var buffer = result.Buffer;
			totalBytesRead += buffer.Length;

			// Process all complete lines in the buffer
			while (TryReadLine(ref buffer, out var line))
			{
				// SSE comment line - skip
				if (line.Length > 0 && line[0] == ':')
					continue;

				// Event type line
				if (line.StartsWith("event:", StringComparison.Ordinal))
				{
					currentEvent = line.Substring(6).Trim();
				}
				// Data line
				else if (line.StartsWith("data:", StringComparison.Ordinal))
				{
					_ = dataBuilder.Append(line.Substring(5).Trim());
				}
				// Empty line - marks end of event
				else if (string.IsNullOrEmpty(line))
				{
					if (dataBuilder.Length > 0)
					{
						eventsParsed++;
						yield return new SseEvent(currentEvent, dataBuilder.ToString());
						currentEvent = null;
						_ = dataBuilder.Clear();
					}
				}
			}

			// Tell the PipeReader how much of the buffer we consumed
			reader.AdvanceTo(buffer.Start, buffer.End);

			// Stop reading if there's no more data coming
			if (result.IsCompleted)
			{
				// Yield any remaining event that hasn't been terminated with an empty line
				if (dataBuilder.Length > 0)
				{
					eventsParsed++;
					yield return new SseEvent(currentEvent, dataBuilder.ToString());
				}
				break;
			}
		}

		// Set metrics on the activity using GenAI conventions
		_ = (activity?.SetTag("gen_ai.response.token_count", eventsParsed));
		_ = (activity?.SetTag("gen_ai.request.input_size", totalBytesRead));
		_ = (activity?.SetTag("gen_ai.model.operation_count", readOperations));
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
