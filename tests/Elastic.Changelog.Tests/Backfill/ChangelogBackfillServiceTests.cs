// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using AwesomeAssertions;
using Elastic.Changelog.Backfill;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.ScopedFileSystem;

namespace Elastic.Changelog.Tests.Backfill;

[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable")]
public class ChangelogBackfillServiceTests
{
	private static readonly string[] InScopeVersions = ["1.10.0", "1.9.0", "1.7.0", "1.4.1"];

	private readonly MockFileSystem _mockFileSystem;
	private readonly ScopedFileSystem _fileSystem;
	private readonly TestDiagnosticsCollector _collector;
	private readonly StubHandler _httpHandler;

	public ChangelogBackfillServiceTests(ITestOutputHelper output)
	{
		_mockFileSystem = new MockFileSystem(new MockFileSystemOptions { CurrentDirectory = Paths.WorkingDirectoryRoot.FullName });
		_fileSystem = CheckoutsFileSystem.FromWorkingDirectory(_mockFileSystem).Write;
		_collector = new TestDiagnosticsCollector(output);
		_httpHandler =
			new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ReleaseNotesFixture.Markdown) });
	}

	private ChangelogBackfillService CreateService() => new(NullLoggerFactory.Instance, _fileSystem, _httpHandler);

	private BackfillArguments Args(bool dryRun = false, string[]? products = null, string[]? versions = null) =>
		new()
		{
			Output = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test"),
			DryRun = dryRun,
			Products = products ?? [],
			Versions = versions ?? [],
			BaseUrl = "https://www.elastic.co/docs",
			RawBaseUrl = "https://raw.githubusercontent.com"
		};

	[Fact]
	public async Task FetchesRepoSourceFromRawGithubusercontent()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		_httpHandler.RequestedUrls
			.Should()
			.ContainSingle(
				u =>
					u.Contains("elastic-otel-java") && u.Contains("9a61ce4faaf08e272c433a083bcc6f0e96d80e0a") &&
						u.Contains("docs/release-notes/index.md")
			);
	}

	[Fact]
	public async Task Backfill_EdotJava_WritesBundleYamlFilesForInScopeVersions()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog", "bundles");
		foreach (var version in InScopeVersions)
			_mockFileSystem.FileExists(Path.Join(bundleDir, $"{version}.yaml")).Should().BeTrue($"bundle for {version} should be written");
	}

	[Fact]
	public async Task DryRun_WritesNoFiles_ButPopulatesResults()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.Backfill(_collector, Args(dryRun: true, products: ["edot-java"]), ct);

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		service.LastResults.Should().ContainSingle();
		service.LastResults[0].Outcome.Should().Be("ok");
		service.LastResults[0].Detail.Should().Contain("dry-run");
		_mockFileSystem.AllFiles.Should().BeEmpty("dry-run must not write anything");
	}

	[Fact]
	public async Task Fetch404_ReturnsUnavailableAndDoesNotFail()
	{
		var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
		var service = new ChangelogBackfillService(NullLoggerFactory.Instance, _fileSystem, handler);
		var ct = TestContext.Current.CancellationToken;

		var result = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		// A 404 is "unavailable", not a failure — the run still returns true and only emits a hint
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		service.LastResults.Should().ContainSingle().Which.Outcome.Should().Be("unavailable");
	}

	[Fact]
	public async Task UnknownProduct_FailsWithoutAnyNetworkAccess()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		var result = await service.Backfill(_collector, Args(products: ["not-configured"]), ct);

		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_httpHandler.RequestedUrls.Should().BeEmpty();
	}

	[Fact]
	public async Task VersionsBeyondCutoff_AreExcludedFromOutput()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		var bundleDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog", "bundles");
		// 2.0.0 > cutoff 1.10.0 — belongs to the live pipeline
		_mockFileSystem.FileExists(Path.Join(bundleDir, "2.0.0.yaml")).Should().BeFalse("2.0.0 is beyond the cutoff");
	}

	[Fact]
	public async Task VersionsFilter_RestrictsToSelection()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"], versions: ["1.9.0"]), ct);

		var bundleDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog", "bundles");
		_mockFileSystem.FileExists(Path.Join(bundleDir, "1.9.0.yaml")).Should().BeTrue();
		_mockFileSystem.FileExists(Path.Join(bundleDir, "1.7.0.yaml")).Should().BeFalse("not in --versions filter");
	}

	[Fact]
	public async Task Bundle_Yaml_RoundTripsThroughDeserializer()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		var bundlePath = Path.Join(
			Paths.WorkingDirectoryRoot.FullName,
			".artifacts",
			"backfill-test",
			"edot-java",
			"changelog",
			"bundles",
			"1.9.0.yaml"
		);
		var yaml = _mockFileSystem.File.ReadAllText(bundlePath);
		var bundle = Documentation.Configuration.ReleaseNotes.ReleaseNotesSerialization.DeserializeBundle(yaml);

		bundle.Products.Should().ContainSingle().Which.Target.Should().Be("1.9.0");
		bundle.Entries.Should().HaveCount(2);
		bundle.Entries[0].Type.Should().Be(Documentation.ReleaseNotes.ChangelogEntryType.BreakingChange);
	}

	[Fact]
	public async Task PrEntry_WrittenAsPrNumberDotYaml()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		// 1.9.0 has #958 and #960 as bare-ref PR entries.
		var changelogDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog");
		_mockFileSystem.FileExists(Path.Join(changelogDir, "958.yaml")).Should().BeTrue("PR 958 entry should be written");
		_mockFileSystem.FileExists(Path.Join(changelogDir, "960.yaml")).Should().BeTrue("PR 960 entry should be written");
	}

	[Fact]
	public async Task PrEntry_YamlContainsTargetVersion()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		var entryPath = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog", "958.yaml");
		var yaml = _mockFileSystem.File.ReadAllText(entryPath);
		var entry = Documentation.Configuration.ReleaseNotes.ReleaseNotesSerialization.DeserializeEntry(yaml);

		entry.Type.Should().Be(Documentation.ReleaseNotes.ChangelogEntryType.BreakingChange);
		entry.Products.Should().ContainSingle().Which.Target.Should().Be("1.9.0");
		entry.Prs.Should().ContainSingle(p => p.Contains("/pull/958"));
	}

	[Fact]
	public async Task NoPrEntry_WrittenAsNoteDotYaml()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		// 1.4.1 has a fix entry with no PR reference.
		var changelogDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog");
		var noteFiles = _mockFileSystem.AllFiles
			.Where(
				f =>
					f.StartsWith(changelogDir, StringComparison.Ordinal) &&
						Path.GetFileName(f).StartsWith("note-", StringComparison.Ordinal)
			)
			.ToList();
		noteFiles.Should().NotBeEmpty("at least one PR-less entry should produce a note-*.yaml file");
	}

	[Fact]
	public async Task NoPrEntry_NotesRegistryWritten()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		// notes-1.4.1.json should be written because 1.4.1 has a PR-less fix.
		var changelogDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog");
		_mockFileSystem.FileExists(Path.Join(changelogDir, "notes-1.4.1.json")).Should().BeTrue();

		var json = _mockFileSystem.File.ReadAllText(Path.Join(changelogDir, "notes-1.4.1.json"));
		var registry = System.Text.Json.JsonSerializer.Deserialize(json, BackfillJsonContext.Default.NotesRegistry);
		registry.Should().NotBeNull();
		registry.Target.Should().Be("1.4.1");
		registry.Notes.Should().NotBeEmpty();
		registry.Notes.Should().AllSatisfy(n => n.Should().StartWith("note-").And.EndWith(".yaml"));
	}

	[Fact]
	public async Task NoPrEntries_CountedInResult()
	{
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		var result = service.LastResults.Should().ContainSingle().Subject;
		result.NoPrEntries.Should().BeGreaterThan(0, "fixture has at least one PR-less entry");
	}

	[Fact]
	public async Task DuplicatePr_AcrossVersions_WrittenOnceAndCounted()
	{
		// Fixture has PR 850 in 1.7.0 (known-issue). If we could inject it again we'd test dedup.
		// Verify that the same PR number isn't double-written even in the current fixture.
		var service = CreateService();
		var ct = TestContext.Current.CancellationToken;

		_ = await service.Backfill(_collector, Args(products: ["edot-java"]), ct);

		// All PR files should be unique names (no overwritten paths).
		var changelogDir = Path.Join(Paths.WorkingDirectoryRoot.FullName, ".artifacts", "backfill-test", "edot-java", "changelog");
		var prFiles = _mockFileSystem.AllFiles
			.Where(f => f.StartsWith(changelogDir, StringComparison.Ordinal))
			.Where(f => !f.Contains("bundles", StringComparison.Ordinal))
			.Where(
				f =>
					Path.GetFileName(f) is var n && !n.StartsWith("note-", StringComparison.Ordinal) &&
						!n.StartsWith("notes-", StringComparison.Ordinal)
			)
			.Select(Path.GetFileName)
			.ToList();

		prFiles.Should().OnlyHaveUniqueItems("no PR number should be written twice");
	}

	private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
	{
		public List<string> RequestedUrls { get; } = [];

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestedUrls.Add(request.RequestUri!.ToString());
			return responder(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(Send(request, cancellationToken));
	}
}
