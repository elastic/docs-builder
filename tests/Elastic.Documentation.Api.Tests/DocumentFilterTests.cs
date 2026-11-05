// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Contract;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using SearchRequest = Elastic.Documentation.Search.Contract.SearchRequest;

namespace Elastic.Documentation.Api.Tests;

public class DocumentFilterTests
{
	private const string UnscopedFilter =
		"""{"bool":{"must_not":[{"terms":{"path.keyword":["/docs","/docs/","/docs/404","/docs/404/"]}},{"term":{"hidden":{"value":true}}}]}}""";

	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	private static string Serialize(Clients.Elasticsearch.QueryDsl.Query query) =>
		Client.RequestResponseSerializer.SerializeToString(query);

	[Test]
	public void DocumentFilterFor_NoScope_ExcludesRootPlaceholdersAndHiddenDocuments() =>
		Serialize(SearchQueryBuilder.DocumentFilterFor(null)).Should().Be(UnscopedFilter);

	[Test]
	public void DocumentFilterFor_Scope_ShowsTheScopedTreeOfThatApiAndDefaultRenderingsOfTheRest() =>
		Serialize(SearchQueryBuilder.DocumentFilterFor(new ApiTreeScope("elasticsearch", "8")))
			.Should()
			.Be(
				"""{"bool":{"minimum_should_match":1,"must_not":{"terms":{"path.keyword":["/docs","/docs/","/docs/404","/docs/404/"]}},"should":[{"bool":{"must_not":[{"term":{"hidden":{"value":true}}},{"term":{"api.key":{"value":"elasticsearch"}}}]}},{"bool":{"filter":[{"term":{"api.key":{"value":"elasticsearch"}}},{"term":{"api.trees.tree":{"value":"8"}}}]}}]}}"""
			);

	[Test]
	public async Task SearchAsync_SemanticQuery_FiltersOutsideTheLexicalAndSemanticBranches()
	{
		using var query = await CapturedQuery(s => s.SearchAsync(new SearchRequest { Query = "how do I run a knn search" }));

		var root = query.RootElement.GetProperty("bool");
		root.GetProperty("filter").GetRawText().Should().Be(UnscopedFilter);
		var branches = root.GetProperty("must").GetProperty("bool").GetProperty("should");
		branches.GetArrayLength().Should().Be(2);
		branches.GetRawText().Should().NotContain("\"hidden\"", "a filter inside one branch lets a hit through the other");
	}

	[Test]
	public async Task AutocompleteAsync_ApiLookupWithScope_FiltersTheWholeLookupOnce()
	{
		var scope = new ApiTreeScope("elasticsearch", "8");

		using var query = await CapturedQuery(
			s => s.AutocompleteAsync(new AutocompleteRequest { Query = "knn", TypeFilter = "api", ApiTree = scope })
		);

		var root = query.RootElement.GetProperty("bool");
		root.GetProperty("filter").GetRawText().Should().Be(Serialize(SearchQueryBuilder.DocumentFilterFor(scope)));
		root.GetProperty("must").GetRawText().Should().NotContain("\"hidden\"");
	}

	[Test]
	public async Task AutocompleteAsync_DocsWideField_FiltersTheWholeQueryOnce()
	{
		using var query = await CapturedQuery(s => s.AutocompleteAsync(new AutocompleteRequest { Query = "track_total_hits" }));

		var root = query.RootElement.GetProperty("bool");
		root.GetProperty("filter").GetRawText().Should().Be(UnscopedFilter);
		root.GetProperty("must").GetRawText().Should().NotContain("\"hidden\"");
	}

	/// <summary>The <c>query</c> of the one request the service sends, over a client that answers with no hits.</summary>
	private static async Task<JsonDocument> CapturedQuery(Func<DefaultSearchService<DocumentationDocument>, Task> search)
	{
		byte[]? body = null;
		var settings = new ElasticsearchClientSettings(new InMemoryRequestInvoker(
			Encoding.UTF8.GetBytes("""{"hits":{"hits":[]}}"""),
			headers: new Dictionary<string, IEnumerable<string>> { ["x-elastic-product"] = ["Elasticsearch"] }
		)).DisableDirectStreaming().OnRequestCompleted(details => body = details.RequestBodyInBytes);
		var service = new DefaultSearchService<DocumentationDocument>(
			new ElasticsearchClient(settings),
			"docs",
			new SearchQueryConfiguration(),
			NullLogger<DefaultSearchService<DocumentationDocument>>.Instance
		);

		await search(service);

		body.Should().NotBeNull();
		using var request = JsonDocument.Parse(body);
		return JsonDocument.Parse(request.RootElement.GetProperty("query").GetRawText());
	}
}
