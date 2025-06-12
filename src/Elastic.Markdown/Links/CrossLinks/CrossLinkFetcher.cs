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

	public required bool FromConfiguration { get; init; }

	public required FrozenDictionary<string, LinkRegistryEntry> LinkIndexEntries { get; init; }

	public static FetchedCrossLinks Empty { get; } = new()
	{
		DeclaredRepositories = [],
		LinkReferences = new Dictionary<string, RepositoryLinks>().ToFrozenDictionary(),
		FromConfiguration = false,
		LinkIndexEntries = new Dictionary<string, LinkRegistryEntry>().ToFrozenDictionary()
	};
}

public abstract class CrossLinkFetcher(ILinkIndexReader linkIndexProvider, ILoggerFactory logger) : IDisposable
{
	private readonly ILogger _logger = logger.CreateLogger(nameof(CrossLinkFetcher));
	private readonly HttpClient _client = new();
	private LinkRegistry? _linkIndex;

	public static RepositoryLinks Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.RepositoryLinks)!;

	public abstract Task<FetchedCrossLinks> Fetch(Cancel ctx);

	public async Task<LinkRegistry> FetchLinkIndex(Cancel ctx)
	{
		if (_linkIndex is not null)
		{
			_logger.LogTrace("Using cached link index");
			return _linkIndex;
		}

		_logger.LogInformation("Getting link index");
		_linkIndex = await linkIndexProvider.GetRegistry(ctx);
		return _linkIndex;
	}

	protected async Task<LinkRegistryEntry> GetLinkIndexEntry(string repository, Cancel ctx)
	{
		var linkIndex = await FetchLinkIndex(ctx);
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

	protected async Task<RepositoryLinks> Fetch(string repository, string[] keys, Cancel ctx)
	{
		var linkIndex = await FetchLinkIndex(ctx);
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
		var linkReference = await TryGetCachedLinkReference(repository, linkRegistryEntry);
		if (linkReference is not null)
			return linkReference;

		var url = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{linkRegistryEntry.Path}";
		_logger.LogInformation("Fetching links.json for '{Repository}': {Url}", repository, url);
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
			_logger.LogError(e, "Failed to write cached link reference {CachedPath}", cachedPath);
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
				_logger.LogError(e, "Failed to read cached link reference {CachedPath}", cachedPath);
				return null;
			}
		}
		return null;

	}

	public void Dispose()
	{
		_client.Dispose();
		logger.Dispose();
		GC.SuppressFinalize(this);
	}
}
