// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.PageFeedback;

/// <summary>
/// What a page feedback request must say to be stored. The endpoint answers a request that breaks one with a generic invalid-arguments problem (400) that does not name the broken field.
/// </summary>
[Constraints<PageFeedbackRequest>]
public static partial class PageFeedbackRequestConstraints
{
	private const int MaxPageUrlLength = 2048;
	private const int MaxPageTitleLength = 500;
	private const int MaxCommentLength = 2000;

	public static readonly Constraint<PageFeedbackRequest> PageUrlPresent = Constraints<PageFeedbackRequest>.Always(
		r => r.PageUrl,
		v => !string.IsNullOrWhiteSpace(v),
		"The page URL is required."
	);

	public static readonly Constraint<PageFeedbackRequest> PageUrlNotTooLong = Constraints<PageFeedbackRequest>.Always(
		r => r.PageUrl,
		v => v is null || v.Length <= MaxPageUrlLength,
		$"The page URL must be at most {MaxPageUrlLength} characters."
	);

	// a path on this site: "/docs/page", not "//elsewhere.example" or "https://elsewhere.example"
	public static readonly Constraint<PageFeedbackRequest> PageUrlIsLocalPath = Constraints<PageFeedbackRequest>.Always(
		r => r.PageUrl,
		v => string.IsNullOrWhiteSpace(v) ||
			(v.StartsWith('/') && !v.StartsWith("//", StringComparison.Ordinal) && Uri.TryCreate(v, UriKind.Relative, out _)),
		"The page URL must be a path on this site, starting with a single '/'."
	);

	public static readonly Constraint<PageFeedbackRequest> PageTitlePresent = Constraints<PageFeedbackRequest>.Always(
		r => r.PageTitle,
		v => !string.IsNullOrWhiteSpace(v),
		"The page title is required."
	);

	public static readonly Constraint<PageFeedbackRequest> PageTitleNotTooLong = Constraints<PageFeedbackRequest>.Always(
		r => r.PageTitle,
		v => v is null || v.Length <= MaxPageTitleLength,
		$"The page title must be at most {MaxPageTitleLength} characters."
	);

	public static readonly Constraint<PageFeedbackRequest> ReactionIsThumbsUpOrDown = Constraints<PageFeedbackRequest>.Always(
		r => r.Reaction,
		v => v is PageFeedbackReaction.ThumbsUp or PageFeedbackReaction.ThumbsDown,
		"The reaction must be thumbsUp or thumbsDown."
	);

	public static readonly Constraint<PageFeedbackRequest> CommentNotTooLong = Constraints<PageFeedbackRequest>.Always(
		r => r.Comment,
		v => v is null || v.Length <= MaxCommentLength,
		$"The comment must be at most {MaxCommentLength} characters."
	);

	// reasons, their version and the comment go together: a comment explains the reasons that were picked
	public static readonly Constraint<PageFeedbackRequest> ReasonsComeWithAVersion = Constraints<PageFeedbackRequest>.Always(
		r => r,
		r => r.Reasons is null or [] ? r.ReasonSetVersion is null : r.ReasonSetVersion is > 0,
		"Reasons need a reason set version above 0, and a reason set version needs reasons.",
		member: nameof(PageFeedbackRequest.ReasonSetVersion)
	);

	public static readonly Constraint<PageFeedbackRequest> CommentNeedsReasons = Constraints<PageFeedbackRequest>.Always(
		r => r,
		r => r.Reasons is not (null or []) || string.IsNullOrWhiteSpace(r.Comment),
		"A comment needs at least one reason.",
		member: nameof(PageFeedbackRequest.Comment)
	);

	public static readonly Constraint<PageFeedbackRequest> ReasonsMatchTheReaction = Constraints<PageFeedbackRequest>.Always(
		r => r,
		r => r.Reasons is null || r.Reasons.All(reason => Enum.IsDefined(reason) && ReasonFits(r.Reaction, reason)),
		"Each reason must be one that fits the reaction.",
		member: nameof(PageFeedbackRequest.Reasons)
	);

	private static bool ReasonFits(PageFeedbackReaction reaction, PageFeedbackReason reason) => reaction switch
	{
		PageFeedbackReaction.ThumbsUp =>
			reason is PageFeedbackReason.Accurate or PageFeedbackReason.SolvedProblem or PageFeedbackReason.EasyToUnderstand or PageFeedbackReason.HelpfulExamples or PageFeedbackReason.EasyToFind or PageFeedbackReason.AnotherReason,
		PageFeedbackReaction.ThumbsDown =>
			reason is PageFeedbackReason.Inaccurate or PageFeedbackReason.MissingInformation or PageFeedbackReason.HardToUnderstand or PageFeedbackReason.CodeSampleErrors or PageFeedbackReason.OutOfDate or PageFeedbackReason.AnotherReason,
		_ => false
	};
}
