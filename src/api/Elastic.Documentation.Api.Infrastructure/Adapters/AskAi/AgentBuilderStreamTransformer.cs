// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Transforms Agent Builder SSE events to canonical AskAiEvent format
/// </summary>
public class AgentBuilderStreamTransformer(ILogger<AgentBuilderStreamTransformer> logger) : StreamTransformerBase(logger)
{
	// SSE format constants
	private const string EventPrefix = "event:";
	private const string DataPrefix = "data:";
	private const int EventPrefixLength = 6; // "event:".Length
	private const int DataPrefixLength = 5; // "data:".Length

	protected override async Task ProcessStreamAsync(StreamReader reader, PipeWriter writer, CancellationToken cancellationToken)
	{
		Logger.LogInformation("Starting Agent Builder stream transformation");
		string? currentEvent = null;
		var dataBuilder = new StringBuilder();
		var eventCount = 0;

		while (!cancellationToken.IsCancellationRequested)
		{
			var line = await reader.ReadLineAsync(cancellationToken);
			if (line == null)
			{
				Logger.LogInformation("Stream ended after processing {EventCount} events", eventCount);
				break;
			}

			// Parse SSE format
			if (line.Length > 0 && line[0] == ':')
			{
				// Comment/keep-alive - skip
				continue;
			}
			else if (line.StartsWith(EventPrefix, StringComparison.Ordinal))
			{
				currentEvent = line.Substring(EventPrefixLength).Trim();
			}
			else if (line.StartsWith(DataPrefix, StringComparison.Ordinal))
			{
				_ = dataBuilder.Append(line.Substring(DataPrefixLength).Trim());
			}
			else if (string.IsNullOrEmpty(line))
			{
				// End of event - transform and write immediately
				if (currentEvent != null && dataBuilder.Length > 0)
				{
					var eventData = dataBuilder.ToString();
					var transformedEvent = TransformEvent(currentEvent, eventData);
					await WriteEventAsync(transformedEvent, writer, cancellationToken);
					if (transformedEvent != null)
						eventCount++;
				}

				// Reset for next event
				currentEvent = null;
				_ = dataBuilder.Clear();
			}
		}

		Logger.LogInformation("Completed Agent Builder stream transformation. Total events: {EventCount}", eventCount);
	}

	private AskAiEvent? TransformEvent(string eventType, string data)
	{
		try
		{
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			var id = Guid.NewGuid().ToString();

			using var doc = JsonDocument.Parse(data);
			var root = doc.RootElement;

			// Special handling for error events - they may have a different structure
			if (eventType == "error")
			{
				return ParseErrorEventFromRoot(id, timestamp, root);
			}

			// Most Agent Builder events have data nested in a "data" property
			if (!root.TryGetProperty("data", out var innerData))
			{
				Logger.LogDebug("Agent Builder event without 'data' property (skipping): {EventType}", eventType);
				return null;
			}

			return eventType switch
			{
				"conversation_id_set" when innerData.TryGetProperty("conversation_id", out var convId) =>
					new AskAiEvent.ConversationStart(id, timestamp, convId.GetString()!),

				"message_chunk" when innerData.TryGetProperty("text_chunk", out var textChunk) =>
					new AskAiEvent.Chunk(id, timestamp, textChunk.GetString()!),

				"message_complete" when innerData.TryGetProperty("message_content", out var fullContent) =>
					new AskAiEvent.ChunkComplete(id, timestamp, fullContent.GetString()!),

				"reasoning" =>
					// Parse reasoning message if available
					ParseReasoningEvent(id, timestamp, innerData),

				"tool_call" =>
					// Parse tool call
					ParseToolCallEvent(id, timestamp, innerData),

				"tool_result" =>
					// Parse tool result
					ParseToolResultEvent(id, timestamp, innerData),

				"round_complete" =>
					new AskAiEvent.ConversationEnd(id, timestamp),

				"conversation_created" =>
					null, // Skip, already handled by conversation_id_set

				_ => LogUnknownEvent(eventType, data)
			};
		}
		catch (JsonException ex)
		{
			Logger.LogError(ex, "Failed to parse Agent Builder event: {EventType}, data: {Data}", eventType, data);
			return null;
		}
	}

	private AskAiEvent? LogUnknownEvent(string eventType, string data)
	{
		Logger.LogWarning("Unknown Agent Builder event type: {EventType}, data: {Data}", eventType, data);
		return null;
	}

	private AskAiEvent.Reasoning ParseReasoningEvent(string id, long timestamp, JsonElement innerData)
	{
		string? message = null;

		// Try common property names
		if (innerData.TryGetProperty("message", out var msgProp))
			message = msgProp.GetString();
		else if (innerData.TryGetProperty("text", out var textProp))
			message = textProp.GetString();
		else if (innerData.TryGetProperty("content", out var contentProp))
			message = contentProp.GetString();
		else if (innerData.TryGetProperty("reasoning", out var reasoningProp))
			message = reasoningProp.GetString();
		else if (innerData.TryGetProperty("status", out var statusProp))
			message = statusProp.GetString();

		return new AskAiEvent.Reasoning(id, timestamp, message ?? "Thinking...");
	}

	private AskAiEvent.ToolResult ParseToolResultEvent(string id, long timestamp, JsonElement innerData)
	{
		// Extract tool_call_id and results
		var toolCallId = innerData.TryGetProperty("tool_call_id", out var tcId) ? tcId.GetString() : id;

		// Serialize the entire results array as the result string
		var result = innerData.TryGetProperty("results", out var resultsElement)
			? resultsElement.GetRawText()
			: "{}";

		return new AskAiEvent.ToolResult(id, timestamp, toolCallId ?? id, result);
	}

	private AskAiEvent ParseToolCallEvent(string id, long timestamp, JsonElement innerData)
	{
		// Extract fields from Agent Builder's tool_call structure
		var toolCallId = innerData.TryGetProperty("tool_call_id", out var tcId) ? tcId.GetString() : id;
		var toolId = innerData.TryGetProperty("tool_id", out var tId) ? tId.GetString() : "unknown";

		// Check if this is a search tool (docs-esql or similar)
		if (toolId != null && toolId.Contains("docs", StringComparison.OrdinalIgnoreCase))
		{
			// Agent Builder uses "keyword_query" in params
			if (innerData.TryGetProperty("params", out var paramsElement) &&
				paramsElement.TryGetProperty("keyword_query", out var keywordQueryProp))
			{
				var searchQuery = keywordQueryProp.GetString();
				if (!string.IsNullOrEmpty(searchQuery))
				{
					return new AskAiEvent.SearchToolCall(id, timestamp, toolCallId ?? id, searchQuery);
				}
			}
		}

		// Fallback to generic tool call
		var args = innerData.TryGetProperty("params", out var paramsEl)
			? paramsEl.GetRawText()
			: "{}";

		return new AskAiEvent.ToolCall(id, timestamp, toolCallId ?? id, toolId ?? "unknown", args);
	}

	private AskAiEvent.ErrorEvent ParseErrorEventFromRoot(string id, long timestamp, JsonElement root)
	{
		// Try to extract error message from different possible structures
		string? errorMessage = null;

		// Check if there's a data property first
		if (root.TryGetProperty("data", out var dataElement))
		{
			if (dataElement.TryGetProperty("error", out var errorProp))
			{
				// Agent Builder sends: {"data":{"error":{"code":"...","message":"...","meta":{...}}}}
				if (errorProp.ValueKind == JsonValueKind.Object && errorProp.TryGetProperty("message", out var msgProp))
				{
					errorMessage = msgProp.GetString();
				}
				else if (errorProp.ValueKind == JsonValueKind.String)
				{
					errorMessage = errorProp.GetString();
				}
			}
			else if (dataElement.TryGetProperty("message", out var directMsg))
			{
				errorMessage = directMsg.GetString();
			}
		}
		// Or error might be at root level
		else if (root.TryGetProperty("error", out var rootError))
		{
			if (rootError.ValueKind == JsonValueKind.Object && rootError.TryGetProperty("message", out var msgProp))
			{
				errorMessage = msgProp.GetString();
			}
			else if (rootError.ValueKind == JsonValueKind.String)
			{
				errorMessage = rootError.GetString();
			}
		}
		else if (root.TryGetProperty("message", out var rootMsg))
		{
			errorMessage = rootMsg.GetString();
		}

		Logger.LogError("Error event received from Agent Builder: {ErrorMessage}", errorMessage ?? "Unknown error");

		return new AskAiEvent.ErrorEvent(id, timestamp, errorMessage ?? "Unknown error occurred");
	}
}
