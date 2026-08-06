// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.PageFeedback;

public interface IPageFeedbackService
{
	Task<bool> UpsertFeedbackAsync(PageFeedbackRecord record, CancellationToken ctx);
	Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken ctx);
}

public record PageFeedbackRecord(
	Guid FeedbackId,
	string PageUrl,
	string PageTitle,
	PageFeedbackReaction Reaction,
	PageFeedbackReason? Reason,
	int? ReasonSetVersion,
	string? Comment,
	string? Euid
);
