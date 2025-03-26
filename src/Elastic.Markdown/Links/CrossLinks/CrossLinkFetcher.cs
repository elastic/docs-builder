// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Text.Json;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.CrossLinks;

public record FetchedCrossLinks
{
	public required FrozenDictionary<string, LinkReference> LinkReferences { get; init; }

	public required HashSet<string> DeclaredRepositories { get; init; }

	public required bool FromConfiguration { get; init; }

	public required FrozenDictionary<string, LinkIndexEntry> LinkIndexEntries { get; init; }

	public static FetchedCrossLinks Empty { get; } = new()
	{
		DeclaredRepositories = [],
		LinkReferences = new Dictionary<string, LinkReference>().ToFrozenDictionary(),
		FromConfiguration = false,
		LinkIndexEntries = new Dictionary<string, LinkIndexEntry>().ToFrozenDictionary()
	};
}

public abstract class CrossLinkFetcher(ILoggerFactory logger) : IDisposable
{
	private readonly ILogger _logger = logger.CreateLogger(nameof(CrossLinkFetcher));
	private readonly HttpClient _client = new();
	private LinkIndex? _linkIndex;

	public static LinkReference Deserialize(string json) =>
		JsonSerializer.Deserialize(json, SourceGenerationContext.Default.LinkReference)!;

	public abstract Task<FetchedCrossLinks> Fetch();

	protected async Task<LinkIndex> FetchLinkIndex()
	{
		if (_linkIndex is not null)
		{
			_logger.LogTrace("Using cached link index");
			return _linkIndex;
		}
		var url = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/link-index.json";
		_logger.LogInformation("Fetching {Url}", url);
		var json = await _client.GetStringAsync(url);
		_linkIndex = LinkIndex.Deserialize(json);
		return _linkIndex;
	}

	protected async Task<LinkIndexEntry> GetLinkIndexEntry(string repository)
	{
		var linkIndex = await FetchLinkIndex();
		if (linkIndex.Repositories.TryGetValue(repository, out var repositoryLinks))
			return repositoryLinks.First().Value;
		throw new Exception($"Repository {repository} not found in link index");
	}

	protected async Task<LinkReference> Fetch(string repository)
	{
		var linkIndex = await FetchLinkIndex();
		if (!linkIndex.Repositories.TryGetValue(repository, out var repositoryLinks))
			throw new Exception($"Repository {repository} not found in link index");

		if (repositoryLinks.TryGetValue("main", out var linkIndexEntry))
			return await FetchLinkIndexEntry(repository, linkIndexEntry);
		if (repositoryLinks.TryGetValue("master", out linkIndexEntry))
			return await FetchLinkIndexEntry(repository, linkIndexEntry);
		throw new Exception($"Repository {repository} not found in link index");

	}

	protected async Task<LinkReference> FetchLinkIndexEntry(string repository, LinkIndexEntry linkIndexEntry)
	{
		var linkReference = await TryGetCachedLinkReference(repository, linkIndexEntry);
		if (linkReference is not null)
			return linkReference;

		var url = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{linkIndexEntry.Path}";
		_logger.LogInformation("Fetching links.json for '{Repository}': {Url}", repository, url);
		var json = await _client.GetStringAsync(url);
		linkReference = Deserialize(json);
		WriteLinksJsonCachedFile(repository, linkIndexEntry, json);
		return linkReference;
	}

	private void WriteLinksJsonCachedFile(string repository, LinkIndexEntry linkIndexEntry, string json)
	{
		var cachedFileName = $"links-elastic-{repository}-{linkIndexEntry.Branch}-{linkIndexEntry.ETag}.json";
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

	private async Task<LinkReference?> TryGetCachedLinkReference(string repository, LinkIndexEntry linkIndexEntry)
	{
		var cachedFileName = $"links-elastic-{repository}-main-{linkIndexEntry.ETag}.json";
		var cachedPath = Path.Combine(Paths.ApplicationData.FullName, "links", cachedFileName);
		if (File.Exists(cachedPath))
		{
			try
			{
				var json = await File.ReadAllTextAsync(cachedPath);
				var linkReference = Deserialize(json);
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
