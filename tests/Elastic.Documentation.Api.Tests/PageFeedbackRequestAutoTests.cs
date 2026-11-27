// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using CsCheck;
using Elastic.Documentation.PageFeedback;
using Elastic.Documentation.Tests.Domain;
using Nullean.Entities;
using Nullean.Entities.CsCheck;

namespace Elastic.Documentation.Api.Tests;

/// <summary>
/// Generated page feedback requests against every rule of <see cref="PageFeedbackRequest"/>. The record has no operation that returns a new request
/// (the endpoint deserializes it), so the operation axes have nothing to run: the invariant, absence and vacancy axes cover the rules, over two generators.
/// <see cref="Legal"/> is every request the endpoint stores, at the boundaries of every limit; <see cref="Hostile"/> adds blank and over-long text,
/// nulls, undefined enum values, duplicate and mismatched reasons and out-of-range versions.
/// </summary>
public class PageFeedbackRequestAutoTests
{
	private static readonly PageFeedbackReason[] UpReasons =
	[
		PageFeedbackReason.Accurate,
		PageFeedbackReason.SolvedProblem,
		PageFeedbackReason.EasyToUnderstand,
		PageFeedbackReason.HelpfulExamples,
		PageFeedbackReason.EasyToFind,
		PageFeedbackReason.AnotherReason
	];

	private static readonly PageFeedbackReason[] DownReasons =
	[
		PageFeedbackReason.Inaccurate,
		PageFeedbackReason.MissingInformation,
		PageFeedbackReason.HardToUnderstand,
		PageFeedbackReason.CodeSampleErrors,
		PageFeedbackReason.OutOfDate,
		PageFeedbackReason.AnotherReason
	];

	private static readonly Gen<PageFeedbackReason> AnyReason = Gen.OneOf(
		Gen.OneOf(UpReasons),
		Gen.OneOf(DownReasons),
		Gen.Int[-3, 40].Select(i => (PageFeedbackReason)i)
	);

	private static Gen<string> Text(int max) =>
		Gen.Frequency(
			(3, Gen.String.AlphaNumeric.Where(s => s.Length > 0)),
			(1, Gen.Const(() => new string('x', max))),
			(1, Gen.Const(() => new string('y', max - 1))),
			(1, Gen.OneOf("a b", "café", "éé", "line\nbreak", "😀", "<b>x</b>"))
		);

	private static readonly Gen<string> LegalUrl = Gen.Frequency(
		(3, Gen.String.AlphaNumeric.Select(s => "/docs/" + s)),
		(1, Gen.Const(() => "/")),
		(1, Gen.Const(() => "/" + new string('a', 2047))),
		(1, Gen.OneOf("/a?b=c#d", "/a b", "/é", "/a%20b", "/a/../b"))
	);

	private static readonly Gen<PageFeedbackRequest> Legal = Gen.Select(LegalUrl, Text(500), Gen.Bool, Gen.Int[1, 6].Select(
		n => n
	), Gen.Int[0, 5], Gen.OneOf(1, 2, int.MaxValue), Gen.Frequency(
		(1, Gen.Const(() => (string?)null)),
		(3, Text(2000).Select(s => (string?)s))
	), Gen.Int[0, 3], (url, title, up, take, pick, version, comment, shape) =>
	{
		var reaction = up ? PageFeedbackReaction.ThumbsUp : PageFeedbackReaction.ThumbsDown;
		var pool = up ? UpReasons : DownReasons;
		// shape: 0 a reaction alone (a blank comment is still alone), 1 to 3 reasons that fit, one of them repeated
		if (shape == 0)
			return new PageFeedbackRequest(url, title, reaction, null, null, string.IsNullOrWhiteSpace(comment) ? comment : null);
		IReadOnlyList<PageFeedbackReason> reasons =
		[
			.. Enumerable.Range(0, Math.Min(take, shape + 1)).Select(i => pool[(pick + i) % pool.Length]),
			pool[pick % pool.Length]
		];
		return new PageFeedbackRequest(url, title, reaction, reasons, version, comment);
	}).Where(
		PageFeedbackRequestConstraintsTests.OldCheck
	); // selected by the old hand-written check, so a rule that rejects a legal request fails the axes instead of shrinking the data

	private static readonly Gen<PageFeedbackRequest> Hostile = Gen.OneOf(
		GenAuto.Of<PageFeedbackRequest>(AutoMode.Mixed),
		GenAuto.Of<PageFeedbackRequest>(AutoMode.Adversarial),
		Gen.Select(
			Gen.OneOf(null, "", " ", "\t", "/", "//x", "https://x", "docs", "/a b", new string('a', 2049), "/" + new string('a', 2048)),
			Gen.OneOf(null, "", " ", new string('t', 500), new string('t', 501)),
			Gen.Int[-1, 3].Select(i => (PageFeedbackReaction)i),
			Gen.Frequency(
				(1, Gen.Const(() => (IReadOnlyList<PageFeedbackReason>?)null)),
				(4, AnyReason.List[0, 4].Select(l => (IReadOnlyList<PageFeedbackReason>?)l))
			),
			Gen.OneOf<int?>(null, int.MinValue, -1, 0, 1, int.MaxValue),
			Gen.OneOf(null, "", " ", "ok", new string('c', 2000), new string('c', 2001)),
			(url, title, reaction, reasons, version, comment) => new PageFeedbackRequest(url!, title!, reaction, reasons, version, comment)
		)
	);

	public static IEnumerable<Func<AxisCase>> Axes() =>
		AutoTest.For(PageFeedbackRequest.Constraints, Legal).Seed(RunSeed.Value).CounterExamples(CounterExamples).AsDataSource();

	private static IReadOnlyDictionary<string, Func<PageFeedbackRequest, PageFeedbackRequest>> CounterExamples
	{
		get;
	} = new Dictionary<string, Func<PageFeedbackRequest, PageFeedbackRequest>>
	{
		["page-url-present"] = r => r with { PageUrl = " " },
		["page-url-not-too-long"] = r => r with { PageUrl = "/" + new string('a', 2048) },
		["page-url-is-local-path"] = r => r with { PageUrl = "//elsewhere.example/x" },
		["page-title-present"] = r => r with { PageTitle = "" },
		["page-title-not-too-long"] = r => r with { PageTitle = new string('t', 501) },
		["reaction-is-thumbs-up-or-down"] =
			r => r with { Reaction = PageFeedbackReaction.Unspecified, Reasons = null, ReasonSetVersion = null, Comment = null },
		["comment-not-too-long"] =
			r => r with { Reasons = [PageFeedbackReason.AnotherReason], ReasonSetVersion = 1, Comment = new string('c', 2001) },
		["reasons-come-with-a-version"] = r => r with { Reasons = [PageFeedbackReason.AnotherReason], ReasonSetVersion = null },
		["comment-needs-reasons"] = r => r with { Reasons = null, ReasonSetVersion = null, Comment = "orphan" },
		["reasons-match-the-reaction"] =
			r => r with { Reaction = PageFeedbackReaction.ThumbsUp, Reasons = [PageFeedbackReason.Inaccurate], ReasonSetVersion = 1 }
	};

	/// <summary>
	/// Law: every rule of a page feedback request holds for every legal request the generator builds, and every rule can be broken by some mutation of one.
	/// Consequence if broken: a rule that cannot fire reports green while checking nothing, and the endpoint stores feedback nothing checked.
	/// </summary>
	[Test]
	[MethodDataSource(nameof(Axes))]
	public Task EveryDeclaredAxisHolds(AxisCase axis) => axis.RunAsync();

	/// <summary>
	/// Law: no rule throws on any request the generator builds, however hostile.
	/// Consequence if broken: a crafted request crashes the endpoint with a 500 instead of the 400 it answers.
	/// </summary>
	[Test]
	public void HostileRequests_NeverThrow() =>
		Hostile.Sample(
			r =>
			{
				_ = PageFeedbackRequest.Constraints.Validate(r);
				_ = PageFeedbackRequest.Constraints.Report(r);
				return true;
			},
			seed: RunSeed.Value,
			iter: 20_000
		);

	[Test]
	public void HostileRequests_BreakEveryRuleAndSomeBreakNone()
	{
		var broken = new HashSet<string?>();
		var legal = 0;
		Hostile.Sample(
			r =>
			{
				var violations = PageFeedbackRequest.Constraints.Validate(r);
				foreach (var v in violations)
					_ = broken.Add(v.ConstraintId);
				if (violations.Count == 0)
					legal++;
				return true;
			},
			seed: RunSeed.Value,
			iter: 20_000
		);

		broken.Should().BeEquivalentTo(CounterExamples.Keys);
		legal.Should().BeGreaterThan(0);
	}
}
