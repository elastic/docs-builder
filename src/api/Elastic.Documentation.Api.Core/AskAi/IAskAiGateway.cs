// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Response from an AI gateway containing the stream and conversation metadata
/// </summary>
/// <param name="Stream">The SSE response stream</param>
/// <param name="GeneratedConversationId">
/// Non-null ONLY if the gateway generated a new conversation ID for this request.
/// When set, the transformer should emit a ConversationStart event with this ID.
/// Null means either: (1) user provided an ID (continuing conversation), or (2) gateway doesn't generate IDs (e.g., Agent Builder).
/// </param>
public record AskAiGatewayResponse(Stream Stream, Guid? GeneratedConversationId);

public interface IAskAiGateway
{
	Task<AskAiGatewayResponse> AskAi(AskAiRequest askAiRequest, Cancel ctx = default);
}
