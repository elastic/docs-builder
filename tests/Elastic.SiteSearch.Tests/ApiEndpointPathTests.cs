// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search.Contract;

namespace Elastic.SiteSearch.Tests;

public class ApiEndpointPathTests
{
	[Test]
	public void IndexKeys_LeadingParameterWithAlternatePath_ReturnsOwnPathThenAlternatesDeduped() =>
		ApiEndpointPath.IndexKeys(["/{index}/_search", "/_search"]).Should().Equal("{index}/_search", "_search");

	[Test]
	public void IndexKeys_TrailingParameter_ReturnsPathWithAndWithoutParameter() =>
		ApiEndpointPath.IndexKeys(["/_cat/indices/{index}"]).Should().Equal("_cat/indices/{index}", "_cat/indices");

	[Test]
	public void IndexKeys_LeadingAndTrailingParameters_ReturnsFullLeadingTrailingAndBothStripped() =>
		ApiEndpointPath.IndexKeys(["/{index}/_doc/{id}"]).Should().Equal("{index}/_doc/{id}", "_doc/{id}", "{index}/_doc", "_doc");

	[Test]
	public void IndexKeys_OnlyParameter_ReturnsFullPathOnly() => ApiEndpointPath.IndexKeys(["/{index}"]).Should().Equal("{index}");

	[Test]
	public void QueryCandidates_ValueThenEndpoint_ReturnsFullRunThenSingleSegments() =>
		ApiEndpointPath
			.QueryCandidates("my-index/_search")
			.Should()
			.Equal(new EndpointCandidate("my-index/_search", 2), new EndpointCandidate("my-index", 1), new EndpointCandidate("_search", 1));

	[Test]
	public void QueryCandidates_ThreeSegments_ReturnsRunsLongestFirstThenLeftmostFirst() =>
		ApiEndpointPath
			.QueryCandidates("_cat/indices/logs-2026")
			.Should()
			.Equal(
				new EndpointCandidate("_cat/indices/logs-2026", 3),
				new EndpointCandidate("_cat/indices", 2),
				new EndpointCandidate("indices/logs-2026", 2),
				new EndpointCandidate("_cat", 1),
				new EndpointCandidate("indices", 1),
				new EndpointCandidate("logs-2026", 1)
			);

	[Test]
	public void QueryCandidates_SingleSegment_ReturnsThatSegment() =>
		ApiEndpointPath.QueryCandidates("_search").Should().Equal(new EndpointCandidate("_search", 1));

	[Test]
	public void QueryCandidates_Empty_ReturnsEmpty() => ApiEndpointPath.QueryCandidates("").Should().BeEmpty();

	[Test]
	[Arguments("GET /_search", "_search")]
	[Arguments("post   /{index}/_Search ", "{index}/_search")]
	[Arguments("GET", "")]
	[Arguments("/", "")]
	[Arguments("getting started", "getting started")]
	public void NormalizeQuery_TypedQuery_StripsMethodAndLeadingSlashAndLowercases(string input, string expected) =>
		ApiEndpointPath.NormalizeQuery(input).Should().Be(expected);
}
