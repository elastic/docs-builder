// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.Search.Contract;
using FakeItEasy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Tests;

public class NavigationSearchServiceTests
{
	[Test]
	public async Task NavigationSearchAsync_PropertyHit_LinksToTheAnchorOnTheOperationPage()
	{
		var inner = A.Fake<ISearchService<DocumentationDocument>>();
		var document = new DocumentationDocument
		{
			Title = "Search",
			SearchTitle = "Search",
			Path = "/docs/api/doc/elasticsearch/operation/operation-search",
			Api = new ApiOperationMetadata
			{
				Endpoints = ["_search", "*/_search"],
				Method = "POST",
				Path = "/{index}/_search",
				OperationId = "search",
				Routes =
				[
					new ApiRouteEntry { Path = "/{index}/_search", Methods = ["POST", "GET"] },
					new ApiRouteEntry { Path = "/_search", Methods = ["POST", "GET"] }
				]
			}
		};
		var property = new ApiPropertyEntry
		{
			Name = "knn",
			Path = "knn",
			Type = "object",
			Location = "request",
			Anchor = "req-knn",
			Description = "Approximate kNN"
		};
		_ = A.CallTo(
			() => inner.AutocompleteAsync(A<AutocompleteRequest>._, A<CancellationToken>._)
		).Returns(new AutocompleteResponse<DocumentationDocument>
		{
			Results =
			[
				new SearchResultItem<DocumentationDocument>
				{
					Document = document,
					Title = "Search",
					Description = "Run a search",
					Score = 5,
					Properties = [property]
				}
			],
			TotalResults = 1,
			PageNumber = 1,
			PageSize = 20,
			Aggregations = new AutocompleteAggregations()
		});
		using var accessor = new ElasticsearchClientAccessor(
			new DocumentationEndpoints
			{
				Elasticsearch = new ElasticsearchEndpoint { Uri = new Uri("https://localhost:9200"), ApiKey = "test" }
			},
			new SearchConfiguration { Synonyms = [], Rules = [], DiminishTerms = [] }
		);
		using var service = new NavigationSearchService(inner, accessor, NullLogger<NavigationSearchService>.Instance);

		var response = await service.NavigationSearchAsync(new NavigationSearchRequest { Query = "knn", TypeFilter = "api" });

		var result = response.Results.Single();
		result.Url.Should().Be("/docs/api/doc/elasticsearch/operation/operation-search");
		var linked = result.Properties.Single();
		linked.Url.Should().Be("/docs/api/doc/elasticsearch/operation/operation-search#req-knn");
		linked.Anchor.Should().Be("req-knn");
		linked.Location.Should().Be("request");
		result
			.Routes
			.Select(r => (r.Path, string.Join(",", r.Methods)))
			.Should()
			.Equal(("/{index}/_search", "POST,GET"), ("/_search", "POST,GET"));
	}
}
