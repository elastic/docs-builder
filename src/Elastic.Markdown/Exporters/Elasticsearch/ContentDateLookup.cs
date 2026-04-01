// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

/// <summary>
/// Manages a persistent lookup index that tracks per-URL content hashes and timestamps.
/// The lookup index never rolls over, so <c>content_last_updated</c> survives document index rollovers.
/// </summary>
public class ContentDateLookup(DistributedTransport transport, ILogger logger, string buildType, string environment)
{
	private const int PageSize = 1000;
	private const string PitKeepAlive = "2m";

	private readonly string _indexName = $"docs-{buildType}-content-dates-{environment}";
	private readonly ConcurrentDictionary<string, (string Hash, DateTimeOffset Date)> _existing = [];
	private readonly ConcurrentDictionary<string, (string Hash, DateTimeOffset Date)> _changed = [];

	/// <summary>Creates the lookup index if it does not already exist.</summary>
	public async Task BootstrapAsync(Cancel ct)
	{
		var head = await transport.HeadAsync(_indexName, ct);
		if (head.ApiCallDetails.HttpStatusCode == 200)
		{
			logger.LogInformation("Content date lookup index {Index} already exists", _indexName);
			return;
		}

		var mapping = new JsonObject
		{
			["settings"] = new JsonObject { ["number_of_shards"] = 1, ["number_of_replicas"] = 0 },
			["mappings"] = new JsonObject
			{
				["properties"] = new JsonObject
				{
					["url"] = new JsonObject { ["type"] = "keyword" },
					["content_hash"] = new JsonObject { ["type"] = "keyword" },
					["content_last_updated"] = new JsonObject { ["type"] = "date" }
				}
			}
		};

		var response = await transport.PutAsync<StringResponse>(_indexName, PostData.String(mapping.ToJsonString()), ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogWarning("Failed to create content date lookup index {Index}: {Info}", _indexName, response.ApiCallDetails.DebugInformation);
		else
			logger.LogInformation("Created content date lookup index {Index}", _indexName);
	}

	/// <summary>Loads all existing entries from the lookup index into memory.</summary>
	public async Task LoadAsync(Cancel ct)
	{
		_existing.Clear();

		var head = await transport.HeadAsync(_indexName, ct);
		if (head.ApiCallDetails.HttpStatusCode != 200)
		{
			logger.LogInformation("Content date lookup index {Index} does not exist, starting fresh", _indexName);
			return;
		}

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
				{
					logger.LogWarning("Content date lookup search failed (page {Page}): {Info}", page, response.ApiCallDetails.DebugInformation);
					break;
				}

				var root = response.Body;
				pitId = root?["pit_id"]?.GetValue<string>() ?? pitId;

				hitCount = 0;
				if (root?["hits"]?["hits"] is JsonArray hitsArray)
				{
					foreach (var hit in hitsArray)
					{
						if (hit is null)
							continue;

						hitCount++;

						if (hit["sort"] is JsonArray sortArray)
							lastSortValues = sortArray.Select(e => e?.ToString() ?? "").ToArray();

						var source = hit["_source"];
						if (source is null)
							continue;

						var url = source["url"]?.GetValue<string>();
						var contentHash = source["content_hash"]?.GetValue<string>();
						var dateStr = source["content_last_updated"]?.GetValue<string>();

						if (url is null || contentHash is null || dateStr is null)
							continue;

						if (!DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
							continue;

						_existing[url] = (contentHash, date);
					}
				}

				page++;

			} while (hitCount == PageSize);
		}
		finally
		{
			await ClosePitAsync(pitId, CancellationToken.None);
		}

		logger.LogInformation("Loaded {Count} entries from content date lookup index {Index}", _existing.Count, _indexName);
	}

	/// <summary>
	/// Returns the correct <c>content_last_updated</c> for a document.
	/// If the content hash matches the stored value, the old timestamp is preserved.
	/// Otherwise, returns <paramref name="batchTimestamp"/> and tracks the entry for saving.
	/// </summary>
	public DateTimeOffset Resolve(string url, string newContentHash, DateTimeOffset batchTimestamp)
	{
		if (_existing.TryGetValue(url, out var entry) && entry.Hash == newContentHash)
			return entry.Date;

		_changed[url] = (newContentHash, batchTimestamp);
		return batchTimestamp;
	}

	/// <summary>Bulk upserts changed/new entries back to the lookup index.</summary>
	public async Task SaveAsync(Cancel ct)
	{
		if (_changed.IsEmpty)
		{
			logger.LogInformation("No content date changes to save");
			return;
		}

		logger.LogInformation("Saving {Count} content date entries to {Index}", _changed.Count, _indexName);

		var sb = new StringBuilder();
		foreach (var (url, (hash, date)) in _changed)
		{
			var id = ContentDateId(url);
			var dateStr = date.ToString("o");
			_ = sb.AppendLine($$$"""{"index": {"_index": "{{{_indexName}}}", "_id": "{{{id}}}"}}""");
			_ = sb.AppendLine($$$"""{"url": "{{{EscapeJson(url)}}}", "content_hash": "{{{EscapeJson(hash)}}}", "content_last_updated": "{{{dateStr}}}"}""");
		}

		var response = await transport.PostAsync<StringResponse>("/_bulk", PostData.String(sb.ToString()), ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogWarning("Failed to save content date entries: {Info}", response.ApiCallDetails.DebugInformation);
		else
			logger.LogInformation("Saved {Count} content date entries", _changed.Count);
	}

	private static string ContentDateId(string url) =>
		Documentation.Search.ContentHash.Create(url);

	private static string EscapeJson(string value) =>
		JsonEncodedText.Encode(value).ToString();

	private async Task<string> OpenPitAsync(Cancel ct)
	{
		var response = await transport.PostAsync<DynamicResponse>(
			$"/{_indexName}/_pit?keep_alive={PitKeepAlive}", PostData.Empty, ct);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to open PIT on {_indexName}: {response.ApiCallDetails.HttpStatusCode} {response.ApiCallDetails.DebugInformation}");

		var pitId = response.Body.Get<string>("id");
		if (string.IsNullOrEmpty(pitId))
			throw new InvalidOperationException("PIT response did not contain an id");

		return pitId;
	}

	private async Task ClosePitAsync(string pitId, Cancel ct)
	{
		try
		{
			var body = new JsonObject { ["id"] = pitId }.ToJsonString();
			_ = await transport.DeleteAsync<VoidResponse>("/_pit", new DefaultRequestParameters(), PostData.String(body), ct);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to close PIT on content date lookup (non-fatal)");
		}
	}

	private static string BuildSearchBody(string pitId, string[]? searchAfter)
	{
		var body = new JsonObject
		{
			["size"] = PageSize,
			["_source"] = new JsonArray("url", "content_hash", "content_last_updated"),
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
