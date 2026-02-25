// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Links.CrossLinks;

public record FetchedCrossLinks
{
	public required FrozenDictionary<string, RepositoryLinks> LinkReferences { get; init; }

	public required HashSet<string> DeclaredRepositories { get; init; }

	public required FrozenDictionary<string, LinkRegistryEntry> LinkIndexEntries { get; init; }

	/// <summary>
	/// Optional map of repository name to link index registry URL for error messages.
	/// When null or missing, falls back to the public S3 URL.
	/// </summary>
	public FrozenDictionary<string, string>? RegistryUrlsByRepository { get; init; }

	/// <summary>
	/// Set of repository names that belong to a codex (non-public) registry.
	/// Used by the URI resolver to generate codex URLs instead of public preview URLs.
	/// </summary>
	public FrozenSet<string>? CodexRepositories { get; init; }

	public static FetchedCrossLinks Empty { get; } = new()
	{
		DeclaredRepositories = [],
		LinkReferences = new Dictionary<string, RepositoryLinks>().ToFrozenDictionary(),
		LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary(),
		RegistryUrlsByRepository = null,
		CodexRepositories = null
	};
}

public abstract class CrossLinkFetcher(ILoggerFactory logFactory, ILinkIndexReader linkIndexProvider) : IDisposable
{
	protected ILogger Logger { get; } = logFactory.CreateLogger(nameof(CrossLinkFetcher));
	private LinkRegistry? _linkIndex;

	public static RepositoryLinks Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.RepositoryLinks)!;

	public abstract Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx);

	private int _logOnce;
	public async Task<LinkRegistry> FetchLinkRegistry(Cancel ctx)
	{
		var result = Interlocked.Increment(ref _logOnce);
		if (_linkIndex is not null)
		{
			if (result == 1)
				Logger.LogTrace("Using cached link index registry (link-index.json)");
			return _linkIndex;
		}

		Logger.LogInformation("Fetching link index registry (link-index.json)");
		_linkIndex = await linkIndexProvider.GetRegistry(ctx);
		return _linkIndex;
	}

	protected async Task<LinkRegistryEntry> GetLinkIndexEntry(string repository, Cancel ctx)
	{
		var linkIndex = await FetchLinkRegistry(ctx);
		if (!linkIndex.Repositories.TryGetValue(repository, out var repositoryLinks))
			throw new Exception($"Repository {repository} not found in link index");
		return GetNextContentSourceLinkIndexEntry(repositoryLinks, repository);
	}

	protected static LinkRegistryEntry GetNextContentSourceLinkIndexEntry(IDictionary<string, LinkRegistryEntry> repositoryLinks, string repository)
	{
		var linkIndexEntry =
			(repositoryLinks.TryGetValue("main", out var link)
				? link
				: repositoryLinks.TryGetValue("master", out link) ? link : null)
				?? throw new Exception($"Repository {repository} found in link index, but no main or master branch found");
		return linkIndexEntry;
	}

	protected async Task<RepositoryLinks> FetchCrossLinks(string repository, string[] keys, Cancel ctx)
	{
		var linkIndex = await FetchLinkRegistry(ctx);
		if (!linkIndex.Repositories.TryGetValue(repository, out var repositoryLinks))
			throw new Exception($"Repository {repository} not found in link index");

		foreach (var key in keys)
		{
			if (repositoryLinks.TryGetValue(key, out var linkIndexEntry))
				return await FetchLinkIndexEntry(repository, linkIndexEntry, ctx);
		}

		throw new Exception($"Repository found in link index however none of: '{string.Join(", ", keys)}' branches found");
	}

	protected Task<RepositoryLinks> FetchLinkIndexEntry(string repository, LinkRegistryEntry linkRegistryEntry, Cancel ctx) =>
		FetchLinkIndexEntryFromReader(linkIndexProvider, repository, linkRegistryEntry, ctx);

	/// <summary>
	/// Fetches repository links from a specific reader. Used for dual-registry (public + codex) fetching.
	/// </summary>
	protected async Task<RepositoryLinks> FetchLinkIndexEntryFromReader(
		ILinkIndexReader reader,
		string repository,
		LinkRegistryEntry linkRegistryEntry,
		Cancel ctx)
	{
		var linkReference = await TryGetCachedLinkReference(repository, linkRegistryEntry);
		if (linkReference is not null)
		{
			Logger.LogInformation("Using locally cached links.json for '{Repository}' from {RegistryUrl}", repository, reader.RegistryUrl);
			return linkReference;
		}

		Logger.LogInformation("Fetching links.json for '{Repository}' from {RegistryUrl}", repository, reader.RegistryUrl);
		linkReference = await reader.GetRepositoryLinks(linkRegistryEntry.Path, ctx);
		WriteLinksJsonCachedFile(repository, linkRegistryEntry, linkReference);
		return linkReference;
	}

	/// <summary>
	/// Fetches cross-links for a repository from a specific reader. Used for dual-registry fetching.
	/// </summary>
	protected static async Task<RepositoryLinks> FetchCrossLinksFromReader(
		ILinkIndexReader reader,
		string repository,
		CrossLinkFetcher fetcher,
		Cancel ctx)
	{
		var linkIndex = await reader.GetRegistry(ctx);
		if (!linkIndex.Repositories.TryGetValue(repository, out var repositoryLinks))
			throw new Exception($"Repository {repository} not found in link index");
		var entry = GetNextContentSourceLinkIndexEntry(repositoryLinks, repository);
		return await fetcher.FetchLinkIndexEntryFromReader(reader, repository, entry, ctx);
	}

	private void WriteLinksJsonCachedFile(string repository, LinkRegistryEntry linkRegistryEntry, RepositoryLinks linkReference)
	{
		var cachedFileName = $"links-elastic-{repository}-{linkRegistryEntry.Branch}-{linkRegistryEntry.ETag}.json";
		var cachedPath = Path.Combine(Paths.ApplicationData.FullName, "links", cachedFileName);
		if (File.Exists(cachedPath))
			return;
		try
		{
			_ = Directory.CreateDirectory(Path.GetDirectoryName(cachedPath)!);
			File.WriteAllText(cachedPath, RepositoryLinks.Serialize(linkReference));
		}
		catch (Exception e)
		{
			Logger.LogError(e, "Failed to write cached link reference {CachedPath}", cachedPath);
		}
	}

	private readonly ConcurrentDictionary<string, RepositoryLinks> _cachedLinkReferences = new();

	private async Task<RepositoryLinks?> TryGetCachedLinkReference(string repository, LinkRegistryEntry linkRegistryEntry)
	{
		var cachedFileName = $"links-elastic-{repository}-{linkRegistryEntry.Branch}-{linkRegistryEntry.ETag}.json";
		var cachedPath = Path.Combine(Paths.ApplicationData.FullName, "links", cachedFileName);
		if (_cachedLinkReferences.TryGetValue(cachedFileName, out var cachedLinkReference))
			return cachedLinkReference;

		if (File.Exists(cachedPath))
		{
			try
			{
				var json = await File.ReadAllTextAsync(cachedPath);
				var linkReference = Deserialize(json);
				_ = _cachedLinkReferences.TryAdd(cachedFileName, linkReference);
				return linkReference;
			}
			catch (Exception e)
			{
				Logger.LogError(e, "Failed to read cached link reference {CachedPath}", cachedPath);
				return null;
			}
		}
		return null;

	}

	public void Dispose()
	{
		logFactory.Dispose();
		GC.SuppressFinalize(this);
	}
}
