// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.AskAi;

/// <summary>
/// Use case for handling Ask AI message feedback submissions.
/// </summary>
public class AskAiMessageFeedbackUsecase(
	IAskAiMessageFeedbackGateway feedbackGateway,
	ILogger<AskAiMessageFeedbackUsecase> logger)
{
	private static readonly ActivitySource FeedbackActivitySource = new(TelemetryConstants.AskAiFeedbackSourceName);

	public async Task SubmitFeedback(AskAiMessageFeedbackRequest request, CancellationToken ctx)
	{
		using var activity = FeedbackActivitySource.StartActivity("ask-ai.message-feedback", ActivityKind.Internal);
		_ = activity?.SetTag("gen_ai.conversation.id", request.ConversationId);
		_ = activity?.SetTag("gen_ai.response.id", request.MessageId);
		_ = activity?.SetTag("feedback.reaction", request.Reaction.ToString().ToLowerInvariant());

		logger.LogInformation(
			"Recording message feedback for message {MessageId} in conversation {ConversationId}: {Reaction}",
			request.MessageId,
			request.ConversationId,
			request.Reaction);

		var record = new AskAiMessageFeedbackRecord(
			request.MessageId,
			request.ConversationId,
			request.Reaction
		);

		await feedbackGateway.RecordFeedbackAsync(record, ctx);
	}
}
