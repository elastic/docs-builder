// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Nullean.Snapshots;

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

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: Run a search
			description: Returns hits that match the query
			url: https://cdn.example/api/doc/elasticsearch/operation/operation-search
			resource: https://cdn.example/api/doc/elasticsearch/operation/operation-search
			products:
			  - Elasticsearch
			---

			# Run a search

			Returns hits.
			"""
		);
	}

	[Test]
	public void Write_OmitsOptionalKeysWhenMissing()
	{
		var markdown = ApiMarkdownFrontMatter.Write("# API Explorer\n", new ApiPageFrontMatter("API Explorer", null, "/api", null));

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: API Explorer
			url: /api
			resource: /api
			---

			# API Explorer
			"""
		);
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

		// AppendLine writes the platform newline; normalise so the multi-line assertions hold on Windows too.
		var markdown = ApiMarkdownFrontMatter.Write(
			"# Elasticsearch\n",
			new ApiPageFrontMatter("Elasticsearch", null, "/api", null, facts)
		).ReplaceLineEndings("\n");

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: Elasticsearch
			url: /api
			resource: /api
			version: "1.2"
			license: "Apache 2.0: see terms"
			servers:
			  - "https://api.example.com"
			  - "{scheme}://x"
			spec:
			  json: https://cdn.example/api/doc/es.json
			  yaml: https://cdn.example/api/doc/es.yaml
			---

			# Elasticsearch
			"""
		);
	}

	[Test]
	public void Write_OmitsEmptyRailFacts()
	{
		var facts = new ApiRailFacts(null, null, [], null, null);

		var markdown = ApiMarkdownFrontMatter.Write("# X\n", new ApiPageFrontMatter("X", null, "/api", null, facts));

		markdown.Should().MatchInlineSnapshot(
			"""
			---
			type: api
			title: X
			url: /api
			resource: /api
			---

			# X
			"""
		);
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

		stripped.Should().MatchInlineSnapshot(
			"""
			# Kibana spaces

			Spaces enable you to organize dashboards.
			"""
		);
	}
}
