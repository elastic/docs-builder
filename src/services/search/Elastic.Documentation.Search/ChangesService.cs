// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Microsoft.Extensions.Logging;
using EsSearchResponse = Elastic.Clients.Elasticsearch.SearchResponse<Elastic.Documentation.Search.DocumentationDocument>;

namespace Elastic.Documentation.Search;

/// <summary>
/// Elasticsearch service for the documentation changes feed.
/// Queries content_last_updated > since with search_after cursor pagination.
/// Uses a shared Point In Time (PIT) for consistent pagination across requests.
/// </summary>
public partial class ChangesService(
	ElasticsearchClientAccessor clientAccessor,
	SharedPointInTimeManager pitManager,
	ILogger<ChangesService> logger
) : IChangesService
{
	public async Task<ChangesResponse> GetChangesAsync(ChangesRequest request, Cancel ctx = default)
	{
		var cursor = DecodeCursor(request.Cursor);
		var pageSize = Math.Clamp(request.PageSize, 1, ChangesDefaults.MaxPageSize);

		var internalRequest = new ChangesInternalRequest
		{
			Since = request.Since,
			PageSize = pageSize,
			Cursor = cursor
		};

		var result = await GetChangesInternalAsync(internalRequest, ctx);

		var nextCursor = result.NextCursor is not null
			? EncodeCursor(result.NextCursor)
			: null;

		var hasMore = nextCursor is not null;

		LogChanges(logger, request.Since, result.Pages.Count, hasMore);

		return new ChangesResponse
		{
			Pages = result.Pages,
			HasMore = hasMore,
			NextCursor = nextCursor
		};
	}

	private async Task<ChangesResult> GetChangesInternalAsync(ChangesInternalRequest request, Cancel ctx = default)
	{
		var fetchSize = request.PageSize + 1;

		try
		{
			var pitId = await pitManager.GetPitIdAsync(ctx);

			var response = await Search(request, pitId, fetchSize, ctx);

			if (!response.IsValidResponse && IsExpiredPit(response))
			{
				LogPitExpired(logger);
				pitId = await pitManager.GetPitIdAsync(ctx, expiredPitId: pitId);
				response = await Search(request, pitId, fetchSize, ctx);
			}

			if (!response.IsValidResponse)
			{
				var reason = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown";
				throw new InvalidOperationException(
					$"Elasticsearch changes query failed (HTTP {response.ApiCallDetails?.HttpStatusCode}): {reason}"
				);
			}

			pitManager.RefreshKeepAlive();

			return BuildResult(response, request.PageSize);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error querying Elasticsearch for changes since {Since}", request.Since);
			throw;
		}
	}

	private async Task<EsSearchResponse> Search(
		ChangesInternalRequest request, string pitId, int fetchSize, Cancel ctx
	) =>
		await clientAccessor.Client.SearchAsync<DocumentationDocument>(s =>
		{
			_ = s
				.Size(fetchSize)
				.TrackTotalHits(t => t.Enabled(false))
				.Pit(p => p.Id(pitId).KeepAlive(SharedPointInTimeManager.PitKeepAlive))
				.Query(q => q.Range(r => r
					.Date(dr => dr
						.Field(f => f.ContentLastUpdated)
						.Gt(request.Since.ToString("O"))
					)
				))
				.Sort(
					so => so.Field(f => f.ContentLastUpdated, sf => sf.Order(SortOrder.Asc)),
					so => so.Field(f => f.Url, sf => sf.Order(SortOrder.Asc))
				)
				.Source(sf => sf
					.Filter(f => f
						.Includes(
							e => e.Url,
							e => e.Title,
							e => e.SearchTitle,
							e => e.ContentType,
							e => e.ContentLastUpdated
						)
					)
				);

			if (request.Cursor is { } cursor)
			{
				_ = s.SearchAfter(
					FieldValue.Long(cursor.ContentLastUpdatedEpochMs),
					FieldValue.String(cursor.Url)
				);
			}
		}, ctx);

	private static bool IsExpiredPit(EsSearchResponse response) =>
		response.ElasticsearchServerError?.Error?.Type is "search_phase_execution_exception"
		|| response.ElasticsearchServerError?.Error?.Reason?.Contains("point in time", StringComparison.OrdinalIgnoreCase) == true
		|| response.ElasticsearchServerError?.Error?.Reason?.Contains("No search context found", StringComparison.OrdinalIgnoreCase) == true;

	private static ChangesResult BuildResult(EsSearchResponse response, int pageSize)
	{
		var hits = response.Hits.ToList();
		var hasMore = hits.Count > pageSize;

		var pages = hits
			.Take(pageSize)
			.Where(h => h.Source is not null)
			.Select(h =>
			{
				var doc = h.Source!;
				return new ChangedPageDto
				{
					Url = doc.Url,
					Title = doc.Title,
					LastUpdated = doc.ContentLastUpdated
				};
			})
			.ToList();

		var nextCursor = (ChangesPageCursor?)null;
		if (hasMore)
		{
			var lastHit = hits[pageSize - 1];
			if (lastHit.Sort is { Count: >= 2 })
			{
				var sortEpoch = lastHit.Sort.ElementAt(0);
				var sortUrl = lastHit.Sort.ElementAt(1);

				// ES returns date sort values as double (JSON has no int/float distinction)
				var epochMs = sortEpoch.TryGetLong(out var l) ? l!.Value
					: sortEpoch.TryGetDouble(out var d) ? (long)d!.Value
					: default(long?);

				if (epochMs is not null && sortUrl.TryGetString(out var url))
					nextCursor = new ChangesPageCursor(epochMs.Value, url!);
			}
		}

		return new ChangesResult
		{
			Pages = pages,
			NextCursor = nextCursor
		};
	}

	private static ChangesPageCursor? DecodeCursor(string? cursor)
	{
		if (string.IsNullOrWhiteSpace(cursor))
			return null;

		try
		{
			var remainder = cursor.Length % 4;
			var paddingLength = (4 - remainder) % 4;
			var base64 = cursor
				.Replace('-', '+')
				.Replace('_', '/')
				+ new string('=', paddingLength);

			var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;
			var arrayLength = root.GetArrayLength();
			if (root.ValueKind != JsonValueKind.Array || arrayLength < 2)
				return null;

			var epochEl = root[0];
			var urlEl = root[1];
			if (epochEl.ValueKind != JsonValueKind.Number || urlEl.ValueKind != JsonValueKind.String)
				return null;

			return new ChangesPageCursor(epochEl.GetInt64(), urlEl.GetString()!);
		}
		catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
		{
			return null;
		}
	}

	private static string EncodeCursor(ChangesPageCursor cursor)
	{
		var buffer = new ArrayBufferWriter<byte>();
		using var writer = new Utf8JsonWriter(buffer);
		writer.WriteStartArray();
		writer.WriteNumberValue(cursor.ContentLastUpdatedEpochMs);
		writer.WriteStringValue(cursor.Url);
		writer.WriteEndArray();
		writer.Flush();

		return Convert.ToBase64String(buffer.WrittenSpan)
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}

	[LoggerMessage(Level = LogLevel.Information,
		Message = "Changes feed returned {Count} pages since {Since} (hasMore: {HasMore})")]
	private static partial void LogChanges(ILogger logger, DateTimeOffset since, int count, bool hasMore);

	[LoggerMessage(Level = LogLevel.Warning, Message = "PIT expired or not found, opening a new one and retrying with existing search_after position")]
	private static partial void LogPitExpired(ILogger logger);
}
