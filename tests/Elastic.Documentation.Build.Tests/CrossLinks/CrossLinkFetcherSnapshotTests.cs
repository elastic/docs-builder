// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;

namespace Elastic.Documentation.Build.Tests.CrossLinks;

/// <summary>
/// The link registry and the on-disk cache of <c>links.json</c> that every cross-link fetcher shares. The reader is a
/// fake that counts its calls, and the cache lives in a mock file system, so nothing leaves the process.
/// </summary>
public class CrossLinkFetcherSnapshotTests
{
	private sealed class FakeReader(LinkRegistry registry, Dictionary<string, LinkManifest> links) : ILinkIndexReader
	{
		public List<string> Calls { get; } = [];

		public string RegistryUrl => "https://registry.example.test";

		public Task<LinkRegistry> GetRegistry(Cancel cancellationToken = default)
		{
			Calls.Add("registry");
			return Task.FromResult(registry);
		}

		public Task<LinkManifest> GetRepositoryLinks(string key, Cancel cancellationToken = default)
		{
			Calls.Add($"links {key}");
			return Task.FromResult(links[key]);
		}
	}

	/// <summary>Reads the registry and then every repository the way the link index fetcher does, through the shared base class.</summary>
	private sealed class Fetcher(ILinkIndexReader reader, MockFileSystem fileSystem) : CrossLinkFetcher(
		NullLoggerFactory.Instance,
		reader,
		new ApplicationDataFileSystem(fileSystem)
	)
	{
		public override async Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx)
		{
			var registry = await FetchLinkRegistry(ctx);
			var references = new Dictionary<string, LinkManifest>();
			var entries = new Dictionary<string, LinkRegistryEntry>();
			foreach (var (repository, branches) in registry.Repositories)
			{
				var entry = GetNextContentSourceLinkIndexEntry(branches, repository);
				entries.Add(repository, entry);
				references.Add(repository, await FetchLinkIndexEntry(repository, entry, ctx));
			}

			return new FetchedCrossLinks
			{
				DeclaredRepositories = [.. references.Keys],
				LinkReferences = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(references),
				LinkIndexEntries = System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(entries)
			};
		}

		public Task<LinkManifest> Fetch(string repository, params string[] keys) => FetchCrossLinks(repository, keys, default);

		public Task<LinkRegistryEntry> Entry(string repository) => GetLinkIndexEntry(repository, default);
	}

	private static LinkRegistryEntry Entry(string repository, string branch, string etag) =>
		new()
		{
			Repository = repository,
			Branch = branch,
			ETag = etag,
			Path = $"elastic/{repository}/{branch}/links.json",
			GitReference = "ref"
		};

	private static LinkManifest Links(string repository) =>
		LinkManifestJson.Read(
			$$"""
		{
		  "origin": { "branch": "main", "remote": "https://github.com/example-org/{{repository}}", "ref": "abc" },
		  "url_path_prefix": "/elastic/{{repository}}/tree/main",
		  "cross_links": [],
		  "links": { "index.md": {} }
		}
		"""
		);

	private static (FakeReader Reader, MockFileSystem FileSystem, Fetcher Fetcher) Create(
		params (string Repository, string Branch, string ETag)[] entries
	)
	{
		var repositories = new Dictionary<string, Dictionary<string, LinkRegistryEntry>>();
		var links = new Dictionary<string, LinkManifest>();
		foreach (var (repository, branch, etag) in entries)
		{
			if (!repositories.TryGetValue(repository, out var branches))
				repositories[repository] = branches = [];
			branches[branch] = Entry(repository, branch, etag);
			links[$"elastic/{repository}/{branch}/links.json"] = Links(repository);
		}

		var reader = new FakeReader(new LinkRegistry { Repositories = repositories }, links);
		var fileSystem = new MockFileSystem();
		return (reader, fileSystem, new Fetcher(reader, fileSystem));
	}

	private static string Cache(MockFileSystem fileSystem) =>
		string.Join(
			'\n',
			fileSystem
				.Directory
				.EnumerateFiles(Paths.ApplicationData.FullName, "*", SearchOption.AllDirectories)
				.Select(f => fileSystem.Path.GetRelativePath(Paths.ApplicationData.FullName, f).Replace('\\', '/'))
				.Order(StringComparer.Ordinal)
		);

	[Test]
	public async Task FetchCrossLinks_TwoFetches_ReadTheRegistryOnceAndCacheTheLinksOnDisk()
	{
		var (reader, fileSystem, fetcher) = Create(("alpha", "main", "etag-a"), ("beta", "master", "etag-b"));

		var first = await fetcher.FetchCrossLinks(default);
		var second = await fetcher.FetchCrossLinks(default);

		var sb = new StringBuilder();
		_ = sb.AppendLine("reader calls: " + string.Join(", ", reader.Calls));
		_ = sb.AppendLine("declared: " + string.Join(", ", first.DeclaredRepositories.Order()));
		_ = sb.AppendLine("entries: " + string.Join(", ", first.LinkIndexEntries.Select(e => $"{e.Key}={e.Value.Path}").Order()));
		_ = sb.AppendLine("same references: " + (second.LinkReferences.Count == first.LinkReferences.Count));
		_ = sb.AppendLine("cache files:");
		_ = sb.AppendLine(Cache(fileSystem));
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public async Task FetchCrossLinks_CachedFileOnDisk_IsUsedInsteadOfTheReader()
	{
		var (_, fileSystem, firstFetcher) = Create(("alpha", "main", "etag-a"));
		_ = await firstFetcher.FetchCrossLinks(default);

		// a new fetcher reads the registry again but finds the links.json with the same etag in the cache
		var (reader, _, _) = Create(("alpha", "main", "etag-a"));
		var secondFetcher = new Fetcher(reader, fileSystem);
		_ = await secondFetcher.FetchCrossLinks(default);

		reader.Calls.Should().Equal("registry");
	}

	[Test]
	public async Task FetchCrossLinks_ANewEtag_FetchesAgain()
	{
		var (_, fileSystem, firstFetcher) = Create(("alpha", "main", "etag-1"));
		_ = await firstFetcher.FetchCrossLinks(default);

		var (reader, _, _) = Create(("alpha", "main", "etag-2"));
		_ = await new Fetcher(reader, fileSystem).FetchCrossLinks(default);

		reader.Calls.Should().Equal("registry", "links elastic/alpha/main/links.json");
		Cache(fileSystem)
			.Should()
			.MatchInlineSnapshot(
				"""
			links/links-elastic-alpha-main-etag-1.json
			links/links-elastic-alpha-main-etag-2.json
			"""
			);
	}

	[Test]
	public async Task FetchCrossLinks_BranchSelection_PrefersMainThenMasterThenFails()
	{
		var (_, _, mainAndMaster) = Create(("alpha", "main", "e1"), ("alpha", "master", "e2"), ("alpha", "9.0", "e3"));
		var (_, _, masterOnly) = Create(("alpha", "master", "e2"));
		var (_, _, noDefault) = Create(("alpha", "9.0", "e3"));
		var (_, _, nothing) = Create();

		var sb = new StringBuilder();
		_ = sb.AppendLine("main and master: " + (await mainAndMaster.Entry("alpha")).Path);
		_ = sb.AppendLine("master only: " + (await masterOnly.Entry("alpha")).Path);
		foreach (var (name, fetcher) in new[] { ("no default branch", noDefault), ("unknown repository", nothing) })
		{
			var error = await Assert.That(async () => await fetcher.Entry("alpha")).Throws<Exception>();
			_ = sb.AppendLine($"{name}: {error!.Message}");
		}

		sb
			.ToString()
			.Should()
			.MatchInlineSnapshot(
				"""
			main and master: elastic/alpha/main/links.json
			master only: elastic/alpha/master/links.json
			no default branch: Repository alpha found in link index, but no main or master branch found
			unknown repository: Repository alpha not found in link index

			"""
			);
	}

	[Test]
	public async Task FetchCrossLinks_ByBranchKeys_UsesTheFirstKeyThatExists()
	{
		var (reader, _, fetcher) = Create(("alpha", "9.0", "e1"), ("alpha", "8.x", "e2"));

		var links = await fetcher.Fetch("alpha", "main", "8.x", "9.0");
		var sb = new StringBuilder();
		_ = sb.AppendLine("reader calls: " + string.Join(", ", reader.Calls));
		_ = sb.AppendLine("origin: " + links.Origin.Remote);
		foreach (var (name, keys) in new (string, string[])[] { ("no key matches", ["main"]), ("no keys", []) })
		{
			var error = await Assert.That(async () => await fetcher.Fetch("alpha", keys)).Throws<Exception>();
			_ = sb.AppendLine($"{name}: {error!.Message}");
		}

		var unknown = await Assert.That(async () => await fetcher.Fetch("zeta", "main")).Throws<Exception>();
		_ = sb.AppendLine($"unknown repository: {unknown!.Message}");
		sb.ToString().Should().MatchSnapshot();
	}

	[Test]
	public void Deserialize_AndSerialize_AreTheSameAsRepositoryLinks()
	{
		var json = Links("alpha");

		LinkManifestJson.Read(LinkJson.Serialize(json)).Should().BeEquivalentTo(json);
	}
}
