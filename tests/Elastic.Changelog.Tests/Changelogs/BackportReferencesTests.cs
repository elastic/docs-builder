// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.GitHub;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Reading the original of a backport from text. The cases are the conventions of the DRA repositories:
/// a bot title with <c>(#N)</c>, a Mergify title with <c>(backport #N)</c>, a cherry-pick trailer, and the body forms.
/// </summary>
public class BackportReferencesTests
{
	private const string Sha = "0123456789abcdef0123456789abcdef01234567";

	private static BackportCandidates Extract(string title, string? body = null, int number = 500, params string[] commits) =>
		BackportReferences.Extract("elastic", "kibana", number, title, body, commits);

	// ── titles and subjects ─────────────────────────────────────────────

	[Test]
	[Arguments("[9.5] Improve duplicate transport version error messages (#158896)", 158896)]
	[Arguments("[9.5](backport #7571) fix: gracefully handle missing secrets in policy", 7571)]
	[Arguments("[9.5] (backport #19511) deps: Bump updatecli version to v0.121.0", 19511)]
	[Arguments("fix(esql): correct TS STATS alias collisions with grouping (backport #155312 to 9.5)", 155312)]
	public void Extract_TitleConventions_FindTheOriginal(string title, int original) => Extract(title).TitleRefs.Should().Equal(original);

	[Test]
	public void Extract_TitleWithOwnNumber_LeavesItOut() =>
		Extract("[9.5] Fix it (#290169) (#500)", number: 500).TitleRefs.Should().Equal(290169);

	[Test]
	public void Extract_ReferenceToAnotherRepository_IsNotASameRepositoryPullRequest() =>
		Extract("[9.5](backport #16542) [docs] Add known issue for elastic/fleet-server#7739").TitleRefs.Should().Equal(16542);

	[Test]
	public void Extract_IssueNumbersInTheTitle_AreReturnedAndLeftToTheCheck() =>
		Extract("unmute tests #160061, #160062, #160063").TitleRefs.Should().Equal(160061, 160062, 160063);

	[Test]
	public void Extract_CommitSubjects_FindTheOriginalOfEachCommit() =>
		Extract("[9.5] Two changes", commits: ["Fix the first thing (#111)\n\nbody mentions #999", "Fix the second thing (#222)"])
			.SubjectRefs
			.Should()
			.Equal(111, 222);

	// ── cherry-pick trailers ────────────────────────────────────────────

	[Test]
	public void Extract_CherryPickTrailer_ReturnsTheCommitSha() =>
		Extract("[9.5] Fix", commits: [$"Fix (#111)\n\n(cherry picked from commit {Sha})"]).CherryPickShas.Should().Equal(Sha);

	[Test]
	public void Extract_SameTrailerTwice_IsReturnedOnce() =>
		Extract("[9.5] Fix", commits: [$"a\n(cherry picked from commit {Sha})", $"b\n(cherry picked from commit {Sha.ToUpperInvariant()})"])
			.CherryPickShas
			.Should()
			.Equal(Sha);

	// ── bodies ──────────────────────────────────────────────────────────

	[Test]
	public void Extract_BackportToolBodyWithALink_FindsTheOriginal() =>
		Extract(
			"[9.5] Report the index mode",
			"""
			# Backport

			This will backport the following commits from `main` to `9.5`:
			 - [Report the index mode (#290169)](https://github.com/elastic/kibana/pull/290169)
			"""
		).BodyRefs.Should().Equal(290169);

	[Test]
	public void Extract_MergifyBody_FindsTheOriginal() =>
		Extract(
			"[9.5](backport #19511) deps",
			"Bumps a dependency.\n\nThis is an automatic backport of pull request #19511 done by [Mergify](https://mergify.com)."
		).BodyRefs.Should().Equal(19511);

	[Test]
	public void Extract_BackportActionBodyWithAList_FindsEveryOriginal() =>
		Extract("[9.5] Two fixes", "Backports the following commits to 9.5:\n - First fix (#158896)\n - Second fix (#158897)")
			.BodyRefs
			.Should()
			.Equal(158896, 158897);

	[Test]
	public void Extract_ManualBackportBody_FindsTheOriginal() =>
		Extract(
			"[9.5] fix(search): handle binary keyword doc values",
			"Backport of #160941 to `9.5`.\n\nThe automatic backport could not cherry-pick."
		).BodyRefs.Should().Equal(160941);

	[Test]
	public void Extract_BulletListThatIsNotAboutABackport_IsIgnored() =>
		Extract("Fix the thing", "Related work:\n - see (#111)\n - and (#222)").BodyRefs.Should().BeEmpty();

	[Test]
	public void Extract_BodyLinkToAnotherRepository_IsIgnored() =>
		Extract("[9.5] Fix", "Backport of https://github.com/elastic/elasticsearch/pull/5").BodyRefs.Should().BeEmpty();

	// ── reverts ─────────────────────────────────────────────────────────

	[Test]
	[Arguments("Revert \"[9.5](backport #7794) refactor: clone ParsedPolicy\"")]
	[Arguments("Revert \"Revert \\\"[8.19] ESQL: Push down SORT _score (#160651)\\\"\"")]
	[Arguments("revert: the last change (#200)")]
	public void IsRevert_RevertTitles_AreRecognised(string title) => BackportReferences.IsRevert(title).Should().BeTrue();

	[Test]
	[Arguments("[9.5] Fix the revert button (#200)")]
	[Arguments("[9.5](backport #7794) refactor: clone ParsedPolicy")]
	public void IsRevert_OtherTitles_AreNot(string title) => BackportReferences.IsRevert(title).Should().BeFalse();

	// ── choosing ────────────────────────────────────────────────────────

	[Test]
	public void Choose_TrailerRoot_WinsOverText()
	{
		var roots = BackportReferences.Choose([10], [20], [20], [30]);

		roots!.Roots.Should().Equal(10);
		roots.Route.Should().Be(BackportRootRoute.CherryPickTrailer);
		roots.RoutesDisagree.Should().BeTrue();
	}

	[Test]
	public void Choose_NoTrailer_PrefersWhatSubjectAndTitleAgreeOn()
	{
		var roots = BackportReferences.Choose([], [20, 21], [20], [99]);

		roots!.Roots.Should().Equal(20);
		roots.Route.Should().Be(BackportRootRoute.CommitSubjectAndTitle);
	}

	[Test]
	public void Choose_SubjectAndTitleThatDoNotOverlap_FallsBackToTheSubject()
	{
		var roots = BackportReferences.Choose([], [20], [30], []);

		roots!.Roots.Should().Equal(20);
		roots.Route.Should().Be(BackportRootRoute.CommitSubject);
		roots.RoutesDisagree.Should().BeTrue();
	}

	[Test]
	public void Choose_OnlyTheTitle_UsesTheTitle() =>
		BackportReferences.Choose([], [], [20], [])!.Route.Should().Be(BackportRootRoute.Title);

	[Test]
	public void Choose_OnlyTheBody_UsesTheBodyAndIsNotADisagreement()
	{
		var roots = BackportReferences.Choose([], [], [], [20, 21]);

		roots!.Roots.Should().Equal(20, 21);
		roots.Route.Should().Be(BackportRootRoute.Body);
		roots.RoutesDisagree.Should().BeFalse();
	}

	[Test]
	public void Choose_AgreeingRoutes_AreNotADisagreement() =>
		BackportReferences.Choose([10], [10], [10], []).Should().Match<BackportRoots>(r => !r.RoutesDisagree);

	[Test]
	public void Choose_NothingFound_ReturnsNull() => BackportReferences.Choose([], [], [], []).Should().BeNull();
}
