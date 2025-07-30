// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Core.Chat;

// Base message matching the Zod schema exactly
public abstract record LlmGatewayMessage(
	long Timestamp,
	string Id
);

// Shared data structures
public record InputTokens(
	int? System = null,
	int? Human = null,
	int? Ai = null
);

public record ThreadTokens(
	int? Human = null,
	int? Ai = null
);

public record Usage(
	int CompletionTokens,
	CompletionTokensDetails CompletionTokensDetails,
	int PromptTokens,
	PromptTokensDetails PromptTokensDetails,
	int TotalTokens
);

public record CompletionTokensDetails(
	int AcceptedPredictionTokens,
	int AudioTokens,
	int ReasoningTokens,
	int RejectedPredictionTokens
);

public record PromptTokensDetails(
	int AudioTokens,
	int CachedTokens
);

public record ToolCall(
	string? Id,
	string Name,
	object Args
);

// Discriminated union message types (matching TypeScript exactly)
public record AgentStartMessage(
	long Timestamp,
	string Id,
	AgentStartData Data
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "agent_start";
}

public record AgentStartData(
	InputTokens Input,
	ThreadTokens Thread
);

public record AgentEndMessage(
	long Timestamp,
	string Id
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "agent_end";

	[JsonPropertyName("data")]
	public object Data => new { };
}

public record ChatModelStartMessage(
	long Timestamp,
	string Id
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "chat_model_start";

	[JsonPropertyName("data")]
	public object Data => new { };
}

public record ChatModelEndMessage(
	long Timestamp,
	string Id,
	ChatModelEndData Data
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "chat_model_end";
}

public record ChatModelEndData(
	Usage Usage,
	string ModelName
);

public record ToolCallMessage(
	long Timestamp,
	string Id,
	ToolCallData Data
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "tool_call";
}

public record ToolCallData(
	ToolCall[] ToolCalls,
	string? Id = null
);

public record ToolMessage(
	long Timestamp,
	string Id,
	ToolMessageData Data
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "tool_message";
}

public record ToolMessageData(
	string ToolCallId,
	string Result
);

public record AiMessage(
	long Timestamp,
	string Id,
	AiMessageData Data
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "ai_message";
}

public record AiMessageData(
	string Content
);

public record AiMessageChunk(
	long Timestamp,
	string Id,
	AiMessageChunkData Data
) : LlmGatewayMessage(Timestamp, Id)
{
	[JsonPropertyName("type")]
	public string Type => "ai_message_chunk";
}

public record AiMessageChunkData(
	string Content
);

// For streaming results
public record ChatStreamResult(
	IAsyncEnumerable<LlmGatewayMessage> Messages,
	string ConversationId
);
