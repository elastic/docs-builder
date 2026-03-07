// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Indexing;

/// <summary>Information about a discovered Elasticsearch index.</summary>
public sealed record IndexInfo(string Name, string Status, long DocsCount, string StoreSize, string CreationDateString)
{
	public DateTimeOffset CreationDate =>
		long.TryParse(CreationDateString, out var millis)
			? DateTimeOffset.FromUnixTimeMilliseconds(millis)
			: DateTimeOffset.MinValue;
}

/// <summary>Result of a cleanup operation.</summary>
public sealed record CleanupResult(
	IReadOnlyList<IndexInfo> Kept,
	IReadOnlyList<IndexInfo> Deleted,
	IReadOnlyList<string> DeletedAliases
);

/// <summary>
/// Discovers and cleans up old timestamped Elasticsearch indices for a given prefix.
/// </summary>
public class IndexCleanupService(DistributedTransport transport, ILogger logger)
{
	/// <summary>Lists all indices matching a pattern, sorted by creation date descending.</summary>
	public async Task<IReadOnlyList<IndexInfo>> ListIndicesAsync(string pattern, CancellationToken ct = default)
	{
		var response = await transport.GetAsync<StringResponse>(
			$"/_cat/indices/{pattern}?format=json&h=index,status,docs.count,store.size,creation.date",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			if (response.ApiCallDetails.HttpStatusCode == 404)
				return [];

			logger.LogWarning("Failed to list indices for {Pattern}: {Status}",
				pattern, response.ApiCallDetails.HttpStatusCode);
			return [];
		}

		var indices = JsonSerializer.Deserialize<List<CatIndexEntry>>(response.Body, CatJsonContext.Default.ListCatIndexEntry);
		if (indices is null)
			return [];

		return indices
			.Select(i => new IndexInfo(
				i.Index ?? "",
				i.Status ?? "unknown",
				long.TryParse(i.DocsCount, out var docs) ? docs : 0,
				i.StoreSize ?? "0b",
				i.CreationDate ?? "0"))
			.OrderByDescending(i => i.CreationDate)
			.ToList();
	}

	/// <summary>Deletes indices and their aliases, keeping the most recent <paramref name="keepLast"/>.</summary>
	public async Task<CleanupResult> CleanupAsync(
		string indexPrefix,
		int keepLast,
		IProgress<(string phase, int current, int total)>? progress = null,
		CancellationToken ct = default
	)
	{
		var allIndices = await ListIndicesAsync($"{indexPrefix}*", ct);
		if (allIndices.Count == 0)
			return new CleanupResult([], [], []);

		var kept = allIndices.Take(keepLast).ToList();
		var toDelete = allIndices.Skip(keepLast).ToList();
		var deletedAliases = new List<string>();

		if (toDelete.Count == 0)
			return new CleanupResult(kept, [], []);

		for (var i = 0; i < toDelete.Count; i++)
		{
			var index = toDelete[i];
			progress?.Report(("Deleting indices", i + 1, toDelete.Count));

			var aliases = await GetAliasesForIndexAsync(index.Name, ct);
			foreach (var alias in aliases)
			{
				await DeleteAliasAsync(index.Name, alias, ct);
				deletedAliases.Add(alias);
			}

			await DeleteIndexAsync(index.Name, ct);
			logger.LogInformation("Deleted index {Index} ({Docs} docs, {Size})",
				index.Name, index.DocsCount, index.StoreSize);
		}

		return new CleanupResult(kept, toDelete, deletedAliases);
	}

	/// <summary>Deletes all indices and aliases matching a prefix (no keep-last).</summary>
	public async Task<CleanupResult> DeleteAllAsync(
		string indexPrefix,
		IProgress<(string phase, int current, int total)>? progress = null,
		CancellationToken ct = default
	) => await CleanupAsync(indexPrefix, keepLast: 0, progress, ct);

	private async Task<List<string>> GetAliasesForIndexAsync(string indexName, CancellationToken ct)
	{
		var response = await transport.GetAsync<StringResponse>($"/{indexName}/_alias", ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			return [];

		try
		{
			using var doc = JsonDocument.Parse(response.Body);
			if (!doc.RootElement.TryGetProperty(indexName, out var indexElement))
				return [];
			if (!indexElement.TryGetProperty("aliases", out var aliasesElement))
				return [];

			return aliasesElement.EnumerateObject().Select(p => p.Name).ToList();
		}
		catch
		{
			return [];
		}
	}

	private async Task DeleteAliasAsync(string indexName, string alias, CancellationToken ct)
	{
		var response = await transport.DeleteAsync<StringResponse>($"/{indexName}/_alias/{alias}", PostData.Empty, default, ct);
		if (response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogDebug("Deleted alias {Alias} from {Index}", alias, indexName);
	}

	private async Task DeleteIndexAsync(string indexName, CancellationToken ct)
	{
		var response = await transport.DeleteAsync<StringResponse>($"/{indexName}", PostData.Empty, default, ct);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogWarning("Failed to delete index {Index}: {Status}",
				indexName, response.ApiCallDetails.HttpStatusCode);
	}
}

[JsonSerializable(typeof(List<CatIndexEntry>))]
internal sealed partial class CatJsonContext : JsonSerializerContext;

internal sealed class CatIndexEntry
{
	[JsonPropertyName("index")]
	public string? Index { get; set; }

	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("docs.count")]
	public string? DocsCount { get; set; }

	[JsonPropertyName("store.size")]
	public string? StoreSize { get; set; }

	[JsonPropertyName("creation.date")]
	public string? CreationDate { get; set; }
}
