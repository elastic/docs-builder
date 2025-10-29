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
/// Transforms LLM Gateway SSE events to canonical AskAiEvent format
/// </summary>
public class LlmGatewayStreamTransformer(ILogger<LlmGatewayStreamTransformer> logger) : StreamTransformerBase(logger)
{
	protected override AskAiEvent? TransformJsonEvent(string? eventType, JsonElement json)
	{
		// LLM Gateway format: ["custom", {type: "...", ...}]
		if (json.ValueKind != JsonValueKind.Array || json.GetArrayLength() < 2)
		{
			Logger.LogWarning("LLM Gateway data is not in expected array format");
			return null;
		}

		// Extract the actual message object from index 1 (index 0 is always "custom")
		var message = json[1];
		var type = message.GetProperty("type").GetString();
		var timestamp = message.GetProperty("timestamp").GetInt64();
		var id = message.GetProperty("id").GetString()!;
		var messageData = message.GetProperty("data");

		return type switch
		{
			"agent_start" =>
				// LLM Gateway doesn't provide conversation ID, so generate one
				new AskAiEvent.ConversationStart(id, timestamp, Guid.NewGuid().ToString()),

			"ai_message_chunk" when messageData.TryGetProperty("content", out var content) =>
				new AskAiEvent.Chunk(id, timestamp, content.GetString()!),

			"ai_message" when messageData.TryGetProperty("content", out var fullContent) =>
				new AskAiEvent.ChunkComplete(id, timestamp, fullContent.GetString()!),

			"tool_call" when messageData.TryGetProperty("toolCalls", out var toolCalls) =>
				TransformToolCall(id, timestamp, toolCalls),

			"tool_message" when messageData.TryGetProperty("toolCallId", out var toolCallId)
				&& messageData.TryGetProperty("result", out var result) =>
				new AskAiEvent.ToolResult(id, timestamp, toolCallId.GetString()!, result.GetString()!),

			"agent_end" =>
				new AskAiEvent.ConversationEnd(id, timestamp),

			"chat_model_start" or "chat_model_end" =>
				null, // Skip model lifecycle events

			_ => LogUnknownEvent(type, json)
		};
	}

	private AskAiEvent? TransformToolCall(string id, long timestamp, JsonElement toolCalls)
	{
		try
		{
			if (toolCalls.ValueKind != JsonValueKind.Array || toolCalls.GetArrayLength() == 0)
				return null;

			// Take first tool call (can extend to handle multiple if needed)
			var toolCall = toolCalls[0];
			var toolCallId = toolCall.TryGetProperty("id", out var tcId) ? tcId.GetString() : id;
			var toolName = toolCall.GetProperty("name").GetString()!;
			var args = toolCall.GetProperty("args");

			if (toolName is not null and "ragSearch")
			{
				// LLM Gateway uses "searchQuery" in args
				if (args.TryGetProperty("searchQuery", out var searchQueryProp))
				{
					var searchQuery = searchQueryProp.GetString();
					if (!string.IsNullOrEmpty(searchQuery))
					{
						return new AskAiEvent.SearchToolCall(id, timestamp, toolCallId ?? id, searchQuery);
					}
				}
			}

			// Fallback to generic tool call
			return new AskAiEvent.ToolCall(
				id,
				timestamp,
				toolCallId ?? id,
				toolName ?? "unknown",
				args.GetRawText()
			);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Failed to transform tool call");
			return null;
		}
	}

	private AskAiEvent? LogUnknownEvent(string? type, JsonElement _)
	{
		Logger.LogWarning("Unknown LLM Gateway event type: {Type}", type);
		return null;
	}
}
