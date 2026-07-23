// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

internal sealed class ElasticsearchPageFeedbackGateway(
	PageFeedbackTransport transport,
	PageFeedbackIndex index,
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
			Comment = record.Comment,
			Euid = record.Euid,
			Timestamp = DateTimeOffset.UtcNow
		};

		try
		{
			var json = JsonSerializer.Serialize(document, PageFeedbackJsonContext.Default.PageFeedbackDocument);
			var response = await transport.Transport.PutAsync<StringResponse>(
				$"{index.Name}/_doc/{record.FeedbackId}",
				PostData.String(json),
				ctx);

			if (response.ApiCallDetails.HasSuccessfulStatusCode)
				return true;

			logger.LogWarning(
				"Failed to index page feedback {FeedbackId}: HTTP {StatusCode}",
				record.FeedbackId,
				response.ApiCallDetails.HttpStatusCode);
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
}
