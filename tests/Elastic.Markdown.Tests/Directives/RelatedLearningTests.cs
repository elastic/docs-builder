// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst.Directives.RelatedLearning;

namespace Elastic.Markdown.Tests.Directives;

public class RelatedLearningBasicTests(ITestOutputHelper output) : DirectiveTest<RelatedLearningBlock>(output,
"""
:::{related-learning}
:ids: apm-with-elastic
:::
"""
)
{
	[Fact]
	public void ResolvesCatalogId()
	{
		Block!.Items.Should().ContainSingle();
		Block.Items[0].Id.Should().Be("apm-with-elastic");
		Block.Items[0].Title.Should().Be("APM with Elastic");
		Block.Items[0].Url.Should().Be("https://www.elastic.co/training/apm-with-elastic");
	}

	[Fact]
	public void DefaultsHeadingAndSlug()
	{
		Block!.Heading.Should().Be(RelatedLearningBlock.DefaultHeading);
		Block.Slug.Should().Be(RelatedLearningBlock.DefaultSlug);
	}

	[Fact]
	public void RendersHeadingIdAndExternalLink()
	{
		Html.Should().Contain("id=\"related-learning-heading\"");
		Html.Should().Contain("<h2>");
		Html.Should().Contain("Related learning");
		Html.Should().Contain("href=\"https://www.elastic.co/training/apm-with-elastic\"");
		Html.Should().Contain("target=\"_blank\"");
		Html.Should().Contain("rel=\"noopener noreferrer\"");
		Html.Should().Contain("APM with Elastic");
		CountOccurrences(Html, "id=\"related-learning-heading\"").Should().Be(1);
	}

	[Fact]
	public void PageTocIncludesDefaultSlug()
	{
		File.PageTableOfContent.Should().ContainKey(RelatedLearningBlock.DefaultSlug);
		File.PageTableOfContent[RelatedLearningBlock.DefaultSlug].Heading.Should().Be(RelatedLearningBlock.DefaultHeading);
		File.PageTableOfContent[RelatedLearningBlock.DefaultSlug].Level.Should().Be(2);
	}

	[Fact]
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

public class RelatedLearningOrderTests(ITestOutputHelper output) : DirectiveTest<RelatedLearningBlock>(output,
"""
:::{related-learning}
:ids: index-basics, apm-with-elastic
:::
"""
)
{
	[Fact]
	public void DisplayOrderMatchesIds()
	{
		Block!.Items.Select(i => i.Id).Should().Equal("index-basics", "apm-with-elastic");
		var indexPos = Html.IndexOf("Index Basics", StringComparison.Ordinal);
		var apmPos = Html.IndexOf("APM with Elastic", StringComparison.Ordinal);
		indexPos.Should().BePositive();
		apmPos.Should().BeGreaterThan(indexPos);
	}
}

public class RelatedLearningHeadingOverrideTests(ITestOutputHelper output) : DirectiveTest<RelatedLearningBlock>(output,
"""
:::{related-learning}
:ids: elastic-agent
:heading: Learn Elastic Agent
:::
"""
)
{
	[Fact]
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

public class RelatedLearningUnknownIdTests(ITestOutputHelper output) : DirectiveTest<RelatedLearningBlock>(output,
"""
:::{related-learning}
:ids: not-a-module
:::
"""
)
{
	[Fact]
	public void EmitsErrorAndRendersNothing()
	{
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("unknown catalog ID 'not-a-module'"));
		Block!.Items.Should().BeEmpty();
		Html.Should().NotContain("related-learning");
		File.PageTableOfContent.Should().NotContainKey(RelatedLearningBlock.DefaultSlug);
	}
}

public class RelatedLearningDuplicateIdTests(ITestOutputHelper output) : DirectiveTest<RelatedLearningBlock>(output,
"""
:::{related-learning}
:ids: apm-with-elastic, apm-with-elastic
:::
"""
)
{
	[Fact]
	public void WarnsAndKeepsFirstOccurrence()
	{
		Block!.Items.Should().ContainSingle().Which.Id.Should().Be("apm-with-elastic");
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning && d.Message.Contains("duplicate catalog ID 'apm-with-elastic'"));
	}
}

public class RelatedLearningEmptyIdsTests(ITestOutputHelper output) : DirectiveTest<RelatedLearningBlock>(output,
"""
:::{related-learning}
:::
"""
)
{
	[Fact]
	public void EmitsErrorWhenIdsMissing()
	{
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error && d.Message.Contains("requires :ids:"));
		Block!.Items.Should().BeEmpty();
	}
}
