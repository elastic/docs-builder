// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.Api.PageFeedback;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Api.Tests;

public class PageFeedbackRequestConstraintsTests
{
	private static PageFeedbackRequest Valid { get; } = new(
		"/docs/page",
		"A page",
		PageFeedbackReaction.ThumbsDown,
		[PageFeedbackReason.Inaccurate],
		1,
		"It was wrong"
	);

	private static bool Accepted(PageFeedbackRequest request) =>
		!PageFeedbackRequestConstraints.All.Validate(request).Any(v => v.Severity == Severity.Error);

	/// <summary>
	/// Law: the constraint set accepts exactly the requests the hand-written check used to accept, for generated requests, hostile ones included
	/// (blank and oversized strings, undefined enums, nulls, empty lists). The old check is kept here as the reference.
	/// Consequence if broken: the endpoint starts rejecting feedback it used to store, or storing what it used to reject.
	/// </summary>
	[Test]
	public void TheRulesAcceptWhatTheOldCheckAccepted() =>
		GenAuto.Of<PageFeedbackRequest>(AutoMode.Mixed).Sample(
			request => Accepted(request).Should().Be(OldCheck(request), request.ToString()),
			iter: 3000,
			threads: 1
		);

	[Test]
	public void AValidRequest_IsAccepted() => PageFeedbackRequestConstraints.All.Validate(Valid).Should().BeEmpty();

	/// <summary>
	/// Law: each broken request names the field and the rule it breaks.
	/// Consequence if broken: the client gets a 400 and has to guess why.
	/// </summary>
	[Test]
	[Arguments("pageUrl blank", "page-url-present", "PageUrl")]
	[Arguments("pageUrl protocol-relative", "page-url-is-local-path", "PageUrl")]
	[Arguments("pageUrl too long", "page-url-not-too-long", "PageUrl")]
	[Arguments("pageTitle blank", "page-title-present", "PageTitle")]
	[Arguments("pageTitle too long", "page-title-not-too-long", "PageTitle")]
	[Arguments("reaction unspecified", "reaction-is-thumbs-up-or-down", "Reaction")]
	[Arguments("comment too long", "comment-not-too-long", "Comment")]
	[Arguments("reasons without a version", "reasons-come-with-a-version", "ReasonSetVersion")]
	[Arguments("comment without reasons", "comment-needs-reasons", "Comment")]
	[Arguments("reason for the other reaction", "reasons-match-the-reaction", "Reasons")]
	public void ABrokenRequest_NamesTheFieldAndTheRule(string breakage, string rule, string member)
	{
		var request = breakage switch
		{
			"pageUrl blank" => Valid with { PageUrl = " " },
			"pageUrl protocol-relative" => Valid with { PageUrl = "//elsewhere.example/x" },
			"pageUrl too long" => Valid with { PageUrl = "/" + new string('a', 2048) },
			"pageTitle blank" => Valid with { PageTitle = "" },
			"pageTitle too long" => Valid with { PageTitle = new string('t', 501) },
			"reaction unspecified" =>
				Valid with { Reaction = PageFeedbackReaction.Unspecified, Reasons = null, ReasonSetVersion = null, Comment = null },
			"comment too long" => Valid with { Comment = new string('c', 2001) },
			"reasons without a version" => Valid with { ReasonSetVersion = null },
			"comment without reasons" => Valid with { Reasons = null, ReasonSetVersion = null },
			"reason for the other reaction" => Valid with { Reasons = [PageFeedbackReason.Accurate] },
			_ => throw new ArgumentOutOfRangeException(nameof(breakage))
		};

		var violations = PageFeedbackRequestConstraints.All.Validate(request);

		violations.Should().Contain(v => v.ConstraintId == rule && v.Member == member);
	}

	// the check the endpoint had before it was a set of rules: kept as the specification the rules are held to
	private static bool OldCheck(PageFeedbackRequest request) =>
		!string.IsNullOrWhiteSpace(request.PageUrl)
			&& request.PageUrl.Length <= 2048
			&& request.PageUrl.StartsWith('/')
			&& !request.PageUrl.StartsWith("//", StringComparison.Ordinal)
			&& Uri.TryCreate(request.PageUrl, UriKind.Relative, out _)
			&& !string.IsNullOrWhiteSpace(request.PageTitle)
			&& request.PageTitle.Length <= 500
			&& request.Reaction is PageFeedbackReaction.ThumbsUp or PageFeedbackReaction.ThumbsDown
			&& (request.Comment is null || request.Comment.Length <= 2000)
			&& OldDetails(request);

	private static bool OldDetails(PageFeedbackRequest request)
	{
		if (request.Reasons is null or [])
			return request.ReasonSetVersion is null && string.IsNullOrWhiteSpace(request.Comment);

		return request.ReasonSetVersion is > 0 && request.Reasons.All(r => Enum.IsDefined(r) && OldFits(request.Reaction, r));
	}

	private static bool OldFits(PageFeedbackReaction reaction, PageFeedbackReason reason) => reaction switch
	{
		PageFeedbackReaction.ThumbsUp =>
			reason is PageFeedbackReason.Accurate or PageFeedbackReason.SolvedProblem or PageFeedbackReason.EasyToUnderstand or PageFeedbackReason.HelpfulExamples or PageFeedbackReason.EasyToFind or PageFeedbackReason.AnotherReason,
		PageFeedbackReaction.ThumbsDown =>
			reason is PageFeedbackReason.Inaccurate or PageFeedbackReason.MissingInformation or PageFeedbackReason.HardToUnderstand or PageFeedbackReason.CodeSampleErrors or PageFeedbackReason.OutOfDate or PageFeedbackReason.AnotherReason,
		_ => false
	};
}
