// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using AwesomeAssertions;
using Elastic.Changelog.Caching;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Time.Testing;

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

	private GitHubCommitRangeService Service(HttpMessageHandler handler) =>
		new(new TestLoggerFactory(), new GitHubApiTransport(handler, "test-token"));

	/// <summary>
	/// The query for the title, labels and update time of the pull requests of a range. Unless a test asks for something else,
	/// every pull request has a title and no label.
	/// </summary>
	private static string DefaultDetails(string query)
	{
		var pullRequests = System
			.Text
			.RegularExpressions
			.Regex
			.Matches(query, @" p(\d+): pullRequest")
			.Select(
				m =>
					$$"""
				"p{{m.Groups[1].Value}}": { "title": "PR {{m.Groups[1].Value}}", "updatedAt": "2026-10-01T00:00:00Z", "labels": { "pageInfo": { "hasNextPage": false, "endCursor": null }, "nodes": [] } }
				"""
			);

		return "{ \"data\": { \"repository\": { " + string.Join(",", pullRequests) + " } } }";
	}

	private static bool IsDetailsQuery(string body) => body.Contains("pullRequest(number:", StringComparison.Ordinal);

	private static StubHandler Handler(
		Func<HttpRequestMessage, string?> compareResponder,
		Func<HttpRequestMessage, string> graphQlResponder,
		Func<string, string>? detailsResponder = null
	) =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path == "/graphql")
			{
				var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
				return Json(IsDetailsQuery(body) ? (detailsResponder ?? DefaultDetails)(body) : graphQlResponder(req));
			}

			if (path.StartsWith($"/repos/{Owner}/{Repo}/compare/", StringComparison.Ordinal))
			{
				var body = compareResponder(req);
				return body == null ? new HttpResponseMessage(HttpStatusCode.NotFound) : Json(body);
			}

			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	// ── cache ───────────────────────────────────────────────────────────

	private readonly string _cacheDirectory = Path.Combine(
		Path.GetTempPath(),
		"docs-builder-range-cache-tests",
		Guid.NewGuid().ToString("N")
	);

	public override void Dispose()
	{
		if (Directory.Exists(_cacheDirectory))
			Directory.Delete(_cacheDirectory, recursive: true);

		base.Dispose();
		GC.SuppressFinalize(this);
	}

	private async Task<(CommitRangeResolution? Result, int Associations, int Details)> ResolveWithCacheAsync(
		IReadOnlyList<(string Sha, string[] PrNodes)> commits,
		Func<string, string>? details = null,
		TimeProvider? time = null
	)
	{
		var (associations, detailQueries) = (0, 0);
		var cache = new ChangelogCache(_cacheDirectory, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance, time);
		var handler = Handler(_ => CompareJson(commits.Count, commits.Select(c => c.Sha)), req =>
		{
			_ = Interlocked.Increment(ref associations);
			var asked = System
				.Text
				.RegularExpressions
				.Regex
				.Matches(req.Content!.ReadAsStringAsync().GetAwaiter().GetResult(), "(?<=u0022)[0-9a-f]{40}")
				.Select(m => m.Value)
				.ToHashSet();
			return GraphQlJson([.. commits.Where(c => asked.Contains(c.Sha))]);
		}, query =>
		{
			_ = Interlocked.Increment(ref detailQueries);
			return (details ?? DefaultDetails)(query);
		});
		var service = new GitHubCommitRangeService(
			new TestLoggerFactory(),
			new GitHubApiTransport(handler, "test-token", (_, _) => Task.CompletedTask),
			cache
		);

		var result = await service.ResolvePullRequestsAsync(Collector, Args with { }, TestContext.Current!.Execution.CancellationToken);
		await cache.SaveAsync(TestContext.Current!.Execution.CancellationToken);
		return (result, associations, detailQueries);
	}

	[Test]
	public async Task ResolvePullRequests_WithACache_TheSecondRunDoesNotAskAboutKnownCommits()
	{
		var (sha1, sha2) = (Sha(1), Sha(2));
		IReadOnlyList<(string, string[])> commits =
		[
			(sha1, [PrNode(11, mergeCommitSha: sha1)]),
			(sha2, [PrNode(12, mergeCommitSha: sha2)])
		];

		var first = await ResolveWithCacheAsync(commits);
		var second = await ResolveWithCacheAsync(commits);

		first.Associations.Should().Be(1);
		second.Associations.Should().Be(0, "both commits were learned by the first run");
		second.Result!.PullRequests.Select(p => p.Number).Should().Equal(first.Result!.PullRequests.Select(p => p.Number));
		second.Result.PullRequests.Select(p => p.CommitShas.Single()).Should().Equal(sha1, sha2);
	}

	[Test]
	public async Task ResolvePullRequests_WithACache_LabelsAreReadAgainEveryRun()
	{
		var sha1 = Sha(1);
		IReadOnlyList<(string, string[])> commits = [(sha1, [PrNode(11, mergeCommitSha: sha1)])];

		static string WithLabel(string label) =>
			$$"""{ "data": { "repository": { "p11": { "title": "T", "updatedAt": "2026-10-01T00:00:00Z", "labels": { "pageInfo": { "hasNextPage": false, "endCursor": null }, "nodes": [ { "name": "{{label}}" } ] } } } } }""";

		var first = await ResolveWithCacheAsync(commits, _ => WithLabel("before"));
		var second = await ResolveWithCacheAsync(commits, _ => WithLabel("after"));

		first.Result!.PullRequests.Single().Labels.Should().Equal("before");
		second.Associations.Should().Be(0);
		// A label can change after the merge, so it is never cached.
		second.Result!.PullRequests.Single().Labels.Should().Equal("after");
		second
			.Result
			.PullRequests
			.Single()
			.UpdatedAt
			.Should()
			.Be(DateTimeOffset.Parse("2026-10-01T00:00:00Z", CultureInfo.InvariantCulture));
	}

	[Test]
	public async Task ResolvePullRequests_WithACache_CommitsWithoutAMergedPullRequestAreAskedAgain()
	{
		var (direct, unmerged, merged) = (Sha(1), Sha(2), Sha(3));
		IReadOnlyList<(string, string[])> commits =
		[
			(direct, []),
			(unmerged, [PrNode(20, merged: false)]),
			(merged, [PrNode(30, mergeCommitSha: merged)])
		];

		_ = await ResolveWithCacheAsync(commits);
		var second = await ResolveWithCacheAsync(commits);

		second.Associations.Should().Be(1, "GitHub may link a pull request to the other two later, so only the merged one is remembered");
		second.Result!.CommitsWithoutPullRequest.Should().BeEquivalentTo([direct, unmerged]);
		second.Result.PullRequests.Select(p => p.Number).Should().Equal(30);
	}

	[Test]
	public async Task ResolvePullRequests_WithACache_ACommitWithoutAPullRequestIsBelievedOnceAFreshAnswerAfterADayConfirmsIt()
	{
		var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
		var (direct, merged) = (Sha(1), Sha(2));
		IReadOnlyList<(string, string[])> commits = [(direct, []), (merged, [PrNode(30, mergeCommitSha: merged)])];

		_ = await ResolveWithCacheAsync(commits, time: time);
		var sameDay = await ResolveWithCacheAsync(commits, time: time);
		time.Advance(TimeSpan.FromHours(25));
		var nextDay = await ResolveWithCacheAsync(commits, time: time);
		var dayAfter = await ResolveWithCacheAsync(commits, time: time);

		sameDay.Associations.Should().Be(1, "the direct commit is too young to be believed");
		nextDay.Associations.Should().Be(1, "a negative older than a day is asked of GitHub once more");
		dayAfter.Associations.Should().Be(0, "the fresh negative confirmed it, so nothing is asked");
		dayAfter.Result!.CommitsWithoutPullRequest.Should().Equal(direct);
		dayAfter.Result.PullRequests.Select(p => p.Number).Should().Equal(30);
	}

	[Test]
	public async Task ResolvePullRequests_ANewCommitInTheRange_IsTheOnlyOneAsked()
	{
		var (sha1, sha2) = (Sha(1), Sha(2));
		_ = await ResolveWithCacheAsync([(sha1, [PrNode(11, mergeCommitSha: sha1)])]);

		var second = await ResolveWithCacheAsync([(sha1, [PrNode(11, mergeCommitSha: sha1)]), (sha2, [PrNode(12, mergeCommitSha: sha2)])]);

		second.Associations.Should().Be(1);
		second.Result!.PullRequests.Select(p => p.Number).Should().Equal(11, 12);
	}

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
	public async Task ResolvePullRequests_ComparePageGatewayError_IsRetriedAndResolves()
	{
		var sha1 = Sha(1);
		var compareRequests = 0;
		var handler = new StubHandler(req =>
		{
			if (req.RequestUri!.AbsolutePath == "/graphql")
			{
				var body = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
				return Json(IsDetailsQuery(body) ? DefaultDetails(body) : GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1)])]));
			}

			return ++compareRequests == 1 ? new HttpResponseMessage(HttpStatusCode.BadGateway) : Json(CompareJson(1, [sha1]));
		});
		var service = new GitHubCommitRangeService(
			new TestLoggerFactory(),
			new GitHubApiTransport(handler, "test-token", (_, _) => Task.CompletedTask)
		);

		var result = await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		compareRequests.Should().Be(2);
		result.PullRequests.Select(pr => pr.Number).Should().Equal(11);
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

	private const string NoPullRequests = """{ "data": { "repository": {} } }""";

	[Test]
	public async Task ResolvePullRequests_ManyComparePages_AreReadSideBySideAndKeepCommitOrder()
	{
		var shas = Enumerable.Range(1, 1050).Select(Sha).ToList();
		var (inFlight, maxInFlight) = (0, 0);
		var pages = new System.Collections.Concurrent.ConcurrentQueue<int>();
		var handler = new DelayedHandler(async req =>
		{
			if (req.RequestUri!.AbsolutePath == "/graphql")
				return Json(NoPullRequests);

			var page = int.Parse(HttpUtility.ParseQueryString(req.RequestUri!.Query)["page"]!, CultureInfo.InvariantCulture);
			pages.Enqueue(page);
			var now = Interlocked.Increment(ref inFlight);
			int seen;
			while (now > (seen = Volatile.Read(ref maxInFlight)) && Interlocked.CompareExchange(ref maxInFlight, now, seen) != seen) { }

			// Later pages answer sooner, so the pages do not finish in order.
			await Task.Delay(Math.Max(10, 130 - (page * 10)));
			_ = Interlocked.Decrement(ref inFlight);
			return Json(CompareJson(shas.Count, shas.Skip((page - 1) * 100).Take(100)));
		});

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.TotalCommits.Should().Be(1050);
		result.CommitsWithoutPullRequest.Should().Equal(shas);
		pages.Order().Should().Equal(Enumerable.Range(1, 11));
		maxInFlight.Should().BeGreaterThan(1).And.BeLessThanOrEqualTo(BatchRunner.DefaultDegree);
	}

	private sealed class DelayedHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			responder(request);
	}

	[Test]
	public async Task ResolvePullRequests_GitHubReturnsShorterPagesThanAsked_ReadsOnUntilTheRangeIsComplete()
	{
		var shas = Enumerable.Range(1, 230).Select(Sha).ToList();
		var handler = Handler(
			req =>
			{
				// GitHub caps a page at 60 here, though 100 were asked for, so three pages cover less than the total says.
				var page = int.Parse(HttpUtility.ParseQueryString(req.RequestUri!.Query)["page"]!, CultureInfo.InvariantCulture);
				return CompareJson(shas.Count, shas.Skip((page - 1) * 60).Take(60));
			},
			_ => NoPullRequests
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.CommitsWithoutPullRequest.Should().Equal(shas);
	}

	[Test]
	public async Task ResolvePullRequests_AMiddleComparePageFails_IsAnErrorNotAPartialRange()
	{
		var shas = Enumerable.Range(1, 450).Select(Sha).ToList();
		var handler = Handler(
			req =>
			{
				var page = int.Parse(HttpUtility.ParseQueryString(req.RequestUri!.Query)["page"]!, CultureInfo.InvariantCulture);
				return page == 3 ? null : CompareJson(shas.Count, shas.Skip((page - 1) * 100).Take(100));
			},
			_ => NoPullRequests
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("could not compare"));
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
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1)])]),
			query => query.Contains("cursor-1", StringComparison.Ordinal) ? LabelLastPage : LabelFirstPage
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle().Which.Labels.Should().Equal("bug", "backport");
	}

	private const string LabelFirstPage =
		"""
		{ "data": { "repository": { "p11": { "title": "Fix (#5)", "updatedAt": "2026-10-01T00:00:00Z",
		  "labels": { "pageInfo": { "hasNextPage": true, "endCursor": "cursor-1" }, "nodes": [ { "name": "bug" } ] } } } } }
		""";

	private const string LabelLastPage =
		"""{ "data": { "repository": { "pullRequest": { "labels": { "pageInfo": { "hasNextPage": false, "endCursor": null }, "nodes": [ { "name": "backport" } ] } } } } }""";

	/// <summary>A range of one pull request whose first label page is truncated, and whose later page is served by <paramref name="laterPage"/>.</summary>
	private async Task<CommitRangeResolution?> ResolveWithLabelPageAsync(Func<string> laterPage)
	{
		var sha1 = Sha(1);
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1)])]),
			query => query.Contains("cursor-1", StringComparison.Ordinal) ? laterPage() : LabelFirstPage
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
	public async Task ResolvePullRequests_GraphQlRequestsTimeOut_IsAnErrorNotACrash()
	{
		var sha1 = Sha(1);
		var handler = new StubHandler(
			req => req.RequestUri!.AbsolutePath == "/graphql"
				? throw new TaskCanceledException(
					"The request was canceled due to the configured HttpClient.Timeout of 60 seconds elapsing."
				)
				: Json(CompareJson(1, [sha1]))
		);
		var service = new GitHubCommitRangeService(
			new TestLoggerFactory(),
			new GitHubApiTransport(handler, "test-token", (_, _) => Task.CompletedTask)
		);

		var result = await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("timed out"));
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
