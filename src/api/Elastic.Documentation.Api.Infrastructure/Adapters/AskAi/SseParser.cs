// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Represents a parsed Server-Sent Event (SSE)
/// </summary>
/// <param name="EventType">The event type from the "event:" field, or null if not specified</param>
/// <param name="Data">The accumulated data from all "data:" fields</param>
public record SseEvent(string? EventType, string Data);

/// <summary>
/// Parser for Server-Sent Events (SSE) following the W3C SSE specification.
/// </summary>
public static class SseParser
{
	/// <summary>
	/// Parse Server-Sent Events (SSE) from a PipeReader following the W3C SSE specification.
	/// This method handles the standard SSE format with event:, data:, and comment lines.
	/// </summary>
	public static async IAsyncEnumerable<SseEvent> ParseAsync(
		PipeReader reader,
		[EnumeratorCancellation] CancellationToken cancellationToken = default
	)
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
