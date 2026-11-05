// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots.Html;

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
[InheritsTests]
public class ChangelogHighlightsOptionDefaultOffTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionDefaultOffTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		) => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Test]
	public void HighlightsDisabledByDefault() => Block!.HighlightsEnabled.Should().BeFalse();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());

	[Test]
	public void TocOmitsHighlights()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().NotContain(t => t.Heading == "Highlights");
	}
}

/// <summary>:highlights: with default type filter — Highlights section plus type sections, no separated types.</summary>
[InheritsTests]
public class ChangelogHighlightsOptionEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionEnabledTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:highlights:
		:::
		"""
		) => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Test]
	public void HighlightsEnabledWhenFlagPresent() => Block!.HighlightsEnabled.Should().BeTrue();

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());

	[Test]
	public void TocIncludesHighlights()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		toc.Should().Contain(t => t.Heading == "Highlights" && t.Level == 3);
		toc.Should().Contain(t => t.Slug == "elasticsearch-9.3.0-highlights");
	}

	[Test]
	public void GeneratedAnchorsIncludeHighlights() => Block!.GeneratedAnchors.Should().Contain("elasticsearch-9.3.0-highlights");
}

/// <summary>:highlights: + :type: all — Highlights section and separated types.</summary>
[InheritsTests]
public class ChangelogHighlightsOptionWithTypeAllTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionWithTypeAllTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:highlights:
		:::
		"""
		) => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>:type: all without :highlights: — no Highlights section (breaking change from prior All behavior).</summary>
[InheritsTests]
public class ChangelogHighlightsOptionTypeAllWithoutFlagTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionTypeAllWithoutFlagTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:::
		"""
		) => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Test]
	public void TypeAllAloneDoesNotEmitHighlightsSection()
	{
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Highlighted feature");
		Html.Should().NotContain("id=\"elasticsearch-9.3.0-highlights\"");
		Block!.GeneratedTableOfContent.Should().NotContain(t => t.Heading == "Highlights");
	}
}

/// <summary>Legacy :type: highlight warns and falls back to default.</summary>
[InheritsTests]
public class ChangelogHighlightsLegacyTypeHighlightTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsLegacyTypeHighlightTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: highlight
		:::
		"""
		) => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Test]
	public void FallsBackToDefaultTypeFilter() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Default);

	[Test]
	public void HighlightsRemainDisabled() => Block!.HighlightsEnabled.Should().BeFalse();

	[Test]
	public void EmitsWarningPointingToHighlightsOption() =>
		Collector
			.Diagnostics
			.Should()
			.Contain(
				d => d.Message.Contains("Invalid :type: value 'highlight'", StringComparison.Ordinal) && d.Message.Contains(
					":highlights:",
					StringComparison.Ordinal
				)
			);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>:highlights: + :description-visibility: keep-descriptions shows bodies in the Highlights section.</summary>
[InheritsTests]
public class ChangelogHighlightsOptionWithDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHighlightsOptionWithDescriptionsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:highlights:
		:description-visibility: keep-descriptions
		:::
		"""
		) => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(ChangelogHighlightsFixtures.BundleYaml));

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// :highlights: + keep-highlight-descriptions — prose only under Highlights; type sections stay title/links.
/// </summary>
[InheritsTests]
public class ChangelogKeepHighlightDescriptionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogKeepHighlightDescriptionsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:highlights:
		:description-visibility: keep-highlight-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void ParsesKeepHighlightDescriptions() =>
		Block!.DescriptionVisibility.Should().Be(ChangelogDescriptionVisibility.KeepHighlightDescriptions);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>keep-highlight-descriptions without :highlights: hides descriptions everywhere.</summary>
[InheritsTests]
public class ChangelogKeepHighlightDescriptionsWithoutHighlightsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogKeepHighlightDescriptionsWithoutHighlightsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:description-visibility: keep-highlight-descriptions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
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
		"""
			)
		);

	[Test]
	public void RendersHtml() =>
		Html.Should().MatchInlineHtmlSnapshot(
			html: DocsHtml.Body(),
			expected: """
		<div class="heading-wrapper" id="9.3.0">
			<h2>
				<a class="headerlink" href="#9.3.0">9.3.0</a>
			</h2>
		</div>
		<div class="heading-wrapper" id="elasticsearch-9.3.0-features-enhancements">
			<h3>
				<a class="headerlink" href="#elasticsearch-9.3.0-features-enhancements">Features and enhancements</a>
			</h3>
		</div>
		<ul>
			<li>Highlighted feature.
				<a href="https://github.com/elastic/elasticsearch/pull/111111">#111111</a>
			</li>
			<li>Regular feature.
				<a href="https://github.com/elastic/elasticsearch/pull/222222">#222222</a>
			</li>
		</ul>
		"""
		);
}
