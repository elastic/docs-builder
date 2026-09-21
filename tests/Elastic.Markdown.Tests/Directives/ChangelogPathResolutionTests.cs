// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions.TestingHelpers;
using AwesomeAssertions;
using Elastic.Markdown.Myst.Directives.Changelog;

namespace Elastic.Markdown.Tests.Directives;

/// <summary>
/// Tests for path resolution in the changelog directive.
/// All local paths must be prefixed with '/' (docset-root-relative).
/// Non-'/' arguments are interpreted as CDN product names.
/// </summary>
public class ChangelogBundlesFolderRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBundlesFolderRelativePathTests() : base(
			// language=markdown
			"""
		:::{changelog} /custom/path/bundles
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/custom/path/bundles/1.0.0.yaml",
			new MockFileData(
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
		  prs:
		  - "12345"
		"""
			)
		);

	[Test]
	public void ResolvesDocsetRootRelativePath() => Block!.Found.Should().BeTrue();

	[Test]
	public void PathCombinedWithDocsetRoot() =>
		Block!.BundlesFolderPath.Should().EndWith("custom/path/bundles".Replace('/', Path.DirectorySeparatorChar));

	[Test]
	public void RendersContent() => Html.Should().Contain("Test feature");
}

public class ChangelogBundlesFolderDocsetRootRelativeTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBundlesFolderDocsetRootRelativeTests() : base(
			// language=markdown
			"""
		:::{changelog} /release-notes/versions
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/release-notes/versions/2.0.0.yaml",
			new MockFileData(
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
		  prs:
		  - "67890"
		"""
			)
		);

	[Test]
	public void ResolvesDocsetRootRelativePath() => Block!.Found.Should().BeTrue();

	[Test]
	public void SlashPrefixIsTrimmed() =>
		Block!.BundlesFolderPath.Should().EndWith("release-notes/versions".Replace('/', Path.DirectorySeparatorChar));

	[Test]
	public void PathDoesNotContainDoubleSlashes() => Block!.BundlesFolderPath.Should().NotContain("//");

	[Test]
	public void RendersContent() => Html.Should().Contain("Another feature");
}

public class ChangelogConfigRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigRelativePathTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:config: config/my-changelog.yml
		:type: all
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/1.0.0.yaml",
			new MockFileData(
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
			  prs:
			  - "11111"
			- title: Blocked entry
			  type: deprecation
			  products:
			  - product: test-product
			    target: 1.0.0
			  description: Deprecated.
			  impact: None.
			  action: Upgrade.
			  prs:
			  - "22222"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/config/my-changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - deprecation
			"""
			)
		);
	}

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Feature entry");
		Html.Should().Contain("Blocked entry");
	}
}

public class ChangelogConfigDocsetRootRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigDocsetRootRelativePathTests() : base(
			// language=markdown
			"""
		:::{changelog}
		:config: /settings/changelog-config.yml
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/changelog/bundles/1.0.0.yaml",
			new MockFileData(
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
			  prs:
			  - "33333"
			- title: Internal feature
			  type: feature
			  products:
			  - product: test-product
			    target: 1.0.0
			  areas:
			  - Internal
			  prs:
			  - "44444"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/settings/changelog-config.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			"""
			)
		);
	}

	[Test]
	public void PublishBlockerIsNull() => Block!.PublishBlocker.Should().BeNull();

	[Test]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Regular feature");
		Html.Should().Contain("Internal feature");
	}
}

public class ChangelogBundlesFolderNestedRelativePathTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogBundlesFolderNestedRelativePathTests() : base(
			// language=markdown
			"""
		:::{changelog} /deeply/nested/path/to/bundles
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/deeply/nested/path/to/bundles/3.0.0.yaml",
			new MockFileData(
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
		  prs:
		  - "99999"
		"""
			)
		);

	[Test]
	public void ResolvesDeepNestedPath() => Block!.Found.Should().BeTrue();

	[Test]
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
	public ChangelogPathEdgeCaseTests() : base(
			// language=markdown
			"""
		:::{changelog} /relative/bundles
		:::
		"""
		) =>
		FileSystem.AddFile(
			"docs/relative/bundles/1.0.0.yaml",
			new MockFileData(
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
		  prs:
		  - "55555"
		"""
			)
		);

	[Test]
	public void ResolvesSlashPrefixedPath() => Block!.Found.Should().BeTrue();

	[Test]
	public void RendersContent() => Html.Should().Contain("Edge case feature");
}

public class ChangelogConfigAndBundlesRelativePathsTests : DirectiveTest<ChangelogBlock>
{
	public ChangelogConfigAndBundlesRelativePathsTests() : base(
			// language=markdown
			"""
		:::{changelog} /bundles/v1
		:config: config/changelog.yml
		:::
		"""
		)
	{
		FileSystem.AddFile(
			"docs/bundles/v1/1.0.0.yaml",
			new MockFileData(
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
			  prs:
			  - "66666"
			- title: Blocked by config
			  type: other
			  products:
			  - product: combined-product
			    target: 1.0.0
			  prs:
			  - "77777"
			"""
			)
		);

		FileSystem.AddFile(
			"docs/config/changelog.yml",
			new MockFileData(
				// language=yaml
				"""
			rules:
			  publish:
			    exclude_types:
			      - other
			"""
			)
		);
	}

	[Test]
	public void BothPathsResolveCorrectly()
	{
		Block!.Found.Should().BeTrue();
		Block!.PublishBlocker.Should().BeNull();
	}

	[Test]
	public void RendersAllEntries_NoFiltering()
	{
		// Directive does not apply rules.publish; all entries are shown
		Html.Should().Contain("Combined feature");
		Html.Should().Contain("Blocked by config");
	}
}
