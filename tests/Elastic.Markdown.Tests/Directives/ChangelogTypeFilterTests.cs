// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

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
	public void DefaultBehaviorShowsFeatures()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("New feature");
	}

	[Test]
	public void DefaultBehaviorShowsBugFixes()
	{
		Html.Should().Contain(">Fixes<");
		Html.Should().Contain("Bug fix");
	}

	[Test]
	public void DefaultBehaviorExcludesBreakingChanges()
	{
		Html.Should().NotContain("Breaking changes");
		Html.Should().NotContain("Breaking API change");
	}

	[Test]
	public void DefaultBehaviorExcludesKnownIssues()
	{
		Html.Should().NotContain("Known issues");
		Html.Should().NotContain("Known issue");
	}

	[Test]
	public void DefaultBehaviorExcludesDeprecations()
	{
		Html.Should().NotContain("Deprecations");
		Html.Should().NotContain("Deprecated feature");
	}
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
	public void ShowsAllEntryTypes()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("New feature");
		Html.Should().Contain(">Fixes<");
		Html.Should().Contain("Bug fix");
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Breaking API change");
		Html.Should().Contain("Known issues");
		Html.Should().Contain("Known issue");
		Html.Should().Contain("Deprecations");
		Html.Should().Contain("Deprecated feature");
	}

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
	public void ShowsBreakingChanges()
	{
		Html.Should().NotContain("### Breaking changes");
		Html.Should().NotContain(">Breaking changes<");
		Html.Should().Contain("Breaking API change");
	}

	[Test]
	public void ExcludesOtherTypes()
	{
		Html.Should().NotContain("Features and enhancements");
		Html.Should().NotContain("New feature");
		Html.Should().NotContain("Known issues");
		Html.Should().NotContain("Known issue");
	}
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
	public void ShowsDeprecations()
	{
		Html.Should().NotContain("### Deprecations");
		Html.Should().NotContain(">Deprecations<");
		Html.Should().Contain("Deprecated feature");
		Html.Should().Contain("Another deprecation");
	}

	[Test]
	public void ExcludesOtherTypes()
	{
		Html.Should().NotContain("Features and enhancements");
		Html.Should().NotContain("New feature");
	}
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
	public void ShowsKnownIssues()
	{
		Html.Should().NotContain("### Known issues");
		Html.Should().NotContain(">Known issues<");
		Html.Should().Contain("Known issue 1");
		Html.Should().Contain("Known issue 2");
	}

	[Test]
	public void ExcludesOtherTypes()
	{
		Html.Should().NotContain("Features and enhancements");
		Html.Should().NotContain("New feature");
	}
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
	public void EmitsWarningForInvalidValue() => Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid :type: value"));

	[Test]
	public void DefaultBehaviorIsApplied()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("New feature");
		Html.Should().NotContain("Breaking changes");
	}
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
	public void ShowsAllTypes()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Breaking changes");
	}
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
	public void ShowsAllTypesWithSubsections()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Search feature");
		Html.Should().Contain("Indexing feature");
		Html.Should().Contain("Breaking changes");
	}
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
		Html.Should().NotContain("There are no known issues associated with this release");
		Html.Should().NotContain("9.3.0");
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
		Html.Should().NotContain("There are no breaking changes associated with this release");
		Html.Should().NotContain("9.3.0");
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
		Html.Should().NotContain("There are no deprecations associated with this release");
		Html.Should().NotContain("9.3.0");
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
		Html.Should().NotContain("No new features, enhancements, or fixes");
		Html.Should().NotContain("9.3.0");
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
		Html.Should().NotContain("No new features, enhancements, or fixes");
		Html.Should().NotContain("9.3.0");
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
	public void RendersOnlyPopulatedVersions()
	{
		Html.Should().Contain("Known issue in 9.3");
		Html.Should().Contain("9.3.0");
		Html.Should().NotContain("9.2.0");
	}
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
	public void ShowsVersionAndDescriptionWithoutPlaceholder()
	{
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("This release was pulled due to critical issues");
		Html.Should().NotContain("No new features, enhancements, or fixes");
	}
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
		Html.Should().NotContain("9.3.0");
		Html.Should().NotContain("Released:");
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
		Html.Should().NotContain("9.3.0");
		Html.Should().NotContain("General release notes for this version");
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
	public void GroupsEntriesByAreaWithoutSectionHeading()
	{
		Html.Should().NotContain("### Deprecations");
		Html.Should().NotContain(">Deprecations<");
		Html.Should().NotContain("{dropdown}");
		Html.Should().Contain("Search");
		Html.Should().Contain("Search deprecation");
		Html.Should().Contain("Indexing");
		Html.Should().Contain("Indexing deprecation");
	}
}
