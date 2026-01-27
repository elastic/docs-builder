// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogBasicTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBasicTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") =>
		// Create the default bundles folder with a test bundle
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
  lifecycle: ga
entries:
- title: Add new feature
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Search
  pr: "123456"
  description: This is a great new feature.
- title: Fix important bug
  type: bug-fix
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Indexing
  pr: "123457"
"""));

	[Fact]
	public void ParsesChangelogBlock() => Block.Should().NotBeNull();

	[Fact]
	public void SetsCorrectDirectiveType() => Block!.Directive.Should().Be("changelog");

	[Fact]
	public void FindsBundlesFolder() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().EndWith("changelog/bundles");

	[Fact]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(1);

	[Fact]
	public void RendersMarkdownContent()
	{
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("Features and enhancements");
		Html.Should().Contain("Add new feature");
		Html.Should().Contain("Fixes");
		Html.Should().Contain("Fix important bug");
	}
}

public class ChangelogMultipleBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMultipleBundlesTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""")
	{
		// Create multiple bundles with different versions
		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.2.0
entries:
- title: Feature in 9.2.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.2.0
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
  pr: "222222"
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.10.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.10.0
entries:
- title: Feature in 9.10.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.10.0
  pr: "333333"
"""));
	}

	[Fact]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(3);

	[Fact]
	public void RendersInSemverOrder()
	{
		// Should be sorted by semver descending: 9.10.0 > 9.3.0 > 9.2.0
		var idx910 = Html.IndexOf("9.10.0", StringComparison.Ordinal);
		var idx93 = Html.IndexOf("9.3.0", StringComparison.Ordinal);
		var idx92 = Html.IndexOf("9.2.0", StringComparison.Ordinal);

		idx910.Should().BeLessThan(idx93, "9.10.0 should appear before 9.3.0");
		idx93.Should().BeLessThan(idx92, "9.3.0 should appear before 9.2.0");
	}

	[Fact]
	public void RendersAllVersions()
	{
		Html.Should().Contain("9.10.0");
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("9.2.0");
	}
}

public class ChangelogCustomPathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogCustomPathTests(ITestOutputHelper output) : base(output,
"""
:::{changelog} release-notes/bundles
:::
""") => FileSystem.AddFile("docs/release-notes/bundles/1.0.0.yaml", new MockFileData(
"""
products:
- product: my-product
  target: 1.0.0
entries:
- title: First release
  type: feature
  products:
  - product: my-product
    target: 1.0.0
  pr: "1"
"""));

	[Fact]
	public void FindsBundlesFolder() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().EndWith("release-notes/bundles");

	[Fact]
	public void RendersContent()
	{
		Html.Should().Contain("1.0.0");
		Html.Should().Contain("First release");
	}
}

public class ChangelogNotFoundTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
"""
:::{changelog} missing-bundles
:::
""")
{
	[Fact]
	public void ReportsFolderNotFound() => Block!.Found.Should().BeFalse();

	[Fact]
	public void EmitsErrorForMissingFolder()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

public class ChangelogDefaultPathMissingTests(ITestOutputHelper output) : DirectiveTest<ChangelogBlock>(output,
"""
:::{changelog}
:::
""")
{
	[Fact]
	public void EmitsErrorForMissingDefaultFolder()
	{
		// No bundles folder created, so it should emit an error
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("does not exist"));
	}
}

public class ChangelogWithBreakingChangesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithBreakingChangesTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Breaking change in API
  type: breaking-change
  products:
  - product: elasticsearch
    target: 9.3.0
  description: The API has changed significantly.
  impact: Users must update their code.
  action: Follow the migration guide.
  pr: "222222"
"""));

	[Fact]
	public void RendersBreakingChangesSection()
	{
		Html.Should().Contain("Breaking changes");
		Html.Should().Contain("Breaking change in API");
	}

	[Fact]
	public void RendersImpactAndAction()
	{
		Html.Should().Contain("Impact");
		Html.Should().Contain("Users must update their code");
		Html.Should().Contain("Action");
		Html.Should().Contain("Follow the migration guide");
	}
}

public class ChangelogWithDeprecationsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithDeprecationsTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Deprecated old API
  type: deprecation
  products:
  - product: elasticsearch
    target: 9.3.0
  description: The old API is deprecated.
  impact: The API will be removed in a future version.
  action: Use the new API instead.
  pr: "333333"
"""));

	[Fact]
	public void RendersDeprecationsSection()
	{
		Html.Should().Contain("Deprecations");
		Html.Should().Contain("Deprecated old API");
	}
}

public class ChangelogEmptyBundleTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyBundleTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries: []
"""));

	[Fact]
	public void HandlesEmptyBundle()
	{
		Html.Should().Contain("No new features, enhancements, or fixes");
	}
}

public class ChangelogEmptyFolderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogEmptyFolderTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") =>
		// Create the folder but don't add any YAML files
		FileSystem.AddDirectory("docs/changelog/bundles");

	[Fact]
	public void ReportsFolderEmpty() => Block!.Found.Should().BeFalse();

	[Fact]
	public void EmitsErrorForEmptyFolder()
	{
		Collector.Diagnostics.Should().NotBeNullOrEmpty();
		Collector.Diagnostics.Should().OnlyContain(d => d.Message.Contains("contains no YAML files"));
	}
}

public class ChangelogAbsolutePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogAbsolutePathTests(ITestOutputHelper output) : base(output,
"""
:::{changelog} /release-notes/bundles
:::
""") => FileSystem.AddFile("docs/release-notes/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Test feature
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  pr: "444444"
"""));

	[Fact]
	public void FindsBundlesFolderWithAbsolutePath() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SetsCorrectBundlesFolderPath() => Block!.BundlesFolderPath.Should().Contain("release-notes");
}

public class ChangelogSubsectionsDisabledByDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsDisabledByDefaultTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Feature in Search
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Search
  pr: "111111"
- title: Feature in Indexing
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Indexing
  pr: "222222"
"""));

	[Fact]
	public void SubsectionsPropertyDefaultsToFalse() => Block!.Subsections.Should().BeFalse();

	[Fact]
	public void DoesNotRenderAreaHeaders()
	{
		// When subsections is false, area headers should not be rendered
		Html.Should().NotContain("<strong>Search</strong>");
		Html.Should().NotContain("<strong>Indexing</strong>");
	}

	[Fact]
	public void RendersEntriesWithoutGrouping()
	{
		// Both entries should be rendered without area grouping
		Html.Should().Contain("Feature in Search");
		Html.Should().Contain("Feature in Indexing");
	}
}

public class ChangelogSubsectionsEnabledTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsEnabledTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:subsections:
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Feature in Search
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Search
  pr: "111111"
- title: Feature in Indexing
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Indexing
  pr: "222222"
"""));

	[Fact]
	public void SubsectionsPropertyIsTrue() => Block!.Subsections.Should().BeTrue();

	[Fact]
	public void RendersAreaHeaders()
	{
		// When subsections is true, area headers should be rendered
		Html.Should().Contain("<strong>Search</strong>");
		Html.Should().Contain("<strong>Indexing</strong>");
	}

	[Fact]
	public void RendersEntriesUnderCorrectAreas()
	{
		// Both entries should be rendered
		Html.Should().Contain("Feature in Search");
		Html.Should().Contain("Feature in Indexing");
	}
}

public class ChangelogSubsectionsExplicitFalseTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSubsectionsExplicitFalseTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:subsections: false
:::
""") => FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.3.0
entries:
- title: Feature in Search
  type: feature
  products:
  - product: elasticsearch
    target: 9.3.0
  areas:
  - Search
  pr: "111111"
"""));

	[Fact]
	public void SubsectionsPropertyIsFalse() => Block!.Subsections.Should().BeFalse();

	[Fact]
	public void DoesNotRenderAreaHeaders() => Html.Should().NotContain("<strong>Search</strong>");
}

public class ChangelogDateVersionedBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDateVersionedBundlesTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""")
	{
		// Create multiple bundles with date-based versions (Cloud Serverless style)
		FileSystem.AddFile("docs/changelog/bundles/2025-08-01.yaml", new MockFileData(
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
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-08-15.yaml", new MockFileData(
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
  pr: "222222"
"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-08-05.yaml", new MockFileData(
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
  pr: "333333"
"""));
	}

	[Fact]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(3);

	[Fact]
	public void RendersInDateOrderDescending()
	{
		// Should be sorted by date descending: 2025-08-15 > 2025-08-05 > 2025-08-01
		var idx15 = Html.IndexOf("2025-08-15", StringComparison.Ordinal);
		var idx05 = Html.IndexOf("2025-08-05", StringComparison.Ordinal);
		var idx01 = Html.IndexOf("2025-08-01", StringComparison.Ordinal);

		idx15.Should().BeLessThan(idx05, "2025-08-15 should appear before 2025-08-05");
		idx05.Should().BeLessThan(idx01, "2025-08-05 should appear before 2025-08-01");
	}

	[Fact]
	public void RendersAllDateVersions()
	{
		Html.Should().Contain("2025-08-15");
		Html.Should().Contain("2025-08-05");
		Html.Should().Contain("2025-08-01");
	}

	[Fact]
	public void RendersEntriesForDateVersions()
	{
		Html.Should().Contain("August 15th feature");
		Html.Should().Contain("August 5th feature");
		Html.Should().Contain("August 1st feature");
	}
}

public class ChangelogMixedVersionTypesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMixedVersionTypesTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""")
	{
		// Create bundles with mixed version types (semver and dates)
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
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
  pr: "222222"
"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-08-05.yaml", new MockFileData(
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
  pr: "333333"
"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-07-01.yaml", new MockFileData(
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
  pr: "444444"
"""));
	}

	[Fact]
	public void LoadsAllBundles() => Block!.LoadedBundles.Should().HaveCount(4);

	[Fact]
	public void SemverVersionsAppearBeforeDates()
	{
		// Semver versions should appear before date versions
		var idx93 = Html.IndexOf("9.3.0", StringComparison.Ordinal);
		var idx92 = Html.IndexOf("9.2.0", StringComparison.Ordinal);
		var idxDate1 = Html.IndexOf("2025-08-05", StringComparison.Ordinal);
		var idxDate2 = Html.IndexOf("2025-07-01", StringComparison.Ordinal);

		// Semver versions should come before dates
		idx93.Should().BeLessThan(idxDate1, "Semver 9.3.0 should appear before date 2025-08-05");
		idx92.Should().BeLessThan(idxDate1, "Semver 9.2.0 should appear before date 2025-08-05");

		// Within semver, should be sorted correctly
		idx93.Should().BeLessThan(idx92, "9.3.0 should appear before 9.2.0");

		// Within dates, should be sorted correctly (descending)
		idxDate1.Should().BeLessThan(idxDate2, "2025-08-05 should appear before 2025-07-01");
	}

	[Fact]
	public void RendersAllVersions()
	{
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("9.2.0");
		Html.Should().Contain("2025-08-05");
		Html.Should().Contain("2025-07-01");
	}

	[Fact]
	public void RendersAllEntries()
	{
		Html.Should().Contain("Semver 9.3.0 feature");
		Html.Should().Contain("Semver 9.2.0 feature");
		Html.Should().Contain("Date-based feature");
		Html.Should().Contain("Earlier date feature");
	}
}

public class ChangelogRawVersionFallbackTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogRawVersionFallbackTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""")
	{
		// Create bundles with non-standard version formats (edge case)
		FileSystem.AddFile("docs/changelog/bundles/release-alpha.yaml", new MockFileData(
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
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/release-beta.yaml", new MockFileData(
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
  pr: "222222"
"""));
	}

	[Fact]
	public void LoadsBundles() => Block!.LoadedBundles.Should().HaveCount(2);

	[Fact]
	public void RendersNonStandardVersions()
	{
		// Both non-standard versions should be rendered (sorted lexicographically)
		Html.Should().Contain("release-alpha");
		Html.Should().Contain("release-beta");
		Html.Should().Contain("Alpha release feature");
		Html.Should().Contain("Beta release feature");
	}

	[Fact]
	public void SortsLexicographically()
	{
		// "release-beta" > "release-alpha" lexicographically
		var idxBeta = Html.IndexOf("release-beta", StringComparison.Ordinal);
		var idxAlpha = Html.IndexOf("release-alpha", StringComparison.Ordinal);

		idxBeta.Should().BeLessThan(idxAlpha, "release-beta should appear before release-alpha (descending lexicographic)");
	}
}

public class ChangelogMergeSameTargetTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMergeSameTargetTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:merge:
:::
""")
	{
		// Cloud Serverless scenario: multiple repos contributing to the same dated release
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
"""
products:
- product: kibana
  target: 2025-08-05
entries:
- title: Kibana feature for August 5th
  type: feature
  products:
  - product: kibana
    target: 2025-08-05
  areas:
  - Dashboard
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/elasticsearch-2025-08-05.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 2025-08-05
entries:
- title: Elasticsearch feature for August 5th
  type: feature
  products:
  - product: elasticsearch
    target: 2025-08-05
  areas:
  - Search
  pr: "222222"
- title: Elasticsearch bugfix for August 5th
  type: bug-fix
  products:
  - product: elasticsearch
    target: 2025-08-05
  pr: "222223"
"""));

		FileSystem.AddFile("docs/changelog/bundles/serverless-2025-08-05.yaml", new MockFileData(
"""
products:
- product: elasticsearch-serverless
  target: 2025-08-05
entries:
- title: Serverless feature for August 5th
  type: feature
  products:
  - product: elasticsearch-serverless
    target: 2025-08-05
  areas:
  - API
  pr: "333333"
"""));

		// A different release date with single bundle
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-01.yaml", new MockFileData(
"""
products:
- product: kibana
  target: 2025-08-01
entries:
- title: Kibana feature for August 1st
  type: feature
  products:
  - product: kibana
    target: 2025-08-01
  pr: "444444"
"""));
	}

	[Fact]
	public void MergeSameTargetPropertyIsTrue() => Block!.MergeSameTarget.Should().BeTrue();

	[Fact]
	public void MergesBundlesWithSameTarget() =>
		// Three bundles with 2025-08-05 should be merged into one
		// Plus one bundle with 2025-08-01 = 2 total bundles
		Block!.LoadedBundles.Should().HaveCount(2);

	[Fact]
	public void MergedBundleContainsAllEntries()
	{
		// The 2025-08-05 merged bundle should have 4 entries (1 + 2 + 1)
		var aug5Bundle = Block!.LoadedBundles.FirstOrDefault(b => b.Version == "2025-08-05");
		aug5Bundle.Should().NotBeNull();
		aug5Bundle.Entries.Should().HaveCount(4);
	}

	[Fact]
	public void MergedBundleHasCombinedRepoName()
	{
		var aug5Bundle = Block!.LoadedBundles.FirstOrDefault(b => b.Version == "2025-08-05");
		aug5Bundle.Should().NotBeNull();
		// Repos should be combined and sorted alphabetically
		aug5Bundle.Repo.Should().Contain("elasticsearch");
		aug5Bundle.Repo.Should().Contain("kibana");
		aug5Bundle.Repo.Should().Contain("elasticsearch-serverless");
		aug5Bundle.Repo.Should().Contain("+");
	}

	[Fact]
	public void RendersOnlyOneSectionPerMergedTarget()
	{
		// Should render only one ## 2025-08-05 header, not three
		// The version appears in the header and potentially in anchor links
		// But there should be only ONE h2 header for this version
		var h2Count = CountOccurrences(Html, "<h2");
		h2Count.Should().Be(2, "Should have exactly 2 h2 headers (one per merged target)");
	}

	[Fact]
	public void RendersAllEntriesFromMergedBundles()
	{
		Html.Should().Contain("Kibana feature for August 5th");
		Html.Should().Contain("Elasticsearch feature for August 5th");
		Html.Should().Contain("Elasticsearch bugfix for August 5th");
		Html.Should().Contain("Serverless feature for August 5th");
	}

	[Fact]
	public void MaintainsCorrectDateOrder()
	{
		// 2025-08-05 should appear before 2025-08-01 (descending order)
		var idx05 = Html.IndexOf("2025-08-05", StringComparison.Ordinal);
		var idx01 = Html.IndexOf("2025-08-01", StringComparison.Ordinal);

		idx05.Should().BeLessThan(idx01, "2025-08-05 should appear before 2025-08-01");
	}

	private static int CountOccurrences(string text, string pattern)
	{
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
		{
			count++;
			index += pattern.Length;
		}
		return count;
	}
}

public class ChangelogMergeDifferentTargetsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMergeDifferentTargetsTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:merge:
:::
""")
	{
		// Bundles with different targets should remain separate even with :merge:
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.2.0
entries:
- title: Feature in 9.2.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.2.0
  pr: "222222"
"""));

		FileSystem.AddFile("docs/changelog/bundles/9.1.0.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 9.1.0
entries:
- title: Feature in 9.1.0
  type: feature
  products:
  - product: elasticsearch
    target: 9.1.0
  pr: "333333"
"""));
	}

	[Fact]
	public void KeepsDifferentTargetsSeparate()
	{
		// All three bundles have different targets, so no merging should happen
		Block!.LoadedBundles.Should().HaveCount(3);
	}

	[Fact]
	public void RendersAllVersionsSeparately()
	{
		Html.Should().Contain("9.3.0");
		Html.Should().Contain("9.2.0");
		Html.Should().Contain("9.1.0");
	}

	[Fact]
	public void MaintainsSemverOrder()
	{
		var idx93 = Html.IndexOf("9.3.0", StringComparison.Ordinal);
		var idx92 = Html.IndexOf("9.2.0", StringComparison.Ordinal);
		var idx91 = Html.IndexOf("9.1.0", StringComparison.Ordinal);

		idx93.Should().BeLessThan(idx92, "9.3.0 should appear before 9.2.0");
		idx92.Should().BeLessThan(idx91, "9.2.0 should appear before 9.1.0");
	}
}

public class ChangelogMergeDisabledByDefaultTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMergeDisabledByDefaultTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:::
""")
	{
		// Same setup as merge enabled, but without :merge: option
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
"""
products:
- product: kibana
  target: 2025-08-05
entries:
- title: Kibana feature
  type: feature
  products:
  - product: kibana
    target: 2025-08-05
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/elasticsearch-2025-08-05.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 2025-08-05
entries:
- title: Elasticsearch feature
  type: feature
  products:
  - product: elasticsearch
    target: 2025-08-05
  pr: "222222"
"""));
	}

	[Fact]
	public void MergeSameTargetPropertyDefaultsToFalse() => Block!.MergeSameTarget.Should().BeFalse();

	[Fact]
	public void DoesNotMergeBundlesWithSameTarget()
	{
		// Without :merge:, same-target bundles should remain separate
		Block!.LoadedBundles.Should().HaveCount(2);
	}

	[Fact]
	public void RendersSeparateSectionsForSameTarget()
	{
		// Should render both bundles as separate sections
		Html.Should().Contain("Kibana feature");
		Html.Should().Contain("Elasticsearch feature");
	}
}

public class ChangelogMergeExplicitFalseTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMergeExplicitFalseTests(ITestOutputHelper output) : base(output,
"""
:::{changelog}
:merge: false
:::
""")
	{
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
"""
products:
- product: kibana
  target: 2025-08-05
entries:
- title: Kibana feature
  type: feature
  products:
  - product: kibana
    target: 2025-08-05
  pr: "111111"
"""));

		FileSystem.AddFile("docs/changelog/bundles/elasticsearch-2025-08-05.yaml", new MockFileData(
"""
products:
- product: elasticsearch
  target: 2025-08-05
entries:
- title: Elasticsearch feature
  type: feature
  products:
  - product: elasticsearch
    target: 2025-08-05
  pr: "222222"
"""));
	}

	[Fact]
	public void MergeSameTargetPropertyIsFalse() => Block!.MergeSameTarget.Should().BeFalse();

	[Fact]
	public void DoesNotMergeBundlesWhenExplicitlyDisabled() => Block!.LoadedBundles.Should().HaveCount(2);
}
