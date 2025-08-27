// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.Json;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Serialization;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.CrossLinks;

public record FetchedCrossLinks
{
	public required FrozenDictionary<string, RepositoryLinks> LinkReferences { get; init; }

	public required HashSet<string> DeclaredRepositories { get; init; }

	public required FrozenDictionary<string, LinkRegistryEntry> LinkIndexEntries { get; init; }

	public static FetchedCrossLinks Empty { get; } = new()
	{
		DeclaredRepositories = [],
		LinkReferences = new Dictionary<string, RepositoryLinks>().ToFrozenDictionary(),
		LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary()
	};
}

public abstract class CrossLinkFetcher(ILoggerFactory logFactory, ILinkIndexReader linkIndexProvider) : IDisposable
{
	protected ILogger Logger { get; } = logFactory.CreateLogger(nameof(CrossLinkFetcher));
	private readonly HttpClient _client = new();
	private LinkRegistry? _linkIndex;

	public static RepositoryLinks Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.RepositoryLinks)!;

	public abstract Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx);

	public async Task<LinkRegistry> FetchLinkRegistry(Cancel ctx)
	{
		if (_linkIndex is not null)
		{
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

	protected async Task<RepositoryLinks> FetchLinkIndexEntry(string repository, LinkRegistryEntry linkRegistryEntry, Cancel ctx)
	{
		var url = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{linkRegistryEntry.Path}";
		var linkReference = await TryGetCachedLinkReference(repository, linkRegistryEntry);
		if (linkReference is not null)
		{
			Logger.LogInformation("Using locally cached links.json for '{Repository}': {Url}", repository, url);
			return linkReference;
		}

		Logger.LogInformation("Fetching links.json for '{Repository}': {Url}", repository, url);
		var json = await _client.GetStringAsync(url, ctx);
		linkReference = Deserialize(json);
		WriteLinksJsonCachedFile(repository, linkRegistryEntry, json);
		return linkReference;
	}

	private void WriteLinksJsonCachedFile(string repository, LinkRegistryEntry linkRegistryEntry, string json)
	{
		var cachedFileName = $"links-elastic-{repository}-{linkRegistryEntry.Branch}-{linkRegistryEntry.ETag}.json";
		var cachedPath = Path.Combine(Paths.ApplicationData.FullName, "links", cachedFileName);
		if (File.Exists(cachedPath))
			return;
		try
		{
			_ = Directory.CreateDirectory(Path.GetDirectoryName(cachedPath)!);
			File.WriteAllText(cachedPath, json);
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
		_client.Dispose();
		logFactory.Dispose();
		GC.SuppressFinalize(this);
	}
}
