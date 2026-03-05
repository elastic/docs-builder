// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Caching;

/// <summary>
/// Loads cached document metadata from Elasticsearch using PIT API for large datasets.
/// </summary>
public class ElasticsearchCrawlCache(
	ILogger<ElasticsearchCrawlCache> logger,
	DistributedTransport transport
) : ICrawlCache
{
	private const int BatchSize = 10000;
	private const string PitKeepAlive = "5m";

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
		CancellationToken ct = default
	)
	{
		var cache = new Dictionary<string, CachedDocInfo>(StringComparer.OrdinalIgnoreCase);

		// Open Point-in-Time for consistent reads
		var pitId = await OpenPitAsync(indexAlias, ct);
		if (pitId is null)
		{
			logger.LogWarning("Failed to open PIT - index may not exist or be empty");
			return cache;
		}

		try
		{
			object[]? searchAfter = null;
			var loaded = 0;

			while (!ct.IsCancellationRequested)
			{
				var (docs, nextSearchAfter) = await SearchAfterAsync(pitId, searchAfter, ct);

				if (docs.Count == 0)
					break;

				foreach (var doc in docs)
				{
					cache[doc.Url] = doc;
					loaded++;
				}

				progress?.Report((loaded, docs[^1].Url));
				searchAfter = nextSearchAfter;

				if (docs.Count < BatchSize)
					break;
			}

			logger.LogInformation("Loaded {Count:N0} documents from cache", cache.Count);
		}
		finally
		{
			await ClosePitAsync(pitId, ct);
		}

		return cache;
	}

	private async Task<string?> OpenPitAsync(string indexAlias, CancellationToken ct)
	{
		try
		{
			var response = await transport.PostAsync<StringResponse>(
				$"{indexAlias}/_pit?keep_alive={PitKeepAlive}",
				PostData.Empty,
				ct
			);

			if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			{
				logger.LogWarning("Failed to open PIT: {Status}", response.ApiCallDetails.HttpStatusCode);
				return null;
			}

			var result = JsonSerializer.Deserialize(response.Body, CachingJsonContext.Default.OpenPitResponse);
			return result?.Id;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Error opening PIT for {Alias}", indexAlias);
			return null;
		}
	}

	private async Task ClosePitAsync(string pitId, CancellationToken ct)
	{
		try
		{
			_ = await transport.DeleteAsync<StringResponse>(
				"_pit",
				PostData.String($$"""{"id":"{{pitId}}"}"""),
				default,
				ct
			);
		}
		catch (Exception ex)
		{
			logger.LogDebug(ex, "Error closing PIT");
		}
	}

	private async Task<(List<CachedDocInfo> docs, object[]? searchAfter)> SearchAfterAsync(
		string pitId,
		object[]? searchAfter,
		CancellationToken ct
	)
	{
		// Build search_after JSON if present
		var searchAfterJson = searchAfter is not null
			? $$"""
			  ,"search_after": [{{string.Join(",", searchAfter.Select(FormatSortValue))}}]
			  """
			: "";

		// Build request body as raw JSON
		var requestBody = $$"""
			{
				"size": {{BatchSize}},
				"pit": { "id": "{{pitId}}", "keep_alive": "{{PitKeepAlive}}" },
				"sort": [{ "url": "asc" }],
				"_source": ["url", "hash", "last_updated", "http_etag", "http_last_modified", "enrichment_key", "enrichment_prompt_hash"]
				{{searchAfterJson}}
			}
			""";

		var response = await transport.PostAsync<StringResponse>(
			"_search",
			PostData.String(requestBody),
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			return ([], null);

		var result = JsonSerializer.Deserialize(response.Body, CachingJsonContext.Default.SearchResponse);

		if (result?.Hits?.Hits is null)
			return ([], null);

		var docs = new List<CachedDocInfo>(result.Hits.Hits.Count);
		object[]? lastSort = null;

		foreach (var hit in result.Hits.Hits)
		{
			if (hit.Source is null)
				continue;

			var doc = new CachedDocInfo(
				hit.Source.Url ?? string.Empty,
				hit.Source.Hash ?? string.Empty,
				hit.Source.LastUpdated ?? DateTimeOffset.MinValue,
				hit.Source.HttpEtag,
				hit.Source.HttpLastModified,
				hit.Source.EnrichmentKey,
				hit.Source.EnrichmentPromptHash
			);

			docs.Add(doc);
			lastSort = hit.Sort;
		}

		return (docs, lastSort);
	}

	private static string FormatSortValue(object value) =>
		value switch
		{
			string s => $"\"{s}\"",
			_ => value.ToString() ?? "null"
		};
}
