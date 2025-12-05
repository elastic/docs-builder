// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Request model for submitting feedback on a specific Ask AI message.
/// Using Guid type ensures automatic validation during JSON deserialization.
/// </summary>
public record AskAiMessageFeedbackRequest(
	Guid MessageId,
	Guid ConversationId,
	Reaction Reaction
);

/// <summary>
/// The user's reaction to an Ask AI message.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Reaction>))]
public enum Reaction
{
	[JsonStringEnumMemberName("thumbsUp")]
	ThumbsUp,

	[JsonStringEnumMemberName("thumbsDown")]
	ThumbsDown
}
