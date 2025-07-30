// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Core.Chat;
using Core.Search;
using Core.Suggestions;

namespace Core.Serialization;

[JsonSerializable(typeof(ChatRequest))]
[JsonSerializable(typeof(SimpleChatRequest))]
[JsonSerializable(typeof(LlmGatewayMessage))]
[JsonSerializable(typeof(AgentStartMessage))]
[JsonSerializable(typeof(AgentEndMessage))]
[JsonSerializable(typeof(ChatModelStartMessage))]
[JsonSerializable(typeof(ChatModelEndMessage))]
[JsonSerializable(typeof(ToolCallMessage))]
[JsonSerializable(typeof(ToolMessage))]
[JsonSerializable(typeof(AiMessage))]
[JsonSerializable(typeof(AiMessageChunk))]
[JsonSerializable(typeof(SearchQuery))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(SuggestionRequest))]
[JsonSerializable(typeof(SuggestionResponse))]
public partial class ApiJsonContext : JsonSerializerContext;
