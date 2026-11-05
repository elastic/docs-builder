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
using Elastic.Documentation.Testing;
using FakeItEasy;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// Tests for commit-range bundling (<c>changelog bundle --start-git-ref --end-git-ref</c>):
/// the PR list is derived from the range, each PR's entry follows the pool-first /
/// inferred-from-PR-metadata precedence, the bundle records <c>git_ref</c>, and dry-run
/// reports without writing.
/// </summary>
public class BundleGitRefTests() : ChangelogTestBase()
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
		new(new TestLoggerFactory(), handler, sleep: (_, _) => Task.CompletedTask);

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

	private async Task<string> WriteProfileConfig(
		string outputDir,
		string pivotProducts = "",
		string bundleExtra = "",
		string topLevel = "",
		string outputProduct = "cloud-hosted"
	)
	{
		// language=yaml
		var configContent = """
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			    breaking-change: ">breaking"
			PIVOT_PRODUCTS
			TOP_LEVEL
			bundle:
			  output_directory: PLACEHOLDER
			  repo: widget
			  owner: elastic
			  BUNDLE_EXTRA
			  profiles:
			    promotion:
			      output_products: "OUTPUT_PRODUCT {version}"
			"""
			.Replace("PLACEHOLDER", outputDir)
			.Replace("PIVOT_PRODUCTS", pivotProducts)
			.Replace("TOP_LEVEL", topLevel.Length > 0 ? topLevel : "# no top-level settings")
			.Replace("OUTPUT_PRODUCT", outputProduct)
			.Replace("BUNDLE_EXTRA", bundleExtra.Length > 0 ? bundleExtra : "# no extra settings");

		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current!.Execution.CancellationToken);
		return configPath;
	}

	private ChangelogBundlingService Service(
		StubHandler handler,
		IGitHubCommitRangeService rangeService,
		IGitHubPrService? prService = null,
		IGitHubBackportService? backportService = null,
		DecisionLog? decisions = null
	) =>
		new(
			LoggerFactory,
			FileSystem,
			ConfigurationContext,
			Fetcher(handler),
			prService ?? A.Fake<IGitHubPrService>(),
			rangeService,
			backportService: backportService,
			decisions: decisions
		);

	[Test]
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
			Body =
				"Some context.\n\n## Release Note\nAutocomplete now ranks recent indices first.\n\nInternal details.\n\n## Impact\nSaved searches that relied on alphabetical order change position.\n\n## Action\nPin the indices you need at the top.",
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

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		Collector.Errors.Should().Be(0);

		// No profile output pattern → {repo}-{product}-{version}.yaml when bundle.repo is set.
		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories);
		outputFiles.Should().ContainSingle();
		FileSystem.Path.GetFileName(outputFiles[0]).Should().Be("widget-cloud-hosted-2026-08-13.yaml");

		var bundle = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current!.Execution.CancellationToken);

		// Pool entries win over inference.
		bundle.Should().MatchSnapshot();

		Logged.Should().Contain(l => l.StartsWith("notes:") && l.Contains("first paragraph only"));

		// PR 400 is reported, not silently dropped.
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			warning  No checked-in changelog entry was found for PR https://github.com/elastic/widget/pull/400 and its metadata could not be fetched from GitHub. The bundle will not include an entry for this PR.
			"""
			);
	}

	[Test]
	public async Task ProfileMode_BackportLabelConfigured_BundlesTheOriginalInsteadOfTheBackport()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir, bundleExtra: "backports:\n    labels: [backport]");

		// PR 300 is a backport of PR 100, which has a pool entry. The backport itself has none, so
		// without tracing it would be synthesized from its own metadata.
		var range = A.Fake<IGitHubCommitRangeService>();
		_ = A.CallTo(
			() => range.ResolvePullRequestsAsync(A<IDiagnosticsCollector>._, A<CommitRangeArguments>._, A<Cancel>._)
		).Returns(new CommitRangeResolution
		{
			TotalCommits = 1,
			PullRequests = [RangePr(300) with { Labels = ["backport"] }],
			CommitsWithoutPullRequest = []
		});
		var backports = A.Fake<IGitHubBackportService>();
		_ = A.CallTo(
			() => backports.ResolveRootsAsync(
				A<IDiagnosticsCollector>._,
				"elastic",
				"widget",
				A<IReadOnlyCollection<int>>._,
				A<IReadOnlyDictionary<int, DateTimeOffset>>._,
				A<Cancel>._
			)
		).Returns(
			(IReadOnlyDictionary<int, BackportRoots?>?)new Dictionary<int, BackportRoots?>
			{
				[300] = new BackportRoots([100], BackportRootRoute.Title, RoutesDisagree: false)
			}
		);
		var prService = A.Fake<IGitHubPrService>();

		var service = Service(PoolHandler(), range, prService, backports);
		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		var bundle = await FileSystem.File.ReadAllTextAsync(
			FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories).Single(),
			TestContext.Current!.Execution.CancellationToken
		);
		bundle.Should().MatchInlineSnapshot(
			"""
			products:
			  - product: cloud-hosted
			    target: 2026-08-13
			    repo: widget
			    owner: elastic
			release-date: {today}
			git_ref: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2
			entries:
			  - file:
			      name: 100.yaml
			      checksum: c2b6527c04530bf3cfffb127578481f3a1131e54
			    type: feature
			    title: Faster hosted search
			    products:
			      - product: cloud-hosted
			        versions:
			          - 2026-08-13
			        lifecycle: ga
			"""
		);
		A.CallTo(prService).MustNotHaveHappened();
	}

	[Test]
	public async Task ProfileMode_NoBackportSetting_DoesNotTraceLabeledPullRequests()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		var range = A.Fake<IGitHubCommitRangeService>();
		_ = A.CallTo(
			() => range.ResolvePullRequestsAsync(A<IDiagnosticsCollector>._, A<CommitRangeArguments>._, A<Cancel>._)
		).Returns(new CommitRangeResolution
		{
			TotalCommits = 1,
			PullRequests = [RangePr(100) with { Labels = ["backport"] }],
			CommitsWithoutPullRequest = []
		});
		var backports = A.Fake<IGitHubBackportService>();

		var service = Service(PoolHandler(), range, backportService: backports);
		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		A.CallTo(backports).MustNotHaveHappened();
	}

	[Test]
	public async Task ProfileMode_WithADecisionLog_RecordsThePerPullRequestDecisionsInsteadOfHints()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		var prService = A.Fake<IGitHubPrService>();
		_ = A.CallTo(
			() => prService.FetchPrInfoAsync("https://github.com/elastic/widget/pull/300", A<string?>._, A<string?>._, A<Cancel>._)
		).Returns(new GitHubPrInfo
		{
			Title = "Sharper autocomplete",
			Body = "## Release Note\nAutocomplete now ranks recent indices first.\n\nInternal details.",
			Labels = [">feature"],
			LinkedIssues = []
		});
		_ = A.CallTo(
			() => prService.FetchPrInfoAsync("https://github.com/elastic/widget/pull/301", A<string?>._, A<string?>._, A<Cancel>._)
		).Returns(new GitHubPrInfo { Title = "Quieter logs", Body = "No release note section.", Labels = [">feature"], LinkedIssues = [] });

		var lines = new List<string>();
		using var decisions = new DecisionLog(new LineLogger(lines));
		var service = Service(PoolHandler(), RangeService(100, 200, 300, 301), prService, decisions: decisions);

		var result = await service.BundleChangelogs(
			Collector,
			new BundleChangelogsArguments
			{
				Profile = "promotion",
				ProfileArgument = "2026-08-13",
				Config = configPath,
				StartGitRef = StartRef,
				EndGitRef = EndRef
			},
			TestContext.Current!.Execution.CancellationToken
		);
		decisions.Flush();

		result.Should().BeTrue();
		lines.Should().Contain("entries: pool #100 #200 · inferred (PR body) #300 · inferred (title) #301");
		lines.Should().Contain("notes: first paragraph only #300 · no release note #301");
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead."
			);
	}

	private sealed class LineLogger(List<string> lines) : Microsoft.Extensions.Logging.ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

		public void Log<TState>(
			Microsoft.Extensions.Logging.LogLevel logLevel,
			Microsoft.Extensions.Logging.EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		) => lines.Add(formatter(state, exception));
	}

	[Test]
	public async Task ProfileMode_TwoTargetsOfOneRepository_ReadThePoolOnce()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);

		var pool = PoolHandler();
		var service = Service(pool, RangeService(100, 200));

		async Task<bool> BundleAsync(string version) =>
			await service.BundleChangelogs(
				Collector,
				new BundleChangelogsArguments
				{
					Profile = "promotion",
					ProfileArgument = version,
					Config = configPath,
					StartGitRef = StartRef,
					EndGitRef = EndRef
				},
				TestContext.Current!.Execution.CancellationToken
			);

		(await BundleAsync("2026-08-13")).Should().BeTrue();
		var afterFirst = pool.RequestedPaths.Count(r => r.EndsWith("registry.json", StringComparison.Ordinal));
		(await BundleAsync("2026-08-14")).Should().BeTrue();

		afterFirst.Should().Be(1);
		pool
			.RequestedPaths
			.Count(r => r.EndsWith("registry.json", StringComparison.Ordinal))
			.Should()
			.Be(1, "the second target reads the pool the first one read");
		pool.RequestedPaths.Count(r => r.EndsWith("100.yaml", StringComparison.Ordinal)).Should().Be(1);
		FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories).Should().HaveCount(2);
	}

	[Test]
	public async Task ProfileMode_ConfigWithoutRepo_UsesTheRepositoryFromTheArguments()
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		// language=yaml
		var configContent =
			$"""
			pivot:
			  types:
			    feature: ">feature"
			    bug-fix: ">bug"
			    breaking-change: ">breaking"
			bundle:
			  output_directory: {outputDir}
			  profiles:
			    promotion:
			      product: cloud-hosted
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current!.Execution.CancellationToken);

		var rangeService = RangeService(100);
		var service = Service(PoolHandler(), rangeService);
		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef,
			Repo = "widget",
			Owner = "elastic"
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		_ = A.CallTo(
			() => rangeService.ResolvePullRequestsAsync(
				A<IDiagnosticsCollector>._,
				A<CommitRangeArguments>.That.Matches(a => a.Repo == "widget" && a.Owner == "elastic"),
				A<Cancel>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task ProfileMode_CdnNotesForTheTarget_AreMergedIntoTheBundle()
	{
		// language=yaml
		const string note =
			"""
			title: Heads up for the August release
			type: known-issue
			products:
			  - product: cloud-hosted
			    target: 2026-08-13
			    lifecycle: ga
			""";
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(RegistryJson);
			if (path.EndsWith("100.yaml", StringComparison.Ordinal))
				return Yaml(PoolEntryByName);
			if (path.EndsWith("sturdier-snapshots.yaml", StringComparison.Ordinal))
				return Yaml(PoolEntryByPrs);
			if (path.EndsWith("notes-2026-08-13.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/ """{"schema_version":1,"notes":[{"path":"main/note-heads-up.yml","bundle_seq":0}]}""");
			if (path.EndsWith("note-heads-up.yml", StringComparison.Ordinal))
				return Yaml(note);
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir);
		var service = Service(handler, RangeService(100));
		var input = new BundleChangelogsArguments
		{
			Profile = "promotion",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);
		var bundle = await FileSystem.File.ReadAllTextAsync(
			FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories).Single(),
			TestContext.Current!.Execution.CancellationToken
		);
		bundle.Should().MatchInlineSnapshot(
			"""
			products:
			  - product: cloud-hosted
			    target: 2026-08-13
			    repo: widget
			    owner: elastic
			release-date: {today}
			git_ref: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2
			entries:
			  - file:
			      name: 100.yaml
			      checksum: c2b6527c04530bf3cfffb127578481f3a1131e54
			    type: feature
			    title: Faster hosted search
			    products:
			      - product: cloud-hosted
			        versions:
			          - 2026-08-13
			        lifecycle: ga
			  - file:
			      name: main/note-heads-up.yml
			      checksum: c58b01d5eaf57a4b95f4686599684868e02f79ab
			    type: known-issue
			    title: Heads up for the August release
			    products:
			      - product: cloud-hosted
			        versions:
			          - 2026-08-13
			        lifecycle: ga
			"""
		);
	}

	[Test]
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

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories).Should().BeEmpty("dry-run must not write a bundle");
	}

	[Test]
	public async Task StartRefWithoutEndRef_Errors()
	{
		var service = Service(PoolHandler(), RangeService());

		var input = new BundleChangelogsArguments { Repo = "widget", StartGitRef = StartRef };

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  --start-git-ref and --end-git-ref must be provided together; the start ref is never inferred from previous bundles."
			);
	}

	[Test]
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

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  --start-git-ref/--end-git-ref cannot be combined with other filter sources: --prs. The PR list is derived from the commit range itself."
			);
	}

	[Test]
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
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current!.Execution.CancellationToken);

		var service = Service(PoolHandler(), RangeService());

		var input = new BundleChangelogsArguments
		{
			Profile = "filtered",
			ProfileArgument = "2026-08-13",
			Config = configPath,
			StartGitRef = StartRef,
			EndGitRef = EndRef
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  Profile 'filtered' has a 'products' pattern configured. A git commit range cannot be combined with a products pattern filter; use 'output_products' and 'rules' to shape the bundle."
			);
	}

	[Test]
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

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue(
			$"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}"
		);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories);
		outputFiles.Should().ContainSingle();
		var bundle = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current!.Execution.CancellationToken);
		bundle.Should().MatchInlineSnapshot(
			"""
			products:
			  - product: cloud-hosted
			    target: 2026-08-13
			    repo: widget
			    owner: elastic
			release-date: {today}
			git_ref: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2
			entries:
			  - file:
			      name: 999.yaml
			      checksum: 4d4d007a3c2b6c3d737ef99d58bd66c2cfac8c14
			    type: other
			    title: Unlabeled change
			    products:
			      - product: cloud-hosted
			        versions:
			          - 2026-08-13
			    prs:
			      - "https://github.com/elastic/widget/pull/999"
			"""
		);
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			warning https://github.com/elastic/widget/pull/999 Could not derive a changelog type from the labels of PR #999; defaulting to 'other'. Configure pivot.types in changelog.yml to map labels to types.
			"""
			);
		Logged.Should().Contain(l => l.StartsWith("notes:") && l.Contains("no release note"));
	}

	private const string KibanaProducts =
		"""
		products:
		  default:
		    - product: kibana
		    - product: cloud-serverless
		  implies:
		    security: [kibana, cloud-serverless]
		rules:
		  bundle:
		    products:
		      kibana:
		        exclude_types: [docs]
		      cloud-serverless:
		        exclude_types: [docs]
		      security:
		        exclude_types: [docs]
		""";

	private const string ElasticsearchProducts =
		"""
		products:
		  default:
		    - product: elasticsearch
		    - product: cloud-serverless
		rules:
		  bundle:
		    products:
		      elasticsearch:
		        exclude_types: [docs]
		      cloud-serverless:
		        exclude_types: [docs]
		""";

	/// <summary>Bundles one synthesized pull request for <paramref name="outputProduct"/>. The bundle text is null when none was written.</summary>
	private async Task<(bool Result, string? Bundle)> BundleSinglePr(
		string[] labels,
		string pivotProducts,
		string topLevel,
		string outputProduct
	)
	{
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);
		var configPath = await WriteProfileConfig(outputDir, pivotProducts, topLevel: topLevel, outputProduct: outputProduct);

		var prService = A.Fake<IGitHubPrService>();
		_ = A.CallTo(() => prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Faster entity resolution",
			Body = "## Release Note\nEntity resolution runs faster.",
			Labels = [.. labels],
			LinkedIssues = []
		});
		var service = Service(PoolHandler(), RangeService(300), prService);

		var result = await service.BundleChangelogs(
			Collector,
			new BundleChangelogsArguments
			{
				Profile = "promotion",
				ProfileArgument = "2026-08-13",
				Config = configPath,
				StartGitRef = StartRef,
				EndGitRef = EndRef
			},
			TestContext.Current!.Execution.CancellationToken
		);

		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml", SearchOption.AllDirectories);
		if (files.Length == 0)
			return (result, null);
		var content = await FileSystem.File.ReadAllTextAsync(files.Single(), TestContext.Current!.Execution.CancellationToken);
		return (result, content.Replace("\r\n", "\n"));
	}

	/// <summary>The products block of the single entry, after the bundle's own products.</summary>
	private static string EntryProductsOf(string bundle) => bundle[bundle.IndexOf("  products:\n", StringComparison.Ordinal)..];

	[Test]
	[Arguments(new[] { ">feature", "Team:SecuritySolution" }, new[] { "security", "kibana", "cloud-serverless" })]
	[Arguments(new[] { ">feature" }, new[] { "kibana", "cloud-serverless" })]
	public async Task InferredEntry_Products_LabelsWinThenImpliesThenDefaults(string[] labels, string[] expectedProducts)
	{
		var (result, bundle) = await BundleSinglePr(
			labels,
			"  products:\n    security: \"Team:SecuritySolution\"",
			KibanaProducts,
			"cloud-serverless"
		);

		result.Should().BeTrue();
		bundle.Should().NotBeNull();
		// The entry stays in the cloud-serverless bundle: the per-product rules.bundle check finds that product on it.
		var entry = EntryProductsOf(bundle);
		foreach (var product in new[] { "security", "kibana", "cloud-serverless" })
		{
			if (expectedProducts.Contains(product))
				entry.Should().Contain($"- product: {product}\n");
			else
				entry.Should().NotContain($"- product: {product}\n");
		}
	}

	[Test]
	public async Task InferredEntry_Products_DefaultsCarryTheirLifecycleAndNoVersion()
	{
		var (result, bundle) = await BundleSinglePr([">feature"], string.Empty, KibanaProducts, "cloud-serverless");

		result.Should().BeTrue();
		var entry = EntryProductsOf(bundle!);
		entry.Should().Contain("- product: kibana\n        lifecycle: ga\n");
		entry.Should().Contain("- product: cloud-serverless\n        lifecycle: ga\n");
		entry.Should().NotContain("versions:");
	}

	[Test]
	[Arguments(new[] { ">feature", "stateful" }, false)]
	[Arguments(new[] { ">feature", "serverless" }, true)]
	[Arguments(new[] { ">feature", "stateful", "serverless" }, true)]
	[Arguments(new[] { ">feature" }, true)]
	public async Task InferredEntry_Products_AStackOnlyLabelKeepsThePullRequestOutOfTheServerlessBundle(string[] labels, bool inBundle)
	{
		var (result, bundle) = await BundleSinglePr(
			labels,
			"  products:\n    elasticsearch: stateful\n    cloud-serverless: serverless",
			ElasticsearchProducts,
			"cloud-serverless"
		);

		if (inBundle)
		{
			result.Should().BeTrue();
			bundle.Should().Contain("Faster entity resolution");
			return;
		}

		// The only entry carries elasticsearch alone, so rules.bundle drops it and the range bundle fails.
		result.Should().BeFalse();
		bundle.Should().BeNull();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			hint  Applied rules - excluded_disjoint: 1 entries
			error  No changelog entries remained after applying rules.bundle filter
			"""
			);
	}

	[Test]
	public async Task InferredEntry_Products_TwoMappedProductsAndNoDefault_FailAPullRequestWithoutAProductLabel()
	{
		var (result, bundle) = await BundleSinglePr(
			[">feature"],
			"  products:\n    elasticsearch: stateful\n    cloud-serverless: serverless",
			string.Empty,
			"cloud-serverless"
		);

		result.Should().BeFalse();
		bundle.Should().BeNull();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			error https://github.com/elastic/widget/pull/300 No matching product label found on PR https://github.com/elastic/widget/pull/300. Add one of the following labels: stateful, serverless.
			"""
			);
	}

	[Test]
	public async Task InferredEntry_Products_TwoMappedProductsAndNoDefault_SkipLabelNeedsNoProductLabel()
	{
		// The skip label leaves the pull request out through rules.create, so its missing product label is not an error.
		_ = await BundleSinglePr(
			["skip-me"],
			"  products:\n    elasticsearch: stateful\n    cloud-serverless: serverless",
			"rules:\n  create:\n    exclude: 'skip-me'",
			"cloud-serverless"
		);

		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			error  No changelog entries could be resolved for commit range aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1..aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2 of elastic/widget.
			"""
			);
	}

	[Test]
	public async Task InferredEntry_Products_WildcardOutputProductsAndPerProductRules_SkipLabelStillApplies()
	{
		// A wildcard output product resolves to no product before the rules.create check. The global skip label
		// must still leave the pull request out, so its missing product label is not an error.
		_ = await BundleSinglePr(
			["skip-me"],
			"  products:\n    elasticsearch: stateful\n    cloud-serverless: serverless",
			"rules:\n  create:\n    exclude: 'skip-me'\n    products:\n      elasticsearch:\n        exclude: 'other'",
			"*"
		);

		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			error  No changelog entries could be resolved for commit range aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1..aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2 of elastic/widget.
			"""
			);
	}

	[Test]
	public async Task Plan_GitRefProfileWithoutOutputPattern_ResolvesConventionalPathAndNetworkNeeds()
	{
		// The bundle-create CI action relies on --plan's output_path to locate the generated file,
		// so the plan must mirror the {repo}-{product}-{version}.yaml convention of the real run.
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

		var plan = await service.PlanBundleAsync(
			Collector,
			input,
			hasReleaseVersion: false,
			TestContext.Current!.Execution.CancellationToken
		);

		plan.Should().NotBeNull();
		plan.NeedsNetwork.Should().BeTrue();
		plan.NeedsGithubToken.Should().BeTrue();
		plan.OutputPath.Should().NotBeNull();
		FileSystem.Path.GetFileName(plan.OutputPath).Should().Be("widget-cloud-hosted-2026-08-13.yaml");
	}

	[Test]
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

	[Test]
	[Arguments("100.yaml", new[] { 100 })]
	[Arguments("100-200.yaml", new[] { 100, 200 })]
	[Arguments("123-bug-fix-some-slug.yaml", new[] { 123 })]
	[Arguments("sturdier-snapshots.yaml", new int[0])]
	[Arguments("1755000000-my-title.yaml", new[] { 1755000000 })]
	public void ParseLeadingPrNumbers_CoversNamingSchemes(string fileName, int[] expected) =>
		ChangelogPrIdentity.ParseLeadingPrNumbers(fileName).Should().Equal(expected);

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

	private const string RequiredAreaPivot =
		"  required: [areas]\n  areas:\n    Search: \":Search\"\n  products:\n    elasticsearch: stateful\n    cloud-serverless: serverless";

	[Test]
	public async Task InferredEntry_Areas_RequiredAndNoAreaLabel_FailAPullRequest()
	{
		var (result, bundle) = await BundleSinglePr(
			[">feature", "serverless"],
			RequiredAreaPivot,
			ElasticsearchProducts,
			"cloud-serverless"
		);

		result.Should().BeFalse();
		bundle.Should().BeNull();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			error https://github.com/elastic/widget/pull/300 No matching area label found on PR https://github.com/elastic/widget/pull/300. Add one of the following labels: :Search.
			"""
			);
	}

	[Test]
	public async Task InferredEntry_Areas_RequiredAndAreaLabelPresent_KeepThePullRequestInTheBundle()
	{
		var (result, bundle) = await BundleSinglePr(
			[">feature", "serverless", ":Search"],
			RequiredAreaPivot,
			ElasticsearchProducts,
			"cloud-serverless"
		);

		result.Should().BeTrue();
		bundle.Should().Contain("Faster entity resolution");
	}

	[Test]
	public async Task InferredEntry_Products_RequiredDespiteProductsDefault_FailAPullRequestWithoutAProductLabel()
	{
		var (result, bundle) = await BundleSinglePr(
			[">feature"],
			"  required: [products]\n  products:\n    elasticsearch: stateful\n    cloud-serverless: serverless",
			ElasticsearchProducts,
			"cloud-serverless"
		);

		result.Should().BeFalse();
		bundle.Should().BeNull();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning {repo}/Guid_1/changelog.yml bundle.profiles.promotion.output_products is deprecated. Use 'product: <id>' instead.
			error https://github.com/elastic/widget/pull/300 No matching product label found on PR https://github.com/elastic/widget/pull/300. Add one of the following labels: stateful, serverless.
			"""
			);
	}
}
