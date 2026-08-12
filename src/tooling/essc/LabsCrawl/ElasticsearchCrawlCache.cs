// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Ingest.Elasticsearch.Helpers;
using Elastic.Transport;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.LabsCrawl;

public class ElasticsearchCrawlCache(
	ILogger<ElasticsearchCrawlCache> logger,
	DistributedTransport transport
)
{
	private const int BatchSize = 10000;
	private const string PitKeepAlive = "5m";

	private static readonly string[] CacheSourceIncludes =
	[
		"path", "hash", "last_updated", "http.etag", "http.last_modified"
	];

	public async Task<bool> IndexExistsAsync(string indexAlias, CancellationToken ct = default)
	{
		try
		{
			var response = await transport.HeadAsync(indexAlias, ct);
			return response.ApiCallDetails.HasSuccessfulStatusCode;
		}
		catch (Exception ex)
		{
			logger.LogDebug(ex, "Index check failed for {Alias}", indexAlias);
			return false;
		}
	}

	public async Task<Dictionary<string, CachedDocInfo>> LoadCacheAsync(
		string indexAlias,
		IProgress<(int loaded, string? currentUrl)>? progress = null,
		CancellationToken ct = default)
	{
		var cache = new Dictionary<string, CachedDocInfo>(StringComparer.OrdinalIgnoreCase);

		try
		{
			await using var search = new PointInTimeSearch<SourceDocument>(
				transport,
				new PointInTimeSearchOptions
				{
					Index = indexAlias,
					Size = BatchSize,
					KeepAlive = PitKeepAlive,
					Sort = /*lang=json,strict*/ """{"path":"asc"}""",
					SourceIncludes = CacheSourceIncludes,
					Slices = 1
				},
				CachingJsonContext.Default.Options);

			var loaded = 0;
			await foreach (var page in search.SearchPagesAsync(ct))
			{
				foreach (var src in page.Documents)
				{
					if (string.IsNullOrEmpty(src.Path))
						continue;

					cache[src.Path] = new CachedDocInfo(
						src.Path,
						src.Hash ?? string.Empty,
						src.LastUpdated ?? DateTimeOffset.MinValue,
						src.Http?.Etag,
						src.Http?.LastModified);

					loaded++;
					progress?.Report((loaded, src.Path));
				}
			}

			logger.LogInformation("Loaded {Count:N0} documents from cache", cache.Count);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to load crawl cache from {Alias}", indexAlias);
		}

		return cache;
	}
}
