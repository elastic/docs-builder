// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Shared bundle used by highlights option tests: one highlighted feature, one normal feature,
/// one bug fix, and a breaking change (excluded by the default type filter).
/// </summary>
static file class ChangelogHighlightsFixtures
{
	public const string BundleYaml =
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Highlighted feature
		  type: feature
		  highlight: true
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This is the highlight description.
		  prs:
		  - "111111"
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "222222"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "333333"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "444444"
		""";
}

/// <summary>Default (omitted) :highlights: — inline only, no Highlights section.</summary>
public class ChangelogHighlightsOptionDefaultOffTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionDefaultOffTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Fact]
	public void HighlightsDisabledByDefault() =>
		Block!.HighlightsEnabled.Should().BeFalse();

	[Fact]
	public void OmitsHighlightsSection() =>
		Html.Should().NotContain("Highlights");

	[Fact]
	public void StillRendersHighlightedEntryUnderTypeSection()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Highlighted feature");
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Fixes");
		Html.Should().Contain("Bug fix");
	}

	[Fact]
	public void ExcludesSeparatedTypesByDefault() =>
		Html.Should().NotContain("Breaking changes");

	[Fact]
	public void TocOmitsHighlights()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().NotContain(t => t.Heading == "Highlights");
	}
}

/// <summary>:highlights: with default type filter — Highlights section plus type sections, no separated types.</summary>
public class ChangelogHighlightsOptionEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionEnabledTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:highlights:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Fact]
	public void HighlightsEnabledWhenFlagPresent() =>
		Block!.HighlightsEnabled.Should().BeTrue();

	[Fact]
	public void RendersHighlightsSection() =>
		Html.Should().Contain("Highlights");

	[Fact]
	public void DuplicatesHighlightedEntryInTypeSection()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Highlighted feature");
		Html.Should().Contain("Regular feature");
	}

	[Fact]
	public void ExcludesSeparatedTypesWithoutTypeAll() =>
		Html.Should().NotContain("Breaking changes");

	[Fact]
	public void TocIncludesHighlights()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().Contain(t => t.Heading == "Highlights" && t.Level == 3);
		toc.Should().Contain(t => t.Slug == "elasticsearch-9.3.0-highlights");
	}

	[Fact]
	public void GeneratedAnchorsIncludeHighlights() =>
		Block!.GeneratedAnchors.Should().Contain("elasticsearch-9.3.0-highlights");
}

/// <summary>:highlights: + :type: all — Highlights section and separated types.</summary>
public class ChangelogHighlightsOptionWithTypeAllTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionWithTypeAllTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:highlights:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Fact]
	public void RendersHighlightsAndSeparatedTypes()
	{
		Html.Should().Contain("Highlights");
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Features and enhancements");
	}
}

/// <summary>:type: all without :highlights: — no Highlights section (breaking change from prior All behavior).</summary>
public class ChangelogHighlightsOptionTypeAllWithoutFlagTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionTypeAllWithoutFlagTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Fact]
	public void TypeAllAloneDoesNotEmitHighlightsSection()
	{
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Highlighted feature");
		Html.Should().NotContain("id=\"elasticsearch-9.3.0-highlights\"");
		Block!.GeneratedTableOfContent.Should().NotContain(t => t.Heading == "Highlights");
	}
}

/// <summary>Legacy :type: highlight warns and falls back to default.</summary>
public class ChangelogHighlightsLegacyTypeHighlightTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsLegacyTypeHighlightTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: highlight
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Fact]
	public void FallsBackToDefaultTypeFilter() =>
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Default);

	[Fact]
	public void HighlightsRemainDisabled() =>
		Block!.HighlightsEnabled.Should().BeFalse();

	[Fact]
	public void EmitsWarningPointingToHighlightsOption()
	{
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("Invalid :type: value 'highlight'", StringComparison.Ordinal) &&
			d.Message.Contains(":highlights:", StringComparison.Ordinal));
	}

	[Fact]
	public void RendersDefaultTypeSectionsNotHighlightsOnly()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Highlighted feature");
		Html.Should().NotContain("id=\"elasticsearch-9.3.0-highlights\"");
	}
}

/// <summary>:highlights: + :description-visibility: keep-descriptions shows bodies in the Highlights section.</summary>
public class ChangelogHighlightsOptionWithDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionWithDescriptionsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:highlights:
		:description-visibility: keep-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Fact]
	public void ShowsDescriptionInHighlightsSection() =>
		Html.Should().Contain("This is the highlight description.");
}

/// <summary>
/// :highlights: + keep-highlight-descriptions — prose only under Highlights; type sections stay title/links.
/// </summary>
public class ChangelogKeepHighlightDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogKeepHighlightDescriptionsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:highlights:
		:description-visibility: keep-highlight-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Highlighted feature
		  type: feature
		  highlight: true
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This is the highlight description.
		  prs:
		  - "111111"
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This is the regular feature description.
		  prs:
		  - "222222"
		"""));

	[Fact]
	public void ParsesKeepHighlightDescriptions() =>
		Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.KeepHighlightDescriptions);

	[Fact]
	public void ShowsDescriptionInHighlightsSection() =>
		Html.Should().Contain("This is the highlight description.");

	[Fact]
	public void HidesDescriptionsInTypeSections() =>
		Html.Should().NotContain("This is the regular feature description.");

	[Fact]
	public void StillRendersTitlesInTypeSections()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Highlighted feature");
		Html.Should().Contain("Regular feature");
	}
}

/// <summary>keep-highlight-descriptions without :highlights: hides descriptions everywhere.</summary>
public class ChangelogKeepHighlightDescriptionsWithoutHighlightsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogKeepHighlightDescriptionsWithoutHighlightsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:description-visibility: keep-highlight-descriptions
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries:
		- title: Highlighted feature
		  type: feature
		  highlight: true
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This is the highlight description.
		  prs:
		  - "111111"
		- title: Regular feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: This is the regular feature description.
		  prs:
		  - "222222"
		"""));

	[Fact]
	public void OmitsHighlightsSection() =>
		Html.Should().NotContain("id=\"elasticsearch-9.3.0-highlights\"");

	[Fact]
	public void HidesAllRecordDescriptions()
	{
		Html.Should().NotContain("This is the highlight description.");
		Html.Should().NotContain("This is the regular feature description.");
	}

	[Fact]
	public void StillRendersTitles()
	{
		Html.Should().Contain("Highlighted feature");
		Html.Should().Contain("Regular feature");
	}
}
