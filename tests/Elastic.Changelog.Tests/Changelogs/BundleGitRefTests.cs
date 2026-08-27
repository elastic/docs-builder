// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for commit-range bundling (<c>changelog bundle --start-git-ref --end-git-ref</c>):
/// the PR list is derived from the range, each PR's entry follows the pool-first /
/// inferred-from-PR-metadata precedence, the bundle records <c>git_ref</c>, and dry-run
/// reports without writing.
/// </summary>
public class BundleGitRefTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	private const string StartRef = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1";
	private const string EndRef = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2";

	// Pool entry named by PR number (the CI naming scheme for private repos): matches PR 100 by file name.
	// language=yaml
	private const string PoolEntryByName =
		"""
		title: Faster hosted search
		type: feature
		products:
		  - product: cloud-hosted
		    target: 2026-08-13
		    lifecycle: ga
		""";

	// Pool entry with a non-numeric name: matches PR 200 only via its prs reference.
	// language=yaml
	private const string PoolEntryByPrs =
		"""
		title: Sturdier snapshots
		type: bug-fix
		products:
		  - product: cloud-hosted
		    target: 2026-08-13
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/widget/pull/200
		""";

	// language=json
	private const string RegistryJson =
		"""{ "schema_version": 1, "product": "widget", "bundles": [ { "file": "100.yaml" }, { "file": "sturdier-snapshots.yaml" } ] }""";

	private StubHandler PoolHandler() =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(RegistryJson);
			if (path.EndsWith("100.yaml", StringComparison.Ordinal))
				return Yaml(PoolEntryByName);
			if (path.EndsWith("sturdier-snapshots.yaml", StringComparison.Ordinal))
				return Yaml(PoolEntryByPrs);
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	private CdnChangelogEntryFetcher Fetcher(StubHandler handler) =>
		new(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);

	private static CommitRangePullRequest RangePr(int number) =>
		new() { Number = number, Url = $"https://github.com/elastic/widget/pull/{number}", CommitShas = [$"sha-{number}"] };

	private static IGitHubCommitRangeService RangeService(params int[] prNumbers)
	{
		var service = A.Fake<IGitHubCommitRangeService>();
		_ = A.CallTo(
			() => service.ResolvePullRequestsAsync(A<IDiagnosticsCollector>._, A<CommitRangeArguments>._, A<Cancel>._)
		).Returns(new CommitRangeResolution
		{
			TotalCommits = prNumbers.Length,
			PullRequests = prNumbers.Select(RangePr).ToList(),
			CommitsWithoutPullRequest = []
		});
		return service;
	}

	private async Task<string> WriteProfileConfig(string outputDir)
	{
		// language=yaml
		var configContent = """
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			    breaking-change: ">breaking"
			bundle:
			  output_directory: PLACEHOLDER
			  repo: widget
			  owner: elastic
			  profiles:
			    promotion:
			      output_products: "cloud-hosted {version}"
			""".Replace(
			"PLACEHOLDER",
			outputDir
		);

		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);
		return configPath;
	}

	private ChangelogBundlingService Service(
		StubHandler handler,
		IGitHubCommitRangeService rangeService,
		IGitHubPrService? prService = null
	) =>
		new(LoggerFactory, FileSystem, ConfigurationContext, null, Fetcher(handler), prService ?? A.Fake<IGitHubPrService>(), rangeService);

	[Fact]
	public async Task ProfileMode_PoolFirstWithInferredFallback_WritesBundleWithGitRef()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		// PR 100: pool hit by file name. PR 200: pool hit by prs reference. PR 300: no pool entry —
		// synthesized from PR metadata (release-note text + label-derived type). PR 400: metadata
		// unavailable — reported missing, bundle still ships.
		var prService = A.Fake<IGitHubPrService>();
		_ = A.CallTo(
			() => prService.FetchPrInfoAsync("https://github.com/elastic/widget/pull/300", A<string?>._, A<string?>._, A<Cancel>._)
		).Returns(new GitHubPrInfo
		{
			Title = "Sharper autocomplete",
			Body = "Some context.\n\n## Release Note\nAutocomplete now ranks recent indices first.\n\nInternal details.",
			Labels = [">feature"],
			LinkedIssues = []
		});
		_ = A.CallTo(
			() => prService.FetchPrInfoAsync("https://github.com/elastic/widget/pull/400", A<string?>._, A<string?>._, A<Cancel>._)
		).Returns((GitHubPrInfo?)null);

		var service = Service(PoolHandler(), RangeService(100, 200, 300, 400), prService);

		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		Collector.Errors.Should().Be(0);

		// No profile output pattern → the standardized {product}-{version}.yaml convention applies.
		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().ContainSingle();
		FileSystem.Path.GetFileName(outputFiles[0]).Should().Be("cloud-hosted-2026-08-13.yaml");

		var bundle = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);

		// Pool entries win over inference.
		bundle.Should().Contain("Faster hosted search");
		bundle.Should().Contain("Sturdier snapshots");

		// The synthesized entry carries PR-body release-note text, the label-derived type, and the
		// profile's output products.
		bundle.Should().Contain("Sharper autocomplete");
		bundle.Should().Contain("Autocomplete now ranks recent indices first.");
		bundle.Should().Contain("name: 300.yaml");

		// The published endpoint ref is recorded as bundle metadata.
		bundle.Should().Contain($"git_ref: {EndRef}");

		// PR 400 is reported, not silently dropped.
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("pull/400") && d.Message.Contains("could not be fetched"));
	}

	[Fact]
	public async Task ProfileMode_DryRun_ResolvesButWritesNothing()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		var service = Service(PoolHandler(), RangeService(100, 200));

		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef,
			DryRun = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.GetFiles(outputDir, "*.yaml").Should().BeEmpty("dry-run must not write a bundle");
	}

	[Fact]
	public async Task StartRefWithoutEndRef_Errors()
	{
		var service = Service(PoolHandler(), RangeService());

		var input = new BundleChangelogsArguments { Repo = "widget", StartGitRef = StartRef };

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("must be provided together"));
	}

	[Fact]
	public async Task GitRefCombinedWithOtherFilter_Errors()
	{
		var service = Service(PoolHandler(), RangeService());

		var input = new BundleChangelogsArguments
		{
			Repo = "widget",
			StartGitRef = StartRef,
			EndGitRef = EndRef,
			Prs = ["https://github.com/elastic/widget/pull/1"]
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("cannot be combined with other filter sources"));
	}

	[Fact]
	public async Task ProfileWithProductsPattern_Errors()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		// language=yaml
		var configContent = """
			bundle:
			  output_directory: PLACEHOLDER
			  repo: widget
			  owner: elastic
			  profiles:
			    filtered:
			      products: "cloud-hosted {version} *"
			""".Replace(
			"PLACEHOLDER",
			outputDir
		);
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = Service(PoolHandler(), RangeService());

		var input = new BundleChangelogsArguments
		{
			Profile = "filtered",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("products pattern"));
	}

	[Fact]
	public async Task InferredEntry_NoTypeLabel_DefaultsToOtherWithWarning()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		var prService = A.Fake<IGitHubPrService>();
		_ = A.CallTo(() => prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Unlabeled change",
			Body = "No release note block here.",
			Labels = ["unmapped-label"],
			LinkedIssues = []
		});

		var service = Service(PoolHandler(), RangeService(999), prService);

		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().ContainSingle();
		var bundle = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		bundle.Should().Contain("Unlabeled change");
		bundle.Should().Contain("type: other");
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Warning && d.Message.Contains("defaulting to 'other'"));
	}

	[Fact]
	public async Task Plan_GitRefProfileWithoutOutputPattern_ResolvesConventionalPathAndNetworkNeeds()
	{
		// The bundle-create CI action relies on --plan's output_path to locate the generated file,
		// so the plan must mirror the {product}-{version}.yaml convention of the real run.
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		var service = Service(PoolHandler(), RangeService());

		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var plan = await service.PlanBundleAsync(Collector, input, hasReleaseVersion: false, TestContext.Current.CancellationToken);

		plan.Should().NotBeNull();
		plan.NeedsNetwork.Should().BeTrue();
		plan.NeedsGithubToken.Should().BeTrue();
		plan.OutputPath.Should().NotBeNull();
		FileSystem.Path.GetFileName(plan.OutputPath).Should().Be("cloud-hosted-2026-08-13.yaml");
	}

	[Fact]
	public void GitRangeReport_ToMarkdown_ListsPrSourcesAndOrphanCommits()
	{
		var report = new GitRangeBundleReport
		{
			StartRef = StartRef,
			EndRef = EndRef,
			TotalCommits = 3,
			Rows =
			[
				new GitRangePrReportRow
				{
					Number = 100,
					Url = "https://github.com/elastic/widget/pull/100",
					Source = GitRangePrSourceKind.Pool,
					EntryFileNames = ["100.yaml"]
				},
				new GitRangePrReportRow
				{
					Number = 300,
					Url = "https://github.com/elastic/widget/pull/300",
					Source = GitRangePrSourceKind.InferredPrBody,
					EntryFileNames = ["300.yaml"]
				},
				new GitRangePrReportRow
				{
					Number = 400,
					Url = "https://github.com/elastic/widget/pull/400",
					Source = GitRangePrSourceKind.Missing
				}
			],
			CommitsWithoutPullRequest = ["deadbeef"]
		};

		var markdown = report.ToMarkdown();

		markdown.Should().Contain($"`{StartRef}..{EndRef}`");
		markdown.Should().Contain("| [#100](https://github.com/elastic/widget/pull/100) | pool | `100.yaml` |");
		markdown.Should().Contain("| [#300](https://github.com/elastic/widget/pull/300) | inferred (PR body) | `300.yaml` |");
		markdown.Should().Contain("| [#400](https://github.com/elastic/widget/pull/400) | missing | — |");
		markdown.Should().Contain("- `deadbeef`");
	}

	[Theory]
	[InlineData("100.yaml", new[] { 100 })]
	[InlineData("100-200.yaml", new[] { 100, 200 })]
	[InlineData("123-bug-fix-some-slug.yaml", new[] { 123 })]
	[InlineData("sturdier-snapshots.yaml", new int[0])]
	[InlineData("1755000000-my-title.yaml", new[] { 1755000000 })]
	public void ParseLeadingPrNumbers_CoversNamingSchemes(string fileName, int[] expected) =>
		GitRangeEntryResolver.ParseLeadingPrNumbers(fileName).Should().Equal(expected);

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

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
