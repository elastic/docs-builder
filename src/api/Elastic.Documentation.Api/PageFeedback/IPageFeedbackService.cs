// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.PageFeedback;

public interface IPageFeedbackService
{
	Task<bool> UpsertFeedbackAsync(PageFeedbackRecord record, CancellationToken ctx);
}

public record PageFeedbackRecord(
	Guid FeedbackId,
	string PageUrl,
	string PageTitle,
	PageFeedbackReaction Reaction,
	IReadOnlyList<PageFeedbackReason>? Reasons,
	int? ReasonSetVersion,
	string? Comment
)
{
	public static PageFeedbackRecord From(Guid feedbackId, PageFeedbackRequest request) =>
		new(
			feedbackId,
			request.PageUrl,
			request.PageTitle,
			request.Reaction,
			request.Reasons is null or [] ? null : request.Reasons,
			request.ReasonSetVersion,
			string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
		);
}
