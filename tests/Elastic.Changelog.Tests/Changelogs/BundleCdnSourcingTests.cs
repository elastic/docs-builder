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
/// Tests for the <c>changelog bundle</c> command sourcing its individual changelog entries from the
/// public CDN (the default when no <c>--directory</c> is passed and bundle.use_local_changelogs is false).
/// Probe-based: one GET per PR number keyed as <c>{pr}.yaml</c>; no pool registry consulted.
/// </summary>
public class BundleCdnSourcingTests(ITestOutputHelper output) : ChangelogTestBase(output)
{
	// language=yaml
	private const string EntryAlpha = """
		title: Alpha
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/100
		""";

	// language=yaml
	private const string EntryBravo = """
		title: Bravo
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.3.0
		    lifecycle: ga
		prs:
		  - https://github.com/elastic/elasticsearch/pull/999
		""";

	// language=yaml
	private static string MarkerFor(int parentPr) => $"link: \"{parentPr}\"\n";

	private static StubHandler ProbeHandler() => new(req =>
	{
		var path = req.RequestUri!.AbsolutePath;
		if (path.EndsWith("/100.yaml", StringComparison.Ordinal))
			return Yaml(EntryAlpha);
		if (path.EndsWith("/999.yaml", StringComparison.Ordinal))
			return Yaml(EntryBravo);
		return new HttpResponseMessage(HttpStatusCode.NotFound);
	});

	// No-op sleeper so any entry retry stays instant in tests.
	private static CdnChangelogEntryFetcher Fetcher(ITestOutputHelper output, StubHandler handler) =>
		new(new TestLoggerFactory(output), handler, sleep: (_, _) => Task.CompletedTask);

	private CdnChangelogEntryFetcher Fetcher() => Fetcher(Output, ProbeHandler());

	private string OutputPath() =>
		FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");

	[Fact]
	public async Task OptionMode_RepoResolvable_ProbesEntriesByPrNumber()
	{
		// Probe-based: each PR URL is probed as {pr}.yaml directly; no registry.json is read.
		var handler = ProbeHandler();
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/100", "https://github.com/elastic/elasticsearch/pull/999"],
			Output = output,
			Repo = "elasticsearch"
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Entries are probed by PR number from the authoring pool.
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/100.yaml", StringComparison.Ordinal));
		handler.RequestedPaths.Should().Contain(p => p.EndsWith("/999.yaml", StringComparison.Ordinal));
		handler.RequestedPaths.Should().NotContain(p => p.Contains("registry.json", StringComparison.Ordinal));

		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("Alpha");
		bundle.Should().Contain("Bravo");
	}

	[Fact]
	public async Task OptionMode_OwnerAndBranchOverride_ProbesFromThatPool()
	{
		var handler = ProbeHandler();
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/100"],
			Output = output,
			Owner = "acme-corp",
			Repo = "elasticsearch",
			Branch = "8.x"
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		handler.RequestedPaths.Should().Contain(p =>
			p.Contains("/acme-corp/elasticsearch/8.x/", StringComparison.Ordinal) &&
			p.EndsWith("/100.yaml", StringComparison.Ordinal));
	}

	[Fact]
	public async Task OptionMode_OwnerFromCombinedRepo_ProbesFromThatPool()
	{
		var handler = ProbeHandler();
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/widget/pull/100"],
			Output = output,
			Repo = "acme-corp/widget"
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		handler.RequestedPaths.Should().Contain(p =>
			p.Contains("/acme-corp/widget/main/", StringComparison.Ordinal));
	}

	[Fact]
	public async Task OptionMode_NoResolvableRepo_FallsBackToLocal()
	{
		var localDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog");
		FileSystem.Directory.CreateDirectory(localDir);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(localDir, "1-local.yaml"), EntryAlpha, TestContext.Current.CancellationToken);

		var configContent =
			$"""
			bundle:
			  directory: {localDir}
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var handler = ProbeHandler();
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, ConfigurationContext, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments { Config = configPath, Output = output, All = true };

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("local fallback must not reach the CDN");

		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: 1-local.yaml");
	}

	[Fact]
	public async Task OptionMode_UseLocalChangelogs_ForcesLocalEvenWithResolvableRepo()
	{
		var localDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog");
		FileSystem.Directory.CreateDirectory(localDir);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(localDir, "1-local.yaml"), EntryAlpha, TestContext.Current.CancellationToken);

		var configContent =
			$"""
			bundle:
			  directory: {localDir}
			  repo: elasticsearch
			  use_local_changelogs: true
			""";
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var handler = ProbeHandler();
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, ConfigurationContext, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }],
			Output = output
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("use_local_changelogs must not reach the CDN");

		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: 1-local.yaml");
	}

	[Fact]
	public async Task CdnAll_ReturnsError()
	{
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, Fetcher());

		var input = new BundleChangelogsArguments { All = true, Output = OutputPath(), Repo = "elasticsearch" };

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--all") && d.Message.Contains("--force-local"));
	}

	[Fact]
	public async Task CdnInputProducts_ReturnsError()
	{
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, Fetcher());

		var input = new BundleChangelogsArguments
		{
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "ga" }],
			Output = OutputPath(),
			Repo = "elasticsearch"
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--input-products") && d.Message.Contains("--force-local"));
	}

	[Fact]
	public async Task CdnIssues_ReturnsError()
	{
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, Fetcher());

		var input = new BundleChangelogsArguments
		{
			Issues = ["https://github.com/elastic/elasticsearch/issues/42"],
			Output = OutputPath(),
			Repo = "elasticsearch"
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("--issues") && d.Message.Contains("--force-local"));
	}

	[Fact]
	public async Task ProbeMiss_WarnsAndSkips()
	{
		// A 404 probe for a PR with no changelog entry warns and skips; the other entry is still included.
		// One request per PR (no retries on 404).
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/100.yaml", StringComparison.Ordinal))
				return Yaml(EntryAlpha);
			return new HttpResponseMessage(HttpStatusCode.NotFound); // 999 has no entry
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, fetcher);
		var output = OutputPath();

		var result = await service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/100", "https://github.com/elastic/elasticsearch/pull/999"],
			Output = output,
			Repo = "elasticsearch"
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue("probe miss is a warn, not a fatal error");
		Collector.Errors.Should().Be(0);
		// 404 is authoritative on the probe path — not retried.
		handler.RequestedPaths.Count(p => p.EndsWith("/999.yaml", StringComparison.Ordinal))
			.Should().Be(1, "404 must not be retried on the probe path");
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("Alpha", "the entry found by probe must appear in the bundle");
	}

	[Fact]
	public async Task ProbedEntry_5xxTransient_Retried()
	{
		// A transient 5xx on the probe path uses the retry budget; the final content is returned.
		var callCount = 0;
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.Contains("/100.yaml", StringComparison.Ordinal))
			{
				callCount++;
				if (callCount < 3)
					return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
				return Yaml(EntryAlpha);
			}
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, maxAttempts: 4, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, fetcher);

		var result = await service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/100"],
			Output = OutputPath(),
			Repo = "elasticsearch"
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		callCount.Should().BeGreaterThan(1, "5xx must be retried");
	}

	[Fact]
	public async Task MarkerEntry_ResolvesDepthOne()
	{
		// PR 200 is a marker pointing to PR 100 (the primary). Bundling PR 200 should resolve to the Alpha entry.
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/100.yaml", StringComparison.Ordinal))
				return Yaml(EntryAlpha);
			if (path.EndsWith("/200.yaml", StringComparison.Ordinal))
				return Yaml(MarkerFor(100));
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, fetcher);

		var output = OutputPath();
		var result = await service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/200"],
			Output = output,
			Repo = "elasticsearch"
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("Alpha", "the marker must resolve to its parent entry");
		bundle.Should().NotContain("link:", "the marker itself must not appear in the output");
	}

	[Fact]
	public async Task MarkerEntry_DuplicateMarkersToSameParent_OneEntryInBundle()
	{
		// PRs 200 and 201 are both markers for PR 100. Bundling both should yield one Alpha entry.
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/100.yaml", StringComparison.Ordinal))
				return Yaml(EntryAlpha);
			if (path.EndsWith("/200.yaml", StringComparison.Ordinal) || path.EndsWith("/201.yaml", StringComparison.Ordinal))
				return Yaml(MarkerFor(100));
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, fetcher);

		var output = OutputPath();
		var result = await service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Prs = [
				"https://github.com/elastic/elasticsearch/pull/200",
				"https://github.com/elastic/elasticsearch/pull/201"
			],
			Output = output,
			Repo = "elasticsearch"
		}, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Split("title: Alpha").Length.Should().Be(2, "exactly one Alpha entry must appear");
	}

	[Fact]
	public async Task MarkerEntry_Chain_FailsBundle()
	{
		// Marker chains (marker → marker) are hard errors.
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/200.yaml", StringComparison.Ordinal))
				return Yaml(MarkerFor(100));
			if (path.EndsWith("/100.yaml", StringComparison.Ordinal))
				return Yaml(MarkerFor(99)); // 100 is also a marker
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, fetcher);

		var result = await service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/200"],
			Output = OutputPath(),
			Repo = "elasticsearch"
		}, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("Marker chain"));
	}

	[Fact]
	public async Task MarkerEntry_ParentMissing_FailsBundle()
	{
		// A marker whose parent doesn't exist is a hard error (the pipeline promises the parent exists).
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/200.yaml", StringComparison.Ordinal))
				return Yaml(MarkerFor(100));
			return new HttpResponseMessage(HttpStatusCode.NotFound); // 100.yaml missing
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, null, null, fetcher);

		var result = await service.BundleChangelogs(Collector, new BundleChangelogsArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/200"],
			Output = OutputPath(),
			Repo = "elasticsearch"
		}, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("marker") && d.Message.Contains("100"));
	}

	[Fact]
	public async Task ProfileGitHubRelease_ScopesByOutputProductsAndFiltersByReleasePrs()
	{
		// A github_release profile resolves the authoring repo from the profile (to scope the CDN entry
		// pool) and the PR filter from the release body. Only the entry referenced by the release survives.
		var releaseService = A.Fake<IGitHubReleaseService>();
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(outputDir);

		// language=yaml
		var configContent =
			"""
			bundle:
			  output_directory: PLACEHOLDER
			  owner: elastic
			  profiles:
			    es-release:
			      source: github_release
			      repo: elasticsearch
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", outputDir);
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var releaseBody = "* Alpha by @user in https://github.com/elastic/elasticsearch/pull/100\n";
		A.CallTo(() => releaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.3.0", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.3.0", Name = "9.3.0", Body = releaseBody });

		var service = new ChangelogBundlingService(LoggerFactory, FileSystem, ConfigurationContext, releaseService, Fetcher());

		var input = new BundleChangelogsArguments
		{
			Profile = "es-release",
			ProfileArgument = "9.3.0",
			Config = configPath
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var outputFiles = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		outputFiles.Should().NotBeEmpty();
		var bundle = await FileSystem.File.ReadAllTextAsync(outputFiles[0], TestContext.Current.CancellationToken);
		bundle.Should().Contain("Alpha");
		bundle.Should().NotContain("Bravo");
	}

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
