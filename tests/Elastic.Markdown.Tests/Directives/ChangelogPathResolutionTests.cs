// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using Elastic.Markdown.Myst.Directives.Changelog;
using FluentAssertions;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for path resolution in the changelog directive.
/// Verifies that Path.Combine issues are properly handled for:
/// - Relative paths (combined with docset root)
/// - Docset-root-relative paths (starting with '/', combined with docset root after trimming)
/// - Absolute filesystem paths (used as-is, prevents Path.Combine from silently dropping base)
/// </summary>
public class ChangelogBundlesFolderRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBundlesFolderRelativePathTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog} custom/path/bundles
		:::
		""") => FileSystem.AddFile("docs/custom/path/bundles/1.0.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: test-product
		  target: 1.0.0
		entries:
		- title: Test feature
		  type: feature
		  products:
		  - product: test-product
		    target: 1.0.0
		  pr: "12345"
		"""));

	[Fact]
	public void ResolvesRelativePath() => Block!.Found.Should().BeTrue();

	[Fact]
	public void PathCombinedWithDocsetRoot() =>
		Block!.BundlesFolderPath.Should().EndWith("custom/path/bundles".Replace('/', Path.DirectorySeparatorChar));

	[Fact]
	public void RendersContent() => Html.Should().Contain("Test feature");
}

public class ChangelogBundlesFolderDocsetRootRelativeTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBundlesFolderDocsetRootRelativeTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog} /release-notes/versions
		:::
		""") => FileSystem.AddFile("docs/release-notes/versions/2.0.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: test-product
		  target: 2.0.0
		entries:
		- title: Another feature
		  type: feature
		  products:
		  - product: test-product
		    target: 2.0.0
		  pr: "67890"
		"""));

	[Fact]
	public void ResolvesDocsetRootRelativePath() => Block!.Found.Should().BeTrue();

	[Fact]
	public void SlashPrefixIsTrimmed() =>
		Block!.BundlesFolderPath.Should().EndWith("release-notes/versions".Replace('/', Path.DirectorySeparatorChar));

	[Fact]
	public void PathDoesNotContainDoubleSlashes() =>
		Block!.BundlesFolderPath.Should().NotContain("//");

	[Fact]
	public void RendersContent() => Html.Should().Contain("Another feature");
}

public class ChangelogConfigRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigRelativePathTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:config: config/my-changelog.yml
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/1.0.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: test-product
			  target: 1.0.0
			entries:
			- title: Feature entry
			  type: feature
			  products:
			  - product: test-product
			    target: 1.0.0
			  pr: "11111"
			- title: Blocked entry
			  type: deprecation
			  products:
			  - product: test-product
			    target: 1.0.0
			  description: Deprecated.
			  impact: None.
			  action: Upgrade.
			  pr: "22222"
			"""));

		FileSystem.AddFile("docs/config/my-changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - deprecation
			"""));
	}

	[Fact]
	public void LoadsConfigFromRelativePath() => Block!.PublishBlocker.Should().NotBeNull();

	[Fact]
	public void ConfigBlocksCorrectTypes() => Block!.PublishBlocker!.Types.Should().Contain("deprecation");

	[Fact]
	public void FiltersBlockedEntries()
	{
		Html.Should().Contain("Feature entry");
		Html.Should().NotContain("Blocked entry");
	}
}

public class ChangelogConfigDocsetRootRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigDocsetRootRelativePathTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog}
		:config: /settings/changelog-config.yml
		:::
		""")
	{
		FileSystem.AddFile("docs/changelog/bundles/1.0.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: test-product
			  target: 1.0.0
			entries:
			- title: Regular feature
			  type: feature
			  products:
			  - product: test-product
			    target: 1.0.0
			  pr: "33333"
			- title: Internal feature
			  type: feature
			  products:
			  - product: test-product
			    target: 1.0.0
			  areas:
			  - Internal
			  pr: "44444"
			"""));

		FileSystem.AddFile("docs/settings/changelog-config.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    areas:
			      - Internal
			"""));
	}

	[Fact]
	public void LoadsConfigFromDocsetRootRelativePath() => Block!.PublishBlocker.Should().NotBeNull();

	[Fact]
	public void ConfigBlocksCorrectAreas() => Block!.PublishBlocker!.Areas.Should().Contain("Internal");

	[Fact]
	public void FiltersBlockedEntries()
	{
		Html.Should().Contain("Regular feature");
		Html.Should().NotContain("Internal feature");
	}
}

public class ChangelogBundlesFolderNestedRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBundlesFolderNestedRelativePathTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog} deeply/nested/path/to/bundles
		:::
		""") => FileSystem.AddFile("docs/deeply/nested/path/to/bundles/3.0.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: nested-product
		  target: 3.0.0
		entries:
		- title: Nested feature
		  type: feature
		  products:
		  - product: nested-product
		    target: 3.0.0
		  pr: "99999"
		"""));

	[Fact]
	public void ResolvesDeepNestedPath() => Block!.Found.Should().BeTrue();

	[Fact]
	public void RendersContent() => Html.Should().Contain("Nested feature");
}

/// <summary>
/// Tests that verify the path does not erroneously get rooted when processing paths.
/// These tests ensure Path.Combine behavior is correct for edge cases.
/// </summary>
/// <remarks>
/// Note: Testing true absolute filesystem paths (like C:\path on Windows) is platform-dependent.
/// The ResolvePath method uses Path.IsPathRooted() which behaves differently on Windows vs Unix.
/// On Windows: C:\path returns true for IsPathRooted
/// On Unix: /path returns true for IsPathRooted, but our convention treats leading / as docset-root-relative
///
/// The implementation correctly handles this by checking StartsWith('/') before IsPathRooted,
/// ensuring our docset-root-relative convention takes precedence.
/// </remarks>
public class ChangelogPathEdgeCaseTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogPathEdgeCaseTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog} ./relative/bundles
		:::
		""") => FileSystem.AddFile("docs/relative/bundles/1.0.0.yaml", new MockFileData(
		// language=yaml
		"""
		products:
		- product: edge-product
		  target: 1.0.0
		entries:
		- title: Edge case feature
		  type: feature
		  products:
		  - product: edge-product
		    target: 1.0.0
		  pr: "55555"
		"""));

	[Fact]
	public void ResolvesPathWithDotSlashPrefix() => Block!.Found.Should().BeTrue();

	[Fact]
	public void RendersContent() => Html.Should().Contain("Edge case feature");
}

public class ChangelogConfigAndBundlesRelativePathsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigAndBundlesRelativePathsTests(ITestOutputHelper output) : base(output,
		// language=markdown
		"""
		:::{changelog} bundles/v1
		:config: config/changelog.yml
		:::
		""")
	{
		FileSystem.AddFile("docs/bundles/v1/1.0.0.yaml", new MockFileData(
			// language=yaml
			"""
			products:
			- product: combined-product
			  target: 1.0.0
			entries:
			- title: Combined feature
			  type: feature
			  products:
			  - product: combined-product
			    target: 1.0.0
			  pr: "66666"
			- title: Blocked by config
			  type: other
			  products:
			  - product: combined-product
			    target: 1.0.0
			  pr: "77777"
			"""));

		FileSystem.AddFile("docs/config/changelog.yml", new MockFileData(
			// language=yaml
			"""
			block:
			  publish:
			    types:
			      - other
			"""));
	}

	[Fact]
	public void BothPathsResolveCorrectly()
	{
		Block!.Found.Should().BeTrue();
		Block!.PublishBlocker.Should().NotBeNull();
	}

	[Fact]
	public void ConfigFiltersEntries()
	{
		Html.Should().Contain("Combined feature");
		Html.Should().NotContain("Blocked by config");
	}
}
