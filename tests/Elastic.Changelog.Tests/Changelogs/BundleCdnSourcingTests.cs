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

	// language=json
	private const string RegistryJson =
		"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "1-alpha.yaml" }, { "file": "2-bravo.yaml" } ] }""";

	private static StubHandler RegistryHandler() => new(req =>
	{
		var path = req.RequestUri!.AbsolutePath;
		if (path.EndsWith("/registry.json", StringComparison.Ordinal))
			return Json(RegistryJson);
		if (path.EndsWith("1-alpha.yaml", StringComparison.Ordinal))
			return Yaml(EntryAlpha);
		if (path.EndsWith("2-bravo.yaml", StringComparison.Ordinal))
			return Yaml(EntryBravo);
		return new HttpResponseMessage(HttpStatusCode.NotFound);
	});

	// No-op sleeper so any entry retry stays instant in tests.
	private static CdnChangelogEntryFetcher Fetcher(ITestOutputHelper output, StubHandler handler) =>
		new(new TestLoggerFactory(output), handler, sleep: (_, _) => Task.CompletedTask);

	private CdnChangelogEntryFetcher Fetcher() => Fetcher(Output, RegistryHandler());

	private string OutputPath() =>
		FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");

	[Fact]
	public async Task OptionMode_RepoResolvable_SourcesAllEntriesFromRepoPoolOnCdn()
	{
		// Under the artifact-root layout the CDN entry pool is keyed by the authoring repo, not the
		// target product. A resolvable repo (here via --repo) is what enables CDN sourcing.
		var handler = RegistryHandler();
		var service = new ChangelogBundlingService(LoggerFactory, null, FileSystem, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }],
			Output = output,
			Repo = "elasticsearch",
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		// Entries are sourced from the authoring pool, with org/branch defaulting: changelog/{org}/{repo}/{branch}/...
		handler.RequestedPaths.Should().Contain("/changelog/elastic/elasticsearch/main/registry.json");

		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("Alpha");
		bundle.Should().Contain("Bravo");
		bundle.Should().Contain("name: 1-alpha.yaml");
	}

	[Fact]
	public async Task OptionMode_OwnerAndBranchOverride_SourcesFromThatPoolOnCdn()
	{
		// Explicit owner/branch select a specific pool; the branch is stored verbatim (dots kept).
		var handler = RegistryHandler();
		var service = new ChangelogBundlingService(LoggerFactory, null, FileSystem, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }],
			Output = output,
			Owner = "acme-corp",
			Repo = "elasticsearch",
			Branch = "8.x",
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);
		handler.RequestedPaths.Should().Contain("/changelog/acme-corp/elasticsearch/8.x/registry.json");
	}

	[Fact]
	public async Task OptionMode_NoResolvableRepo_FallsBackToLocal()
	{
		// With no --repo, no bundle.repo in config, and no git-remote resolution at the service layer, the
		// authoring repo is unresolvable. With no --directory and no use_local_changelogs, the bundler
		// still falls back to local folder sourcing rather than hitting the CDN.
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

		var handler = RegistryHandler();
		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments { Config = configPath, Output = output, All = true, Resolve = true };

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("local fallback must not reach the CDN");

		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: 1-local.yaml");
	}

	[Fact]
	public async Task OptionMode_UseLocalChangelogs_ForcesLocalEvenWithResolvableRepo()
	{
		// use_local_changelogs is the explicit opt-out: even when the authoring repo resolves (bundle.repo),
		// entries are read from the local folder and the CDN is never touched.
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

		var handler = RegistryHandler();
		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, null, Fetcher(Output, handler));
		var output = OutputPath();

		var input = new BundleChangelogsArguments
		{
			Config = configPath,
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }],
			Output = output,
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Where(d => d.Severity == Severity.Error).Select(d => d.Message))}");
		handler.RequestedPaths.Should().BeEmpty("use_local_changelogs must not reach the CDN");

		var bundle = await FileSystem.File.ReadAllTextAsync(output, TestContext.Current.CancellationToken);
		bundle.Should().Contain("name: 1-local.yaml");
	}

	[Fact]
	public async Task RegistryFailure_FailsBundle()
	{
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output),
			new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)), sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, null, FileSystem, null, fetcher);

		var input = new BundleChangelogsArguments
		{
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }],
			Output = OutputPath(),
			Repo = "elasticsearch",
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("registry"));
	}

	[Fact]
	public async Task EntryMissingAfterRetries_FailsBundle()
	{
		// The registry lists two entries but the CDN never serves one of them. After the retry budget is
		// spent the bundle must fail rather than silently omit the missing release entry.
		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(RegistryJson);
			if (path.EndsWith("1-alpha.yaml", StringComparison.Ordinal))
				return Yaml(EntryAlpha);
			return new HttpResponseMessage(HttpStatusCode.NotFound); // 2-bravo.yaml never propagates
		});
		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(Output), handler, maxAttempts: 2, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(LoggerFactory, null, FileSystem, null, fetcher);

		var input = new BundleChangelogsArguments
		{
			InputProducts = [new ProductArgument { Product = "elasticsearch", Target = "*", Lifecycle = "*" }],
			Output = OutputPath(),
			Repo = "elasticsearch",
			Resolve = true
		};

		var result = await service.BundleChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeFalse();
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("2-bravo.yaml"));
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
			      output: "elasticsearch-{version}.yaml"
			      output_products: "elasticsearch {version} {lifecycle}"
			""".Replace("PLACEHOLDER", outputDir);
		var configPath = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "changelog.yml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(configPath)!);
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var releaseBody = "* Alpha by @user in https://github.com/elastic/elasticsearch/pull/100\n";
		A.CallTo(() => releaseService.FetchReleaseAsync("elastic", "elasticsearch", "9.3.0", TestContext.Current.CancellationToken))
			.Returns(new GitHubReleaseInfo { TagName = "v9.3.0", Name = "9.3.0", Body = releaseBody });

		var service = new ChangelogBundlingService(LoggerFactory, ConfigurationContext, FileSystem, releaseService, Fetcher());

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
