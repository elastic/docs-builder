// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.GitHub;
using Elastic.Changelog.GithubRelease;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs.Create;

/// <summary>
/// Tests for gh-release extraction parity (B5 — elastic/docs-builder#3775): the same fidelity
/// ladder as commit-range bundling. A checked-in entry from the pool wins; otherwise release-note
/// text from the PR body becomes the description and linked issues are carried over; title/link-only
/// remains the last resort.
/// </summary>
public class GhReleaseExtractionParityTests() : ChangelogTestBase()
{
	private readonly IGitHubReleaseService _releaseService = A.Fake<IGitHubReleaseService>();
	private readonly IGitHubPrService _prService = A.Fake<IGitHubPrService>();
	private readonly IGitHubCommitRangeService _commitRangeService = A.Fake<IGitHubCommitRangeService>();

	// language=yaml
	private const string PoolEntry =
		"""
		title: Curated checked-in title
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.2.0
		    lifecycle: ga
		""";

	private GitHubReleaseChangelogService Service(StubHandler handler) =>
		new(
			LoggerFactory,
			ConfigurationContext,
			FileSystem,
			_releaseService,
			_prService,
			commitRangeService: _commitRangeService,
			entryFetcher: new CdnChangelogEntryFetcher(new TestLoggerFactory(), handler, sleep: (_, _) => Task.CompletedTask)
		);

	/// <summary>Pool with a single entry named after PR 12345 (the CI naming scheme).</summary>
	private static StubHandler PoolWithEntry() =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/
					"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "12345.yaml" } ] }"""
				);
			if (path.EndsWith("12345.yaml", StringComparison.Ordinal))
				return Yaml(PoolEntry);
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	private static StubHandler EmptyPool() => new(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

	/// <summary>Pool with a single entry named after PR 12345 whose YAML fails to parse.</summary>
	private static StubHandler PoolWithUnparseableEntry() =>
		new(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/
					"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "12345.yaml" } ] }"""
				);
			if (path.EndsWith("12345.yaml", StringComparison.Ordinal))
				return Yaml("title: [unterminated");
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});

	private void ArrangeRelease(params int[] prNumbers)
	{
		if (prNumbers.Length == 0)
			prNumbers = [12345];

		A.CallTo(() => _releaseService.FetchReleaseAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._)).Returns(new GitHubReleaseInfo
		{
			TagName = "v9.2.0",
			Name = "9.2.0",
			Body = ""
		});

		A.CallTo(() => _releaseService.FetchPreviousTagAsync("elastic", "elasticsearch", "v9.2.0", A<Cancel>._)).Returns(
			PreviousTagResult.Found("v9.1.0")
		);

		var prs = prNumbers.Select(
			n => new CommitRangePullRequest
			{
				Number = n,
				Url = $"https://github.com/elastic/elasticsearch/pull/{n}",
				CommitShas = ["abc123"]
			}
		).ToList();

		A.CallTo(
			() => _commitRangeService.ResolvePullRequestsAsync(
				A<IDiagnosticsCollector>._,
				A<CommitRangeArguments>.That.Matches(
					a => a.Owner == "elastic" && a.Repo == "elasticsearch" && a.StartRef == "v9.1.0" && a.EndRef == "v9.2.0"
				),
				A<Cancel>._
			)
		).Returns(new CommitRangeResolution { TotalCommits = prNumbers.Length, PullRequests = prs, CommitsWithoutPullRequest = [] });
	}

	private CreateChangelogsFromReleaseArguments Input(string outputDir, bool createBundle = false) =>
		new() { Repository = "elastic/elasticsearch", Version = "v9.2.0", Output = outputDir, CreateBundle = createBundle };

	private string OutputDir()
	{
		var dir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(dir);
		return dir;
	}

	[Test]
	public async Task PrWithCheckedInPoolEntry_UsesItVerbatimOverSynthesis()
	{
		ArrangeRelease();
		var outputDir = OutputDir();

		var result = await Service(PoolWithEntry()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue();
		var entryPath = FileSystem.Path.Join(outputDir, "12345.yaml");
		FileSystem.File.Exists(entryPath).Should().BeTrue("the pool entry keeps its original file name");
		var content = await FileSystem.File.ReadAllTextAsync(entryPath, TestContext.Current!.Execution.CancellationToken);
		content.Should().Contain("Curated checked-in title", "checked-in entries win over synthesis");

		A.CallTo(() => _prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).MustNotHaveHappened();
	}

	[Test]
	public async Task PrWithUnparseablePoolEntry_FallsBackToPrMetadataSynthesis()
	{
		// A pool file that matches the PR by name but fails to parse must not be treated as a
		// successful write: ProcessPrReference should still fall through to PR-body extraction /
		// title fallback, and the PR must still count as an error worth surfacing.
		ArrangeRelease();
		_ = A.CallTo(() => _prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Fix query parsing edge case",
			Body = "Just a description of the change, no release-note block.",
			Labels = [],
			LinkedIssues = []
		});
		var outputDir = OutputDir();

		var result = await Service(PoolWithUnparseableEntry()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue("synthesis from PR metadata still produces an entry even though the pool file was unusable");
		FileSystem
			.File
			.Exists(FileSystem.Path.Join(outputDir, "12345.yaml"))
			.Should()
			.BeFalse("the malformed pool file is never written verbatim");
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().ContainSingle("a synthesized entry must exist for the PR despite the unusable pool file");
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		content.Should().Contain("Fix query parsing edge case");
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains("could not be parsed", StringComparison.Ordinal),
				"the parse failure on the checked-in entry must still be surfaced"
			);
	}

	[Test]
	public async Task TwoPrsSharingAnUnparseablePoolEntry_BothFallBackToSynthesis()
	{
		// Regression for a follow-up bot review finding: WrittenPoolFiles must only remember a file
		// name once it has genuinely been written. Otherwise the *first* PR to hit an unparseable
		// shared entry marks it "claimed", and a *second* PR matching the same still-unwritten file
		// (via leading-number filename matching) is wrongly treated as already satisfied and silently
		// dropped instead of falling back to PR-metadata synthesis.
		ArrangeRelease(12345, 12346);

		_ = A.CallTo(
			() => _prService.FetchPrInfoAsync(
				A<string>.That.Matches(u => u.EndsWith("12345", StringComparison.Ordinal)),
				A<string?>._,
				A<string?>._,
				A<Cancel>._
			)
		).Returns(new GitHubPrInfo
		{
			Title = "Fix query parsing edge case",
			Body = "No release note here.",
			Labels = [],
			LinkedIssues = []
		});
		_ = A.CallTo(
			() => _prService.FetchPrInfoAsync(
				A<string>.That.Matches(u => u.EndsWith("12346", StringComparison.Ordinal)),
				A<string?>._,
				A<string?>._,
				A<Cancel>._
			)
		).Returns(new GitHubPrInfo
		{
			Title = "Improve indexing throughput",
			Body = "No release note here either.",
			Labels = [],
			LinkedIssues = []
		});

		var handler = new StubHandler(req =>
		{
			var path = req.RequestUri!.AbsolutePath;
			if (path.EndsWith("/registry.json", StringComparison.Ordinal))
				return Json(/*lang=json,strict*/
					"""{ "schema_version": 1, "product": "elasticsearch", "bundles": [ { "file": "12345-12346.yaml" } ] }"""
				);
			if (path.EndsWith("12345-12346.yaml", StringComparison.Ordinal))
				return Yaml("title: [unterminated");
			return new HttpResponseMessage(HttpStatusCode.NotFound);
		});
		var outputDir = OutputDir();

		var result = await Service(handler).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue("both PRs must fall back to PR-metadata synthesis despite the shared unparseable pool file");
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(2, "each PR must synthesize its own entry rather than one silently disappearing");
		var contents = await Task.WhenAll(
			files.Select(f => FileSystem.File.ReadAllTextAsync(f, TestContext.Current!.Execution.CancellationToken))
		);
		contents.Should().Contain(c => c.Contains("Fix query parsing edge case", StringComparison.Ordinal));
		contents.Should().Contain(c => c.Contains("Improve indexing throughput", StringComparison.Ordinal));
	}

	[Test]
	public async Task PrBodyReleaseNote_BecomesEntryDescription()
	{
		ArrangeRelease();
		_ = A.CallTo(() => _prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Fix query parsing edge case",
			Body = "Context.\n\n## Release Note\nQueries with trailing wildcards no longer fail.\n\nInternal notes.",
			Labels = [],
			LinkedIssues = []
		});
		var outputDir = OutputDir();

		var result = await Service(EmptyPool()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue();
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().ContainSingle();
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		content.Should().Contain("Queries with trailing wildcards no longer fail.");
	}

	[Test]
	public async Task LinkedIssues_AreCarriedOntoTheEntry()
	{
		ArrangeRelease();
		_ = A.CallTo(() => _prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Fix query parsing edge case",
			Body = "Fixes #999",
			Labels = [],
			LinkedIssues = ["https://github.com/elastic/elasticsearch/issues/999"]
		});
		var outputDir = OutputDir();

		var result = await Service(EmptyPool()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue();
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		content.Should().Contain("https://github.com/elastic/elasticsearch/issues/999");
	}

	[Test]
	public async Task NoReleaseNoteInBody_FallsBackToTitleOnly()
	{
		ArrangeRelease();
		_ = A.CallTo(() => _prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Fix query parsing edge case",
			Body = "Just a description of the change, no release-note block.",
			Labels = [],
			LinkedIssues = []
		});
		var outputDir = OutputDir();

		var result = await Service(EmptyPool()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue();
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var content = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		content.Should().NotContain("description:");
		content.Should().Contain("Fix query parsing edge case");
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Severity == Severity.Hint && d.Message.Contains("No release note description was found", StringComparison.Ordinal)
			);
	}

	[Fact]
	public async Task PrBodyReleaseNote_WithTrailingParagraph_EmitsHint()
	{
		ArrangeRelease();
		_ = A.CallTo(() => _prService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<Cancel>._)).Returns(new GitHubPrInfo
		{
			Title = "Fix query parsing edge case",
			Body = "## Release note\nFirst paragraph.\n\nSecond paragraph.",
			Labels = [],
			LinkedIssues = []
		});
		var outputDir = OutputDir();

		var result = await Service(EmptyPool()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir),
			TestContext.Current.CancellationToken
		);

		result.Success.Should().BeTrue();
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Hint && d.Message.Contains("Only the first paragraph was used", StringComparison.Ordinal));
	}

	[Test]
	public async Task Bundle_IncludesPoolEntriesWithScrubbedPrsReferences()
	{
		// The pool entry has no prs field (scrubbed); a PR-URL filter could never match it. The
		// bundle selects exactly the files this run created, so it still ships.
		ArrangeRelease();
		var outputDir = OutputDir();

		var result = await Service(PoolWithEntry()).CreateChangelogsFromRelease(
			Collector,
			Input(outputDir, createBundle: true),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Success.Should().BeTrue();
		var bundlesDir = FileSystem.Path.Join(outputDir, "bundles");
		var bundleFiles = FileSystem.Directory.GetFiles(bundlesDir, "*.yml");
		bundleFiles.Should().ContainSingle();
		var bundle = await FileSystem.File.ReadAllTextAsync(bundleFiles[0], TestContext.Current!.Execution.CancellationToken);
		bundle.Should().Contain("Curated checked-in title");
		bundle.Should().Contain("name: 12345.yaml");
	}

	private static HttpResponseMessage Json(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

	private static HttpResponseMessage Yaml(string body) =>
		new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/yaml") };

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => responder(request);

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
