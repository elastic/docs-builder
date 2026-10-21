// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using AwesomeAssertions;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Loading;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Tests.DocSet;

/// <summary>
/// Every path a cross-link artifact takes into the <see cref="CrossLinkFetcher"/> (fetched, cached on disk, the link index) is checked: what it breaks is
/// logged, with the same severity mapping as before, and never throws or fails the build.
/// </summary>
public class CrossLinkFetcherCheckTests
{
	// a page that is not markdown: an Error rule of LinkManifestConstraints
	private const string BrokenManifest =
		"""
		{
		  "origin": { "branch": "main", "remote": "https://example.test/repo.git", "ref": "abc", "name": "repo" },
		  "url_path_prefix": "/docs",
		  "links": { "index.md": { "anchors": [] }, "notes.txt": { "anchors": [] } },
		  "cross_links": []
		}
		""";

	private const string RegistryWithoutMain =
		"""
		{ "repositories": { "kibana": { "8.x": { "repository": "kibana", "path": "elastic/kibana/8.x/links.json", "branch": "8.x", "etag": "e1", "ref": "abc", "updated_at": "2026-01-01T00:00:00Z" } } } }
		""";

	private static readonly LinkRegistryEntry Entry = new()
	{
		Repository = "kibana",
		Branch = "8.x",
		Path = "elastic/kibana/8.x/links.json",
		ETag = "e1",
		GitReference = "abc",
		UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
	};

	private static string CachedPath => Path.Join(Paths.ApplicationData.FullName, "links", "links-elastic-kibana-8.x-e1.json");

	private sealed class StubReader(string manifestJson, string registryJson) : ILinkIndexReader
	{
		public int ManifestReads { get; private set; }

		public string RegistryUrl => "https://registry.test/link-index.json";

		public Task<Result<LinkRegistry>> GetRegistry(Cancel cancellationToken = default) =>
			Task.FromResult(LinkLoader.LoadRegistry(registryJson, "link-index.json"));

		public Task<Result<LinkManifest>> GetRepositoryLinks(string key, Cancel cancellationToken = default)
		{
			ManifestReads++;
			return Task.FromResult(LinkLoader.LoadManifest(manifestJson, key));
		}
	}

	private sealed class TestFetcher(
		ILoggerFactory logFactory,
		ILinkIndexReader reader,
		ApplicationDataFileSystem fileSystem
	) : CrossLinkFetcher(logFactory, reader, fileSystem)
	{
		public override Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx) => throw new NotSupportedException();

		public Task<LinkManifest> Links(LinkRegistryEntry entry, Cancel ctx) => FetchLinkIndexEntry(entry.Repository, entry, ctx);

		public Task<LinkRegistry> Registry(Cancel ctx) => FetchLinkRegistry(ctx);
	}

	private static (TestFetcher Fetcher, StubReader Reader, List<(LogLevel Level, string Message)> Log, MockFileSystem Fs) Arrange(
		string manifestJson,
		string? cached = null
	)
	{
		var fs = new MockFileSystem();
		if (cached is not null)
			fs.AddFile(CachedPath, new MockFileData(cached));
		var log = new List<(LogLevel, string)>();
		var reader = new StubReader(manifestJson, RegistryWithoutMain);
		return (new TestFetcher(new CapturingLoggerFactory(log), reader, new ApplicationDataFileSystem(fs)), reader, log, fs);
	}

	[Test]
	public async Task AFetchedLinksJson_IsCheckedAndItsErrorIsLoggedAsAWarning()
	{
		var (fetcher, reader, log, _) = Arrange(BrokenManifest);

		var manifest = await fetcher.Links(Entry, Cancel.None);

		manifest.Links.Should().ContainKey("notes.txt", "a finding never drops or throws");
		reader.ManifestReads.Should().Be(1);
		log.Should().ContainSingle(
			e => e.Level == LogLevel.Warning && e.Message.Contains(LinkManifestConstraints.LinkManifestPagePathsAreMarkdown.Name)
		);
	}

	[Test]
	public async Task TheCachedLinksJson_IsCheckedToo_AndTheReaderIsNotAsked()
	{
		var (fetcher, reader, log, _) = Arrange(BrokenManifest, cached: BrokenManifest);

		var manifest = await fetcher.Links(Entry, Cancel.None);

		reader.ManifestReads.Should().Be(0, "the cache answered");
		manifest.Links.Should().ContainKey("notes.txt");
		log.Should().ContainSingle(
			e => e.Level == LogLevel.Warning && e.Message.Contains(LinkManifestConstraints.LinkManifestPagePathsAreMarkdown.Name)
		);
	}

	[Test]
	public async Task ACachedFileThatDoesNotParse_IsLoggedAndRefetched()
	{
		var (fetcher, reader, log, _) = Arrange(BrokenManifest, cached: "{ not json");

		var manifest = await fetcher.Links(Entry, Cancel.None);

		reader.ManifestReads.Should().Be(1, "an unreadable cache is a miss");
		manifest.Links.Should().ContainKey("index.md");
		log.Should().Contain(e => e.Level == LogLevel.Error && e.Message.Contains("Failed to read cached link reference"));
	}

	[Test]
	public async Task TheLinkIndex_IsCheckedAndItsErrorIsLoggedAsAWarning()
	{
		var (fetcher, _, log, _) = Arrange(BrokenManifest);

		var registry = await fetcher.Registry(Cancel.None);

		registry.Repositories.Should().ContainKey("kibana");
		log.Should().ContainSingle(
			e => e.Level == LogLevel.Warning && e.Message.Contains(LinkRegistryConstraints.RepositoryHasMainOrMaster.Name)
		);
	}

	/// <summary>
	/// The readers of the link index hand out checked results, never bare records, so a consumer cannot skip the check.
	/// </summary>
	[Test]
	public void TheLinkIndexReaderReturnsCheckedResults()
	{
		var methods = typeof(ILinkIndexReader).GetMethods(BindingFlags.Public | BindingFlags.Instance);

		methods.Should().NotBeEmpty();
		methods
			.Where(m => m.ReturnType.IsGenericType)
			.Select(m => m.ReturnType.GetGenericArguments().Single())
			.Should()
			.OnlyContain(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Result<>), "Task<Result<T>>, never Task<T>");
	}

	private sealed class CapturingLoggerFactory(List<(LogLevel Level, string Message)> entries) : ILoggerFactory
	{
		public void AddProvider(ILoggerProvider provider) { }

		public ILogger CreateLogger(string categoryName) => new Logger(entries);

		public void Dispose() { }

		private sealed class Logger(List<(LogLevel Level, string Message)> entries) : ILogger
		{
			public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(
				LogLevel logLevel,
				EventId eventId,
				TState state,
				Exception? exception,
				Func<TState, Exception?, string> formatter
			) => entries.Add((logLevel, formatter(state, exception)));
		}
	}
}
