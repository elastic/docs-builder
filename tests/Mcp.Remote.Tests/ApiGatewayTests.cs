// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.ApiReference;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Mcp.Remote.Tests;

public class ApiGatewayTests
{
	// Both pages come back for a typed "/my-index/_search": the index matches every template a typed path could fill.
	private const string SearchCandidates =
		"""
		{
		  "took": 1,
		  "timed_out": false,
		  "hits": {
		    "total": { "value": 2, "relation": "eq" },
		    "hits": [
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/elasticsearch/operation/operation-get",
		        "_source": {
		          "path": "/docs/api/elasticsearch/operation/operation-get",
		          "title": "Get a document by its ID",
		          "search_title": "Get a document by its ID",
		          "content_type": "api",
		          "api": {
		            "key": "elasticsearch",
		            "method": "GET",
		            "path": "/{index}/_doc/{id}",
		            "operation_id": "get",
		            "group": "Document",
		            "endpoints": ["*/_doc/*", "*/_doc", "_doc/*", "_doc"],
		            "routes": [ { "path": "/{index}/_doc/{id}", "methods": ["GET"] } ],
		            "trees": [ { "tree": "main", "root": "/docs/api/elasticsearch" } ]
		          }
		        }
		      },
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/elasticsearch/operation/operation-search",
		        "_source": {
		          "path": "/docs/api/elasticsearch/operation/operation-search",
		          "title": "Run a search",
		          "search_title": "Run a search",
		          "content_type": "api",
		          "body": "Get search hits that match the query.",
		          "api": {
		            "key": "elasticsearch",
		            "method": "POST",
		            "path": "/_search",
		            "operation_id": "search",
		            "group": "Search",
		            "endpoints": ["_search", "*/_search"],
		            "routes": [
		              { "path": "/_search", "methods": ["POST", "GET"] },
		              { "path": "/{index}/_search", "methods": ["POST", "GET"] }
		            ],
		            "trees": [ { "tree": "main", "root": "/docs/api/elasticsearch" } ],
		            "properties": [
		              {
		                "name": "index",
		                "path": "index",
		                "type": "string",
		                "location": "path",
		                "anchor": "operation-search-index",
		                "description": "A comma-separated list of data streams, indices, and aliases to search."
		              }
		            ]
		          }
		        }
		      }
		    ]
		  }
		}
		""";

	// Create, get and delete index share the route /{index}, which has no literal segment and so no endpoint key.
	private const string IndexRoutes =
		"""
		{
		  "took": 1,
		  "timed_out": false,
		  "hits": {
		    "total": { "value": 2, "relation": "eq" },
		    "hits": [
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/elasticsearch/operation/operation-indices-get",
		        "_source": {
		          "path": "/docs/api/elasticsearch/operation/operation-indices-get",
		          "title": "Get index information",
		          "search_title": "Get index information",
		          "content_type": "api",
		          "api": {
		            "key": "elasticsearch",
		            "method": "GET",
		            "path": "/{index}",
		            "operation_id": "indices-get",
		            "endpoints": [],
		            "routes": [ { "path": "/{index}", "methods": ["GET"] } ]
		          }
		        }
		      },
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/elasticsearch/operation/operation-indices-create",
		        "_source": {
		          "path": "/docs/api/elasticsearch/operation/operation-indices-create",
		          "title": "Create an index",
		          "search_title": "Create an index",
		          "content_type": "api",
		          "api": {
		            "key": "elasticsearch",
		            "method": "PUT",
		            "path": "/{index}",
		            "operation_id": "indices-create",
		            "endpoints": [],
		            "routes": [ { "path": "/{index}", "methods": ["PUT"] } ]
		          }
		        }
		      }
		    ]
		  }
		}
		""";

	// The default tree renders the landing page differently from version 8, so the index holds two renderings.
	private const string LandingRenderings =
		"""
		{
		  "took": 1,
		  "timed_out": false,
		  "hits": {
		    "total": { "value": 2, "relation": "eq" },
		    "hits": [
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/elasticsearch/8",
		        "_source": {
		          "path": "/docs/api/elasticsearch/8",
		          "title": "Elasticsearch API (8)",
		          "search_title": "Elasticsearch API (8)",
		          "content_type": "api",
		          "hidden": true,
		          "api": {
		            "key": "elasticsearch", "landing": true, "method": "", "path": "", "operation_id": "", "endpoints": [], "routes": [],
		            "trees": [ { "tree": "8", "root": "/docs/api/elasticsearch/8" } ]
		          }
		        }
		      },
		      {
		        "_index": "test-index",
		        "_id": "/docs/api/elasticsearch",
		        "_source": {
		          "path": "/docs/api/elasticsearch",
		          "title": "Elasticsearch API",
		          "search_title": "Elasticsearch API",
		          "content_type": "api",
		          "api": {
		            "key": "elasticsearch", "landing": true, "method": "", "path": "", "operation_id": "", "endpoints": [], "routes": [],
		            "trees": [ { "tree": "main", "root": "/docs/api/elasticsearch" }, { "tree": "9", "root": "/docs/api/elasticsearch/9" } ]
		          }
		        }
		      }
		    ]
		  }
		}
		""";

	[Test]
	public async Task GetOperationAsync_ConcreteEndpoint_ReturnsTheOperationWhoseRouteItFills()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(new ApiScope("elasticsearch", null), new ApiEndpointKey("GET /my-index/_search")),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().NotBeNull();
		detail.Operation.OperationId.Should().Be("search");
		detail.Operation.Endpoints.Should().Equal("POST /_search", "GET /_search", "POST /{index}/_search", "GET /{index}/_search");
		detail.Versions.Should().Equal("main");
		detail.Fields.Should().ContainSingle().Which.Location.Should().Be("path");
		detail.Body.Should().Be("Get search hits that match the query.");
		detail.OtherMatches.Should().BeEmpty();
	}

	[Test]
	public async Task GetOperationAsync_EndpointNoRouteFits_ReturnsNull()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(null, new ApiEndpointKey("DELETE /my-index/_search")),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().BeNull();
	}

	[Test]
	public async Task GetOperationAsync_OperationId_ListsLaterHitsAsOtherMatches()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(null, new ApiOperationIdKey("get")),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().NotBeNull();
		detail.Operation.OperationId.Should().Be("get");
		detail.Operation.Group.Should().Be("Document");
		detail.OtherMatches.Select(o => o.OperationId).Should().Equal("search");
	}

	[Test]
	public async Task FindOperationsAsync_OperationId_ReturnsEveryMatchWithItsDetailsFromOneQuery()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var matches = await gateway.FindOperationsAsync(
			new ApiOperationLookup(null, new ApiOperationIdKey("get")),
			TestContext.Current!.Execution.CancellationToken
		);

		matches.Select(m => m.Operation.OperationId).Should().Equal("get", "search");
		matches.Should().AllSatisfy(m => m.Versions.Should().NotBeNull());
		matches.Should().AllSatisfy(m => m.OtherMatches.Should().BeEmpty());
		stub.RequestCount.Should().Be(1);
	}

	[Test]
	public async Task GetOperationAsync_OperationIdInVersion_FiltersByApiTreeAndOperationId()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		_ = await gateway.GetOperationAsync(
			new ApiOperationLookup(new ApiScope("elasticsearch", "9"), new ApiOperationIdKey("search")),
			TestContext.Current!.Execution.CancellationToken
		);

		stub
			.LastRequestBody
			.Should()
			.MatchInlineJsonSnapshot(
				"""
				{
				  "query": {
				    "bool": {
				      "filter": [
				        {
				          "bool": {
				            "filter": [
				              {
				                "bool": {
				                  "minimum_should_match": 1,
				                  "must_not": {
				                    "terms": {
				                      "path.keyword": [
				                        "/docs",
				                        "/docs/",
				                        "/docs/404",
				                        "/docs/404/"
				                      ]
				                    }
				                  },
				                  "should": [
				                    {
				                      "bool": {
				                        "must_not": [
				                          {
				                            "term": {
				                              "hidden": {
				                                "value": true
				                              }
				                            }
				                          },
				                          {
				                            "term": {
				                              "api.key": {
				                                "value": "elasticsearch"
				                              }
				                            }
				                          }
				                        ]
				                      }
				                    },
				                    {
				                      "bool": {
				                        "filter": [
				                          {
				                            "term": {
				                              "api.key": {
				                                "value": "elasticsearch"
				                              }
				                            }
				                          },
				                          {
				                            "term": {
				                              "api.trees.tree": {
				                                "value": "9"
				                              }
				                            }
				                          }
				                        ]
				                      }
				                    }
				                  ]
				                }
				              },
				              {
				                "term": {
				                  "content_type": {
				                    "value": "api"
				                  }
				                }
				              },
				              {
				                "term": {
				                  "api.key": {
				                    "value": "elasticsearch"
				                  }
				                }
				              }
				            ],
				            "must_not": [
				              {
				                "term": {
				                  "api.landing": {
				                    "value": true
				                  }
				                }
				              },
				              {
				                "term": {
				                  "api.page": {
				                    "value": true
				                  }
				                }
				              }
				            ]
				          }
				        },
				        {
				          "term": {
				            "api.operation_id": {
				              "value": "search"
				            }
				          }
				        }
				      ]
				    }
				  },
				  "size": 10,
				  "_source": {
				    "includes": [
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
				      "api.trees",
				      "api.properties",
				      "ai_short_summary",
				      "body"
				    ]
				  }
				}
				"""
			);
	}

	[Test]
	public async Task SearchOperationsAsync_MapsHitsToOperations()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var page = await gateway.SearchOperationsAsync(
			new ApiOperationSearch("search", null, IncludeDeprecated: false, PageNumber: 1, PageSize: 10),
			TestContext.Current!.Execution.CancellationToken
		);

		page.Total.Should().Be(2);
		page.Hits.Select(h => h.Operation.OperationId).Should().Equal("get", "search");
		page.Hits.Should().AllSatisfy(h => h.Operation.Api.Should().Be("elasticsearch"));
	}

	[Test]
	public async Task SearchOperationsAsync_ExcludesDeprecatedOperationsAndLandingPagesByDefault()
	{
		using var stub = ElasticsearchStub.Start(SearchCandidates);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		_ = await gateway.SearchOperationsAsync(
			new ApiOperationSearch("search", null, IncludeDeprecated: false, PageNumber: 1, PageSize: 10),
			TestContext.Current!.Execution.CancellationToken
		);

		stub
			.LastRequestBody
			.Should()
			.Contain("""{"term":{"api.deprecated":{"value":true}}}""")
			.And
			.Contain("""{"term":{"api.landing":{"value":true}}}""");
	}

	[Test]
	public async Task GetOperationAsync_RouteOfOnlyParameters_MatchesByMethodAndPath()
	{
		using var stub = ElasticsearchStub.Start(IndexRoutes);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(new ApiScope("elasticsearch", null), new ApiEndpointKey("PUT /my-index")),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().NotBeNull();
		detail.Operation.OperationId.Should().Be("indices-create");
		detail.OtherMatches.Should().BeEmpty();
		stub.LastRequestBody.Should().Contain("""{"regexp":{"api.path":{"value":"/\\{[^/]\u002B\\}"}}}""");
	}

	[Test]
	[Arguments("GET /_bulk")]
	[Arguments("GET /_clusterr")]
	[Arguments("DELETE /_search")]
	public async Task GetOperationAsync_SystemRouteNoOperationServes_ReturnsNullRatherThanAnIndexOperation(string endpoint)
	{
		using var stub = ElasticsearchStub.Start(IndexRoutes);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(new ApiScope("elasticsearch", null), new ApiEndpointKey(endpoint)),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().BeNull();
		stub.LastRequestBody.Should().NotContain("regexp");
	}

	[Test]
	public async Task GetOperationAsync_AllIndices_MatchesTheIndexRoute()
	{
		using var stub = ElasticsearchStub.Start(IndexRoutes);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(new ApiScope("elasticsearch", null), new ApiEndpointKey("GET /_all")),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().NotBeNull();
		detail.Operation.OperationId.Should().Be("indices-get");
	}

	[Test]
	public async Task GetOperationAsync_RouteOfOnlyParametersWithoutMethod_ListsEveryOperationOnTheRoute()
	{
		using var stub = ElasticsearchStub.Start(IndexRoutes);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var detail = await gateway.GetOperationAsync(
			new ApiOperationLookup(new ApiScope("elasticsearch", null), new ApiEndpointKey("/my-index")),
			TestContext.Current!.Execution.CancellationToken
		);

		detail.Should().NotBeNull();
		(new[] { detail.Operation.OperationId })
			.Concat(detail.OtherMatches.Select(o => o.OperationId))
			.Should()
			.BeEquivalentTo("indices-get", "indices-create");
	}

	[Test]
	public async Task ListApisAsync_LandingRenderedPerVersion_ListsEveryVersionUnderTheVisiblePage()
	{
		using var stub = ElasticsearchStub.Start(LandingRenderings);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var apis = await gateway.ListApisAsync(TestContext.Current!.Execution.CancellationToken);

		var api = apis.Should().ContainSingle().Subject;
		api.Url.Should().Be("/docs/api/elasticsearch");
		api.Title.Should().Be("Elasticsearch API");
		api.Versions.Should().BeEquivalentTo("8", "main", "9");
	}

	[Test]
	public async Task SearchOperationsAsync_OverloadedCluster_ThrowsSearchUnavailable()
	{
		using var stub = ElasticsearchStub.Start(
			"""{"error":{"type":"es_rejected_execution_exception","reason":"rejected"},"status":429}""",
			statusCode: 429
		);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var search =
			() => gateway.SearchOperationsAsync(
				new ApiOperationSearch("search", null, IncludeDeprecated: false, PageNumber: 1, PageSize: 10),
				TestContext.Current!.Execution.CancellationToken
			);

		_ = await search.Should().ThrowAsync<SearchUnavailableException>();
	}

	[Test]
	public async Task SearchOperationsAsync_BadRequest_ThrowsInvalidOperation()
	{
		using var stub = ElasticsearchStub.Start(
			"""{"error":{"type":"parsing_exception","reason":"bad query"},"status":400}""",
			statusCode: 400
		);
		var gateway = new ApiGateway(stub.Accessor, NullLogger<ApiGateway>.Instance);

		var search =
			() => gateway.SearchOperationsAsync(
				new ApiOperationSearch("search", null, IncludeDeprecated: false, PageNumber: 1, PageSize: 10),
				TestContext.Current!.Execution.CancellationToken
			);

		_ = await search.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bad query*");
	}
}
