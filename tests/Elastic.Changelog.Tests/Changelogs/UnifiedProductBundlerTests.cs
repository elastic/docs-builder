// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Configuration.ReleaseSchedule;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs;

/// <summary>
/// What <c>release unified bundle</c> does with one product: bundle it, skip it with a warning when it is not onboarded to
/// changelogs, or fail. A product that was asked for, or any product under <c>--strict</c>, fails instead of being skipped.
/// </summary>
public class UnifiedProductBundlerTests() : ChangelogTestBase()
{
	private const string Start = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa1";
	private const string End = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa2";

	// language=yaml
	private const string Pivot =
		"""
		pivot:
		  types:
		    feature: ">feature"
		    bug-fix: ">bug"
		    breaking-change: ">breaking"
		""";

	// language=yaml
	private const string PoolEntry =
		"""
		title: Faster hosted search
		type: feature
		products:
		  - product: cloud-hosted
		    target: 2026-08-13
		    lifecycle: ga
		""";

	private const string RegistryJson = """{ "schema_version": 1, "product": "widget", "bundles": [ { "file": "100.yaml" } ] }""";

	private static readonly ProductBundleRange Range = new("widget", "widget", "2026-08-13", Start, End);

	private readonly List<string> _requestedPaths = [];
	private string OutputDirectory => FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "bundles", "widget");

	private string ConfigsDirectory => FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "prototypes");

	private async Task WriteConfig(string content)
	{
		FileSystem.Directory.CreateDirectory(ConfigsDirectory);
		FileSystem.Directory.CreateDirectory(OutputDirectory);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(ConfigsDirectory, "widget.changelog.yml"),
			content,
			TestContext.Current!.Execution.CancellationToken
		);
	}

	private static string ConfigWithProfile(string bundleExtra = "") =>
		$$"""
		{{Pivot}}
		bundle:
		  owner: elastic
		  repo: widget
		  {{bundleExtra}}
		  profiles:
		    promotion:
		      output_products: "cloud-hosted {version}"
		""";

	/// <summary>Serves the pool of <paramref name="poolBranch"/> only; every other registry answers 404.</summary>
	private StubHandler Cdn(string poolBranch = "main") =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			_requestedPaths.Add(path);
			if (!path.Contains($"/{poolBranch}/", StringComparison.Ordinal))
				return new HttpResponseMessage(HttpStatusCode.NotFound);
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Ok(RegistryJson);
			return path.EndsWith("100.yaml", StringComparison.Ordinal) ? Ok(PoolEntry) : new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	private static HttpResponseMessage Ok(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };

	private UnifiedProductBundler Bundler(StubHandler cdn, HttpMessageHandler? repositoryHttp = null)
	{
		var range = A.Fake<IGitHubCommitRangeService>();
		_ = A.CallTo(
			() => range.ResolvePullRequestsAsync(A<IDiagnosticsCollector>._, A<CommitRangeArguments>._, A<Cancel>._)
		).Returns(new CommitRangeResolution
		{
			TotalCommits = 1,
			PullRequests =
			[
				new CommitRangePullRequest { Number = 100, Url = "https://github.com/elastic/widget/pull/100", CommitShas = ["sha-100"] }
			],
			CommitsWithoutPullRequest = []
		});

		var fetcher = new CdnChangelogEntryFetcher(new TestLoggerFactory(), cdn, sleep: (_, _) => Task.CompletedTask);
		var service = new ChangelogBundlingService(
			LoggerFactory,
			FileSystem,
			ConfigurationContext,
			fetcher,
			A.Fake<IGitHubPrService>(),
			range
		);
		var http = new HttpClient(repositoryHttp ?? new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
		return new UnifiedProductBundler(service, http, FileSystem, LoggerFactory.CreateLogger("test"), Collector);
	}

	private Task<ProductBundleOutcome> BundleAsync(UnifiedProductBundler bundler, bool failWhenNotOnboarded = false) =>
		bundler.BundleAsync(
			Range,
			OutputDirectory,
			ConfigsDirectory,
			failWhenNotOnboarded,
			TestContext.Current!.Execution.CancellationToken
		);

	private bool BundleWritten() =>
		FileSystem.Directory.Exists(OutputDirectory)
			&& FileSystem.Directory.GetFiles(OutputDirectory, "*.yaml").Any(f => !f.Contains("changelog-config"));

	[Test]
	public async Task Bundle_PoolExists_WritesTheBundle()
	{
		await WriteConfig(ConfigWithProfile());

		var outcome = await BundleAsync(Bundler(Cdn()));

		outcome.Should().Be(ProductBundleOutcome.Bundled, string.Join("; ", Collector.Diagnostics.Select(d => d.Message)));
		BundleWritten().Should().BeTrue();
	}

	[Test]
	public async Task Bundle_NoChangelogYml_IsSkippedWithAWarning()
	{
		FileSystem.Directory.CreateDirectory(OutputDirectory);

		var outcome = await BundleAsync(Bundler(Cdn()));

		outcome.Should().Be(ProductBundleOutcome.Skipped);
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.Exists(OutputDirectory).Should().BeFalse("a skipped product leaves no folder behind");
	}

	[Test]
	public async Task Bundle_NoChangelogYmlWhenAsked_Fails()
	{
		FileSystem.Directory.CreateDirectory(OutputDirectory);

		var outcome = await BundleAsync(Bundler(Cdn()), failWhenNotOnboarded: true);

		outcome.Should().Be(ProductBundleOutcome.Failed);
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("has no changelog.yml"));
	}

	[Test]
	public async Task Bundle_ConfigWithoutProfiles_IsSkipped()
	{
		await WriteConfig($"{Pivot}\nbundle:\n  owner: elastic\n  repo: widget\n");

		var outcome = await BundleAsync(Bundler(Cdn()));

		outcome.Should().Be(ProductBundleOutcome.Skipped);
		Collector.Errors.Should().Be(0);
		FileSystem.Directory.Exists(OutputDirectory).Should().BeFalse();
	}

	[Test]
	public async Task Bundle_ConfigWithoutProfilesWhenAsked_Fails()
	{
		await WriteConfig($"{Pivot}\nbundle:\n  owner: elastic\n  repo: widget\n");

		var outcome = await BundleAsync(Bundler(Cdn()), failWhenNotOnboarded: true);

		outcome.Should().Be(ProductBundleOutcome.Failed);
		Collector.Errors.Should().BeGreaterThan(0);
	}

	[Test]
	public async Task Bundle_NoEntryPool_IsSkippedWithoutAnError()
	{
		await WriteConfig(ConfigWithProfile());

		var outcome = await BundleAsync(Bundler(Cdn(poolBranch: "elsewhere")));

		outcome.Should().Be(ProductBundleOutcome.Skipped);
		Collector.Errors.Should().Be(0);
		BundleWritten().Should().BeFalse();
		FileSystem.Directory.Exists(OutputDirectory).Should().BeFalse();
	}

	[Test]
	public async Task Bundle_NoEntryPoolWhenAsked_FailsWithTheRegistryError()
	{
		await WriteConfig(ConfigWithProfile());

		var outcome = await BundleAsync(Bundler(Cdn(poolBranch: "elsewhere")), failWhenNotOnboarded: true);

		outcome.Should().Be(ProductBundleOutcome.Failed);
		Collector.Diagnostics.Should().Contain(d => d.Severity == Severity.Error && d.Message.Contains("registry"));
	}

	[Test]
	public async Task Bundle_PoolOnTheConfiguredBranch_IsFoundNotSkipped()
	{
		await WriteConfig(ConfigWithProfile("branch: 9.x"));

		var outcome = await BundleAsync(Bundler(Cdn(poolBranch: "9.x")));

		outcome.Should().Be(ProductBundleOutcome.Bundled, string.Join("; ", Collector.Diagnostics.Select(d => d.Message)));
		_requestedPaths.Should().Contain("/changelog/elastic/widget/9.x/registry.json");
	}

	[Test]
	public async Task Bundle_LocalChangelogsConfigured_DoesNotNeedAPool()
	{
		var entries = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, "docs", "changelog");
		FileSystem.Directory.CreateDirectory(entries);
		await FileSystem.File.WriteAllTextAsync(
			FileSystem.Path.Join(entries, "100.yaml"),
			PoolEntry,
			TestContext.Current!.Execution.CancellationToken
		);
		await WriteConfig(ConfigWithProfile($"use_local_changelogs: true\n  directory: {entries}"));

		var outcome = await BundleAsync(Bundler(Cdn(poolBranch: "elsewhere")));

		outcome.Should().Be(ProductBundleOutcome.Bundled, string.Join("; ", Collector.Diagnostics.Select(d => d.Message)));
		_requestedPaths.Should().BeEmpty("local sourcing never asks the CDN");
	}

	[Test]
	public async Task Bundle_InvalidConfigurationAndNoPool_StillFails()
	{
		// A products pattern cannot be combined with a commit range. That is a configuration error, not a missing pool.
		await WriteConfig(
			$$"""
			{{Pivot}}
			bundle:
			  owner: elastic
			  repo: widget
			  profiles:
			    promotion:
			      products: "cloud-hosted {version}"
			"""
		);

		var outcome = await BundleAsync(Bundler(Cdn(poolBranch: "elsewhere")));

		outcome.Should().Be(ProductBundleOutcome.Failed);
		Collector.Errors.Should().BeGreaterThan(0);
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
