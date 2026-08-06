// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Api;

/// <summary>
/// Types for OpenTelemetry telemetry serialization (AOT-compatible)
/// </summary>
public record MessagePart(string Type, string Content);

public record InputMessage(string Role, MessagePart[] Parts);

public record OutputMessage(string Role, MessagePart[] Parts, string FinishReason);

[JsonSerializable(typeof(AskAiRequest))]
[JsonSerializable(typeof(AskAiMessageFeedbackRequest))]
[JsonSerializable(typeof(Reaction))]
[JsonSerializable(typeof(PageFeedbackRequest))]
[JsonSerializable(typeof(PageFeedbackReaction))]
[JsonSerializable(typeof(PageFeedbackReason))]
[JsonSerializable(typeof(NavigationSearchRequest))]
[JsonSerializable(typeof(NavigationSearchResponse))]
[JsonSerializable(typeof(NavigationSearchAggregations))]
[JsonSerializable(typeof(InputMessage))]
[JsonSerializable(typeof(OutputMessage[]))]
[JsonSerializable(typeof(MessagePart))]
[JsonSerializable(typeof(InputMessage[]))]

[JsonSerializable(typeof(FullSearchRequest))]
[JsonSerializable(typeof(FullSearchResponse))]
[JsonSerializable(typeof(FullSearchAggregations))]

[JsonSerializable(typeof(ChangesResponse))]
[JsonSerializable(typeof(ChangedPageDto))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class ApiJsonContext : JsonSerializerContext;
