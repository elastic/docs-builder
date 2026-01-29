// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

public class ChangelogMergeSameTargetTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogMergeSameTargetTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:merge:
		:::
		""")
	{
		// Cloud Serverless scenario: multiple repos contributing to the same dated release
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
			// language=yaml
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
			// language=yaml
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
			// language=yaml
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
			// language=yaml
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
		aug5Bundle!.Entries.Should().HaveCount(4);
	}

	[Fact]
	public void MergedBundleHasCombinedRepoName()
	{
		var aug5Bundle = Block!.LoadedBundles.FirstOrDefault(b => b.Version == "2025-08-05");
		aug5Bundle.Should().NotBeNull();
		// Repos should be combined and sorted alphabetically
		aug5Bundle!.Repo.Should().Contain("elasticsearch");
		aug5Bundle.Repo.Should().Contain("kibana");
		aug5Bundle.Repo.Should().Contain("elasticsearch-serverless");
		aug5Bundle.Repo.Should().Contain("+");
	}

	[Fact]
	public void RendersOnlyOneVersionHeaderPerMergedTarget()
	{
		// Should render only one version header for 2025-08-05, not three separate ones
		// Count occurrences of the version string in h2 context
		var aug05Count = CountOccurrences(Html, ">2025-08-05<");
		var aug01Count = CountOccurrences(Html, ">2025-08-01<");

		aug05Count.Should().Be(1, "Should have exactly 1 version header for 2025-08-05 (merged)");
		aug01Count.Should().Be(1, "Should have exactly 1 version header for 2025-08-01");
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
		// language=markdown
		"""
		:::{changelog}
		:merge:
		:::
		""")
	{
		// Bundles with different targets should remain separate even with :merge:
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
			"""));

		FileSystem.AddFile("docs/changelog/bundles/9.2.0.yaml", new MockFileData(
			// language=yaml
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
			// language=yaml
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
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Same setup as merge enabled, but without :merge: option
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
			// language=yaml
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
			// language=yaml
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
		// language=markdown
		"""
		:::{changelog}
		:merge: false
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/kibana-2025-08-05.yaml", new MockFileData(
			// language=yaml
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
			// language=yaml
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
