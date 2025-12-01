// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Gateway interface for recording Ask AI message feedback.
/// Infrastructure implementations may use different storage backends (Elasticsearch, database, etc.)
/// </summary>
public interface IAskAiMessageFeedbackGateway
{
	/// <summary>
	/// Records feedback for a specific Ask AI message.
	/// </summary>
	/// <param name="record">The feedback record to store.</param>
	/// <param name="ctx">Cancellation token.</param>
	Task RecordFeedbackAsync(AskAiMessageFeedbackRecord record, CancellationToken ctx);
}

/// <summary>
/// Internal record used to pass message feedback data to the gateway.
/// </summary>
public record AskAiMessageFeedbackRecord(
	string MessageId,
	string ConversationId,
	Reaction Reaction,
	string? Euid = null
);
