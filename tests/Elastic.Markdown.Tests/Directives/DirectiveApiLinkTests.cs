// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Hub;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class HeroApiLinkTests() : DirectiveTest<HeroBlock>(
	"""
	:::{hero}
	:title: Elasticsearch
	:description: Start with the [search API](https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search).
	:primary-action: [Bulk](https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-bulk)
	:secondary-action: [Install](https://www.elastic.co/downloads/elasticsearch)
	:::
	"""
)
{
	[Test]
	public void RecordsApiLinksFromActionsAndDescriptionOnly() =>
		Collector
			.ApiLinks
			.Select(l => l.Url)
			.Distinct()
			.Should()
			.BeEquivalentTo([
				"https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search",
				"https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-bulk"
			]);
}

[InheritsTests]
public class LinkCardApiLinkTests() : DirectiveTest<LinkCardBlock>(
	"""
	:::{link-card}
	title: Search
	link: https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search
	links:
	  - label: Elsewhere
	    url: https://www.elastic.co/docs/solutions/search
	:::
	"""
)
{
	[Test]
	public void RecordsOnlyTheApiTarget() =>
		Collector
			.ApiLinks
			.Select(l => l.Url)
			.Distinct()
			.Should()
			.Equal("https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-search");
}
