// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for the :type: parameter on the changelog directive.
/// By default (no :type:), the directive excludes known issues, breaking changes, and deprecations.
/// With :type: all, all entry types are shown.
/// With :type: breaking-change, only breaking changes are shown.
/// With :type: deprecation, only deprecations are shown.
/// With :type: known-issue, only known issues are shown.
/// </summary>
public class ChangelogTypeFilterDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterDefaultTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  pr: "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  pr: "444444"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  pr: "555555"
		"""));

	[Fact]
	public void DefaultBehaviorExcludesSeparatedTypes()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Default);
	}

	[Fact]
	public void DefaultBehaviorShowsFeatures()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("New feature");
	}

	[Fact]
	public void DefaultBehaviorShowsBugFixes()
	{
		Html.Should().Contain(">Fixes<");
		Html.Should().Contain("Bug fix");
	}

	[Fact]
	public void DefaultBehaviorExcludesBreakingChanges()
	{
		Html.Should().NotContain("Breaking changes");
		Html.Should().NotContain("Breaking API change");
	}

	[Fact]
	public void DefaultBehaviorExcludesKnownIssues()
	{
		Html.Should().NotContain("Known issues");
		Html.Should().NotContain("Known issue");
	}

	[Fact]
	public void DefaultBehaviorExcludesDeprecations()
	{
		Html.Should().NotContain("Deprecations");
		Html.Should().NotContain("Deprecated feature");
	}
}

/// <summary>
/// Tests for :type: all - shows all entry types including separated types.
/// </summary>
public class ChangelogTypeFilterAllTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterAllTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "222222"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  pr: "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  pr: "444444"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  pr: "555555"
		"""));

	[Fact]
	public void TypeFilterIsAll()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.All);
	}

	[Fact]
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
}

/// <summary>
/// Tests for :type: breaking-change - shows only breaking changes.
/// </summary>
public class ChangelogTypeFilterBreakingChangeTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterBreakingChangeTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Breaking API change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: API changed.
		  impact: Users must update.
		  action: Follow guide.
		  pr: "333333"
		- title: Known issue
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  pr: "444444"
		"""));

	[Fact]
	public void TypeFilterIsBreakingChange()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.BreakingChange);
	}

	[Fact]
	public void ShowsBreakingChanges()
	{
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Breaking API change");
	}

	[Fact]
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
public class ChangelogTypeFilterDeprecationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterDeprecationTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Feature deprecated.
		  impact: Will be removed.
		  action: Use new feature.
		  pr: "555555"
		- title: Another deprecation
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Another deprecated feature.
		  impact: Also will be removed.
		  action: Migrate to new API.
		  pr: "666666"
		"""));

	[Fact]
	public void TypeFilterIsDeprecation()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Deprecation);
	}

	[Fact]
	public void ShowsDeprecations()
	{
		Html.Should().Contain("Deprecations");
		Html.Should().Contain("Deprecated feature");
		Html.Should().Contain("Another deprecation");
	}

	[Fact]
	public void ExcludesOtherTypes()
	{
		Html.Should().NotContain("Features and enhancements");
		Html.Should().NotContain("New feature");
	}
}

/// <summary>
/// Tests for :type: known-issue - shows only known issues.
/// </summary>
public class ChangelogTypeFilterKnownIssueTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterKnownIssueTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: known-issue
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Known issue 1
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Issue exists.
		  impact: Some impact.
		  action: Workaround available.
		  pr: "444444"
		- title: Known issue 2
		  type: known-issue
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Another issue.
		  impact: Different impact.
		  action: Different workaround.
		  pr: "555555"
		"""));

	[Fact]
	public void TypeFilterIsKnownIssue()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.KnownIssue);
	}

	[Fact]
	public void ShowsKnownIssues()
	{
		Html.Should().Contain("Known issues");
		Html.Should().Contain("Known issue 1");
		Html.Should().Contain("Known issue 2");
	}

	[Fact]
	public void ExcludesOtherTypes()
	{
		Html.Should().NotContain("Features and enhancements");
		Html.Should().NotContain("New feature");
	}
}

/// <summary>
/// Tests for invalid :type: values - should emit warning and use default behavior.
/// </summary>
public class ChangelogTypeFilterInvalidTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterInvalidTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: invalid-value
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  pr: "222222"
		"""));

	[Fact]
	public void FallsBackToDefaultBehavior()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.Default);
	}

	[Fact]
	public void EmitsWarningForInvalidValue()
	{
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Invalid :type: value"));
	}

	[Fact]
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
public class ChangelogTypeFilterCaseInsensitiveTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterCaseInsensitiveTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: ALL
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  pr: "222222"
		"""));

	[Fact]
	public void AcceptsUppercaseAll()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.All);
	}

	[Fact]
	public void ShowsAllTypes()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Breaking changes");
	}
}

/// <summary>
/// Tests for combining :type: with other options like :subsections:.
/// </summary>
public class ChangelogTypeFilterWithSubsectionsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterWithSubsectionsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:subsections:
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Indexing feature
		  type: feature
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  areas:
		  - Indexing
		  pr: "222222"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  pr: "333333"
		"""));

	[Fact]
	public void TypeFilterAndSubsectionsBothWork()
	{
		Block!.TypeFilter.Should().Be(ChangelogTypeFilter.All);
		Block!.Subsections.Should().BeTrue();
	}

	[Fact]
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
public class ChangelogTypeFilterGeneratedAnchorsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterGeneratedAnchorsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Breaking change
		  type: breaking-change
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Breaking.
		  impact: Impact.
		  action: Action.
		  pr: "222222"
		"""));

	[Fact]
	public void GeneratedAnchorsRespectTypeFilter()
	{
		var anchors = Block!.GeneratedAnchors.ToList();

		// Should have anchor for breaking changes
		anchors.Should().Contain(a => a.Contains("breaking-changes"));

		// Should NOT have anchor for features since we're filtering to breaking-change only
		anchors.Should().NotContain(a => a.Contains("features-enhancements"));
	}
}

/// <summary>
/// Tests that :type: filter affects table of contents correctly.
/// </summary>
public class ChangelogTypeFilterTableOfContentsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterTableOfContentsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		- title: Deprecated feature
		  type: deprecation
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  description: Deprecated.
		  impact: Impact.
		  action: Action.
		  pr: "222222"
		"""));

	[Fact]
	public void TableOfContentsRespectTypeFilter()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();

		// Should have TOC item for deprecations
		tocItems.Should().Contain(t => t.Heading == "Deprecations");

		// Should NOT have TOC item for features since we're filtering to deprecation only
		tocItems.Should().NotContain(t => t.Heading == "Features and enhancements");
	}
}

/// <summary>
/// Tests that empty result shows appropriate message when type filter excludes all entries.
/// For known-issue filter, we should show known-issue-specific message.
/// </summary>
public class ChangelogTypeFilterEmptyKnownIssueTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyKnownIssueTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: known-issue
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		"""));

	[Fact]
	public void ShowsKnownIssueSpecificEmptyMessage()
	{
		// When filtering to known-issue but bundle only has features,
		// should show known-issue-specific message
		Html.Should().Contain("There are no known issues associated with this release");
	}
}

/// <summary>
/// Tests that empty result shows breaking-change-specific message when using breaking-change filter.
/// </summary>
public class ChangelogTypeFilterEmptyBreakingChangeTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyBreakingChangeTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: breaking-change
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		"""));

	[Fact]
	public void ShowsBreakingChangeSpecificEmptyMessage()
	{
		// When filtering to breaking-change but bundle only has features,
		// should show breaking-change-specific message
		Html.Should().Contain("There are no breaking changes associated with this release");
	}
}

/// <summary>
/// Tests that empty result shows deprecation-specific message when using deprecation filter.
/// </summary>
public class ChangelogTypeFilterEmptyDeprecationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyDeprecationTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: deprecation
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		"""));

	[Fact]
	public void ShowsDeprecationSpecificEmptyMessage()
	{
		// When filtering to deprecation but bundle only has features,
		// should show deprecation-specific message
		Html.Should().Contain("There are no deprecations associated with this release");
	}
}

/// <summary>
/// Tests that empty result shows generic message when using default filter.
/// </summary>
public class ChangelogTypeFilterEmptyDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyDefaultTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
		  pr: "111111"
		"""));

	[Fact]
	public void ShowsGenericEmptyMessageForDefaultFilter()
	{
		// When using default filter but bundle only has breaking changes (which are excluded by default),
		// should show the generic "no features, enhancements, or fixes" message
		Html.Should().Contain("No new features, enhancements, or fixes");
	}
}

/// <summary>
/// Tests that empty result shows generic message when using "all" filter with empty bundle.
/// </summary>
public class ChangelogTypeFilterEmptyAllTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogTypeFilterEmptyAllTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:type: all
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: elasticsearch
		  target: 9.3.0
		entries: []
		"""));

	[Fact]
	public void ShowsGenericEmptyMessageForAllFilter()
	{
		// When using "all" filter with empty entries,
		// should show the generic message (not type-specific since All includes everything)
		Html.Should().Contain("No new features, enhancements, or fixes");
	}
}
