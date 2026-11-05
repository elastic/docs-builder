// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for the :type: parameter on the changelog directive.
/// By default (no :type:), the directive excludes known issues, breaking changes, and deprecations.
/// With :type: all, all entry types are shown.
/// With :type: breaking-change, only breaking changes are shown.
/// With :type: deprecation, only deprecations are shown.
/// With :type: known-issue, only known issues are shown.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterDefaultTests() : base(
			// language=markdown
			"""
		:::{changelog}
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "222222"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  prs:
		  - "444444"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  prs:
		  - "555555"
		"""
			)
		);

	[Test]
	public void DefaultBehaviorExcludesSeparatedTypes() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Default);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests for :type: all - shows all entry types including separated types.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterAllTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterAllTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "222222"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  prs:
		  - "444444"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  prs:
		  - "555555"
		"""
			)
		);

	[Test]
	public void TypeFilterIsAll() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.All);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());

	[Test]
	public void TableOfContentsIncludesSeparatedTypeSections()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();

		tocItems.Should().Contain(t => t.Heading == "Breaking changes" && t.Level == 3);
		tocItems.Should().Contain(t => t.Heading == "Known issues" && t.Level == 3);
		tocItems.Should().Contain(t => t.Heading == "Deprecations" && t.Level == 3);
	}

	[Test]
	public void SeparatedTypeTocSlugsMatchHtmlIds()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in tocItems.Where(t => t.Level == 3))
		{
			Html.Should().Contain(
				$"id=\"{item.Slug}\"",
				$"TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id"
			);
		}
	}
}

/// <summary>
/// Tests for :type: breaking-change - shows only breaking changes.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterBreakingChangeTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterBreakingChangeTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  prs:
		  - "444444"
		"""
			)
		);

	[Test]
	public void TypeFilterIsBreakingChange() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.BreakingChange);

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
		<ul>
			<li>
				<p>Breaking API change.
					<a href="https://github.com/elastic/elasticsearch/pull/333333">#333333</a>
				</p>
				<p>
					<strong>Impact:</strong>
					Users must update.</p>
				<p>
					<strong>Action:</strong>
					Follow guide.</p>
			</li>
		</ul>
		"""
		);
}

/// <summary>
/// Tests for :type: deprecation - shows only deprecations.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterDeprecationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterDeprecationTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: deprecation
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  prs:
		  - "555555"
		- title: Another deprecation
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Another deprecated feature.
		  impact: Also will be removed.
		  action: Migrate to new API.
		  prs:
		  - "666666"
		"""
			)
		);

	[Test]
	public void TypeFilterIsDeprecation() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Deprecation);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests for :type: known-issue - shows only known issues.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterKnownIssueTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterKnownIssueTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: known-issue
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Known issue 1
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  prs:
		  - "444444"
		- title: Known issue 2
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Another issue.
		  impact: Different impact.
		  action: Different workaround.
		  prs:
		  - "555555"
		"""
			)
		);

	[Test]
	public void TypeFilterIsKnownIssue() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.KnownIssue);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests for invalid :type: values - should emit warning and use default behavior.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterInvalidTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterInvalidTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: invalid-value
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void FallsBackToDefaultBehavior() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Default);

	[Test]
	public void EmitsWarningForInvalidValue() =>
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"warning {repo}/docs/index.md:3 Invalid :type: value 'invalid-value'. Valid values are: all, breaking-change, deprecation, known-issue. Using default behavior."
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
			<li>New feature.
				<a href="https://github.com/elastic/elasticsearch/pull/111111">#111111</a>
			</li>
		</ul>
		"""
		);
}

/// <summary>
/// Tests for case-insensitive :type: values.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterCaseInsensitiveTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterCaseInsensitiveTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: ALL
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void AcceptsUppercaseAll() => Block!.TypeFilter.Should().Be(ChangelogTypeFilter.All);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests for combining :type: with other options like :subsections:.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterWithSubsectionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterWithSubsectionsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
		:subsections:
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
		- title: Search feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Search
		  prs:
		  - "111111"
		- title: Indexing feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  prs:
		  - "222222"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  prs:
		  - "333333"
		"""
			)
		);

	[Test]
	public void TypeFilterAndSubsectionsBothWork()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.All);
		Block!.Subsections.Should().BeTrue();
	}

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests that :type: filter affects generated anchors correctly.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterGeneratedAnchorsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterGeneratedAnchorsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void GeneratedAnchorsRespectTypeFilter()
	{
		var anchors = Block!.GeneratedAnchors.ToList();

		anchors.Should().NotContain(a => a.Contains("breaking-changes"));
		anchors.Should().NotContain(a => a.Contains("features-enhancements"));
	}
}

/// <summary>
/// Tests that :type: filter affects table of contents correctly.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterTableOfContentsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterTableOfContentsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: deprecation
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Deprecated.
		  impact: Impact.
		  action: Action.
		  prs:
		  - "222222"
		"""
			)
		);

	[Test]
	public void TableOfContentsRespectTypeFilter()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();

		tocItems.Should().NotContain(t => t.Heading == "Deprecations");
		tocItems.Should().Contain(t => t.Heading == "9.3.0" && t.Level == 2);
		tocItems.Should().NotContain(t => t.Heading == "Features and enhancements");
	}
}

/// <summary>
/// Tests that empty bundles are omitted when type filter excludes all entries.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterEmptyKnownIssueTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyKnownIssueTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: known-issue
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void OmitsEmptyVersionBlock()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that empty result shows breaking-change-specific message when using breaking-change filter.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterEmptyBreakingChangeTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyBreakingChangeTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: breaking-change
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void OmitsEmptyVersionBlock()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that empty result shows deprecation-specific message when using deprecation filter.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterEmptyDeprecationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyDeprecationTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: deprecation
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
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void OmitsEmptyVersionBlock()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that empty result shows generic message when using default filter.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterEmptyDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyDefaultTests() : base(
			// language=markdown
			"""
		:::{changelog}
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
		- title: Breaking change only
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void OmitsEmptyVersionBlock()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that empty result shows generic message when using "all" filter with empty bundle.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterEmptyAllTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyAllTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
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
		entries: []
		"""
			)
		);

	[Test]
	public void OmitsEmptyVersionBlock()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that only bundles with matching entries render when multiple bundles are loaded.
/// </summary>
[InheritsTests]
public class ChangelogTypeFilterMixedBundlesEmptyOmissionTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterMixedBundlesEmptyOmissionTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: known-issue
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Known issue in 9.3
			  type: known-issue
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  description: Issue exists.
			  impact: Some impact.
			  action: Workaround available.
			  prs:
			  - "444444"
			"""
			)
		);
		FileSystem.AddFile(
			"docs/changelog/bundles/9.2.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.2.0
			entries:
			- title: Feature only
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.2.0
			  prs:
			  - "111111"
			"""
			)
		);
	}

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
		<ul>
			<li>
				<p>Known issue in 9.3.
					<a href="https://github.com/elastic/elasticsearch/pull/444444">#444444</a>
				</p>
				<p>
					<strong>Impact:</strong>
					Some impact.</p>
				<p>
					<strong>Action:</strong>
					Workaround available.</p>
			</li>
		</ul>
		"""
		);
}

/// <summary>
/// Tests that :type: all preserves bundle description when there are no entries.
/// </summary>
[InheritsTests]
public class ChangelogEmptyBundleWithDescriptionTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyBundleWithDescriptionTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
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
		description: |
		  This release was pulled due to critical issues.
		entries: []
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
		<p>This release was pulled due to critical issues.</p>
		"""
		);
}

/// <summary>
/// Tests that release-date alone does not preserve an otherwise empty release on general pages.
/// </summary>
[InheritsTests]
public class ChangelogEmptyBundleWithReleaseDateOnlyTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyBundleWithReleaseDateOnlyTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: all
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
		release-date: "2026-04-09"
		entries: []
		"""
			)
		);

	[Test]
	public void OmitsVersionBlockWhenOnlyReleaseDate()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that dedicated type pages omit empty bundles even when they have a description.
/// </summary>
[InheritsTests]
public class ChangelogDedicatedPageIgnoresDescriptionTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDedicatedPageIgnoresDescriptionTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: deprecation
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
		description: |
		  General release notes for this version.
		entries:
		- title: New feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  prs:
		  - "111111"
		"""
			)
		);

	[Test]
	public void OmitsVersionBlockWhenNoMatchingEntries()
	{
		Html.Should().MatchInlineHtmlSnapshot(html: DocsHtml.Body(), expected: "");
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}
}

/// <summary>
/// Tests that :subsections: area grouping works on dedicated deprecation pages without a section H3.
/// </summary>
[InheritsTests]
public class ChangelogDedicatedPageWithSubsectionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDedicatedPageWithSubsectionsTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:type: deprecation
		:subsections:
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
		- title: Search deprecation
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Search
		  description: Deprecated search API.
		  impact: Will be removed.
		  action: Migrate to new API.
		  prs:
		  - "555555"
		- title: Indexing deprecation
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  description: Deprecated indexing config.
		  impact: Will be removed.
		  action: Update config.
		  prs:
		  - "666666"
		"""
			)
		);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}
