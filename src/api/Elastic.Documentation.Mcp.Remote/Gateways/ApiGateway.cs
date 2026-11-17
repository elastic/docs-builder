// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Highlighting;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Mcp.Remote.Gateways;

/// <summary>Queries the <c>api.*</c> fields API Explorer pages carry in the docs search index.</summary>
public class ApiGateway(ElasticsearchClientAccessor clientAccessor, ILogger<ApiGateway> logger) : IApiGateway
{
	private const string ApiContentType = "api";

	// Enough for every operation of the largest API in one version tree.
	private const int MaxOperations = 2000;
	private const int MaxApis = 200;
	private const int MaxEndpointCandidates = 50;
	private const int MaxIdMatches = 10;

	// Every required member of ApiOperationMetadata, or a hit fails to deserialize.
	private static readonly string[] SummarySource =
	[
		"title",
		"search_title",
		"path",
		"description",
		"content_type",
		"api.key",
		"api.method",
		"api.path",
		"api.operation_id",
		"api.endpoints",
		"api.routes",
		"api.group",
		"api.deprecated",
		"api.landing",
		"api.trees"
	];

	private static readonly string[] DetailSource = [.. SummarySource, "api.properties", "ai_short_summary", "body"];

	private static readonly Query ApiDocuments = new TermQuery { Field = QueryFieldNames.ContentType, Value = ApiContentType };
	private static readonly Query LandingPages = new TermQuery { Field = QueryFieldNames.ApiLanding, Value = true };
	private static readonly Query DeprecatedOperations = new TermQuery { Field = QueryFieldNames.ApiDeprecated, Value = true };

	/// <inheritdoc />
	public async Task<ApiOperationPage> SearchOperationsAsync(ApiOperationSearch search, CancellationToken ct = default)
	{
		var query = new BoolQuery
		{
			Must = [ApiLookupQueryBuilder.Build(search.Query)],
			Filter = [OperationFilter(search.Scope)],
			MustNot = search.IncludeDeprecated ? [] : [DeprecatedOperations]
		};
		var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(
			s => s
				.Indices(clientAccessor.SearchIndex)
				.Query(query)
				.From((search.PageNumber - 1) * search.PageSize)
				.Size(search.PageSize)
				.Source(sf => sf.Filter(f => f.Includes(SummarySource)))
				// The hit processor reads each word's winning role, and so its field rows, from the named query scores.
				.IncludeNamedQueriesScore(true),
			ct
		);
		EnsureValid(response);

		var options = new HighlightOptions { ApiLookup = true, ListChildren = ApiLookupQueryBuilder.ExpandsChildren(search.Query) };
		var hits = response
			.Hits
			.Where(h => h.Source?.Api is not null)
			.Select(h => SearchResultProcessor.ProcessHit(h, search.Query, clientAccessor.SynonymBiDirectional, options))
			.Select(item => new ApiOperationHit(ToSummary(item.Document, search.Scope), item.Score, [.. item.Properties.Select(ToField)]))
			.ToList();
		return new ApiOperationPage(response.Total, hits);
	}

	/// <inheritdoc />
	public async Task<ApiOperationDetail?> GetOperationAsync(ApiOperationLookup lookup, CancellationToken ct = default)
	{
		var ranked = lookup.Key switch
		{
			ApiOperationIdKey id => await FindByOperationId(lookup.Scope, id.OperationId, ct),
			ApiEndpointKey endpoint => await FindByEndpoint(lookup.Scope, ApiRouteMatch.Parse(endpoint.Endpoint), ct)
		};
		if (ranked.Count == 0)
		{
			logger.LogDebug("No API operation matches {Lookup}", lookup);
			return null;
		}

		var best = ranked[0];
		var api = best.Api!;
		return new ApiOperationDetail
		{
			Operation = ToSummary(best, lookup.Scope),
			Versions = [.. api.Trees.Select(t => t.Tree)],
			Fields = [.. api.Properties.Select(ToField)],
			AiShortSummary = best.AiShortSummary,
			Body = best.Body,
			OtherMatches = [.. ranked.Skip(1).Select(d => ToSummary(d, lookup.Scope))]
		};
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ApiSummary>> ListApisAsync(CancellationToken ct = default)
	{
		var query = new BoolQuery { Filter = [SearchQueryBuilder.DocumentFilterFor(null), ApiDocuments, LandingPages] };
		var documents = await SearchDocuments(query, MaxApis, SummarySource, ct);
		return [
			.. documents.OrderBy(d => d.Api!.Key, StringComparer.Ordinal).Select(
				d => new ApiSummary
				{
					Key = d.Api!.Key,
					Title = d.Title,
					Url = d.Path,
					Description = d.Description,
					Versions = [.. d.Api.Trees.Select(t => t.Tree)]
				}
			)
		];
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ApiOperationSummary>> ListOperationsAsync(ApiScope scope, CancellationToken ct = default)
	{
		var documents = await SearchDocuments(OperationFilter(scope), MaxOperations, SummarySource, ct);
		return [.. documents.Select(d => ToSummary(d, scope))];
	}

	private async Task<IReadOnlyList<DocumentationDocument>> FindByOperationId(ApiScope? scope, string operationId, CancellationToken ct)
	{
		var query = new BoolQuery
		{
			Filter = [OperationFilter(scope), new TermQuery { Field = QueryFieldNames.ApiOperationId, Value = operationId.Trim() }]
		};
		return await SearchDocuments(query, MaxIdMatches, DetailSource, ct);
	}

	/// <summary>Candidates from the index's loose endpoint keys, ranked by how many literal segments of their route the caller typed.</summary>
	private async Task<IReadOnlyList<DocumentationDocument>> FindByEndpoint(ApiScope? scope, TypedEndpoint typed, CancellationToken ct)
	{
		string[] candidates = [.. ApiEndpointPath.QueryCandidates(typed.Path).SelectMany(g => g.Terms)];
		if (candidates.Length == 0)
			return [];

		var query = new BoolQuery
		{
			Filter = [OperationFilter(scope)],
			Should =
			[
				Terms(QueryFieldNames.ApiEndpoints, candidates),
				Terms(QueryFieldNames.ApiEndpointsBare, ApiEndpointPath.BareKeys(candidates))
			],
			MinimumShouldMatch = 1
		};
		var documents = await SearchDocuments(query, MaxEndpointCandidates, DetailSource, ct);
		return [
			.. documents
				.Select(d => (Document: d, Score: ApiRouteMatch.BestScore(typed, d.Api!)))
				.Where(c => c.Score != ApiRouteMatch.NoMatch)
				.OrderByDescending(c => c.Score)
				// A page serving several methods leads with one; prefer the page that leads with the typed method.
				.ThenByDescending(c => string.Equals(c.Document.Api!.Method, typed.Method, StringComparison.OrdinalIgnoreCase))
				.Select(c => c.Document)
		];
	}

	private async Task<IReadOnlyList<DocumentationDocument>> SearchDocuments(Query query, int size, string[] source, CancellationToken ct)
	{
		var response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(
			s => s.Indices(clientAccessor.SearchIndex).Query(query).Size(size).Source(sf => sf.Filter(f => f.Includes(source))),
			ct
		);
		EnsureValid(response);
		return [.. response.Documents.Where(d => d.Api is not null)];
	}

	/// <summary>
	/// Operation pages, never landing pages, of one API when <paramref name="scope"/> is set. Without a version the
	/// default tree applies, because only its documents are not hidden.
	/// </summary>
	private static Query OperationFilter(ApiScope? scope)
	{
		var page = scope is { Version: { } version } ? new ApiPageContext(scope.Key, version) : null;
		List<Query> filters = [SearchQueryBuilder.DocumentFilterFor(page), ApiDocuments];
		if (scope is not null)
			filters.Add(new TermQuery { Field = QueryFieldNames.ApiKey, Value = scope.Key });
		return new BoolQuery { Filter = filters, MustNot = [LandingPages] };
	}

	private static Query Terms(string field, IEnumerable<string> values) =>
		new TermsQuery(field, new TermsQueryField([.. values.Select(v => (FieldValue)v)]));

	private static ApiOperationSummary ToSummary(DocumentationDocument document, ApiScope? scope)
	{
		var api = document.Api!;
		// A hit's path points into its first version tree; a caller reading another tree gets that tree's URL.
		var rebase = scope is { Version: { } version } ? ApiTreeRebase.For(api, new ApiPageContext(scope.Key, version)) : null;
		return new ApiOperationSummary
		{
			Api = api.Key,
			Title = document.Title,
			Url = rebase?.Apply(document.Path) ?? document.Path,
			Description = document.Description,
			Method = api.Method,
			Path = api.Path,
			OperationId = api.OperationId,
			Group = string.IsNullOrEmpty(api.Group) ? null : api.Group,
			Deprecated = api.Deprecated,
			Endpoints = ApiRouteMatch.Endpoints(api)
		};
	}

	private static ApiField ToField(ApiPropertyEntry property) =>
		new()
		{
			Name = property.Name,
			Path = property.Path,
			Type = property.Type,
			Location = property.Location,
			Description = property.Description
		};

	private void EnsureValid<TDocument>(Clients.Elasticsearch.SearchResponse<TDocument> response)
	{
		if (response.IsValidResponse)
			return;
		var reason = response.ElasticsearchServerError?.Error?.Reason ?? "unknown error";
		logger.LogError("API index query failed: {Reason}", reason);
		throw new InvalidOperationException($"The API index query failed: {reason}");
	}
}
