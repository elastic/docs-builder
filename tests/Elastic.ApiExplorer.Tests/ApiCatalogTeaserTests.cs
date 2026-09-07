// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;

namespace Elastic.ApiExplorer.Tests;

public class ApiCatalogTeaserTests
{
	[Fact]
	public void From_NullOrBlank_ReturnsNull()
	{
		ApiCatalogTeaser.From(null).Should().BeNull();
		ApiCatalogTeaser.From("   ").Should().BeNull();
	}

	[Fact]
	public void From_TakesFirstParagraphOnly()
	{
		var description =
			"""
			Elasticsearch is a distributed search and analytics engine.

			It also stores documents and runs aggregations.
			""";

		ApiCatalogTeaser.From(description).Should().Be("Elasticsearch is a distributed search and analytics engine.");
	}

	[Fact]
	public void From_StripsLightMarkdown()
	{
		var description = "The **Kibana** [`saved objects`](https://example.com/so) API.";

		ApiCatalogTeaser.From(description).Should().Be("The Kibana saved objects API.");
	}

	[Fact]
	public void From_StopsAtHeadingWithoutBlankLine()
	{
		var description =
			"""
			Elasticsearch provides REST APIs that are used by the UI components.
			## Documentation source and versions
			This documentation is generated from the OpenAPI spec.
			""";

		ApiCatalogTeaser.From(description).Should().Be("Elasticsearch provides REST APIs that are used by the UI components.");
	}
}
