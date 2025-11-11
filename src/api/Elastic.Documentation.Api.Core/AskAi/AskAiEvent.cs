// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Base class for all AskAI events streamed to the frontend
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ConversationStart), typeDiscriminator: "conversation_start")]
[JsonDerivedType(typeof(MessageChunk), typeDiscriminator: "message_chunk")]
[JsonDerivedType(typeof(MessageComplete), typeDiscriminator: "message_complete")]
[JsonDerivedType(typeof(SearchToolCall), typeDiscriminator: "search_tool_call")]
[JsonDerivedType(typeof(ToolCall), typeDiscriminator: "tool_call")]
[JsonDerivedType(typeof(ToolResult), typeDiscriminator: "tool_result")]
[JsonDerivedType(typeof(Reasoning), typeDiscriminator: "reasoning")]
[JsonDerivedType(typeof(ConversationEnd), typeDiscriminator: "conversation_end")]
[JsonDerivedType(typeof(ErrorEvent), typeDiscriminator: "error")]
public abstract record AskAiEvent(string Id, long Timestamp)
{
	/// <summary>
	/// Conversation has started
	/// </summary>
	public sealed record ConversationStart(
		string Id,
		long Timestamp,
		string ConversationId
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// Streaming text chunk from AI
	/// </summary>
	public sealed record MessageChunk(
		string Id,
		long Timestamp,
		string Content
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// Complete message when streaming is done
	/// </summary>
	public sealed record MessageComplete(
		string Id,
		long Timestamp,
		string FullContent
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// AI is calling the search tool with a specific query
	/// </summary>
	public sealed record SearchToolCall(
		string Id,
		long Timestamp,
		string ToolCallId,
		string SearchQuery
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// AI is calling a tool (generic fallback for unknown tools)
	/// </summary>
	public sealed record ToolCall(
		string Id,
		long Timestamp,
		string ToolCallId,
		string ToolName,
		string Arguments
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// Result from tool execution
	/// </summary>
	public sealed record ToolResult(
		string Id,
		long Timestamp,
		string ToolCallId,
		string Result
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// AI is reasoning/thinking (e.g., searching, planning)
	/// </summary>
	public sealed record Reasoning(
		string Id,
		long Timestamp,
		string? Message
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// Conversation has ended
	/// </summary>
	public sealed record ConversationEnd(
		string Id,
		long Timestamp
	) : AskAiEvent(Id, Timestamp);

	/// <summary>
	/// An error occurred
	/// </summary>
	public sealed record ErrorEvent(
		string Id,
		long Timestamp,
		string Message
	) : AskAiEvent(Id, Timestamp);
}

/// <summary>
/// JSON serialization context for AskAiEvent types (required for source generation)
/// </summary>
[JsonSerializable(typeof(AskAiEvent))]
[JsonSerializable(typeof(AskAiEvent.ConversationStart))]
[JsonSerializable(typeof(AskAiEvent.MessageChunk))]
[JsonSerializable(typeof(AskAiEvent.MessageComplete))]
[JsonSerializable(typeof(AskAiEvent.SearchToolCall))]
[JsonSerializable(typeof(AskAiEvent.ToolCall))]
[JsonSerializable(typeof(AskAiEvent.ToolResult))]
[JsonSerializable(typeof(AskAiEvent.Reasoning))]
[JsonSerializable(typeof(AskAiEvent.ConversationEnd))]
[JsonSerializable(typeof(AskAiEvent.ErrorEvent))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public sealed partial class AskAiEventJsonContext : JsonSerializerContext
{
}
