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
		ApiEndpointPath.IndexKeys(["/{index}/_search", "/_search"]).Should().Equal("*/_search", "_search");

	[Test]
	public void IndexKeys_TrailingParameter_ReturnsPathWithAndWithoutParameter() =>
		ApiEndpointPath.IndexKeys(["/_cat/indices/{index}"]).Should().Equal("_cat/indices/*", "_cat/indices");

	[Test]
	public void IndexKeys_LeadingAndTrailingParameters_ReturnsFullLeadingTrailingAndBothStripped() =>
		ApiEndpointPath.IndexKeys(["/{index}/_doc/{id}"]).Should().Equal("*/_doc/*", "_doc/*", "*/_doc", "_doc");

	[Test]
	public void IndexKeys_InteriorParameter_KeepsItAsWildcard() =>
		ApiEndpointPath.IndexKeys(["/_security/user/{username}/_password"]).Should().Equal("_security/user/*/_password");

	[Test]
	public void IndexKeys_OnlyParameter_ReturnsEmpty() => ApiEndpointPath.IndexKeys(["/{index}"]).Should().BeEmpty();

	[Test]
	public void IndexKeys_OnlyParameters_ReturnsEmpty() => ApiEndpointPath.IndexKeys(["/{index}/{type}"]).Should().BeEmpty();

	[Test]
	public void QueryCandidates_SingleSegment_ReturnsOneLiteralGroup()
	{
		var groups = ApiEndpointPath.QueryCandidates("_search");

		groups.Select(g => g.LiteralSegments).Should().Equal(1);
		groups[0].Terms.Should().Equal("_search");
	}

	[Test]
	public void QueryCandidates_ValueThenEndpoint_ReturnsFullTermThenEachSegmentAsWildcard()
	{
		var groups = ApiEndpointPath.QueryCandidates("my-index/_search");

		groups.Select(g => g.LiteralSegments).Should().Equal(2, 1);
		groups[0].Terms.Should().Equal("my-index/_search");
		groups[1].Terms.Should().Equal("*/_search", "my-index/*");
	}

	[Test]
	public void QueryCandidates_InteriorValue_ReturnsEveryTemplateGroupedByLiteralsDescending()
	{
		var groups = ApiEndpointPath.QueryCandidates("_security/user/alice/_password");

		groups.Select(g => g.LiteralSegments).Should().Equal(4, 3, 2, 1);
		groups[0].Terms.Should().Equal("_security/user/alice/_password");
		groups[1]
			.Terms
			.Should()
			.Equal("*/user/alice/_password", "_security/*/alice/_password", "_security/user/*/_password", "_security/user/alice/*");
		groups[2]
			.Terms
			.Should()
			.Equal(
				"*/*/alice/_password",
				"*/user/*/_password",
				"*/user/alice/*",
				"_security/*/*/_password",
				"_security/*/alice/*",
				"_security/user/*/*"
			);
		groups[3].Terms.Should().Equal("*/*/*/_password", "*/*/alice/*", "*/user/*/*", "_security/*/*/*");
	}

	[Test]
	public void QueryCandidates_TypedWildcard_CountsAsParameterWithoutDuplicates()
	{
		var groups = ApiEndpointPath.QueryCandidates("*/_search");

		groups.Select(g => g.LiteralSegments).Should().Equal(1);
		groups[0].Terms.Should().Equal("*/_search");
	}

	[Test]
	public void QueryCandidates_Empty_ReturnsEmpty() => ApiEndpointPath.QueryCandidates("").Should().BeEmpty();

	[Test]
	[Arguments("GET /_search", "_search")]
	[Arguments("post   /{index}/_Search ", "*/_search")]
	[Arguments("{index}/_doc/{id}", "*/_doc/*")]
	[Arguments("GET", "")]
	[Arguments("/", "")]
	[Arguments("getting started", "getting started")]
	public void NormalizeQuery_TypedQuery_StripsMethodAndLeadingSlashLowercasesAndWildcardsParameters(string input, string expected) =>
		ApiEndpointPath.NormalizeQuery(input).Should().Be(expected);
}
