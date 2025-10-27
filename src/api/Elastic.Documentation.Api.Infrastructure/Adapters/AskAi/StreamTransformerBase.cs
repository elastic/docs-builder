// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
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

	public Task<Stream> TransformAsync(Stream rawStream, CancellationToken cancellationToken = default)
	{
		var pipe = new Pipe();
		var reader = PipeReader.Create(rawStream);

		// Start processing task to transform and write events to pipe
		// Note: We intentionally don't await this task as we need to return the stream immediately
		// The pipe handles synchronization and backpressure between producer and consumer
		_ = ProcessPipeAsync(reader, pipe.Writer, cancellationToken);

		// Return the read side of the pipe as a stream
		return Task.FromResult(pipe.Reader.AsStream());
	}

	/// <summary>
	/// Process the pipe reader and write transformed events to the pipe writer.
	/// This runs concurrently with the consumer reading from the output stream.
	/// </summary>
	private async Task ProcessPipeAsync(PipeReader reader, PipeWriter writer, CancellationToken cancellationToken)
	{
		try
		{
			await ProcessStreamAsync(reader, writer, cancellationToken);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error transforming stream. Stream processing will be terminated.");
			await writer.CompleteAsync(ex);
			await reader.CompleteAsync(ex);
			return;
		}

		await writer.CompleteAsync();
		await reader.CompleteAsync();
	}

	/// <summary>
	/// Process the raw stream and write transformed events to the pipe writer.
	/// Default implementation parses SSE events and JSON, then calls TransformJsonEvent.
	/// </summary>
	protected virtual async Task ProcessStreamAsync(PipeReader reader, PipeWriter writer, CancellationToken cancellationToken)
	{
		await foreach (var sseEvent in ParseSseEventsAsync(reader, cancellationToken))
		{
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
				Logger.LogError(ex, "Failed to parse JSON from SSE event: {Data}", sseEvent.Data);
			}

			if (transformedEvent != null)
			{
				await WriteEventAsync(transformedEvent, writer, cancellationToken);
			}
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

		// Serialize as base AskAiEvent type to include the type discriminator
		var json = JsonSerializer.Serialize<AskAiEvent>(transformedEvent, AskAiEventJsonContext.Default.AskAiEvent);
		var sseData = $"data: {json}\n\n";
		var bytes = Encoding.UTF8.GetBytes(sseData);

		// Write to pipe and flush immediately for real-time streaming
		_ = await writer.WriteAsync(bytes, cancellationToken);
		_ = await writer.FlushAsync(cancellationToken);
	}

	/// <summary>
	/// Parse Server-Sent Events (SSE) from a PipeReader following the W3C SSE specification.
	/// This method handles the standard SSE format with event:, data:, and comment lines.
	/// </summary>
	protected async IAsyncEnumerable<SseEvent> ParseSseEventsAsync(
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
					yield return new SseEvent(currentEvent, dataBuilder.ToString());
				}
				break;
			}
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
