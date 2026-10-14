// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// <see cref="BackportTracer"/> swaps labeled backports in a commit range for the pull requests they came from,
/// and leaves everything else in the range alone.
/// </summary>
public class BackportTracerTests() : ChangelogTestBase()
{
	private static readonly (string, string) Repository = ("elastic", "widget");

	private static CommitRangePullRequest Pr(int number, string? title = null, params string[] labels) =>
		new()
		{
			Number = number,
			Url = $"https://github.com/elastic/widget/pull/{number}",
			CommitShas = [$"sha-{number}"],
			Title = title,
			Labels = labels
		};

	private static CommitRangeResolution Range(params CommitRangePullRequest[] prs) =>
		new() { TotalCommits = prs.Length, PullRequests = prs, CommitsWithoutPullRequest = [] };

	private static BackportRoots Roots(params int[] roots) => new(roots, BackportRootRoute.Title, RoutesDisagree: false);

	private static IGitHubBackportService Backports(Dictionary<int, BackportRoots?> answer)
	{
		var service = A.Fake<IGitHubBackportService>();
		_ = A.CallTo(
			() => service.ResolveRootsAsync(
				A<IDiagnosticsCollector>._,
				A<string>._,
				A<string>._,
				A<IReadOnlyCollection<int>>._,
				A<Cancel>._
			)
		).Returns((IReadOnlyDictionary<int, BackportRoots?>?)answer);
		return service;
	}

	private Task<BackportTrace?> TraceAsync(IGitHubBackportService service, CommitRangeResolution range) =>
		BackportTracer.TraceAsync(Collector, service, Repository, ["backport"], range, TestContext.Current!.Execution.CancellationToken);

	[Test]
	public async Task Trace_LabeledBackport_IsReplacedByItsOriginalInPlace()
	{
		var service = Backports(new() { [20] = Roots(5) });

		var trace = await TraceAsync(service, Range(Pr(10), Pr(20, "Fix (#5)", "backport"), Pr(30)));

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Select(p => p.Number).Should().Equal(10, 5, 30);
		trace.Resolution.PullRequests[1].Url.Should().Be("https://github.com/elastic/widget/pull/5");
		trace.Resolution.PullRequests[1].CommitShas.Should().Equal("sha-20");
		trace.Traced.Should().ContainSingle().Which.Backport.Should().Be(20);
		trace.Unresolved.Should().BeEmpty();
	}

	[Test]
	public async Task Trace_OriginalAlreadyInRange_IsKeptOnceWithAllCommits()
	{
		var service = Backports(new() { [20] = Roots(5) });

		var trace = await TraceAsync(service, Range(Pr(5), Pr(20, "Fix (#5)", "backport")));

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Should().ContainSingle();
		trace.Resolution.PullRequests[0].CommitShas.Should().BeEquivalentTo(["sha-5", "sha-20"]);
	}

	[Test]
	public async Task Trace_TwoBackportsOfOnePullRequest_YieldOneOriginal()
	{
		var service = Backports(new() { [20] = Roots(5), [21] = Roots(5) });

		var trace = await TraceAsync(service, Range(Pr(20, "A (#5)", "backport"), Pr(21, "B (#5)", "backport")));

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Should().ContainSingle().Which.CommitShas.Should().BeEquivalentTo(["sha-20", "sha-21"]);
	}

	[Test]
	public async Task Trace_BackportOfSeveralPullRequests_ExpandsToEachOriginal()
	{
		var service = Backports(new() { [20] = Roots(5, 6) });

		var trace = await TraceAsync(service, Range(Pr(20, "Fix (#5) (#6)", "backport")));

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Select(p => p.Number).Should().Equal(5, 6);
	}

	[Test]
	public async Task Trace_OriginalNotFound_KeepsThePullRequestAndReportsIt()
	{
		var service = Backports(new() { [20] = null });

		var trace = await TraceAsync(service, Range(Pr(20, "Something", "backport")));

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Select(p => p.Number).Should().Equal(20);
		trace.Unresolved.Should().Equal(20);
		trace.ToMarkdown().Should().Contain("#20: original not found");
	}

	[Test]
	public async Task Trace_RevertAndUnlabeledPullRequests_AreNotTraced()
	{
		var service = Backports(new());

		var trace = await TraceAsync(
			service,
			Range(Pr(10, "Fix (#5)"), Pr(20, "Revert \"Fix (#5)\"", "backport"), Pr(30, "Fix", "backport-skip"))
		);

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Select(p => p.Number).Should().Equal(10, 20, 30);
		trace.ToMarkdown().Should().BeEmpty();
		A.CallTo(service).MustNotHaveHappened();
	}

	[Test]
	public async Task Trace_LabelMatchIgnoresCase()
	{
		var service = Backports(new() { [20] = Roots(5) });

		var trace = await TraceAsync(service, Range(Pr(20, "Fix (#5)", "Backport")));

		trace.Should().NotBeNull();
		trace.Resolution.PullRequests.Select(p => p.Number).Should().Equal(5);
	}

	[Test]
	public async Task Trace_GitHubFailure_ReturnsNull()
	{
		var service = A.Fake<IGitHubBackportService>();
		_ = A.CallTo(
			() => service.ResolveRootsAsync(
				A<IDiagnosticsCollector>._,
				A<string>._,
				A<string>._,
				A<IReadOnlyCollection<int>>._,
				A<Cancel>._
			)
		).Returns((IReadOnlyDictionary<int, BackportRoots?>?)null);

		var trace = await TraceAsync(service, Range(Pr(20, "Fix (#5)", "backport")));

		trace.Should().BeNull();
	}
}
