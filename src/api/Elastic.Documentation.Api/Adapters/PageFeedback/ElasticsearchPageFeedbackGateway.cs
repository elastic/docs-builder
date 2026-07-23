// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.PageFeedback;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Serialization;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

internal sealed class ElasticsearchPageFeedbackGateway(
	PageFeedbackTransport transport,
	PageFeedbackIndex index,
	IngestChannel<PageFeedbackDocument> channel,
	ILogger<ElasticsearchPageFeedbackGateway> logger
) : IPageFeedbackService
{
	public async Task<bool> UpsertFeedbackAsync(PageFeedbackRecord record, CancellationToken ctx)
	{
		var document = new PageFeedbackDocument
		{
			FeedbackId = record.FeedbackId.ToString(),
			PageUrl = record.PageUrl,
			PageTitle = record.PageTitle,
			Reaction = record.Reaction == PageFeedbackReaction.ThumbsUp ? "thumbsUp" : "thumbsDown",
			Reason = GetReasonValue(record.Reason),
			ReasonSetVersion = record.ReasonSetVersion,
			Comment = record.Comment,
			Euid = record.Euid,
			Timestamp = DateTimeOffset.UtcNow
		};

		try
		{
			var response = await channel.DirectWriteAsync([document], ctx);

			if (response.AllItemsPersisted())
				return true;

			logger.LogWarning(
				"Failed to index page feedback {FeedbackId}: HTTP {StatusCode}, item statuses {ItemStatuses}",
				record.FeedbackId,
				response.ApiCallDetails.HttpStatusCode,
				response.Items?.Select(item => item.Status).ToArray() ?? []);
			return false;
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Failed to index page feedback {FeedbackId}", record.FeedbackId);
			return false;
		}
	}

	public async Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ctx)
	{
		try
		{
			var response = await transport.Transport.DeleteAsync<StringResponse>(
				$"{index.Name}/_doc/{feedbackId}",
				cancellationToken: ctx);

			if (response.ApiCallDetails.HasSuccessfulStatusCode || response.ApiCallDetails.HttpStatusCode is 404)
				return true;

			logger.LogWarning(
				"Failed to delete page feedback {FeedbackId}: HTTP {StatusCode}",
				feedbackId,
				response.ApiCallDetails.HttpStatusCode);
			return false;
		}
		catch (Exception exception)
		{
			logger.LogWarning(exception, "Failed to delete page feedback {FeedbackId}", feedbackId);
			return false;
		}
	}

	private static string? GetReasonValue(PageFeedbackReason? reason) =>
		reason switch
		{
			null => null,
			PageFeedbackReason.Accurate => "accurate",
			PageFeedbackReason.SolvedProblem => "solvedProblem",
			PageFeedbackReason.EasyToUnderstand => "easyToUnderstand",
			PageFeedbackReason.HelpfulExamples => "helpfulExamples",
			PageFeedbackReason.Inaccurate => "inaccurate",
			PageFeedbackReason.MissingInformation => "missingInformation",
			PageFeedbackReason.HardToUnderstand => "hardToUnderstand",
			PageFeedbackReason.CodeSampleErrors => "codeSampleErrors",
			PageFeedbackReason.AnotherReason => "anotherReason",
			_ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown page feedback reason.")
		};
}
