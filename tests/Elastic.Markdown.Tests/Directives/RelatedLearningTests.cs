// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.RelatedLearning;

namespace Elastic.Markdown.Tests.Directives;

public class RelatedLearningBasicTests() : DirectiveTest<RelatedLearningBlock>("""
:::{related-learning} apm-with-elastic
:::
""")
{
	[Test]
	public void ResolvesCatalogId()
	{
		Block!.Items.Should().ContainSingle();
		var item = Block.Items[0];
		item.Id.Should().Be("apm-with-elastic");
		item.Title.Should().NotBeNullOrWhiteSpace();
		item.Url.Should().NotBeNullOrWhiteSpace();

		Set.Context.RelatedLearningConfiguration.TryGet("apm-with-elastic", out var catalog).Should().BeTrue();
		item.Title.Should().Be(catalog!.Title);
		item.Url.Should().Be(catalog.Url);
	}

	[Test]
	public void DefaultsHeadingAndSlug()
	{
		Block!.Heading.Should().Be(RelatedLearningBlock.DefaultHeading);
		Block.Slug.Should().Be(RelatedLearningBlock.DefaultSlug);
	}

	[Test]
	public void RendersHeadingIdAndExternalLink()
	{
		var item = Block!.Items.Should().ContainSingle().Which;
		Html.Should().Contain("id=\"related-learning-heading\"");
		Html.Should().Contain("<h2>");
		Html.Should().Contain("Related learning");
		Html.Should().Contain($"href=\"{item.Url}\"");
		Html.Should().Contain("target=\"_blank\"");
		Html.Should().Contain("rel=\"noopener noreferrer\"");
		Html.Should().Contain(item.Title);
		CountOccurrences(Html, "id=\"related-learning-heading\"").Should().Be(1);
	}

	[Test]
	public void PageTocIncludesDefaultSlug()
	{
		File.PageTableOfContent.Should().ContainKey(RelatedLearningBlock.DefaultSlug);
		File.PageTableOfContent[RelatedLearningBlock.DefaultSlug].Heading.Should().Be(RelatedLearningBlock.DefaultHeading);
		File.PageTableOfContent[RelatedLearningBlock.DefaultSlug].Level.Should().Be(2);
	}

	[Test]
	public void EmitsNoDiagnostics() => Collector.Diagnostics.Should().BeEmpty();

	private static int CountOccurrences(string haystack, string needle)
	{
		var count = 0;
		var index = 0;
		while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += needle.Length;
		}
		return count;
	}
}

public class RelatedLearningOrderTests() : DirectiveTest<RelatedLearningBlock>(
	"""
:::{related-learning} index-basics, apm-with-elastic
:::
"""
)
{
	[Test]
	public void DisplayOrderMatchesIds()
	{
		Block!.Items.Select(i => i.Id).Should().Equal("index-basics", "apm-with-elastic");
		var indexPos = Html.IndexOf(Block.Items[0].Title, StringComparison.Ordinal);
		var apmPos = Html.IndexOf(Block.Items[1].Title, StringComparison.Ordinal);
		indexPos.Should().BePositive();
		apmPos.Should().BeGreaterThan(indexPos);
	}
}

public class RelatedLearningHeadingOverrideTests() : DirectiveTest<RelatedLearningBlock>(
	"""
:::{related-learning} elastic-agent
:heading: Learn Elastic Agent
:::
"""
)
{
	[Test]
	public void UsesCustomHeadingAndSlugifiedAnchor()
	{
		Block!.Heading.Should().Be("Learn Elastic Agent");
		Block.Slug.Should().Be("learn-elastic-agent");
		Html.Should().Contain("id=\"learn-elastic-agent\"");
		Html.Should().Contain("Learn Elastic Agent");
		File.PageTableOfContent.Should().ContainKey("learn-elastic-agent");
		CountId(Html, "learn-elastic-agent").Should().Be(1);
	}

	private static int CountId(string html, string id)
	{
		var needle = $"id=\"{id}\"";
		var count = 0;
		var index = 0;
		while ((index = html.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += needle.Length;
		}
		return count;
	}
}

public class RelatedLearningUnknownIdTests() : DirectiveTest<RelatedLearningBlock>("""
:::{related-learning} not-a-module
:::
""")
{
	[Test]
	public void EmitsErrorAndRendersNothing()
	{
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("unknown catalog ID 'not-a-module'"));
		Block!.Items.Should().BeEmpty();
		Html.Should().NotContain("related-learning");
		File.PageTableOfContent.Should().NotContainKey(RelatedLearningBlock.DefaultSlug);
	}
}

public class RelatedLearningDuplicateIdTests() : DirectiveTest<RelatedLearningBlock>(
	"""
:::{related-learning} apm-with-elastic, apm-with-elastic
:::
"""
)
{
	[Test]
	public void WarnsAndKeepsFirstOccurrence()
	{
		Block!.Items.Should().ContainSingle().Which.Id.Should().Be("apm-with-elastic");
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Warning && d.Message.Contains("duplicate catalog ID 'apm-with-elastic'"));
	}
}

public class RelatedLearningEmptyIdsTests() : DirectiveTest<RelatedLearningBlock>("""
:::{related-learning}
:::
""")
{
	[Test]
	public void EmitsErrorWhenArgumentMissing()
	{
		Collector
			.Diagnostics
			.Should()
			.Contain(d => d.Severity == Severity.Error && d.Message.Contains("requires at least one catalog ID as an argument"));
		Block!.Items.Should().BeEmpty();
	}
}
