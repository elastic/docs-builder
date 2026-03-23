// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

public record SitemapEntry(string Url, DateTimeOffset LastUpdated);

/// <summary>Reads all url + last_updated pairs from the ES lexical index using search_after with PIT.</summary>
public class EsSitemapReader(DistributedTransport transport, ILogger logger, string indexName)
{
	private const int PageSize = 1000;
	private const string PitKeepAlive = "2m";

	public async IAsyncEnumerable<SitemapEntry> ReadAllAsync([EnumeratorCancellation] Cancel ct = default)
	{
		var pitId = await OpenPitAsync(ct);
		try
		{
			string[]? lastSortValues = null;
			var page = 0;
			int hitCount;

			do
			{
				var body = BuildSearchBody(pitId, lastSortValues);
				var response = await transport.PostAsync<StringResponse>("/_search", PostData.String(body), ct);

				if (!response.ApiCallDetails.HasSuccessfulStatusCode)
					throw new InvalidOperationException(
						$"ES search failed (page {page}): {response.ApiCallDetails.HttpStatusCode} {response.ApiCallDetails.DebugInformation}");

				using var doc = JsonDocument.Parse(response.Body);
				var root = doc.RootElement;

				// Update PIT id — ES may return a new one on each response
				if (root.TryGetProperty("pit_id", out var pitIdProp))
					pitId = pitIdProp.GetString() ?? pitId;

				hitCount = 0;
				if (root.TryGetProperty("hits", out var hitsObj) && hitsObj.TryGetProperty("hits", out var hitsArray))
				{
					foreach (var hit in hitsArray.EnumerateArray())
					{
						hitCount++;

						// Always advance the search_after cursor, even for skipped hits
						if (hit.TryGetProperty("sort", out var sortProp))
							lastSortValues = sortProp.EnumerateArray().Select(e => e.ToString()).ToArray();

						if (!hit.TryGetProperty("_source", out var source))
							continue;

						var url = source.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
						var lastUpdatedStr = source.TryGetProperty("last_updated", out var luProp) ? luProp.GetString() : null;

						if (url is null || lastUpdatedStr is null)
							continue;

						if (!DateTimeOffset.TryParse(lastUpdatedStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastUpdated))
						{
							logger.LogWarning("Sitemap: skipping {Url}, unparseable last_updated: {Value}", url, lastUpdatedStr);
							continue;
						}

						yield return new SitemapEntry(url, lastUpdated);
					}
				}

				page++;
				logger.LogInformation("Sitemap: fetched page {Page} ({Hits} hits)", page, hitCount);

			} while (hitCount == PageSize);
		}
		finally
		{
			await ClosePitAsync(pitId, ct);
		}
	}

	private async Task<string> OpenPitAsync(Cancel ct)
	{
		var response = await transport.PostAsync<DynamicResponse>(
			$"/{indexName}/_pit?keep_alive={PitKeepAlive}", PostData.Empty, ct);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to open PIT on {indexName}: {response.ApiCallDetails.HttpStatusCode} {response.ApiCallDetails.DebugInformation}");

		var pitId = response.Body.Get<string>("id");
		if (string.IsNullOrEmpty(pitId))
			throw new InvalidOperationException("PIT response did not contain an id");

		logger.LogInformation("Opened PIT on {Index}: {PitId}", indexName, pitId[..Math.Min(20, pitId.Length)] + "...");
		return pitId;
	}

	private async Task ClosePitAsync(string pitId, Cancel ct)
	{
		try
		{
			var body = $$"""{"id":"{{pitId}}"}""";
			_ = await transport.DeleteAsync<DynamicResponse>("/_pit", default!, PostData.String(body), ct);
			logger.LogInformation("Closed PIT");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to close PIT (non-fatal)");
		}
	}

	internal static string BuildSearchBody(string pitId, string[]? searchAfter)
	{
		var searchAfterClause = "";
		if (searchAfter is { Length: > 0 })
		{
			var values = string.Join(",", searchAfter.Select(v => $"\"{EscapeJson(v)}\""));
			searchAfterClause = $",\"search_after\":[{values}]";
		}

		return $$"""
			{
				"size": {{PageSize}},
				"_source": ["url", "last_updated"],
				"query": { "bool": { "must_not": [{ "term": { "hidden": true } }] } },
				"pit": { "id": "{{EscapeJson(pitId)}}", "keep_alive": "{{PitKeepAlive}}" },
				"sort": [{ "url": "asc" }]{{searchAfterClause}}
			}
			""";
	}

	private static string EscapeJson(string value) =>
		value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
