// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Api.PageFeedback;

/// <summary>
/// What a page feedback request must say to be stored. The endpoint answers a request that breaks one with a 400 that names the field and the rule,
/// where it used to answer with a bare 400.
/// </summary>
[Constraints]
public static partial class PageFeedbackRequestConstraints
{
	private const int MaxPageUrlLength = 2048;
	private const int MaxPageTitleLength = 500;
	private const int MaxCommentLength = 2000;

	public static Constraint<PageFeedbackRequest> PageUrlPresent { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.PageUrl,
		v => !string.IsNullOrWhiteSpace(v),
		"The page URL is required."
	);

	public static Constraint<PageFeedbackRequest> PageUrlNotTooLong { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.PageUrl,
		v => v is null || v.Length <= MaxPageUrlLength,
		$"The page URL must be at most {MaxPageUrlLength} characters."
	);

	// a path on this site: "/docs/page", not "//elsewhere.example" or "https://elsewhere.example"
	public static Constraint<PageFeedbackRequest> PageUrlIsLocalPath { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.PageUrl,
		v => string.IsNullOrWhiteSpace(v) ||
			(v.StartsWith('/') && !v.StartsWith("//", StringComparison.Ordinal) && Uri.TryCreate(v, UriKind.Relative, out _)),
		"The page URL must be a path on this site, starting with a single '/'."
	);

	public static Constraint<PageFeedbackRequest> PageTitlePresent { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.PageTitle,
		v => !string.IsNullOrWhiteSpace(v),
		"The page title is required."
	);

	public static Constraint<PageFeedbackRequest> PageTitleNotTooLong { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.PageTitle,
		v => v is null || v.Length <= MaxPageTitleLength,
		$"The page title must be at most {MaxPageTitleLength} characters."
	);

	public static Constraint<PageFeedbackRequest> ReactionIsThumbsUpOrDown { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.Reaction,
		v => v is PageFeedbackReaction.ThumbsUp or PageFeedbackReaction.ThumbsDown,
		"The reaction must be thumbsUp or thumbsDown."
	);

	public static Constraint<PageFeedbackRequest> CommentNotTooLong { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r.Comment,
		v => v is null || v.Length <= MaxCommentLength,
		$"The comment must be at most {MaxCommentLength} characters."
	);

	// reasons, their version and the comment go together: a comment explains the reasons that were picked
	public static Constraint<PageFeedbackRequest> ReasonsComeWithAVersion { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r,
		r => r.Reasons is null or [] ? r.ReasonSetVersion is null : r.ReasonSetVersion is > 0,
		"Reasons need a reason set version above 0, and a reason set version needs reasons.",
		member: nameof(PageFeedbackRequest.ReasonSetVersion)
	);

	public static Constraint<PageFeedbackRequest> CommentNeedsReasons { get; } = Constraints<PageFeedbackRequest>.Always(
		r => r,
		r => r.Reasons is not (null or []) || string.IsNullOrWhiteSpace(r.Comment),
		"A comment needs at least one reason.",
		member: nameof(PageFeedbackRequest.Comment)
	);

	public static Constraint<PageFeedbackRequest> ReasonsMatchTheReaction { get; } = Constraints<PageFeedbackRequest>.Always(
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
