// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class ApiMarkdownFrontMatterTests
{
	[Test]
	public void Write_EmitsLlmAndOkfFields()
	{
		var markdown = ApiMarkdownFrontMatter.Write(
			"# Run a search\n\nReturns hits.\n",
			new ApiPageFrontMatter(
				"Run a search",
				"Returns hits that match the query",
				"https://cdn.example/api/doc/elasticsearch/operation/operation-search",
				"Elasticsearch"
			)
		);

		markdown.Should().StartWith("---");
		markdown.Should().Contain("type: api");
		markdown.Should().Contain("title: Run a search");
		markdown.Should().Contain("description: Returns hits that match the query");
		markdown.Should().NotContain("navigation_title:");
		markdown.Should().Contain("url: https://cdn.example/api/doc/elasticsearch/operation/operation-search");
		markdown.Should().Contain("resource: https://cdn.example/api/doc/elasticsearch/operation/operation-search");
		markdown.Should().Contain("products:");
		markdown.Should().Contain("  - Elasticsearch");
		markdown.Should().NotContain("applies_to:");
		markdown.Should().Contain("# Run a search");
	}

	[Test]
	public void Write_OmitsOptionalKeysWhenMissing()
	{
		var markdown = ApiMarkdownFrontMatter.Write("# API Explorer\n", new ApiPageFrontMatter("API Explorer", null, "/api", null));

		markdown.Should().Contain("type: api");
		markdown.Should().Contain("title: API Explorer");
		markdown.Should().Contain("url: /api");
		markdown.Should().Contain("resource: /api");
		markdown.Should().NotContain("navigation_title:");
		markdown.Should().NotContain("description:");
		markdown.Should().NotContain("products:");
		markdown.Should().NotContain("applies_to:");
	}

	[Test]
	public void Write_EmitsRailFactsWhenPresent()
	{
		var facts = new ApiRailFacts(
			"1.2",
			"Apache 2.0: see terms",
			["https://api.example.com", "{scheme}://x"],
			"https://cdn.example/api/doc/es.json",
			"https://cdn.example/api/doc/es.yaml"
		);

		var markdown = ApiMarkdownFrontMatter.Write(
			"# Elasticsearch\n",
			new ApiPageFrontMatter("Elasticsearch", null, "/api", null, facts)
		);

		markdown.Should().Contain("version: \"1.2\"");
		markdown.Should().Contain("license: \"Apache 2.0: see terms\"");
		markdown.Should().Contain("servers:\n  - \"https://api.example.com\"\n  - \"{scheme}://x\"");
		markdown.Should().Contain("spec:\n  json: https://cdn.example/api/doc/es.json\n  yaml: https://cdn.example/api/doc/es.yaml");
	}

	[Test]
	public void Write_OmitsEmptyRailFacts()
	{
		var facts = new ApiRailFacts(null, null, [], null, null);

		var markdown = ApiMarkdownFrontMatter.Write("# X\n", new ApiPageFrontMatter("X", null, "/api", null, facts));

		markdown.Should().NotContain("version:").And.NotContain("license:").And.NotContain("servers:").And.NotContain("spec:");
	}

	[Test]
	public void StripLeadingFrontMatter_RemovesAuthoredYaml()
	{
		var source =
			"""
			---
			navigation_title: Spaces
			---
			# Kibana spaces

			Spaces enable you to organize dashboards.
			""";

		var stripped = ApiMarkdownFrontMatter.StripLeadingFrontMatter(source);

		stripped.Should().StartWith("# Kibana spaces");
		stripped.Should().NotContain("navigation_title:");
	}
}
