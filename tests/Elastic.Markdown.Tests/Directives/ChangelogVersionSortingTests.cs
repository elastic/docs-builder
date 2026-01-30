// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogDateVersionedBundlesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogDateVersionedBundlesTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Create multiple bundles with date-based versions (Cloud Serverless style)
		FileSystem.AddFile("docs/changelog/bundles/2025-08-01.yaml", new MockFileData(
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
			  pr: "111111"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-08-15.yaml", new MockFileData(
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
			  pr: "222222"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-08-05.yaml", new MockFileData(
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
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Create bundles with mixed version types (semver and dates)
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
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
			  pr: "111111"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
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
			  pr: "222222"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-08-05.yaml", new MockFileData(
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
			  pr: "333333"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/2025-07-01.yaml", new MockFileData(
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
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Create bundles with non-standard version formats (edge case)
		FileSystem.AddFile("docs/changelog/bundles/release-alpha.yaml", new MockFileData(
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
			  pr: "111111"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/release-beta.yaml", new MockFileData(
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
