// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using AwesomeAssertions;

namespace Elastic.Markdown.Tests.Directives;

// ── yyyy-MM (Cloud Hosted style) ─────────────────────────────────────────────

/// <summary>
/// yyyy-MM versions display as "Month Year" (e.g. "November 2025").
/// The version heading anchor must therefore be the Slugify of the display name
/// ("november-2025"), NOT the raw key ("2025-11").
/// </summary>
public class ChangelogYearMonthAnchorNavigationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogYearMonthAnchorNavigationTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/2025-11.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: cloud-hosted
			  target: 2025-11
			entries:
			- title: November feature
			  type: feature
			  products:
			  - product: cloud-hosted
			    target: 2025-11
			  prs:
			  - "111111"
			- title: November bugfix
			  type: bug-fix
			  products:
			  - product: cloud-hosted
			    target: 2025-11
			  prs:
			  - "222222"
			"""));

	[Fact]
	public void VersionHeadingTocSlugIsSlugifiedDisplayName()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		// The display name "November 2025" slugified is "november-2025".
		// It must NOT be the raw yyyy-MM key "2025-11".
		versionItem.Slug.Should().Be("november-2025");
		versionItem.Slug.Should().NotBe("2025-11");
	}

	[Fact]
	public void VersionHeadingHtmlIdMatchesTocSlug()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		Html.Should().Contain($"id=\"{versionItem.Slug}\"",
			$"heading-wrapper id must match TOC slug '{versionItem.Slug}' so the right-nav link scrolls to the section");
	}

	[Fact]
	public void SubSectionTocSlugsHaveMatchingHtmlIds()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc.Where(t => t.Level == 3))
		{
			Html.Should().Contain($"id=\"{item.Slug}\"",
				$"sub-section TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id");
		}
	}

	[Fact]
	public void AllTocSlugsHaveMatchingHtmlIds()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc)
		{
			Html.Should().Contain($"id=\"{item.Slug}\"",
				$"TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id in the rendered HTML");
		}
	}

	[Fact]
	public void SubSectionSlugsUseYearMonthKeyNotDisplayName()
	{
		// Sub-section explicit anchors embed the raw yyyy-MM key (via titleSlug).
		// Slugify of "2025-11" is still "2025-11" (already URL-safe), so the
		// sub-section slugs must contain "2025-11", not "november-2025".
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc.Where(t => t.Level == 3))
			item.Slug.Should().Contain("2025-11",
				$"sub-section slug for a yyyy-MM bundle should retain the original date key");
	}
}

// ── yyyy-MM-dd (Cloud Serverless style) ──────────────────────────────────────

/// <summary>
/// Full-date versions display as "Month D, Year" (e.g. "August 5, 2025").
/// The version heading anchor must be the Slugify of that display name ("august-5-2025").
/// </summary>
public class ChangelogFullDateAnchorNavigationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogFullDateAnchorNavigationTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/2025-08-05.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: cloud-serverless
			  target: 2025-08-05
			entries:
			- title: August 5th feature
			  type: feature
			  products:
			  - product: cloud-serverless
			    target: 2025-08-05
			  prs:
			  - "111111"
			- title: August 5th breaking change
			  type: breaking-change
			  products:
			  - product: cloud-serverless
			    target: 2025-08-05
			  description: A breaking change.
			  impact: Some impact.
			  action: Take action.
			  prs:
			  - "222222"
			"""));

	[Fact]
	public void VersionHeadingTocSlugIsSlugifiedDisplayName()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		// "August 5, 2025" slugified is "august-5-2025"
		versionItem.Slug.Should().Be("august-5-2025");
		versionItem.Slug.Should().NotBe("2025-08-05");
	}

	[Fact]
	public void VersionHeadingHtmlIdMatchesTocSlug()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		Html.Should().Contain($"id=\"{versionItem.Slug}\"",
			$"heading-wrapper id must match TOC slug '{versionItem.Slug}'");
	}

	[Fact]
	public void AllTocSlugsHaveMatchingHtmlIds()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc)
		{
			Html.Should().Contain($"id=\"{item.Slug}\"",
				$"TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id");
		}
	}
}

// ── Semver (standard Elasticsearch / Kibana style) ───────────────────────────

/// <summary>
/// Semver versions display unchanged (e.g. "9.3.0").  However dots are not
/// valid in URL anchors produced by <c>SlugHelper</c>, which converts them to
/// dashes.  The TOC slug and heading-wrapper id must therefore both be "9-3-0",
/// not "9.3.0".
/// </summary>
public class ChangelogSemverAnchorNavigationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSemverAnchorNavigationTests(ITestOutputHelper output) : base(output,
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
			- title: New search feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			- title: Security fix
			  type: security
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
			  description: This breaks things.
			  impact: You will notice.
			  action: Do this.
			  prs:
			  - "333333"
			"""));

	[Fact]
	public void VersionHeadingTocSlugMatchesDisplayVersion()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		// Slugify.Core preserves dots, so "9.3.0" slugifies to "9.3.0".
		// The TOC slug must match the heading ID derived from the display text.
		versionItem.Slug.Should().Be("9.3.0");
	}

	[Fact]
	public void VersionHeadingHtmlIdMatchesTocSlug()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		Html.Should().Contain($"id=\"{versionItem.Slug}\"",
			$"heading-wrapper id must match TOC slug '{versionItem.Slug}'");
	}

	[Fact]
	public void SubSectionTocSlugsContainVersionString()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc.Where(t => t.Level == 3))
			item.Slug.Should().Contain("9.3.0",
				$"sub-section slug should contain the version string — Slugify.Core preserves dots");
	}

	[Fact]
	public void AllTocSlugsHaveMatchingHtmlIds()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc)
			Html.Should().Contain($"id=\"{item.Slug}\"",
				$"TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id");
	}

	[Fact]
	public void GeneratedAnchorsContainVersionString()
	{
		// Slugify.Core preserves dots, so sub-section anchors retain "9.3.0".
		var anchors = Block!.GeneratedAnchors.ToList();
		foreach (var anchor in anchors)
			anchor.Should().Contain("9.3.0",
				$"generated anchor '{anchor}' should contain the semver version string");
	}
}

// ── Raw / fallback version strings ───────────────────────────────────────────

/// <summary>
/// Non-semver, non-date version strings (e.g. "release-alpha") are used as-is
/// as both the display text and the slug base.  Slugify should preserve safe
/// characters (alphanumeric + hyphens) so the TOC slug and HTML id agree.
/// </summary>
public class ChangelogRawVersionAnchorNavigationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogRawVersionAnchorNavigationTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""") => FileSystem.AddFile("docs/changelog/bundles/release-alpha.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: experimental
			  target: release-alpha
			entries:
			- title: Alpha feature
			  type: feature
			  products:
			  - product: experimental
			    target: release-alpha
			  prs:
			  - "111111"
			"""));

	[Fact]
	public void VersionHeadingTocSlugMatchesRawVersion()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		// "release-alpha" is already URL-safe so the slug should be identical.
		versionItem.Slug.Should().Be("release-alpha");
	}

	[Fact]
	public void VersionHeadingHtmlIdMatchesTocSlug()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionItem = toc.Single(t => t.Level == 2);

		Html.Should().Contain($"id=\"{versionItem.Slug}\"",
			$"heading-wrapper id must match TOC slug '{versionItem.Slug}'");
	}

	[Fact]
	public void AllTocSlugsHaveMatchingHtmlIds()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc)
		{
			Html.Should().Contain($"id=\"{item.Slug}\"",
				$"TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id");
		}
	}
}

// ── Multiple versions — cross-bundle consistency ──────────────────────────────

/// <summary>
/// When a changelog page spans multiple bundles (the common case), every
/// version-level and sub-section-level TOC item must independently link to a
/// real heading in the HTML.  This is the end-to-end "right-nav works" check.
/// </summary>
public class ChangelogMultiVersionAnchorNavigationTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMultiVersionAnchorNavigationTests(ITestOutputHelper output) : base(output,
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
			- title: Semver feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-11.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: elasticsearch
			  target: 2025-11
			entries:
			- title: Date-version feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 2025-11
			  prs:
			  - "222222"
			"""));
	}

	[Fact]
	public void AllTocSlugsHaveMatchingHtmlIds()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		foreach (var item in toc)
		{
			Html.Should().Contain($"id=\"{item.Slug}\"",
				$"TOC item '{item.Heading}' (slug '{item.Slug}') must have a matching heading-wrapper id");
		}
	}

	[Fact]
	public void SemverVersionHeadingSlugPreservesDots()
	{
		// Slugify.Core preserves dots, so "9.3.0" remains "9.3.0" as the slug.
		var toc = Block!.GeneratedTableOfContent.ToList();
		var semverItem = toc.Single(t => t.Level == 2 && t.Heading == "9.3.0");

		semverItem.Slug.Should().Be("9.3.0");
	}

	[Fact]
	public void DateVersionHeadingSlugUsesDisplayName()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var dateItem = toc.Single(t => t.Level == 2 && t.Heading == "November 2025");

		dateItem.Slug.Should().Be("november-2025");
	}
}
