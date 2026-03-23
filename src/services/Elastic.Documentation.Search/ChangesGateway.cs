// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Api.Core.Changes;
using Elastic.Documentation.Search.Common;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

/// <summary>
/// Elasticsearch gateway for the documentation changes feed.
/// Queries last_updated > since with search_after cursor pagination.
/// Uses Point In Time (PIT) for consistent pagination across requests.
/// </summary>
public partial class ChangesGateway(ElasticsearchClientAccessor clientAccessor, ILogger<ChangesGateway> logger)
	: IChangesGateway
{
	private const string PitKeepAlive = "5m";

	public async Task<ChangesResult> GetChangesAsync(ChangesRequest request, Cancel ctx = default)
	{
		var fetchSize = request.PageSize + 1;

		try
		{
			var pitId = await ResolvePitId(request.Cursor?.PitId, ctx);

			var response = await Search(request, pitId, fetchSize, ctx);

			if (!response.IsValidResponse)
			{
				if (IsExpiredPit(response) && request.Cursor?.PitId is not null)
				{
					LogPitExpired(logger);
					pitId = await OpenPit(ctx);
					response = await Search(request with { Cursor = null }, pitId, fetchSize, ctx);
				}

				if (!response.IsValidResponse)
				{
					logger.LogWarning("Elasticsearch changes query returned invalid response. Reason: {Reason}",
						response.ElasticsearchServerError?.Error.Reason ?? "Unknown");
				}
			}

			// Use the PIT ID from the response if available, as ES may return a new one
			var responsePitId = response.PitId ?? pitId;

			return BuildResult(response, request.PageSize, responsePitId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error querying Elasticsearch for changes since {Since}", request.Since);
			throw;
		}
	}

	private async Task<string> ResolvePitId(string? existingPitId, Cancel ctx)
	{
		if (!string.IsNullOrEmpty(existingPitId))
			return existingPitId;

		return await OpenPit(ctx);
	}

	private async Task<string> OpenPit(Cancel ctx)
	{
		var response = await clientAccessor.Client.OpenPointInTimeAsync(
			clientAccessor.SearchIndex,
			r => r.KeepAlive(PitKeepAlive),
			ctx
		);

		if (!response.IsValidResponse)
		{
			throw new InvalidOperationException(
				$"Failed to open PIT: {response.ElasticsearchServerError?.Error.Reason ?? "Unknown"}"
			);
		}

		LogPitOpened(logger, response.Id);
		return response.Id;
	}

	private async Task<SearchResponse<DocumentationDocument>> Search(
		ChangesRequest request, string pitId, int fetchSize, Cancel ctx
	) =>
		await clientAccessor.Client.SearchAsync<DocumentationDocument>(s =>
		{
			_ = s
				.Size(fetchSize)
				.TrackTotalHits(t => t.Enabled(false))
				.Pit(p => p.Id(pitId).KeepAlive(PitKeepAlive))
				.Query(q => q.Range(r => r
					.Date(dr => dr
						.Field(f => f.LastUpdated)
						.Gt(request.Since.ToString("O"))
					)
				))
				.Sort(
					so => so.Field(f => f.LastUpdated, sf => sf.Order(SortOrder.Asc)),
					so => so.Field(f => f.Url, sf => sf.Order(SortOrder.Asc))
				)
				.Source(sf => sf
					.Filter(f => f
						.Includes(
							e => e.Url,
							e => e.Title,
							e => e.SearchTitle,
							e => e.Type,
							e => e.LastUpdated
						)
					)
				);

			if (request.Cursor is { } cursor)
			{
				_ = s.SearchAfter(
					FieldValue.Long(cursor.LastUpdatedEpochMs),
					FieldValue.String(cursor.Url)
				);
			}
		}, ctx);

	private static bool IsExpiredPit(SearchResponse<DocumentationDocument> response) =>
		response.ElasticsearchServerError?.Error.Type is "search_phase_execution_exception"
		|| response.ElasticsearchServerError?.Error.Reason?.Contains("point in time", StringComparison.OrdinalIgnoreCase) == true
		|| response.ElasticsearchServerError?.Error.Reason?.Contains("No search context found", StringComparison.OrdinalIgnoreCase) == true;

	private static ChangesResult BuildResult(SearchResponse<DocumentationDocument> response, int pageSize, string pitId)
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
					LastUpdated = doc.LastUpdated
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
					nextCursor = new ChangesPageCursor(epochMs.Value, url!, pitId);
			}
		}

		return new ChangesResult
		{
			Pages = pages,
			NextCursor = nextCursor
		};
	}

	[LoggerMessage(Level = LogLevel.Debug, Message = "Opened new PIT: {PitId}")]
	private static partial void LogPitOpened(ILogger logger, string pitId);

	[LoggerMessage(Level = LogLevel.Warning, Message = "PIT expired or not found, opening a new one and retrying without search_after")]
	private static partial void LogPitExpired(ILogger logger);
}
