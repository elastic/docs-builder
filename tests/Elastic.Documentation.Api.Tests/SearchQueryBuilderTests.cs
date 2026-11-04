// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Elastic.Transport.Extensions;

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
			.Be("""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["_search"]}}}}]}}""");

	[Test]
	public void BuildApiEndpointQuery_ValueThenEndpoint_ScoresFullTermAboveWildcardTemplates() =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildApiEndpointQuery("my-index/_search"))
			.Should()
			.Be(
				"""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["my-index/_search"]}}}},{"constant_score":{"boost":3.3333333,"filter":{"terms":{"api.endpoints":["*/_search","my-index/*"]}}}}]}}"""
			);

	[Test]
	public void BuildApiEndpointQuery_InteriorValue_MatchesTemplateAndNeverTheParentPrefix()
	{
		var json = Client.RequestResponseSerializer.SerializeToString(
			SearchQueryBuilder.BuildApiEndpointQuery("_security/user/alice/_password")
		);
		using var doc = JsonDocument.Parse(json);
		var clauses = doc
			.RootElement
			.GetProperty("dis_max")
			.GetProperty("queries")
			.EnumerateArray()
			.Select(q => q.GetProperty("constant_score"))
			.ToArray();
		var terms = clauses.Select(
			c => c
				.GetProperty("filter")
				.GetProperty("terms")
				.GetProperty("api.endpoints")
				.EnumerateArray()
				.Select(t => t.GetString())
				.ToArray()
		).ToArray();

		clauses.Select(c => c.GetProperty("boost").GetSingle()).Should().Equal(5f, 4f, 3f, 2f);
		terms[1].Should().Contain("_security/user/*/_password");
		terms.SelectMany(t => t).Should().NotContain("_security/user");
	}

	[Test]
	[Arguments("   ")]
	[Arguments("GET")]
	[Arguments("/")]
	[Arguments("POST /{index}")]
	public void BuildApiEndpointQuery_NoLiteralSegmentAfterNormalizing_ReturnsNull(string input) =>
		SearchQueryBuilder.BuildApiEndpointQuery(input).Should().BeNull();

	[Test]
	[Arguments("search")]
	[Arguments("  Search ")]
	public void BuildBareWordEndpointQuery_Word_TargetsUnderscoreEndpoint(string input) =>
		Client
			.RequestResponseSerializer
			.SerializeToString(SearchQueryBuilder.BuildBareWordEndpointQuery(input))
			.Should()
			.Be("""{"dis_max":{"queries":[{"constant_score":{"boost":5,"filter":{"terms":{"api.endpoints":["_search"]}}}}]}}""");

	[Test]
	[Arguments("_search")]
	[Arguments("run a search")]
	[Arguments("index/_search")]
	[Arguments("GET /_search")]
	[Arguments("")]
	public void BuildBareWordEndpointQuery_NotABareWord_ReturnsNull(string input) =>
		SearchQueryBuilder.BuildBareWordEndpointQuery(input).Should().BeNull();

	[Test]
	[Arguments("search")]
	[Arguments("create deployment")]
	[Arguments("")]
	public void BuildLoosePartialMatchQuery_FewerThanThreeWords_ReturnsNull(string input) =>
		SearchQueryBuilder.BuildLoosePartialMatchQuery(input).Should().BeNull();

	[Test]
	public void BuildLoosePartialMatchQuery_ThreeWords_MatchesAnyWordsAtLowerWeightWithDocumentFilter()
	{
		var json = Client.RequestResponseSerializer.SerializeToString(
			SearchQueryBuilder.BuildLoosePartialMatchQuery("create deployment cloud")
		);

		json.Should().Contain("\"operator\":\"or\"");
		json.Should().Contain("\"minimum_should_match\":\"75%\"");
		json.Should().Contain("\"boost\":0.5");
		json.Should().Contain("\"hidden\"");
	}

	[Test]
	public void ApplyApiRanking_ThreeWords_OrsStrictAndLooseQueries()
	{
		var strict = SearchQueryBuilder.BuildLexicalQuery("create deployment cloud", new Dictionary<string, string[]>(), [], null);

		var json = Client.RequestResponseSerializer.SerializeToString(
			SearchQueryBuilder.ApplyApiRanking(strict, "create deployment cloud")
		);

		json.Should().StartWith("""{"bool":{"minimum_should_match":1,"should":[""");
		json.Should().Contain("\"minimum_should_match\":\"75%\"");
	}

	[Test]
	public void ApplyApiRanking_TwoWords_ReturnsStrictQueryUnchanged()
	{
		var strict = SearchQueryBuilder.BuildLexicalQuery("create deployment", new Dictionary<string, string[]>(), [], null);

		SearchQueryBuilder.ApplyApiRanking(strict, "create deployment").Should().BeSameAs(strict);
	}

	[Test]
	public void BuildLexicalQuery_EndpointQuery_IncludesApiEndpointsTerms()
	{
		var query = SearchQueryBuilder.BuildLexicalQuery("_search", new Dictionary<string, string[]>(), [], null);

		var json = Client.RequestResponseSerializer.SerializeToString(query);

		json.Should().Contain("""{"terms":{"api.endpoints":["_search"]}}""");
	}
}
