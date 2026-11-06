// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.Directives.Hub;
using Elastic.Markdown.Tests.Inline;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class HeroApiLinkTests() : DirectiveTest<HeroBlock>(
	"""
	:::{hero}
	:title: Kibana
	:description: Start with the [saved objects API](https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing-description).
	:primary-action: [Create](https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing-action)
	:secondary-action: [Existing](https://www.elastic.co/docs/api/doc/kibana/operation/operation-exists)
	:tertiary-action: [Install](https://www.elastic.co/downloads/kibana)
	:::
	"""
)
{
	[Test]
	public void WarnsForMissingApiPagesInTheDescriptionAndActions()
	{
		Collector.HasApiWarning("operation-missing-description");
		Collector.HasApiWarning("operation-missing-action");
	}

	[Test]
	public void DoesNotWarnForExistingOrOtherLinks()
	{
		Collector.HasNoApiWarningFor("operation-exists");
		Collector.HasNoApiWarningFor("downloads");
	}
}

[InheritsTests]
public class LinkCardApiLinkTests() : DirectiveTest<LinkCardBlock>(
	"""
	:::{link-card}
	title: Search
	link: https://www.elastic.co/docs/api/doc/kibana/operation/operation-missing-card
	links:
	  - label: Existing
	    url: https://www.elastic.co/docs/api/doc/kibana/operation/operation-exists
	:::
	"""
)
{
	[Test]
	public void WarnsForTheMissingTarget() => Collector.HasApiWarning("operation-missing-card");

	[Test]
	public void DoesNotWarnForTheExistingTarget() => Collector.HasNoApiWarningFor("operation-exists");
}
