// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Authoring.Tests.Blocks.Hub;

// {on-this-page} lists the hub sections that render as an H2, in page order. A card group
// inside {explore} is an accordion, and an {explore} at :level: 3 is an H3, so neither is listed.

public class OnThisPageWithAllSectionKinds : MarkdownTest
{
	protected override string Markdown =>
		"""
		:::{on-this-page}
		:::

		:::{get-started}
		title: Get started with Kibana
		steps:
		  - title: Install
		    description: Install it.
		:::

		:::{whats-new}
		title: What's new
		id: whats-new
		items:
		  - title: Hub pages
		    description: A product-scoped landing page.
		    link: /index.md
		:::

		::::{card-group}
		:title: Solutions
		:id: solutions

		:::{link-card}
		title: Search
		:::
		::::

		:::::{explore}
		:id: explore
		:title: Explore Kibana docs

		::::{card-group}
		:title: Quick links
		:id: quick-links

		:::{link-card}
		title: Releases
		:::
		::::
		:::::

		:::::{explore}
		:id: use-kibana
		:title: Use Kibana
		:level: 3

		::::{card-group}
		:title: Analyze data
		:id: analyze

		:::{link-card}
		title: Discover
		:::
		::::
		:::::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtmlSnapshot();

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}

public class OnThisPageWithoutSections : MarkdownTest
{
	protected override string Markdown => """
		:::{on-this-page}
		:::
		""";

	[Test, DisplayName("renders HTML")]
	public async Task RendersHtml() => await Docs.ConvertsToHtml("");

	[Test, DisplayName("has no errors")]
	public async Task HasNoErrors() => await Docs.HasNoErrors();
}
