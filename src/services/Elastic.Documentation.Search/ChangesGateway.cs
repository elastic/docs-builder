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
/// </summary>
public class ChangesGateway(ElasticsearchClientAccessor clientAccessor, ILogger<ChangesGateway> logger)
	: IChangesGateway
{
	public async Task<ChangesResult> GetChangesAsync(ChangesRequest request, Cancel ctx = default)
	{
		var fetchSize = request.PageSize + 1;

		try
		{
			var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(s =>
			{
				_ = s
					.Indices(clientAccessor.SearchIndex)
					.Size(fetchSize)
					.TrackTotalHits(t => t.Enabled(false))
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

			if (!response.IsValidResponse)
			{
				logger.LogWarning("Elasticsearch changes query returned invalid response. Reason: {Reason}",
					response.ElasticsearchServerError?.Error.Reason ?? "Unknown");
			}

			return BuildResult(response, request.PageSize);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error querying Elasticsearch for changes since {Since}", request.Since);
			throw;
		}
	}

	private static ChangesResult BuildResult(SearchResponse<DocumentationDocument> response, int pageSize)
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
					nextCursor = new ChangesPageCursor(epochMs.Value, url!);
			}
		}

		return new ChangesResult
		{
			Pages = pages,
			NextCursor = nextCursor
		};
	}
}
