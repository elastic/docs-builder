// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class GfmRenderTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_WithGfmFileType_CreatesSingleGfmFile()
	{
		// Arrange
		// language=yaml
		var changelog =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			description: This is a test feature
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
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("test-feature.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Should create only changelog.md file, not multiple files like regular markdown
		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/changelog.md ===
			## 9.2.0

			### Features and enhancements
			* Test feature. [#100](https://github.com/elastic/elasticsearch/pull/100)

			  This is a test feature
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_WithGfmFileType_IncludesAllSectionTypes()
	{
		// Arrange
		// Create test changelog entries for different types
		// language=yaml
		var feature =
			"""
			title: New feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		// language=yaml
		var breakingChange =
			"""
			title: Breaking change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		// language=yaml
		var deprecation =
			"""
			title: Deprecated API
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		// language=yaml
		var bugFix =
			"""
			title: Bug fix
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		// language=yaml
		var knownIssue =
			"""
			title: Known issue
			type: known-issue
			products:
			  - product: elasticsearch
			    target: 9.2.0
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
		var bundleContent = CreateResolvedBundleContent(
			bundleHeader,
			("feature.yaml", feature),
			("breaking.yaml", breakingChange),
			("deprecation.yaml", deprecation),
			("bugfix.yaml", bugFix),
			("known-issue.yaml", knownIssue)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
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
			=== 9.2.0/changelog.md ===
			## 9.2.0

			### Features and enhancements
			* New feature.

			### Breaking changes
			* Breaking change.

			### Deprecations
			* Deprecated API.

			### Bug fixes
			* Bug fix.

			### Known issues
			* Known issue.
			"""
			);

		// The snapshot shows the section order: features, breaking changes, deprecations, bug fixes, known issues
	}

	[Test]
	public async Task RenderChangelogs_WithGfmFileType_HandlesHighlights()
	{
		// Arrange
		// Create test changelog with highlight
		// language=yaml
		var highlightedFeature =
			"""
			title: Important new feature
			type: feature
			highlight: true
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		// language=yaml
		var normalFeature =
			"""
			title: Regular feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
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
		var bundleContent = CreateResolvedBundleContent(
			bundleHeader,
			("highlight.yaml", highlightedFeature),
			("normal.yaml", normalFeature)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
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
			=== 9.2.0/changelog.md ===
			## 9.2.0

			### Highlights
			* Important new feature.

			### Features and enhancements
			* Important new feature.
			* Regular feature.
			"""
			);

		// The snapshot shows the highlights section ahead of the features
	}

	[Test]
	public async Task RenderChangelogs_WithGfmFileType_HandlesDescriptionsAndHideDescriptions()
	{
		// Arrange
		// Create test changelog with description
		// language=yaml
		var changelog =
			"""
			title: Feature with description
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: |
			  This is a detailed description of the feature.
			  It spans multiple lines.
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
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("feature.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		// Test with descriptions shown (default)
		var inputWithDescriptions = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm,
			HideDescriptions = false
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, inputWithDescriptions, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0/changelog.md ===
			## 9.2.0

			### Features and enhancements
			* Feature with description.

			  This is a detailed description of the feature.
			  It spans multiple lines.
			"""
			);

		// Test with descriptions hidden
		var outputDir2 = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		var inputWithoutDescriptions = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir2,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm,
			HideDescriptions = true
		};

		var result2 = await Service.RenderChangelogs(Collector, inputWithoutDescriptions, TestContext.Current!.Execution.CancellationToken);

		result2.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump.ToText(FileSystem, outputDir2).Should().MatchSnapshot(name: "hidden-descriptions");
	}

	[Test]
	public async Task RenderChangelogs_WithGfmFileType_HandlesBundleDescriptionAndReleaseDate()
	{
		// Arrange
		// Create simple changelog
		// language=yaml
		var changelog =
			"""
			title: Simple feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		// Create bundle file with description and release date
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: "This is a major release with many improvements."
			release-date: "2024-03-15"
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("feature.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
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
			=== 9.2.0/changelog.md ===
			## 9.2.0

			_Released: March 15, 2024_

			This is a major release with many improvements.

			### Features and enhancements
			* Simple feature.
			"""
			);
	}
}
