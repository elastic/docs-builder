// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Elastic.Markdown.Myst.Directives.Changelog;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Directives;

[InheritsTests]
public class ChangelogDateVersionedBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDateVersionedBundlesTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		// Create multiple bundles with date-based versions (Cloud Serverless style)
		FileSystem.AddFile(
			"docs/changelog/bundles/2025-08-01.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-serverless
			  target: 2025-08-01
			entries:
			- title: August 1st feature
			  type: feature
			  products:
			  - product: cloud-serverless
			    target: 2025-08-01
			  prs:
			  - "111111"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/2025-08-15.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-serverless
			  target: 2025-08-15
			entries:
			- title: August 15th feature
			  type: feature
			  products:
			  - product: cloud-serverless
			    target: 2025-08-15
			  prs:
			  - "222222"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/2025-08-05.yaml",
			new MockFileData(
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
			  - "333333"
			"""
			)
		);
	}

	[Test]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(3);

	[Test]
	public void RendersInDateOrderDescending()
	{
		// Should be sorted by date descending: 2025-08-15 > 2025-08-05 > 2025-08-01
		var idx15 = Html.IndexOf("August 15, 2025", StringComparison.Ordinal);
		var idx05 = Html.IndexOf("August 5, 2025", StringComparison.Ordinal);
		var idx01 = Html.IndexOf("August 1, 2025", StringComparison.Ordinal);

		idx15.Should().BeLessThan(idx05, "August 15, 2025 should appear before August 5, 2025");
		idx05.Should().BeLessThan(idx01, "August 5, 2025 should appear before August 1, 2025");
	}

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

[InheritsTests]
public class ChangelogMixedVersionTypesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMixedVersionTypesTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		// Create bundles with mixed version types (semver and dates)
		FileSystem.AddFile(
			"docs/changelog/bundles/9.3.0.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: elasticsearch
			  target: 9.3.0
			entries:
			- title: Semver 9.3.0 feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.3.0
			  prs:
			  - "111111"
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
			- title: Semver 9.2.0 feature
			  type: feature
			  products:
			  - product: elasticsearch
			    target: 9.2.0
			  prs:
			  - "222222"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/2025-08-05.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-serverless
			  target: 2025-08-05
			entries:
			- title: Date-based feature
			  type: feature
			  products:
			  - product: cloud-serverless
			    target: 2025-08-05
			  prs:
			  - "333333"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/2025-07-01.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-serverless
			  target: 2025-07-01
			entries:
			- title: Earlier date feature
			  type: feature
			  products:
			  - product: cloud-serverless
			    target: 2025-07-01
			  prs:
			  - "444444"
			"""
			)
		);
	}

	[Test]
	public void LoadsAllBundles() => Block!.LoadedBundles.Should().HaveCount(4);

	[Test]
	public void SemverVersionsAppearBeforeDates()
	{
		// Semver versions should appear before date versions
		var idx93 = Html.IndexOf("9.3.0", StringComparison.Ordinal);
		var idx92 = Html.IndexOf("9.2.0", StringComparison.Ordinal);
		var idxDate1 = Html.IndexOf("August 5, 2025", StringComparison.Ordinal);
		var idxDate2 = Html.IndexOf("July 1, 2025", StringComparison.Ordinal);

		// Semver versions should come before dates
		idx93.Should().BeLessThan(idxDate1, "Semver 9.3.0 should appear before date August 5, 2025");
		idx92.Should().BeLessThan(idxDate1, "Semver 9.2.0 should appear before date August 5, 2025");

		// Within semver, should be sorted correctly
		idx93.Should().BeLessThan(idx92, "9.3.0 should appear before 9.2.0");

		// Within dates, should be sorted correctly (descending)
		idxDate1.Should().BeLessThan(idxDate2, "August 5, 2025 should appear before July 1, 2025");
	}

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());
}

/// <summary>
/// Tests for year-month (yyyy-MM) versioned bundles, as used by Cloud Hosted release notes.
/// Verifies that headings render as "Month Year" (e.g., "December 2025") and that
/// slugs/anchors retain the original yyyy-MM format.
/// </summary>
[InheritsTests]
public class ChangelogYearMonthVersionTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogYearMonthVersionTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/2025-12.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-hosted
			  target: 2025-12
			entries:
			- title: December feature
			  type: feature
			  products:
			  - product: cloud-hosted
			    target: 2025-12
			  prs:
			  - "111111"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/2025-10.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-hosted
			  target: 2025-10
			entries:
			- title: October feature
			  type: feature
			  products:
			  - product: cloud-hosted
			    target: 2025-10
			  prs:
			  - "222222"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/2025-08.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: cloud-hosted
			  target: 2025-08
			entries:
			- title: August feature
			  type: feature
			  products:
			  - product: cloud-hosted
			    target: 2025-08
			  prs:
			  - "333333"
			- title: August bugfix
			  type: bug-fix
			  products:
			  - product: cloud-hosted
			    target: 2025-08
			  prs:
			  - "333334"
			"""
			)
		);
	}

	[Test]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(3);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());

	[Test]
	public void RendersInDescendingMonthOrder()
	{
		var idxDec = Html.IndexOf("December 2025", StringComparison.Ordinal);
		var idxOct = Html.IndexOf("October 2025", StringComparison.Ordinal);
		var idxAug = Html.IndexOf("August 2025", StringComparison.Ordinal);

		idxDec.Should().BeLessThan(idxOct, "December 2025 should appear before October 2025");
		idxOct.Should().BeLessThan(idxAug, "October 2025 should appear before August 2025");
	}

	[Test]
	public void TocUsesMonthYearHeadings()
	{
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionHeadings = toc.Where(t => t.Level == 2).Select(t => t.Heading).ToList();

		versionHeadings.Should().Contain("December 2025");
		versionHeadings.Should().Contain("October 2025");
		versionHeadings.Should().Contain("August 2025");
	}

	[Test]
	public void TocSlugMatchesHeadingId()
	{
		// The TOC slug must match the heading ID that SectionedHeadingRenderer derives from
		// the display text, so that right-nav links scroll to the correct section.
		var toc = Block!.GeneratedTableOfContent.ToList();
		var versionSlugs = toc.Where(t => t.Level == 2).Select(t => t.Slug).ToList();

		versionSlugs.Should().Contain("december-2025");
		versionSlugs.Should().Contain("october-2025");
		versionSlugs.Should().Contain("august-2025");
	}
}

[InheritsTests]
public class ChangelogRawVersionFallbackTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogRawVersionFallbackTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:::
		"""
		)
	{
		// Create bundles with non-standard version formats (edge case)
		FileSystem.AddFile(
			"docs/changelog/bundles/release-alpha.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: experimental
			  target: release-alpha
			entries:
			- title: Alpha release feature
			  type: feature
			  products:
			  - product: experimental
			    target: release-alpha
			  prs:
			  - "111111"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/changelog/bundles/release-beta.yaml",
			new MockFileData(
				// language=yaml
				"""
			products:
			- product: experimental
			  target: release-beta
			entries:
			- title: Beta release feature
			  type: feature
			  products:
			  - product: experimental
			    target: release-beta
			  prs:
			  - "222222"
			"""
			)
		);
	}

	[Test]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(2);

	[Test]
	public void RendersHtml() => Html.Should().MatchHtmlSnapshot(DocsHtml.Body());

	[Test]
	public void SortsLexicographically()
	{
		// "release-beta" > "release-alpha" lexicographically
		var idxBeta = Html.IndexOf("release-beta", StringComparison.Ordinal);
		var idxAlpha = Html.IndexOf("release-alpha", StringComparison.Ordinal);

		idxBeta.Should().BeLessThan(idxAlpha, "release-beta should appear before release-alpha (descending lexicographic)");
	}
}
