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
		// language=markdown
		"""
		:::{changelog}
		:::
		""") =>
		// Create the default bundles folder with a test bundle
		FileSystem.AddFile("docs/changelog/bundles/9.3.0.yaml", new MockFileData(
			// language=yaml
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
		// language=markdown
		"""
		:::{changelog}
		:::
		""")
	{
		// Create multiple bundles with different versions
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
			  pr: "111111"
			"""));

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
			  pr: "222222"
			"""));

		FileSystem.AddFile("docs/changelog/bundles/9.10.0.yaml", new MockFileData(
			// language=yaml
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
		// language=markdown
		"""
		:::{changelog} release-notes/bundles
		:::
		""") => FileSystem.AddFile("docs/release-notes/bundles/1.0.0.yaml", new MockFileData(
		// language=yaml
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
	// language=markdown
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
	// language=markdown
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

/// <summary>
/// Tests for breaking changes rendering.
/// Breaking changes should always render on the page with the new ordering (critical types first).
/// </summary>
public class ChangelogWithBreakingChangesTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithBreakingChangesTests(ITestOutputHelper output) : base(output,
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

/// <summary>
/// Tests for deprecations rendering.
/// Deprecations should always render on the page with the new ordering (critical types first).
/// </summary>
public class ChangelogWithDeprecationsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogWithDeprecationsTests(ITestOutputHelper output) : base(output,
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
		// language=markdown
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
		// language=markdown
		"""
		:::{changelog} /release-notes/bundles
		:::
		""") => FileSystem.AddFile("docs/release-notes/bundles/9.3.0.yaml", new MockFileData(
		// language=yaml
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

/// <summary>
/// Tests the section order - critical types (breaking changes, security, known issues, deprecations)
/// should appear BEFORE features/fixes.
/// </summary>
public class ChangelogSectionOrderTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogSectionOrderTests(ITestOutputHelper output) : base(output,
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
		- title: Security fix
		  type: security
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
		- title: Bug fix
		  type: bug-fix
		  products:
		  - product: elasticsearch
		    target: 9.3.0
		  pr: "666666"
		"""));

	[Fact]
	public void BreakingChangesAppearsFirst()
	{
		var breakingIdx = Html.IndexOf("Breaking changes", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);
		var fixesIdx = Html.IndexOf(">Fixes<", StringComparison.Ordinal);

		breakingIdx.Should().BeLessThan(featuresIdx, "Breaking changes should appear before Features");
		breakingIdx.Should().BeLessThan(fixesIdx, "Breaking changes should appear before Fixes");
	}

	[Fact]
	public void SecurityAppearsBeforeFeatures()
	{
		var securityIdx = Html.IndexOf(">Security<", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);

		securityIdx.Should().BeLessThan(featuresIdx, "Security should appear before Features");
	}

	[Fact]
	public void KnownIssuesAppearsBeforeFeatures()
	{
		var knownIssuesIdx = Html.IndexOf("Known issues", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);

		knownIssuesIdx.Should().BeLessThan(featuresIdx, "Known issues should appear before Features");
	}

	[Fact]
	public void DeprecationsAppearsBeforeFeatures()
	{
		var deprecationsIdx = Html.IndexOf("Deprecations", StringComparison.Ordinal);
		var featuresIdx = Html.IndexOf("Features and enhancements", StringComparison.Ordinal);

		deprecationsIdx.Should().BeLessThan(featuresIdx, "Deprecations should appear before Features");
	}
}

/// <summary>
/// Tests header levels: ## (h2) for versions, ### (h3) for sections.
/// </summary>
public class ChangelogHeaderLevelsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogHeaderLevelsTests(ITestOutputHelper output) : base(output,
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
		"""));

	[Fact]
	public void VersionHeaderIsH2()
	{
		// Version should be h2
		Html.Should().Contain("<h2");
		Html.Should().Contain("9.3.0");
	}

	[Fact]
	public void OnlyOneH2ForVersion()
	{
		// Only one h2 for the version header
		var h2Count = CountOccurrences(Html, "<h2");
		h2Count.Should().Be(1, "Should have exactly one h2 for the version");
	}

	[Fact]
	public void SectionHeadersAreH3()
	{
		// Section headers should be h3 (children of version)
		Html.Should().Contain("<h3");
		// Should have h3 for features + fixes = 2
		var h3Count = CountOccurrences(Html, "<h3");
		h3Count.Should().Be(2, "Should have h3 for each section (features, fixes)");
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
