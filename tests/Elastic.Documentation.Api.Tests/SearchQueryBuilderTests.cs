// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Elastic.Transport.Extensions;
using Nullean.Snapshots;
using Nullean.Snapshots.Json;

namespace Elastic.Documentation.Api.Tests;

public class SearchQueryBuilderTests
{
	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	[Test]
	[Arguments("GET /_search")]
	[Arguments("_search")]
	[Arguments("/_search")]
	public void BuildApiEndpointQuery_SingleSegment_ReturnsDisMaxOfOneFullBoostTerms(string input) =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildApiEndpointQuery(input))
			.Should()
			.MatchInlineJsonSnapshot(
				"""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["_search"]}}}}]}}"""
			);

	[Test]
	public void BuildApiEndpointQuery_ValueThenEndpoint_ScoresFullTermAboveWildcardTemplates() =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildApiEndpointQuery("my-index/_search"))
			.Should()
			.MatchInlineJsonSnapshot(
				"""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["my-index/_search"]}}}},{"constant_score":{"boost":3.3333333,"filter":{"terms":{"api.endpoints":["*/_search","my-index/*"]}}}}]}}"""
			);

	[Test]
	public void BuildApiEndpointQuery_InteriorValue_MatchesTemplateAndNeverTheParentPrefix() =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildApiEndpointQuery("_security/user/alice/_password"))
			.Should()
			.MatchInlineJsonSnapshot(
				"""
				{
				  "dis_max": {
				    "queries": [
				      {
				        "constant_score": {
				          "boost": 5,
				          "filter": {
				            "terms": {
				              "api.endpoints": [
				                "_security/user/alice/_password"
				              ]
				            }
				          }
				        }
				      },
				      {
				        "constant_score": {
				          "boost": 4,
				          "filter": {
				            "terms": {
				              "api.endpoints": [
				                "*/user/alice/_password",
				                "_security/*/alice/_password",
				                "_security/user/*/_password",
				                "_security/user/alice/*"
				              ]
				            }
				          }
				        }
				      },
				      {
				        "constant_score": {
				          "boost": 3,
				          "filter": {
				            "terms": {
				              "api.endpoints": [
				                "*/*/alice/_password",
				                "*/user/*/_password",
				                "*/user/alice/*",
				                "_security/*/*/_password",
				                "_security/*/alice/*",
				                "_security/user/*/*"
				              ]
				            }
				          }
				        }
				      },
				      {
				        "constant_score": {
				          "boost": 2,
				          "filter": {
				            "terms": {
				              "api.endpoints": [
				                "*/*/*/_password",
				                "*/*/alice/*",
				                "*/user/*/*",
				                "_security/*/*/*"
				              ]
				            }
				          }
				        }
				      }
				    ]
				  }
				}
				"""
			);

	[Test]
	[Arguments("   ")]
	[Arguments("GET")]
	[Arguments("POST /{index}")]
	public void BuildApiEndpointQuery_NoLiteralSegmentAfterNormalizing_ReturnsNull(string input) =>
		SearchQueryBuilder.BuildApiEndpointQuery(input).Should().BeNull();

	[Test]
	[Arguments("/")]
	[Arguments("HEAD /")]
	public void BuildApiEndpointQuery_RootRoute_ReturnsTheRootTerm(string input) =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildApiEndpointQuery(input))
			.Should()
			.MatchInlineJsonSnapshot(
				"""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["/"]}}}}]}}"""
			);

	[Test]
	[Arguments("search")]
	[Arguments("  Search ")]
	public void BuildBareWordEndpointQuery_Word_TargetsUnderscoreEndpoint(string input) =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildBareWordEndpointQuery(input))
			.Should()
			.MatchInlineJsonSnapshot(
				"""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["_search"]}}}}]}}"""
			);

	[Test]
	[Arguments("_search")]
	[Arguments("run a search")]
	[Arguments("index/_search")]
	[Arguments("GET /_search")]
	[Arguments("")]
	public void BuildBareWordEndpointQuery_NotABareWord_ReturnsNull(string input) =>
		SearchQueryBuilder.BuildBareWordEndpointQuery(input).Should().BeNull();

	[Test]
	public void BuildLexicalQuery_EndpointQuery_IncludesApiEndpointsTerms() =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildLexicalQuery("_search", new Dictionary<string, string[]>(), [], null))
			.Should()
			.MatchJsonSnapshot();
}
