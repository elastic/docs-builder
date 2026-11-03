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

		var result = await SearchAsync(document, [property]);

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
		result.Deprecated.Should().BeFalse();
	}

	[Test]
	public async Task NavigationSearchAsync_DeprecatedOperation_FlagsTheResult()
	{
		var document = new DocumentationDocument
		{
			Title = "Get a document source",
			SearchTitle = "Get a document source",
			Path = "/docs/api/doc/elasticsearch/operation/operation-get-source",
			Api = new ApiOperationMetadata
			{
				Endpoints = ["_source"],
				Method = "GET",
				Path = "/{index}/_source/{id}",
				OperationId = "get-source",
				Deprecated = true
			}
		};

		var result = await SearchAsync(document, []);

		result.Deprecated.Should().BeTrue();
	}

	private static async Task<NavigationSearchResultItem> SearchAsync(DocumentationDocument document, ApiPropertyEntry[] properties)
	{
		var inner = A.Fake<ISearchService<DocumentationDocument>>();
		_ = A.CallTo(
			() => inner.AutocompleteAsync(A<AutocompleteRequest>._, A<CancellationToken>._)
		).Returns(new AutocompleteResponse<DocumentationDocument>
		{
			Results =
			[
				new SearchResultItem<DocumentationDocument>
				{
					Document = document,
					Title = document.Title,
					Description = "Run a search",
					Score = 5,
					Properties = properties
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

		var response = await service.NavigationSearchAsync(new NavigationSearchRequest { Query = "q", TypeFilter = "api" });
		return response.Results.Single();
	}
}
