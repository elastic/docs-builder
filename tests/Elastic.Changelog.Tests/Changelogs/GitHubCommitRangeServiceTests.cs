// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Fake-HTTP tests for <see cref="GitHubCommitRangeService"/>: compare-API commit enumeration
/// (including pagination) and GraphQL <c>associatedPullRequests</c> resolution across the
/// squash / merge-commit / no-PR / multi-PR shapes.
/// </summary>
public class GitHubCommitRangeServiceTests() : ChangelogTestBase()
{
	private const string Owner = "elastic";
	private const string Repo = "widget";

	private static readonly CommitRangeArguments Args = new() { Owner = Owner, Repo = Repo, StartRef = "startsha", EndRef = "endsha" };

	private static string Sha(int i) => i.ToString(CultureInfo.InvariantCulture).PadLeft(40, '0');

	private static string CompareJson(int totalCommits, IEnumerable<string> shas, string status = "ahead")
	{
		var commits = string.Join(",", shas.Select(sha => $$"""{ "sha": "{{sha}}" }"""));
		return $$"""{ "status": "{{status}}", "total_commits": {{totalCommits}}, "commits": [{{commits}}] }""";
	}

	/// <summary>One associated PR node for a GraphQL commit object.</summary>
	private static string PrNode(
		int number,
		bool merged = true,
		string? mergeCommitSha = null,
		string repoFullName = Owner + "/" + Repo,
		string baseRefName = "main"
	) =>
		$$"""
		{ "number": {{number}}, "url": "https://github.com/{{repoFullName}}/pull/{{number}}", "merged": {{(merged ? "true" : "false")}},
		  "baseRefName": "{{baseRefName}}",
		  "mergeCommit": {{(mergeCommitSha != null ? $$"""{ "oid": "{{mergeCommitSha}}" }""" : "null")}},
		  "baseRepository": { "nameWithOwner": "{{repoFullName}}" } }
		""";

	private static string GraphQlJson(IReadOnlyList<(string Sha, string[] PrNodes)> commits)
	{
		var sb = new StringBuilder();
		for (var i = 0; i < commits.Count; i++)
		{
			if (i > 0)
				_ = sb.Append(',');
			_ = sb.Append(
				CultureInfo.InvariantCulture,
				$$""" "c{{i}}": { "oid": "{{commits[i].Sha}}", "associatedPullRequests": { "nodes": [{{string.Join(",", commits[i].PrNodes)}}] } }"""
			);
		}

		return $$"""{ "data": { "repository": {{{sb}} } } }""";
	}

	private GitHubCommitRangeService Service(StubHandler handler) =>
		new(new TestLoggerFactory(), new GitHubApiTransport(handler, "test-token"));

	private static StubHandler Handler(
		Func<HttpRequestMessage, string?> compareResponder,
		Func<HttpRequestMessage, string> graphQlResponder
	) =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path == "/graphql")
				return Json(graphQlResponder(req));
			if (path.StartsWith($"/repos/{Owner}/{Repo}/compare/", StringComparison.Ordinal))
			{
				var body = compareResponder(req);
				return body == null ? new HttpResponseMessage(HttpStatusCode.NotFound) : Json(body);
			}

			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	[Test]
	public async Task ResolvePullRequests_SquashCommits_ResolvesOnePrPerCommitInRangeOrder()
	{
		var (sha1, sha2) = (Sha(1), Sha(2));
		var handler = Handler(
			_ => CompareJson(2, [sha1, sha2]),
			_ => GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1)]), (sha2, [PrNode(12, mergeCommitSha: sha2)])])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		result.TotalCommits.Should().Be(2);
		result.PullRequests.Select(pr => pr.Number).Should().Equal(11, 12);
		result.PullRequests[0].Url.Should().Be($"https://github.com/{Owner}/{Repo}/pull/11");
		result.PullRequests[0].CommitShas.Should().Equal(sha1);
		result.CommitsWithoutPullRequest.Should().BeEmpty();
	}

	[Test]
	public async Task ResolvePullRequests_MergeCommitPr_DeduplicatesAcrossCommits()
	{
		// Two branch commits belong to the same merge-commit PR; its merge commit is a third sha
		// that is also part of the range.
		var (sha1, sha2, mergeSha) = (Sha(1), Sha(2), Sha(3));
		var handler = Handler(
			_ => CompareJson(3, [sha1, sha2, mergeSha]),
			_ => GraphQlJson([
				(sha1, [PrNode(20, mergeCommitSha: mergeSha)]),
				(sha2, [PrNode(20, mergeCommitSha: mergeSha)]),
				(mergeSha, [PrNode(20, mergeCommitSha: mergeSha)])
			])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle();
		result.PullRequests[0].Number.Should().Be(20);
		result.PullRequests[0].CommitShas.Should().Equal(sha1, sha2, mergeSha);
	}

	[Test]
	public async Task ResolvePullRequests_CommitWithoutMergedPr_IsReportedNotDropped()
	{
		// One commit has no associated PRs at all; another only an unmerged PR; a third only a PR
		// merged into a different (fork) repository. None may silently disappear.
		var (sha1, sha2, sha3) = (Sha(1), Sha(2), Sha(3));
		var handler = Handler(
			_ => CompareJson(3, [sha1, sha2, sha3]),
			_ => GraphQlJson([
				(sha1, []),
				(sha2, [PrNode(30, merged: false)]),
				(sha3, [PrNode(31, mergeCommitSha: sha3, repoFullName: "someone/fork")])
			])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().BeEmpty();
		result.CommitsWithoutPullRequest.Should().Equal(sha1, sha2, sha3);
	}

	[Test]
	public async Task ResolvePullRequests_MultipleMergedPrs_WarnsAndPicksDeterministically()
	{
		var sha1 = Sha(1);
		var handler = Handler(_ => CompareJson(1, [sha1]), _ => GraphQlJson([(sha1, [PrNode(42), PrNode(7)])]));

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle();
		result.PullRequests[0].Number.Should().Be(7, "ambiguity resolves deterministically to the lowest PR number");
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("multiple merged pull requests"));
	}

	[Test]
	public async Task ResolvePullRequests_MergeCommitMatchWins_NoAmbiguityWarning()
	{
		// A commit associated with two merged PRs, but exactly one of them has this commit as its
		// merge commit — that one wins without a warning.
		var sha1 = Sha(1);
		var handler = Handler(_ => CompareJson(1, [sha1]), _ => GraphQlJson([(sha1, [PrNode(50), PrNode(60, mergeCommitSha: sha1)])]));

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle();
		result.PullRequests[0].Number.Should().Be(60);
		Collector.Diagnostics.Should().NotContain(d => d.Message.Contains("multiple merged pull requests"));
	}

	[Test]
	public async Task ResolvePullRequests_BaseRef_ExcludesPullRequestsMergedIntoOtherBranches()
	{
		var (sha1, sha2) = (Sha(1), Sha(2));
		var handler = Handler(
			_ => CompareJson(2, [sha1, sha2]),
			_ => GraphQlJson([
				(sha1, [PrNode(11, mergeCommitSha: sha1, baseRefName: "feature/highlight")]),
				(sha2, [PrNode(12, mergeCommitSha: sha2)])
			])
		);
		var args = Args with { BaseRef = "main" };

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Select(pr => pr.Number).Should().Equal(12);
		result.ExcludedPullRequests.Should().ContainSingle();
		result.ExcludedPullRequests[0].Number.Should().Be(11);
		result.ExcludedPullRequests[0].BaseRef.Should().Be("feature/highlight");
		// Excluded by base branch is not the same as "no associated PR": report it once.
		result.CommitsWithoutPullRequest.Should().BeEmpty();
	}

	[Test]
	public async Task ResolvePullRequests_BaseRef_StillReportsCommitsWithNoMergedPr()
	{
		var (sha1, sha2) = (Sha(1), Sha(2));
		var handler = Handler(
			_ => CompareJson(2, [sha1, sha2]),
			_ => GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1, baseRefName: "feature/highlight")]), (sha2, [])])
		);
		var args = Args with { BaseRef = "main" };

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.ExcludedPullRequests.Select(pr => pr.Number).Should().Equal(11);
		result.CommitsWithoutPullRequest.Should().Equal(sha2);
	}

	[Test]
	public async Task ResolvePullRequests_NoBaseRef_KeepsPullRequestsFromAnyBranch()
	{
		var sha1 = Sha(1);
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1, baseRefName: "feature/highlight")])])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Select(pr => pr.Number).Should().Equal(11);
		result.ExcludedPullRequests.Should().BeEmpty();
	}

	[Test]
	public async Task ResolvePullRequests_BaseRef_PrefersTheMainPullRequestWhenACommitHasBoth()
	{
		var sha1 = Sha(1);
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => GraphQlJson([(sha1, [PrNode(11, baseRefName: "feature/highlight"), PrNode(12)])])
		);
		var args = Args with { BaseRef = "main" };

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Select(pr => pr.Number).Should().Equal(12);
		result.ExcludedPullRequests.Select(pr => pr.Number).Should().Equal(11);
		Collector.Warnings.Should().Be(0);
	}

	[Test]
	public async Task ResolvePullRequests_Pagination_FollowsAllComparePages()
	{
		// 150 commits: page 1 returns 100, page 2 the remaining 50. GraphQL batches per 25, which run side by side.
		var shas = Enumerable.Range(1, 150).Select(Sha).ToList();
		var comparePages = new List<int>();
		var graphQlBatches = 0;

		var handler = Handler(req =>
		{
			var query = HttpUtility.ParseQueryString(req.RequestUri!.Query);
			var page = int.Parse(query["page"]!, CultureInfo.InvariantCulture);
			comparePages.Add(page);
			var pageShas = shas.Skip((page - 1) * 100).Take(100);
			return CompareJson(150, pageShas);
		}, req =>
		{
			// The batches may arrive in any order, so the answer follows the commits the request names. Every commit
			// resolves to its own squashed PR (number = index + 1).
			_ = Interlocked.Increment(ref graphQlBatches);
			var batch = System
				.Text
				.RegularExpressions
				.Regex
				.Matches(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult(), "(?<=u0022)[0-9a-f]{40}")
				.Select(m => m.Value)
				.ToList();
			return GraphQlJson(batch.Select(sha => (sha, new[] { PrNode(shas.IndexOf(sha) + 1, mergeCommitSha: sha) })).ToList());
		});

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		comparePages.Should().Equal(1, 2);
		graphQlBatches.Should().Be(6);
		result!.TotalCommits.Should().Be(150);
		result.PullRequests.Should().HaveCount(150);
		result.PullRequests[0].Number.Should().Be(1);
		result.PullRequests[^1].Number.Should().Be(150);
	}

	[Test]
	public async Task ResolvePullRequests_EmptyRange_WarnsAndReturnsEmptyResolution()
	{
		var handler = Handler(
			_ => CompareJson(0, [], status: "identical"),
			_ => throw new InvalidOperationException("GraphQL must not be called for an empty range")
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.TotalCommits.Should().Be(0);
		result.PullRequests.Should().BeEmpty();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("identical"));
	}

	[Test]
	public async Task ResolvePullRequests_UnknownRefs_EmitsErrorAndReturnsNull()
	{
		var handler = Handler(_ => null, _ => throw new InvalidOperationException("GraphQL must not be called"));

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("404"));
	}

	[Test]
	public async Task ResolvePullRequests_MissingToken_EmitsErrorWithoutAnyRequest()
	{
		var handler = Handler(
			_ => throw new InvalidOperationException("no request expected"),
			_ => throw new InvalidOperationException("no request expected")
		);
		var service = new GitHubCommitRangeService(new TestLoggerFactory(), new GitHubApiTransport(handler, ""));

		var result = await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("GITHUB_TOKEN"));
		handler.RequestedPaths.Should().BeEmpty();
	}

	[Test]
	public async Task ResolvePullRequests_GraphQlErrors_EmitError()
	{
		var sha1 = Sha(1);
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => /*lang=json,strict*/  """{ "data": null, "errors": [ { "message": "boom" } ] }"""
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("boom"));
	}

	[Test]
	public async Task ResolvePullRequests_PullRequestWithManyLabels_ReadsEveryLabelPage()
	{
		var sha1 = Sha(1);
		const string firstPage =
			"""
			{ "number": 11, "url": "https://github.com/elastic/widget/pull/11", "title": "Fix (#5)", "merged": true, "baseRefName": "main",
			  "mergeCommit": { "oid": "0000000000000000000000000000000000000001" }, "baseRepository": { "nameWithOwner": "elastic/widget" },
			  "labels": { "pageInfo": { "hasNextPage": true, "endCursor": "cursor-1" }, "nodes": [ { "name": "bug" } ] } }
			""";
		var handler = Handler(_ => CompareJson(1, [sha1]), req =>
		{
			var query = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
			return query.Contains("cursor-1", StringComparison.Ordinal)
				? """{ "data": { "repository": { "pullRequest": { "labels": { "pageInfo": { "hasNextPage": false, "endCursor": null }, "nodes": [ { "name": "backport" } ] } } } } }"""
				: GraphQlJson([(sha1, [firstPage])]);
		});

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle().Which.Labels.Should().Equal("bug", "backport");
	}

	private const string LabelFirstPage =
		"""
		{ "number": 11, "url": "https://github.com/elastic/widget/pull/11", "title": "Fix (#5)", "merged": true, "baseRefName": "main",
		  "mergeCommit": { "oid": "0000000000000000000000000000000000000001" }, "baseRepository": { "nameWithOwner": "elastic/widget" },
		  "labels": { "pageInfo": { "hasNextPage": true, "endCursor": "cursor-1" }, "nodes": [ { "name": "bug" } ] } }
		""";

	private const string LabelLastPage =
		"""{ "data": { "repository": { "pullRequest": { "labels": { "pageInfo": { "hasNextPage": false, "endCursor": null }, "nodes": [ { "name": "backport" } ] } } } } }""";

	/// <summary>A range of one pull request whose first label page is truncated, and whose later page is served by <paramref name="laterPage"/>.</summary>
	private async Task<CommitRangeResolution?> ResolveWithLabelPageAsync(Func<string> laterPage)
	{
		var sha1 = Sha(1);
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			req => req.Content!.ReadAsStringAsync().GetAwaiter().GetResult().Contains("cursor-1", StringComparison.Ordinal)
				? laterPage()
				: GraphQlJson([(sha1, [LabelFirstPage])])
		);
		var service = new GitHubCommitRangeService(
			new TestLoggerFactory(),
			new GitHubApiTransport(handler, "test-token", (_, _) => Task.CompletedTask)
		);

		return await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);
	}

	[Test]
	public async Task ResolvePullRequests_LabelPageBodyEmptyEveryTime_IsAnErrorNotACrash()
	{
		var result = await ResolveWithLabelPageAsync(() => string.Empty);

		result.Should().BeNull();
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Error && d.Message.Contains("labels of elastic/widget#11") && d.Message.Contains(
					"not valid JSON"
				)
			);
	}

	[Test]
	public async Task ResolvePullRequests_LabelPageBodyMalformed_IsAnErrorNotACrash()
	{
		var result = await ResolveWithLabelPageAsync(() => "this is not json");

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("not valid JSON"));
	}

	[Test]
	public async Task ResolvePullRequests_LabelPageBodyEmptyOnce_IsRetriedAndReadsEveryLabel()
	{
		var calls = 0;

		var result = await ResolveWithLabelPageAsync(() => ++calls == 1 ? string.Empty : LabelLastPage);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle().Which.Labels.Should().Equal("bug", "backport");
		Collector.Errors.Should().Be(0);
	}

	[Test]
	public async Task ResolvePullRequests_GraphQlBodyEmptyEveryTime_IsAnErrorNotACrash()
	{
		var sha1 = Sha(1);
		var handler = Handler(_ => CompareJson(1, [sha1]), _ => string.Empty);
		var service = new GitHubCommitRangeService(
			new TestLoggerFactory(),
			new GitHubApiTransport(handler, "test-token", (_, _) => Task.CompletedTask)
		);

		var result = await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("not valid JSON"));
	}

	[Test]
	public async Task ResolvePullRequests_GraphQlBodyEmptyOnce_IsRetriedAndSucceeds()
	{
		var sha1 = Sha(1);
		var calls = 0;
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => ++calls == 1 ? string.Empty : GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1)])])
		);
		var service = new GitHubCommitRangeService(
			new TestLoggerFactory(),
			new GitHubApiTransport(handler, "test-token", (_, _) => Task.CompletedTask)
		);

		var result = await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Select(p => p.Number).Should().Equal(11);
		Collector.Errors.Should().Be(0);
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedPaths { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedPaths.Add(request.RequestUri!.AbsolutePath);
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
