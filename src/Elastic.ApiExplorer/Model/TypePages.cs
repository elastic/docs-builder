// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Model;

/// <summary>A navigation group of type pages, such as Query DSL or Aggregations.</summary>
/// <param name="Slug">The category part of the page's model, also used to group pages under one heading.</param>
public sealed record TypePageCategory(string Slug, string Title, string Description);

/// <summary>
/// A type documented on a page of its own: large, recursive and shared by many operations, so rows that use it link
/// there instead of listing it again.
/// </summary>
/// <param name="SchemaId">The <c>components/schemas</c> key; rows link by <c>$ref</c> id, never by display name.</param>
/// <param name="MapOf">For a type the API sends as a map of itself (<c>aggs</c>), the map's spelling on the page.</param>
public sealed record TypePage(string SchemaId, TypePageCategory Category, string? MapOf = null)
{
	/// <summary>The page's title: the last segment of the schema id, <c>QueryContainer</c>.</summary>
	public string DisplayName => SchemaHelpers.FormatSchemaName(SchemaId);
}

/// <summary>The types that get a page of their own. The one place that decides which ones, and where they go.</summary>
public static class TypePages
{
	private static readonly TypePageCategory QueryDsl = new("query-dsl", "Query DSL", "Query type definitions");
	private static readonly TypePageCategory Aggregations = new("aggregations", "Aggregations", "Aggregation type definitions");
	private static readonly TypePageCategory Ingest = new("ingest", "Ingest", "Ingest processor definitions");

	public static readonly IReadOnlyList<TypePage> All =
	[
		new("_types.query_dsl.QueryContainer", QueryDsl),
		new("_types.aggregations.AggregationContainer", Aggregations, MapOf: "Dictionary<string, AggregationContainer>"),
		new("_types.aggregations.Aggregate", Aggregations, MapOf: "Dictionary<string, Aggregate>"),
		new("ingest._types.ProcessorContainer", Ingest)
	];

	private static readonly Dictionary<string, TypePage> BySchemaId = All.ToDictionary(static p => p.SchemaId, StringComparer.Ordinal);

	public static bool TryGet(string? schemaId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out TypePage? page)
	{
		page = null;
		return !string.IsNullOrEmpty(schemaId) && BySchemaId.TryGetValue(schemaId, out page);
	}

	/// <summary>Whether a row that refers to <paramref name="schemaId"/> links to its page; never on that page itself.</summary>
	public static bool Links(string? schemaId, string? currentPageSchemaId) =>
		TryGet(schemaId, out _) && !string.Equals(schemaId, currentPageSchemaId, StringComparison.Ordinal);

	/// <summary>The page of a type under an API root (<c>/api/doc/elasticsearch</c>), as <c>SchemaNavigationItem</c> builds it.</summary>
	public static string Url(string apiRootUrl, string schemaId) =>
		$"{apiRootUrl.TrimEnd('/')}/types/{ApiUrlBuilder.SchemaMoniker(schemaId)}";
}
