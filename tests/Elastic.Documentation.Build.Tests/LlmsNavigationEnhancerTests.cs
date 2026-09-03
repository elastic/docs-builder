// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Landing;
using Elastic.Documentation.Assembler.Navigation;

namespace Elastic.Documentation.Build.Tests;

public class LlmsNavigationEnhancerTests
{
	private static readonly Uri CanonicalBaseUrl = new("https://www.elastic.co");

	private static readonly ApiCatalogEntry[] Catalog =
	[
		new("kibana", "Kibana", "/docs/api/doc/kibana/"),
		new("elasticsearch", "Elasticsearch", "/docs/api/doc/elasticsearch/")
	];

	[Fact]
	public void GenerateApiHubIndex_ListsSortedLandingMarkdownUrls()
	{
		var text = new LlmsNavigationEnhancer().GenerateApiHubIndex(Catalog, CanonicalBaseUrl);

		text.Should().StartWith("# APIs");
		text.Should().Contain("## Products");
		text.Should().Contain("* [Elasticsearch](https://www.elastic.co/docs/api/doc/elasticsearch.md)");
		text.Should().Contain("* [Kibana](https://www.elastic.co/docs/api/doc/kibana.md)");
		text.IndexOf("elasticsearch.md", StringComparison.Ordinal).Should().BeLessThan(text.IndexOf("kibana.md", StringComparison.Ordinal));
	}

	[Fact]
	public void GenerateApiSection_StartsWithApisHeadingAndSameLinks()
	{
		var text = new LlmsNavigationEnhancer().GenerateApiSection(Catalog, CanonicalBaseUrl);

		text.Should().StartWith("## APIs");
		text.Should().Contain("* [Elasticsearch](https://www.elastic.co/docs/api/doc/elasticsearch.md)");
		text.Should().Contain("* [Kibana](https://www.elastic.co/docs/api/doc/kibana.md)");
	}

	[Fact]
	public void GenerateApiHubIndex_EmptyCatalog_ReturnsEmpty() =>
		new LlmsNavigationEnhancer().GenerateApiHubIndex([], CanonicalBaseUrl).Should().BeEmpty();

	[Fact]
	public void GenerateApiSection_EmptyCatalog_ReturnsEmpty() =>
		new LlmsNavigationEnhancer().GenerateApiSection([], CanonicalBaseUrl).Should().BeEmpty();
}
