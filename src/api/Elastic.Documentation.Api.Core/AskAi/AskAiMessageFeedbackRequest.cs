// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Request model for submitting feedback on a specific Ask AI message.
/// </summary>
public record AskAiMessageFeedbackRequest(
	string MessageId,
	string ConversationId,
	Reaction Reaction
);

/// <summary>
/// The user's reaction to an Ask AI message.
/// </summary>
public enum Reaction
{
	ThumbsUp,
	ThumbsDown
}
