// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
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
	private static ApiPropertyEntry Property() =>
		new()
		{
			Name = "knn",
			Path = "knn",
			Type = "object",
			Location = "request",
			Anchor = "req-knn",
			Description = "Approximate kNN"
		};

	private static DocumentationDocument SearchOperation() =>
		new()
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

	/// <summary>The default rendering of <c>search</c>, shared by main and 9, with 8 rendering it alike under its own root.</summary>
	private static DocumentationDocument SharedSearchOperation()
	{
		var document = SearchOperation();
		document.Parents =
		[
			new ParentDocument { Title = "API reference", Path = "/docs/api" },
			new ParentDocument { Title = "Elasticsearch API", Path = "/docs/api/doc/elasticsearch" },
			new ParentDocument { Title = "Search", Path = "/docs/api/doc/elasticsearch/group/endpoint-search" }
		];
		return document with
		{
			Api = document.Api! with
			{
				Key = "elasticsearch",
				Trees =
				[
					new ApiTreeEntry { Tree = "main", Root = "/docs/api/doc/elasticsearch" },
					new ApiTreeEntry { Tree = "8", Root = "/docs/api/doc/elasticsearch/v8" }
				]
			}
		};
	}

	private static DocumentationDocument CloudOperation() =>
		new()
		{
			Title = "Get deployments",
			SearchTitle = "Get deployments",
			Path = "/docs/api/doc/cloud/operation/operation-list-deployments",
			Parents = [new ParentDocument { Title = "Elastic Cloud API", Path = "/docs/api/doc/cloud" }],
			Api = new ApiOperationMetadata
			{
				Endpoints = ["deployments"],
				Method = "GET",
				Path = "/deployments",
				OperationId = "list-deployments",
				Key = "cloud",
				Trees = [new ApiTreeEntry { Tree = "main", Root = "/docs/api/doc/cloud" }]
			}
		};

	private static Task<NavigationSearchResponse> SearchWith(ApiPropertyEntry property) =>
		Search(
			new NavigationSearchRequest { Query = "knn", TypeFilter = "api" },
			A.Fake<ISearchService<DocumentationDocument>>(),
			(SearchOperation(), property)
		);

	private static async Task<NavigationSearchResponse> Search(
		NavigationSearchRequest request,
		ISearchService<DocumentationDocument> inner,
		params (DocumentationDocument Document, ApiPropertyEntry Property)[] hits
	)
	{
		_ = A.CallTo(
			() => inner.AutocompleteAsync(A<AutocompleteRequest>._, A<CancellationToken>._)
		).Returns(new AutocompleteResponse<DocumentationDocument>
		{
			Results = hits.Select(
				hit => new SearchResultItem<DocumentationDocument>
				{
					Document = hit.Document,
					Title = hit.Document.Title,
					Description = "",
					Score = 5,
					Properties = [hit.Property]
				}
			).ToList(),
			TotalResults = hits.Length,
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

		return await service.NavigationSearchAsync(request);
	}

	[Test]
	public async Task NavigationSearchAsync_PropertyHit_LinksToTheAnchorOnTheOperationPage()
	{
		var response = await SearchWith(Property());

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

	[Test]
	public async Task NavigationSearchResponse_WithProperty_SerializesThroughApiJsonContext()
	{
		var response = await SearchWith(Property() with { Path = "retriever.knn" });

		var json = JsonSerializer.Serialize(response, ApiJsonContext.Default.NavigationSearchResponse);

		json.Should().Contain("\"path\":\"retriever.knn\"").And.NotContain("\"parent\"");
	}

	[Test]
	public async Task NavigationSearchAsync_Scope_ForwardsItToTheSearch()
	{
		var inner = A.Fake<ISearchService<DocumentationDocument>>();
		var scope = new ApiTreeScope("elasticsearch", "8");

		_ = await Search(
			new NavigationSearchRequest { Query = "knn", TypeFilter = "api", ApiTree = scope },
			inner,
			(SearchOperation(), Property())
		);

		_ = A.CallTo(
			() => inner.AutocompleteAsync(
				A<AutocompleteRequest>.That.Matches(r => r.ApiTree == scope && r.Query == "knn" && r.TypeFilter == "api"),
				A<CancellationToken>._
			)
		).MustHaveHappenedOnceExactly();
	}

	[Test]
	public async Task NavigationSearchAsync_ScopeInAnotherTreeOfTheHitsApi_PointsTheHitIntoThatTree()
	{
		var request = new NavigationSearchRequest { Query = "knn", TypeFilter = "api", ApiTree = new ApiTreeScope("elasticsearch", "8") };

		var response = await Search(request, A.Fake<ISearchService<DocumentationDocument>>(), (SharedSearchOperation(), Property()));

		var result = response.Results.Single();
		result.Url.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-search");
		result.Properties.Single().Url.Should().Be("/docs/api/doc/elasticsearch/v8/operation/operation-search#req-knn");
		result
			.Parents
			.Select(p => p.Url)
			.Should()
			.Equal("/docs/api", "/docs/api/doc/elasticsearch/v8", "/docs/api/doc/elasticsearch/v8/group/endpoint-search");
	}

	[Test]
	public async Task NavigationSearchAsync_ScopeInElasticsearch_LeavesACloudHitAlone()
	{
		var request = new NavigationSearchRequest
		{
			Query = "deployments",
			TypeFilter = "api",
			ApiTree = new ApiTreeScope("elasticsearch", "8")
		};

		var response = await Search(
			request,
			A.Fake<ISearchService<DocumentationDocument>>(),
			(CloudOperation(), Property() with { Anchor = "req-name" })
		);

		var result = response.Results.Single();
		result.Url.Should().Be("/docs/api/doc/cloud/operation/operation-list-deployments");
		result.Properties.Single().Url.Should().Be("/docs/api/doc/cloud/operation/operation-list-deployments#req-name");
		result.Parents.Select(p => p.Url).Should().Equal("/docs/api/doc/cloud");
	}
}
