// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using Elastic.Documentation.Search.Highlighting;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using EsSearchResponse = Elastic.Clients.Elasticsearch.SearchResponse<Elastic.Documentation.Search.Contract.DocumentationDocument>;

namespace Elastic.Documentation.Search.ApiReference;

/// <summary>Queries the <c>api.*</c> fields API Explorer pages carry in the docs search index.</summary>
public class ApiGateway(ElasticsearchClientAccessor clientAccessor, ILogger<ApiGateway> logger) : IApiGateway
{
	private const string ApiContentType = "api";

	// Enough for every operation of the largest API in one version tree.
	private const int MaxOperations = 2000;
	// Every API's landing page, once per version tree whose landing content differs.
	private const int MaxLandingRenderings = 1000;
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
	private static readonly Query OtherPages = new TermQuery { Field = QueryFieldNames.ApiPage, Value = true };
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
		var response = await Search(
			s => s
				.Query(query)
				.From((search.PageNumber - 1) * search.PageSize)
				.Size(search.PageSize)
				.Source(sf => sf.Filter(f => f.Includes(SummarySource)))
				// The hit processor reads each word's winning role, and so its field rows, from the named query scores.
				.IncludeNamedQueriesScore(true),
			ct
		);

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
		var matches = await FindOperationsAsync(lookup, ct);
		return matches.Count == 0 ? null : matches[0] with { OtherMatches = [.. matches.Skip(1).Select(m => m.Operation)] };
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ApiOperationDetail>> FindOperationsAsync(ApiOperationLookup lookup, CancellationToken ct = default)
	{
		var ranked = lookup.Key switch
		{
			ApiOperationIdKey id => await FindByOperationId(lookup.Scope, id.OperationId, ct),
			ApiEndpointKey endpoint => await FindByEndpoint(lookup.Scope, ApiRouteMatch.Parse(endpoint.Endpoint), ct)
		};
		if (ranked.Count == 0)
			logger.LogDebug("No API operation matches {Lookup}", lookup);

		// Both lookups read DetailSource, so every match already carries its trees, properties and body.
		return [
			.. ranked.Select(
				d => new ApiOperationDetail
				{
					Operation = ToSummary(d, lookup.Scope),
					Versions = [.. d.Api!.Trees.Select(t => t.Tree)],
					Fields = [.. d.Api.Properties.Select(ToField)],
					AiShortSummary = d.AiShortSummary,
					Body = d.Body
				}
			)
		];
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ApiSummary>> ListApisAsync(CancellationToken ct = default)
	{
		// Hidden renderings included: each lists only the trees that share its content, so an API's versions are their union.
		var query = new BoolQuery { Filter = [ApiDocuments, LandingPages] };
		var renderings = await SearchDocuments(query, MaxLandingRenderings, [.. SummarySource, "hidden"], ct);
		return [.. renderings.GroupBy(d => d.Api!.Key).OrderBy(g => g.Key, StringComparer.Ordinal).Select(ToApiSummary)];
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

	/// <summary>
	/// Candidates from the index's loose endpoint keys, or for a route made only of parameters such as <c>/{index}</c>,
	/// which has no key, from its path. The best matching route wins; other candidates that match it as well are kept.
	/// </summary>
	private async Task<IReadOnlyList<DocumentationDocument>> FindByEndpoint(ApiScope? scope, TypedEndpoint typed, CancellationToken ct)
	{
		string[] candidates = [.. ApiEndpointPath.QueryCandidates(typed.Path).SelectMany(g => g.Terms)];
		List<Query> routeQueries = candidates.Length == 0
			? []
			: [
				Terms(QueryFieldNames.ApiEndpoints, candidates),
				Terms(QueryFieldNames.ApiEndpointsBare, ApiEndpointPath.BareKeys(candidates))
			];
		if (ApiRouteMatch.MayFillOnlyParameters(typed))
			routeQueries.Add(new RegexpQuery
			{
				Field = QueryFieldNames.ApiPath,
				Value = string.Concat(Enumerable.Repeat(@"/\{[^/]+\}", typed.Segments.Length))
			});
		if (routeQueries.Count == 0)
			return [];

		var query = new BoolQuery { Filter = [OperationFilter(scope)], Should = routeQueries, MinimumShouldMatch = 1 };
		var ranked = (await SearchDocuments(query, MaxEndpointCandidates, DetailSource, ct))
			.Select(d => (Document: d, Rank: RouteRank(typed, d.Api!)))
			.Where(c => c.Rank >= 0)
			.ToList();
		var best = ranked.Select(c => c.Rank).DefaultIfEmpty(-1).Max();
		return [.. ranked.Where(c => c.Rank == best).Select(c => c.Document)];
	}

	/// <summary>
	/// Literal segments matched first; then, since a page serving several methods leads with one, the page that leads with the
	/// typed method. Negative when no route fits.
	/// </summary>
	private static int RouteRank(TypedEndpoint typed, ApiOperationMetadata api)
	{
		var score = ApiRouteMatch.BestScore(typed, api);
		var leadsWithMethod = string.Equals(api.Method, typed.Method, StringComparison.OrdinalIgnoreCase);
		return score == ApiRouteMatch.NoMatch ? -1 : (score * 2) + (leadsWithMethod ? 1 : 0);
	}

	private async Task<IReadOnlyList<DocumentationDocument>> SearchDocuments(Query query, int size, string[] source, CancellationToken ct)
	{
		var response = await Search(s => s.Query(query).Size(size).Source(sf => sf.Filter(f => f.Includes(source))), ct);
		return [.. response.Documents.Where(d => d.Api is not null)];
	}

	/// <summary>Runs a search on the docs index; a timeout or an overloaded cluster becomes <see cref="SearchUnavailableException"/>.</summary>
	private async Task<EsSearchResponse> Search(Action<SearchRequestDescriptor<DocumentationDocument>> configure, CancellationToken ct)
	{
		EsSearchResponse response;
		try
		{
			response = await clientAccessor.Client.SearchAsync<DocumentationDocument>(
				s => configure(s.Indices(clientAccessor.SearchIndex)),
				ct
			);
		}
		catch (TransportException ex) when (SearchUnavailableException.IsTransient(ex))
		{
			throw SearchUnavailableException.Transient(ex.FailureReason, ex);
		}
		if (response.IsValidResponse)
			return response;

		var reason = response.ElasticsearchServerError?.Error?.Reason
			?? response.ApiCallDetails?.OriginalException?.Message
			?? "unknown error";
		if (SearchUnavailableException.IsTransient(response))
			throw SearchUnavailableException.Transient(reason, response.ApiCallDetails?.OriginalException);
		logger.LogError("API index query failed: {Reason}", reason);
		throw new InvalidOperationException($"The API index query failed: {reason}");
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
		return new BoolQuery { Filter = filters, MustNot = [LandingPages, OtherPages] };
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

	private static ApiSummary ToApiSummary(IGrouping<string, DocumentationDocument> renderings)
	{
		// The default tree's rendering is the visible one; its title and URL stand for the API.
		var landing = renderings.FirstOrDefault(d => !d.Hidden) ?? renderings.First();
		return new ApiSummary
		{
			Key = renderings.Key,
			Title = landing.Title,
			Url = landing.Path,
			Description = landing.Description,
			Versions = [.. renderings.SelectMany(d => d.Api!.Trees).Select(t => t.Tree).Distinct(StringComparer.Ordinal)]
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
}
