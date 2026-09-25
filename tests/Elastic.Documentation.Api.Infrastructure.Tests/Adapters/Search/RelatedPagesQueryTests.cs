// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Search;

namespace Elastic.Documentation.Api.Infrastructure.Tests.Adapters.Search;

public class RelatedPagesQueryTests
{
	[Theory]
	[InlineData("/guide/en/elasticsearch/reference/current/index-lifecycle-management.html", "elasticsearch reference index lifecycle management")]
	[InlineData("https://www.elastic.co/docs/8.18/deploy-manage/snapshots/index.html", "deploy manage snapshots")]
	[InlineData("/docs/explore-analyze/query-filter/languages/es%7Cql", "explore analyze query filter languages es ql")]
	[InlineData("/docs/", "")]
	public void FromPath_NormalizesLegacyUrl_ExpectedQuery(string path, string expected) =>
		RelatedPagesQuery.FromPath(path).Should().Be(expected);

	[Fact]
	public void FromPath_LongPath_LimitsQueryTerms()
	{
		var path = "/one/two/three/four/five/six/seven/eight/nine/ten/eleven/twelve/thirteen/fourteen";

		RelatedPagesQuery.FromPath(path).Split(' ').Should().HaveCount(12);
		RelatedPagesQuery.FromPath(path).Should().StartWith("three");
	}
}
