// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests that publish blockers correctly filter the right-hand navigation (TOC) and generated anchors.
/// When all entries of a certain type are blocked, the corresponding section heading should not
/// appear in the TOC or generated anchors.
/// </summary>
public class ChangelogPublishBlockerFiltersTocTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogPublishBlockerFiltersTocTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
			- title: Docs update
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			- title: Other stuff
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "333333"
			"""));

		// Block docs and other types via publish blocker
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - docs
			      - other
			"""));
	}

	[Fact]
	public void TocExcludesBlockedDocumentationSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Documentation");
	}

	[Fact]
	public void TocExcludesBlockedOtherSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Other changes");
	}

	[Fact]
	public void TocRetainsNonBlockedFeaturesSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "Features and enhancements");
	}

	[Fact]
	public void TocRetainsVersionHeader()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "9.3.0" && t.Level == 2);
	}

	[Fact]
	public void AnchorsExcludeBlockedDocumentationSection()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().NotContain(a => a.Contains("docs"));
	}

	[Fact]
	public void AnchorsExcludeBlockedOtherSection()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().NotContain(a => a.EndsWith("-other"));
	}

	[Fact]
	public void AnchorsRetainNonBlockedFeaturesSection()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().Contain(a => a.Contains("features-enhancements"));
	}

	[Fact]
	public void HtmlDoesNotContainBlockedSections()
	{
		Html.Should().NotContain("Documentation");
		Html.Should().NotContain("Other changes");
		Html.Should().Contain("Features and enhancements");
	}
}

/// <summary>
/// Tests that hide-features correctly filter the TOC and generated anchors.
/// When all entries of a certain type are hidden via feature-id, the corresponding section
/// should not appear in the TOC or anchors.
/// </summary>
public class ChangelogHideFeaturesFiltersTocTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHideFeaturesFiltersTocTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") =>
		// Bundle with hide-features that filters out all "other" entries
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			hide-features:
			- hidden-feature-1
			- hidden-feature-2
			entries:
			- title: Visible feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "111111"
			- title: Hidden other change 1
			  type: other
			  feature-id: hidden-feature-1
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			- title: Hidden other change 2
			  type: other
			  feature-id: hidden-feature-2
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "333333"
			"""));

	[Fact]
	public void TocExcludesHiddenOtherSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Other changes");
	}

	[Fact]
	public void TocRetainsVisibleFeaturesSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "Features and enhancements");
	}

	[Fact]
	public void AnchorsExcludeHiddenOtherSection()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().NotContain(a => a.EndsWith("-other"));
	}

	[Fact]
	public void AnchorsRetainVisibleFeaturesSection()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().Contain(a => a.Contains("features-enhancements"));
	}

	[Fact]
	public void HtmlMatchesTocFiltering()
	{
		Html.Should().NotContain("Other changes");
		Html.Should().NotContain("Hidden other change");
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Visible feature");
	}
}

/// <summary>
/// Tests that partial filtering retains the section when some (but not all) entries are filtered.
/// </summary>
public class ChangelogPartialFilterRetainsTocTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogPartialFilterRetainsTocTests(ITestOutputHelper output) : base(output,
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
			hide-features:
			- hidden-feature
			entries:
			- title: Visible other change
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "111111"
			- title: Hidden other change
			  type: other
			  feature-id: hidden-feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			"""));

	[Fact]
	public void TocRetainsSectionWhenSomeEntriesRemain()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "Other changes");
	}

	[Fact]
	public void AnchorsRetainSectionWhenSomeEntriesRemain()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().Contain(a => a.EndsWith("-other"));
	}

	[Fact]
	public void HtmlShowsVisibleEntryOnly()
	{
		Html.Should().Contain("Visible other change");
		Html.Should().NotContain("Hidden other change");
	}
}

/// <summary>
/// Tests that publish blocker and hide-features work together to filter the TOC.
/// </summary>
public class ChangelogCombinedFiltersFilterTocTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogCombinedFiltersFilterTocTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			hide-features:
			- hidden-feature
			entries:
			- title: Visible feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "111111"
			- title: Docs entry blocked by type
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			- title: Bug fix hidden by feature
			  type: bug-fix
			  feature-id: hidden-feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "333333"
			"""));

		// Block docs type via publish blocker
		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - docs
			"""));
	}

	[Fact]
	public void TocExcludesPublishBlockedSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Documentation");
	}

	[Fact]
	public void TocExcludesHideFeatureFilteredSection()
	{
		// The only bug-fix entry is hidden by feature-id, so the Fixes section should not appear
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Fixes");
	}

	[Fact]
	public void TocRetainsUnfilteredSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "Features and enhancements");
	}

	[Fact]
	public void AnchorsExcludeBothFilteredSections()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().NotContain(a => a.Contains("-docs"));
		anchors.Should().NotContain(a => a.Contains("-fixes"));
	}

	[Fact]
	public void HtmlMatchesTocAndAnchors()
	{
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Visible feature");
		Html.Should().NotContain("Documentation");
		Html.Should().NotContain("Docs entry blocked by type");
		Html.Should().NotContain(">Fixes<");
		Html.Should().NotContain("Bug fix hidden by feature");
	}
}

/// <summary>
/// Tests area-based publish blocker filtering on TOC and anchors.
/// When all entries of a type are blocked by area, the section should not appear.
/// </summary>
public class ChangelogPublishBlockerAreaFiltersTocTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogPublishBlockerAreaFiltersTocTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "111111"
			- title: Internal docs
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  pr: "222222"
			- title: Internal other change
			  type: other
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  pr: "333333"
			"""));

		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			"""));
	}

	[Fact]
	public void TocExcludesAreaBlockedDocumentationSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Documentation");
	}

	[Fact]
	public void TocExcludesAreaBlockedOtherSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Other changes");
	}

	[Fact]
	public void TocRetainsNonBlockedFeaturesSection()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "Features and enhancements");
	}

	[Fact]
	public void AnchorsMatchToc()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().NotContain(a => a.Contains("-docs"));
		anchors.Should().NotContain(a => a.EndsWith("-other"));
		anchors.Should().Contain(a => a.Contains("features-enhancements"));
	}
}

/// <summary>
/// Tests that when ALL entries across ALL types in a bundle are filtered out,
/// the version header still appears in the TOC but no section headers do.
/// </summary>
public class ChangelogAllEntriesFilteredTocTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogAllEntriesFilteredTocTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Internal feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  pr: "111111"
			- title: Internal bug fix
			  type: bug-fix
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  areas:
			  - Internal
			  pr: "222222"
			"""));

		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			"""));
	}

	[Fact]
	public void TocContainsVersionHeaderOnly()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().ContainSingle(t => t.Level == 2 && t.Heading == "9.3.0");
		tocItems.Should().NotContain(t => t.Level == 3);
	}

	[Fact]
	public void NoAnchorsGenerated()
	{
		var anchors = Block!.GeneratedAnchors.ToList();
		anchors.Should().BeEmpty();
	}
}

/// <summary>
/// Tests TOC filtering across multiple bundles. Each bundle should independently
/// filter its sections based on which entries survive filtering.
/// </summary>
public class ChangelogMultipleBundlesTocFilteringTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMultipleBundlesTocFilteringTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// 9.3.0 has docs entries that will be blocked
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Feature in 9.3.0
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "111111"
			- title: Docs in 9.3.0
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  pr: "222222"
			"""));

		// 9.2.0 only has docs entries (all will be blocked)
		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 9.2.0
			entries:
			- title: Docs in 9.2.0
			  type: docs
			  products:
			  - product: elasticsearch
			    target: 9.2.0
			  pr: "333333"
			"""));

		FileSystem.AddFile("docs/changelog.yml", new MockFileData(
			// language=yaml
			"""
			rules:
			  publish:
			    exclude_types:
			      - docs
			"""));
	}

	[Fact]
	public void BothVersionHeadersAppearInToc()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "9.3.0" && t.Level == 2);
		tocItems.Should().Contain(t => t.Heading == "9.2.0" && t.Level == 2);
	}

	[Fact]
	public void NeitherVersionHasDocumentationInToc()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().NotContain(t => t.Heading == "Documentation");
	}

	[Fact]
	public void FirstVersionRetainsFeaturesInToc()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();
		tocItems.Should().Contain(t => t.Heading == "Features and enhancements");
	}

	[Fact]
	public void SecondVersionHasNoSectionHeaders()
	{
		var tocItems = Block!.GeneratedTableOfContent.ToList();

		// 9.2.0 is the second version header; find its index and check no h3 follows until end
		var versionIndices = tocItems
			.Select((t, i) => (t, i))
			.Where(x => x.t.Level == 2)
			.Select(x => x.i)
			.ToList();

		// Should have 2 versions
		versionIndices.Should().HaveCount(2);

		// Items after the second version header should all be version-level (none at section level)
		var lastVersionIdx = versionIndices.Last();
		var itemsAfterLastVersion = tocItems.Skip(lastVersionIdx + 1).ToList();
		itemsAfterLastVersion.Should().NotContain(t => t.Level == 3);
	}
}
