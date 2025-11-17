// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.IO.Pipelines;
using System.Text.Json;
using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Transforms LLM Gateway SSE events to canonical AskAiEvent format
/// </summary>
public class LlmGatewayStreamTransformer(ILogger<LlmGatewayStreamTransformer> logger) : StreamTransformerBase(logger)
{
	protected override string GetAgentId() => LlmGatewayAskAiGateway.ModelName;
	protected override string GetAgentProvider() => LlmGatewayAskAiGateway.ProviderName;

	/// <summary>
	/// Override to emit ConversationStart event when conversationId is null (new conversation)
	/// </summary>
	protected override async Task ProcessStreamAsync(PipeReader reader, PipeWriter writer, string? conversationId, Activity? parentActivity, CancellationToken cancellationToken)
	{
		// If conversationId is null, generate a new one and emit ConversationStart event
		// This matches the ThreadId format used in LlmGatewayAskAiGateway
		var actualConversationId = conversationId;
		if (conversationId == null)
		{
			actualConversationId = Guid.NewGuid().ToString();
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			var conversationStartEvent = new AskAiEvent.ConversationStart(
				Id: Guid.NewGuid().ToString(),
				Timestamp: timestamp,
				ConversationId: actualConversationId
			);

			// Set activity tags for the new conversation
			_ = parentActivity?.SetTag("gen_ai.conversation.id", actualConversationId);
			Logger.LogDebug("LLM Gateway conversation started: {ConversationId}", actualConversationId);

			// Write the ConversationStart event to the stream
			await WriteEventAsync(conversationStartEvent, writer, cancellationToken);
		}

		// Continue with normal stream processing using the actual conversation ID
		await base.ProcessStreamAsync(reader, writer, actualConversationId, parentActivity, cancellationToken);
	}
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
			"ai_message_chunk" when messageData.TryGetProperty("content", out var content) =>
				new AskAiEvent.MessageChunk(id, timestamp, content.GetString()!),

			"ai_message" when messageData.TryGetProperty("content", out var fullContent) =>
				new AskAiEvent.MessageComplete(id, timestamp, fullContent.GetString()!),

			"tool_call" when messageData.TryGetProperty("toolCalls", out var toolCalls) =>
				TransformToolCall(id, timestamp, toolCalls),

			"tool_message" when messageData.TryGetProperty("toolCallId", out var toolCallId)
				&& messageData.TryGetProperty("result", out var result) =>
				new AskAiEvent.ToolResult(id, timestamp, toolCallId.GetString()!, result.GetString()!),

			"agent_end" =>
				new AskAiEvent.ConversationEnd(id, timestamp),

			"error" => ParseErrorEvent(id, timestamp, messageData),

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

	private AskAiEvent.ErrorEvent ParseErrorEvent(string id, long timestamp, JsonElement messageData)
	{
		// LLM Gateway error format: {error: "...", message: "..."}
		var errorMessage = messageData.TryGetProperty("message", out var msgProp)
			? msgProp.GetString()
			: messageData.TryGetProperty("error", out var errProp)
				? errProp.GetString()
				: null;

		Logger.LogError("Error event received from LLM Gateway: {ErrorMessage}", errorMessage ?? "Unknown error");

		return new AskAiEvent.ErrorEvent(id, timestamp, errorMessage ?? "Unknown error occurred");
	}
}
