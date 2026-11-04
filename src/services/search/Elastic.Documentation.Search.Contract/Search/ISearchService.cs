// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Search.Contract;

/// <summary>
/// Search gateway, generic over the result document type. Each consumer binds
/// <typeparamref name="TDocument"/> to whatever it indexes:
/// <list type="bullet">
/// <item><description>docs search: <c>ISearchService&lt;DocumentationDocument&gt;</c> over the docs-only index.</description></item>
/// <item><description>website search: <c>ISearchService&lt;WebsiteSearchDocument&gt;</c> over the unified
/// <c>website-search.semantic-{env}</c> index — polymorphic deserialization yields docs/site/labs/guide subtypes.</description></item>
/// </list>
/// </summary>
public interface ISearchService<TDocument> where TDocument : SearchDocumentBase
{
	/// <summary>Full-page search with filters, sorting, aggregations, and optional hybrid lex+semantic execution.</summary>
	Task<SearchResponse<TDocument>> SearchAsync(SearchRequest request, CancellationToken ct = default);

	/// <summary>Typeahead / autocomplete — lexical-only, lean <c>_source</c>, always-on highlighting.</summary>
	Task<AutocompleteResponse<TDocument>> AutocompleteAsync(AutocompleteRequest request, CancellationToken ct = default);
}
