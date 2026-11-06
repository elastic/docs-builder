// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class DescriptionVisibilityTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_DefaultBehavior_IncludesDescriptionsInMarkdown()
	{
		// Arrange
		// Create test changelog entry with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature with description
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			description: This is a detailed description of the test feature that should be visible by default.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = false // Default behavior
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/breaking-changes.md ===
			## 9.2.0 [elasticsearch-9.2.0-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elasticsearch-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elasticsearch-release-notes-9.2.0]
			### Features and enhancements [elasticsearch-9.2.0-features-enhancements]
			* Test feature with description. [#100](https://github.com/elastic/elasticsearch/pull/100)

			  This is a detailed description of the test feature that should be visible by default.
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elasticsearch-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_NoDescriptionsFlag_HidesDescriptionsInMarkdown()
	{
		// Arrange
		// Create test changelog entry with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature with hidden description
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "200"
			description: This description should be hidden when --no-descriptions flag is used.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true // Hide descriptions
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/breaking-changes.md ===
			## 9.2.0 [elasticsearch-9.2.0-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elasticsearch-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elasticsearch-release-notes-9.2.0]
			### Features and enhancements [elasticsearch-9.2.0-features-enhancements]
			* Test feature with hidden description. [#200](https://github.com/elastic/elasticsearch/pull/200)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elasticsearch-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_NoDescriptionsFlag_HidesDescriptionsInAsciidoc()
	{
		// Arrange
		// Create test changelog entry with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature for asciidoc
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "300"
			description: This description should be hidden in asciidoc format when --no-descriptions is used.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Asciidoc,
			HideDescriptions = true // Hide descriptions
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var asciidocFiles = FileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0.asciidoc ===
			[[release-notes-9.2.0]]
			== 9.2.0

			[[features-enhancements-9.2.0]]
			[float]
			=== New features and enhancements

			* Test feature for asciidoc. {es-pull}300[#300] 
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_NoDescriptionsFlag_PreservesImpactAndActionForBreakingChanges()
	{
		// Arrange
		// Create breaking change with description, impact, and action
		// language=yaml
		var changelog1 =
			"""
			title: Breaking change test
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "400"
			description: This description should be hidden but Impact and Action should remain.
			impact: This is the impact section that should always be visible.
			action: This is the action section that should always be visible.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("breaking-change.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true,
			Dropdowns = false // Test flattened mode
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/breaking-changes.md ===
			## 9.2.0 [elasticsearch-9.2.0-breaking-changes]

			* Breaking change test.
			  For more information, check [#400](https://github.com/elastic/elasticsearch/pull/400).

			  **Impact:** This is the impact section that should always be visible.

			  **Action:** This is the action section that should always be visible.

			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elasticsearch-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elasticsearch-release-notes-9.2.0]
			_There are no new features, enhancements, or fixes associated with this release._
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elasticsearch-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_NoDescriptionsFlag_WorksWithDropdownsMode()
	{
		// Arrange
		// Create breaking change with description
		// language=yaml
		var changelog1 =
			"""
			title: Breaking change for dropdown test
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "500"
			description: This description should be hidden in dropdown mode.
			impact: Impact visible in dropdown mode.
			action: Action visible in dropdown mode.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("breaking-change.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true,
			Dropdowns = true // Test dropdown mode
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/breaking-changes.md ===
			## 9.2.0 [elasticsearch-9.2.0-breaking-changes]

			::::{dropdown} Breaking change for dropdown test.

			For more information, check [#500](https://github.com/elastic/elasticsearch/pull/500).

			**Impact**<br>Impact visible in dropdown mode.

			**Action**<br>Action visible in dropdown mode.
			::::
			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elasticsearch-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elasticsearch-release-notes-9.2.0]
			_There are no new features, enhancements, or fixes associated with this release._
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elasticsearch-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_NoDescriptionsFlag_PreservesBundleDescription()
	{
		// Arrange
		// Create test changelog entry with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: This entry description should be hidden.
			""";

		// Create bundle file with bundle-level description
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: |
			  This is the bundle-level description that should always be visible
			  regardless of the --no-descriptions flag.
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/breaking-changes.md ===
			## 9.2.0 [elasticsearch-9.2.0-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elasticsearch-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elasticsearch-release-notes-9.2.0]

			This is the bundle-level description that should always be visible
			regardless of the --no-descriptions flag.
			### Features and enhancements [elasticsearch-9.2.0-features-enhancements]
			* Test feature.
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elasticsearch-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}
}
