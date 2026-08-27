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
public class GitHubCommitRangeServiceTests(ITestOutputHelper output) : ChangelogTestBase(output)
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
	private static string PrNode(int number, bool merged = true, string? mergeCommitSha = null, string repoFullName = Owner + "/" + Repo) =>
		$$"""
		{ "number": {{number}}, "url": "https://github.com/{{repoFullName}}/pull/{{number}}", "merged": {{(merged ? "true" : "false")}},
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
			_ =
				sb.Append(
					CultureInfo.InvariantCulture,
					$$""" "c{{i}}": { "oid": "{{commits[i].Sha}}", "associatedPullRequests": { "nodes": [{{string.Join(",", commits[i].PrNodes)}}] } }"""
				);
		}

		return $$"""{ "data": { "repository": {{{sb}} } } }""";
	}

	private GitHubCommitRangeService Service(StubHandler handler) =>
		new(new TestLoggerFactory(Output), new GitHubApiTransport(handler, "test-token"));

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

	[Fact]
	public async Task ResolvePullRequests_SquashCommits_ResolvesOnePrPerCommitInRangeOrder()
	{
		var (sha1, sha2) = (Sha(1), Sha(2));
		var handler = Handler(
			_ => CompareJson(2, [sha1, sha2]),
			_ => GraphQlJson([(sha1, [PrNode(11, mergeCommitSha: sha1)]), (sha2, [PrNode(12, mergeCommitSha: sha2)])])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		result.TotalCommits.Should().Be(2);
		result.PullRequests.Select(pr => pr.Number).Should().Equal(11, 12);
		result.PullRequests[0].Url.Should().Be($"https://github.com/{Owner}/{Repo}/pull/11");
		result.PullRequests[0].CommitShas.Should().Equal(sha1);
		result.CommitsWithoutPullRequest.Should().BeEmpty();
	}

	[Fact]
	public async Task ResolvePullRequests_MergeCommitPr_DeduplicatesAcrossCommits()
	{
		// Two branch commits belong to the same merge-commit PR; its merge commit is a third sha
		// that is also part of the range.
		var (sha1, sha2, mergeSha) = (Sha(1), Sha(2), Sha(3));
		var handler = Handler(
			_ => CompareJson(3, [sha1, sha2, mergeSha]),
			_ =>
				GraphQlJson([
					(sha1, [PrNode(20, mergeCommitSha: mergeSha)]),
					(sha2, [PrNode(20, mergeCommitSha: mergeSha)]),
					(mergeSha, [PrNode(20, mergeCommitSha: mergeSha)])
				])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle();
		result.PullRequests[0].Number.Should().Be(20);
		result.PullRequests[0].CommitShas.Should().Equal(sha1, sha2, mergeSha);
	}

	[Fact]
	public async Task ResolvePullRequests_CommitWithoutMergedPr_IsReportedNotDropped()
	{
		// One commit has no associated PRs at all; another only an unmerged PR; a third only a PR
		// merged into a different (fork) repository. None may silently disappear.
		var (sha1, sha2, sha3) = (Sha(1), Sha(2), Sha(3));
		var handler = Handler(
			_ => CompareJson(3, [sha1, sha2, sha3]),
			_ =>
				GraphQlJson([
					(sha1, []),
					(sha2, [PrNode(30, merged: false)]),
					(sha3, [PrNode(31, mergeCommitSha: sha3, repoFullName: "someone/fork")])
				])
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().BeEmpty();
		result.CommitsWithoutPullRequest.Should().Equal(sha1, sha2, sha3);
	}

	[Fact]
	public async Task ResolvePullRequests_MultipleMergedPrs_WarnsAndPicksDeterministically()
	{
		var sha1 = Sha(1);
		var handler = Handler(_ => CompareJson(1, [sha1]), _ => GraphQlJson([(sha1, [PrNode(42), PrNode(7)])]));

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle();
		result.PullRequests[0].Number.Should().Be(7, "ambiguity resolves deterministically to the lowest PR number");
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("multiple merged pull requests"));
	}

	[Fact]
	public async Task ResolvePullRequests_MergeCommitMatchWins_NoAmbiguityWarning()
	{
		// A commit associated with two merged PRs, but exactly one of them has this commit as its
		// merge commit — that one wins without a warning.
		var sha1 = Sha(1);
		var handler = Handler(_ => CompareJson(1, [sha1]), _ => GraphQlJson([(sha1, [PrNode(50), PrNode(60, mergeCommitSha: sha1)])]));

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.PullRequests.Should().ContainSingle();
		result.PullRequests[0].Number.Should().Be(60);
		Collector.Diagnostics.Should().NotContain(d => d.Message.Contains("multiple merged pull requests"));
	}

	[Fact]
	public async Task ResolvePullRequests_Pagination_FollowsAllComparePages()
	{
		// 150 commits: page 1 returns 100, page 2 the remaining 50. GraphQL batches per 50.
		var shas = Enumerable.Range(1, 150).Select(Sha).ToList();
		var comparePages = new List<int>();
		var graphQlBatches = 0;

		var handler = Handler(
			req =>
			{
				var query = HttpUtility.ParseQueryString(req.RequestUri!.Query);
				var page = int.Parse(query["page"]!, CultureInfo.InvariantCulture);
				comparePages.Add(page);
				var pageShas = shas.Skip((page - 1) * 100).Take(100);
				return CompareJson(150, pageShas);
			},
			_ =>
			{
				// Batches are issued sequentially in commit order, 50 shas each; every commit
				// resolves to its own squashed PR (number = index + 1).
				var batchIndex = graphQlBatches++;
				var batch = shas.Skip(batchIndex * 50).Take(50).ToList();
				return GraphQlJson(batch.Select(sha => (sha, new[] { PrNode(shas.IndexOf(sha) + 1, mergeCommitSha: sha) })).ToList());
			}
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		Collector.Errors.Should().Be(0);
		comparePages.Should().Equal(1, 2);
		graphQlBatches.Should().Be(3);
		result!.TotalCommits.Should().Be(150);
		result.PullRequests.Should().HaveCount(150);
		result.PullRequests[0].Number.Should().Be(1);
		result.PullRequests[^1].Number.Should().Be(150);
	}

	[Fact]
	public async Task ResolvePullRequests_EmptyRange_WarnsAndReturnsEmptyResolution()
	{
		var handler = Handler(
			_ => CompareJson(0, [], status: "identical"),
			_ => throw new InvalidOperationException("GraphQL must not be called for an empty range")
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().NotBeNull();
		result.TotalCommits.Should().Be(0);
		result.PullRequests.Should().BeEmpty();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("identical"));
	}

	[Fact]
	public async Task ResolvePullRequests_UnknownRefs_EmitsErrorAndReturnsNull()
	{
		var handler = Handler(_ => null, _ => throw new InvalidOperationException("GraphQL must not be called"));

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("404"));
	}

	[Fact]
	public async Task ResolvePullRequests_MissingToken_EmitsErrorWithoutAnyRequest()
	{
		var handler = Handler(
			_ => throw new InvalidOperationException("no request expected"),
			_ => throw new InvalidOperationException("no request expected")
		);
		var service = new GitHubCommitRangeService(new TestLoggerFactory(Output), new GitHubApiTransport(handler, ""));

		var result = await service.ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("GITHUB_TOKEN"));
		handler.RequestedPaths.Should().BeEmpty();
	}

	[Fact]
	public async Task ResolvePullRequests_GraphQlErrors_EmitError()
	{
		var sha1 = Sha(1);
		var handler = Handler(
			_ => CompareJson(1, [sha1]),
			_ => /*lang=json,strict*/  """{ "data": null, "errors": [ { "message": "boom" } ] }"""
		);

		var result = await Service(handler).ResolvePullRequestsAsync(Collector, Args, TestContext.Current.CancellationToken);

		result.Should().BeNull();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("boom"));
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
