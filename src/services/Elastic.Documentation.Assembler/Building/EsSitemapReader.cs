// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Building;

public record SitemapEntry(string Url, DateTimeOffset LastUpdated);

/// <summary>Reads all url + content_last_updated pairs from the ES lexical index using search_after with PIT.</summary>
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
				var response = await transport.PostAsync<JsonResponse>("/_search", PostData.String(body), ct);

				if (!response.ApiCallDetails.HasSuccessfulStatusCode)
					throw new InvalidOperationException(
						$"ES search failed (page {page}): {response.ApiCallDetails.HttpStatusCode} {response.ApiCallDetails.DebugInformation}");

				var root = response.Body;

				// Update PIT id — ES may return a new one on each response
				pitId = root?["pit_id"]?.GetValue<string>() ?? pitId;

				hitCount = 0;
				if (root?["hits"]?["hits"] is JsonArray hitsArray)
				{
					foreach (var hit in hitsArray)
					{
						if (hit is null)
							continue;

						hitCount++;

						// Always advance the search_after cursor, even for skipped hits
						if (hit["sort"] is JsonArray sortArray)
							lastSortValues = sortArray.Select(e => e?.ToString() ?? "").ToArray();

						var source = hit["_source"];
						if (source is null)
							continue;

						var url = source["url"]?.GetValue<string>();
						var lastUpdatedStr = source["content_last_updated"]?.GetValue<string>();

						if (url is null || lastUpdatedStr is null)
							continue;

						if (!DateTimeOffset.TryParse(lastUpdatedStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastUpdated))
						{
							logger.LogWarning("Sitemap: skipping {Url}, unparseable content_last_updated: {Value}", url, lastUpdatedStr);
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
			// Use a non-cancelable token so PIT cleanup always runs to completion
			await ClosePitAsync(pitId, CancellationToken.None);
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
			var body = new JsonObject { ["id"] = pitId }.ToJsonString();
			_ = await transport.DeleteAsync<VoidResponse>("/_pit", new DefaultRequestParameters(), PostData.String(body), ct);
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

	// Returns pre-built JSON string because this assembly is published with AOT trimming.
	// System.Text.Json cannot serialize anonymous types or unregistered objects via reflection in AOT mode,
	// so we build the JSON with JsonObject/JsonArray and send it via PostData.String() instead of PostData.Serializable().
	internal static string BuildSearchBody(string pitId, string[]? searchAfter)
	{
		var body = new JsonObject
		{
			["size"] = PageSize,
			["_source"] = new JsonArray("url", "content_last_updated"),
			["query"] = new JsonObject
			{
				["bool"] = new JsonObject
				{
					["must_not"] = new JsonArray(new JsonObject
					{
						["term"] = new JsonObject { ["hidden"] = true }
					})
				}
			},
			["pit"] = new JsonObject
			{
				["id"] = pitId,
				["keep_alive"] = PitKeepAlive
			},
			["sort"] = new JsonArray(new JsonObject { ["url"] = "asc" })
		};

		if (searchAfter is { Length: > 0 })
		{
			var sortArray = new JsonArray();
			foreach (var value in searchAfter)
				sortArray.Add((JsonNode)value);
			body["search_after"] = sortArray;
		}

		return body.ToJsonString();
	}
}
