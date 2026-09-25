// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Documentation.Assembler.ContentSources;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Build.Tests;

public class StackedPullRequestResolverTests
{
	private const string Repo = "elastic/docs-content";

	/// <summary>In-memory <see cref="IPullRequestLookup"/> keyed by head branch. Records every branch queried.</summary>
	private sealed class FakeLookup(params PullRequestSummary[] pullRequests) : IPullRequestLookup
	{
		public List<string> QueriedHeads { get; } = [];

		public Task<IReadOnlyList<PullRequestSummary>> ListOpenPullRequestsByHead(string repository, string headBranch, Cancel ctx)
		{
			QueriedHeads.Add(headBranch);
			IReadOnlyList<PullRequestSummary> result = pullRequests.Where(pr => pr.HeadBranch == headBranch).ToList();
			return Task.FromResult(result);
		}
	}

	private static PullRequestSummary Pr(int number, string head, string @base, string headRepo = Repo) =>
		new(number, headRepo, head, @base);

	private static StackedPullRequestResolver Resolver(FakeLookup lookup) => new(NullLoggerFactory.Instance, lookup);

	private static bool MainOrVersion(string branch) => branch == "main" || branch.Contains('.');

	[Test]
	public async Task BaseIsContentSource_MatchesWithoutQueryingGitHub()
	{
		// A decoy PR with head=main exists. A PR targeting main must never consult it.
		var lookup = new FakeLookup(Pr(1, head: "main", @base: "9.4"));

		var result = await Resolver(lookup).Resolve(Repo, "main", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.Matched);
		result.ResolvedBranch.Should().Be("main");
		result.ParentPullRequests.Should().BeEmpty();
		lookup.QueriedHeads.Should().BeEmpty();
	}

	[Test]
	public async Task OneLevelStack_ResolvesToParentBase()
	{
		var lookup = new FakeLookup(Pr(100, head: "feature-a", @base: "main"));

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Matched.Should().BeTrue();
		result.ResolvedBranch.Should().Be("main");
		result.ParentPullRequests.Should().Equal(100);
	}

	[Test]
	public async Task MultiLevelStack_ListsParentsNearestFirst()
	{
		var lookup = new FakeLookup(
			Pr(1, head: "feature-a", @base: "main"),
			Pr(2, head: "feature-b", @base: "feature-a"),
			Pr(3, head: "feature-c", @base: "feature-b")
		);

		var result = await Resolver(lookup).Resolve(Repo, "feature-c", MainOrVersion, default);

		result.Matched.Should().BeTrue();
		result.ResolvedBranch.Should().Be("main");
		result.ParentPullRequests.Should().Equal(3, 2, 1);
		lookup.QueriedHeads.Should().Equal("feature-c", "feature-b", "feature-a");
	}

	[Test]
	public async Task StackRootedOnVersionBranch_StopsAtVersionBranchEvenWhenItIsAHead()
	{
		// 9.4 is a content source and also the head of a forward-port PR into main. The walk must stop at 9.4.
		var lookup = new FakeLookup(Pr(1, head: "feature-a", @base: "9.4"), Pr(2, head: "9.4", @base: "main"));

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Matched.Should().BeTrue();
		result.ResolvedBranch.Should().Be("9.4");
		result.ParentPullRequests.Should().Equal(1);
		lookup.QueriedHeads.Should().Equal("feature-a");
	}

	[Test]
	public async Task FeatureBranchWithoutParentPr_NoParent()
	{
		var lookup = new FakeLookup();

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.NoParent);
		result.Matched.Should().BeFalse();
		result.ResolvedBranch.Should().Be("feature-a");
		result.ParentPullRequests.Should().BeEmpty();
	}

	[Test]
	public async Task ChainEndsOnNonContentSource_NoParentAfterWalking()
	{
		var lookup = new FakeLookup(Pr(1, head: "feature-b", @base: "feature-a"));

		var result = await Resolver(lookup).Resolve(Repo, "feature-b", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.NoParent);
		result.ResolvedBranch.Should().Be("feature-a");
		result.ParentPullRequests.Should().Equal(1);
	}

	[Test]
	public async Task TwoOpenPrsWithSameHead_Ambiguous()
	{
		var lookup = new FakeLookup(Pr(1, head: "feature-a", @base: "main"), Pr(2, head: "feature-a", @base: "9.4"));

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.Ambiguous);
		result.Matched.Should().BeFalse();
	}

	[Test]
	public async Task ForkPrWithSameHeadBranchName_Ignored()
	{
		// A fork PR whose head branch happens to share the name is not a stack parent.
		var lookup = new FakeLookup(Pr(1, head: "feature-a", @base: "main", headRepo: "someone/docs-content"));

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.NoParent);
	}

	[Test]
	public async Task ForkPrAlongsideUpstreamPr_UpstreamIsNotAmbiguous()
	{
		var lookup = new FakeLookup(
			Pr(1, head: "feature-a", @base: "main"),
			Pr(2, head: "feature-a", @base: "main", headRepo: "someone/docs-content")
		);

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Matched.Should().BeTrue();
		result.ParentPullRequests.Should().Equal(1);
	}

	[Test]
	public async Task CircularChain_Cycle()
	{
		var lookup = new FakeLookup(Pr(1, head: "feature-a", @base: "feature-b"), Pr(2, head: "feature-b", @base: "feature-a"));

		var result = await Resolver(lookup).Resolve(Repo, "feature-a", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.Cycle);
		result.Matched.Should().BeFalse();
	}

	[Test]
	public async Task ChainDeeperThanMaxDepth_DepthExceeded()
	{
		var depth = StackedPullRequestResolver.MaxDepth + 5;
		var prs = Enumerable.Range(0, depth).Select(i => Pr(i + 1, head: $"f{i}", @base: $"f{i + 1}")).ToArray();
		var lookup = new FakeLookup(prs);

		var result = await Resolver(lookup).Resolve(Repo, "f0", MainOrVersion, default);

		result.Outcome.Should().Be(StackResolutionOutcome.DepthExceeded);
		result.ParentPullRequests.Should().HaveCount(StackedPullRequestResolver.MaxDepth);
	}

	[Test]
	public async Task GitHubLookup_ParsesPullsResponseAndFiltersToRequestedHead()
	{
		const string json =
			"""
			[
			  { "number": 8535, "head": { "ref": "alerting-v2-remove-flowcharts", "repo": { "full_name": "elastic/docs-content" } }, "base": { "ref": "alerting-v2-reorganize-toc" } },
			  { "number": 1, "head": { "ref": "other", "repo": null }, "base": { "ref": "main" } }
			]
			""";
		using var handler = new StubHandler(json);
		using var lookup = new GitHubPullRequestLookup(NullLoggerFactory.Instance, "token", handler);

		var result = await lookup.ListOpenPullRequestsByHead(Repo, "alerting-v2-remove-flowcharts", default);

		handler.LastRequest.Should().NotBeNull();
		handler.LastRequest!.RequestUri!
			.ToString()
			.Should()
			.Be(
				"https://api.github.com/repos/elastic/docs-content/pulls?state=open&per_page=100&head=elastic%3Aalerting-v2-remove-flowcharts"
			);
		handler.LastRequest.Headers.Authorization!.Parameter.Should().Be("token");
		result.Should().HaveCount(2);
		result[0].Should().Be(new PullRequestSummary(8535, Repo, "alerting-v2-remove-flowcharts", "alerting-v2-reorganize-toc"));
		result[1].HeadRepository.Should().BeEmpty();
	}

	[Test]
	public async Task GitHubLookup_NonSuccessStatus_Throws()
	{
		using var handler = new StubHandler("{}", HttpStatusCode.Forbidden);
		using var lookup = new GitHubPullRequestLookup(NullLoggerFactory.Instance, "token", handler);

		var act = async () => await lookup.ListOpenPullRequestsByHead(Repo, "feature-a", default);

		_ = await act.Should().ThrowAsync<HttpRequestException>();
	}

	private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
	{
		public HttpRequestMessage? LastRequest { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			LastRequest = request;
			return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
		}
	}
}
