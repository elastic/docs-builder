// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
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
	protected override string GetAgentId() => AgentBuilderAskAiGateway.ModelName;
	protected override string GetAgentProvider() => AgentBuilderAskAiGateway.ProviderName;
	protected override AskAiEvent? TransformJsonEvent(string? eventType, JsonElement json)
	{
		var type = eventType ?? "message";
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		var id = Guid.NewGuid().ToString();

		// Special handling for error events - they may have a different structure
		if (type == "error")
			return ParseErrorEventFromRoot(id, timestamp, json);

		// Most Agent Builder events have data nested in a "data" property
		if (!json.TryGetProperty("data", out var innerData))
		{
			Logger.LogDebug("Agent Builder event without 'data' property (skipping): {EventType}", type);
			return null;
		}

		return type switch
		{
			"conversation_id_set" when innerData.TryGetProperty("conversation_id", out var convId) =>
				new AskAiEvent.ConversationStart(id, timestamp, convId.GetString()!),

			"message_chunk" when innerData.TryGetProperty("text_chunk", out var textChunk) =>
				new AskAiEvent.MessageChunk(id, timestamp, textChunk.GetString()!),

			"message_complete" when innerData.TryGetProperty("message_content", out var fullContent) =>
				new AskAiEvent.MessageComplete(id, timestamp, fullContent.GetString()!),

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

			_ => LogUnknownEvent(type, json)
		};
	}

	private AskAiEvent? LogUnknownEvent(string eventType, JsonElement _)
	{
		Logger.LogWarning("Unknown Agent Builder event type: {EventType}", eventType);
		return null;
	}

	private static AskAiEvent.Reasoning ParseReasoningEvent(string id, long timestamp, JsonElement innerData)
	{
		// Agent Builder sends: {"data":{"reasoning":"..."}}
		var message = innerData.TryGetProperty("reasoning", out var reasoningProp)
			? reasoningProp.GetString()
			: null;

		return new AskAiEvent.Reasoning(id, timestamp, message ?? "Thinking...");
	}

	private static AskAiEvent.ToolResult ParseToolResultEvent(string id, long timestamp, JsonElement innerData)
	{
		// Extract tool_call_id and results
		var toolCallId = innerData.TryGetProperty("tool_call_id", out var tcId) ? tcId.GetString() : id;

		// Serialize the entire results array as the result string
		var result = innerData.TryGetProperty("results", out var resultsElement)
			? resultsElement.GetRawText()
			: "{}";

		return new AskAiEvent.ToolResult(id, timestamp, toolCallId ?? id, result);
	}

	private static AskAiEvent ParseToolCallEvent(string id, long timestamp, JsonElement innerData)
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

	private static AskAiEvent.ErrorEvent ParseErrorEventFromRoot(string id, long timestamp, JsonElement root)
	{
		// Agent Builder sends: {"error":{"code":"...","message":"...","meta":{...}}}
		var errorMessage = root.TryGetProperty("error", out var errorProp) &&
						   errorProp.TryGetProperty("message", out var msgProp)
			? msgProp.GetString()
			: null;
		return new AskAiEvent.ErrorEvent(id, timestamp, errorMessage ?? "Unknown error occurred");
	}
}
