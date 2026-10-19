// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Clients.Elasticsearch;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Elastic.Transport.Extensions;

namespace Elastic.Documentation.Api.Tests;

public class SearchQueryBuilderTests
{
	private const string ConstantScorePrefix = """{"constant_score":{"boost":5,"filter":{"term":{"api.endpoints":{"value":""" + "\"";
	private const string ConstantScoreSuffix = "\"}}}}}";

	private static readonly ElasticsearchClient Client = new(new ElasticsearchClientSettings(new InMemoryRequestInvoker()));

	[Test]
	[Arguments("GET /_search", "_search")]
	[Arguments("_search", "_search")]
	[Arguments("/_cat/indices", "_cat/indices")]
	[Arguments("post   /{index}/_Search ", "{index}/_search")]
	[Arguments("search", "search")]
	public void BuildApiEndpointQuery_EndpointLikeInput_ReturnsConstantScoreTermOnApiEndpoints(string input, string expectedTerm)
	{
		var query = SearchQueryBuilder.BuildApiEndpointQuery(input);

		Client.RequestResponseSerializer.SerializeToString(query).Should().Be(ConstantScorePrefix + expectedTerm + ConstantScoreSuffix);
	}

	[Test]
	[Arguments("   ")]
	[Arguments("GET")]
	[Arguments("/")]
	public void BuildApiEndpointQuery_NothingLeftAfterNormalizing_ReturnsNull(string input) =>
		SearchQueryBuilder.BuildApiEndpointQuery(input).Should().BeNull();

	[Test]
	public void BuildLexicalQuery_EndpointQuery_IncludesApiEndpointsTerm()
	{
		var query = SearchQueryBuilder.BuildLexicalQuery("_search", new Dictionary<string, string[]>(), [], null);

		var json = Client.RequestResponseSerializer.SerializeToString(query);

		json.Should().Contain("""{"term":{"api.endpoints":{"value":"_search"}}}""");
	}
}
