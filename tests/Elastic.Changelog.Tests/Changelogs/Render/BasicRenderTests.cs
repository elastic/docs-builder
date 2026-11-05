// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class BasicRenderTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_WithValidBundle_CreatesMarkdownFiles()
	{
		// Arrange
		// language=yaml
		var changelog1 =
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
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0"
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
			## 9.2.0 [elastic-9.2.0-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elastic-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elastic-release-notes-9.2.0]
			### Features and enhancements [elastic-9.2.0-features-enhancements]
			* Test feature. [#100](https://github.com/elastic/elastic/pull/100)

			  This is a test feature
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_WithMultipleTypes_DoesNotIncludeCrossFileLinksInIndex()
	{
		// Arrange
		// Create test changelog entries with different types to trigger separated files
		// language=yaml
		var featureChangelog =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "100"
			""";

		// language=yaml
		var deprecationChangelog =
			"""
			title: Deprecated API
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "200"
			""";

		// language=yaml
		var highlightChangelog =
			"""
			title: Highlighted feature
			type: feature
			highlight: true
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "300"
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.3.0
			""";
		var bundleContent = CreateResolvedBundleContent(
			bundleHeader,
			("feature.yaml", featureChangelog),
			("deprecation.yaml", deprecationChangelog),
			("highlight.yaml", highlightChangelog)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Verify index.md exists but does NOT contain cross-file links
		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();
	}

	[Test]
	public async Task RenderChangelogs_WithMultipleBundles_MergesAndRenders()
	{
		// Arrange
		// language=yaml
		var changelog1 =
			"""
			title: First feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "200"
			""";

		// Create bundle files
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var bundle1 = FileSystem.Path.Join(bundleDir, "bundle1.yaml");
		var bundleContent1 = CreateResolvedBundleContent(bundleHeader, ("1755268130-first.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current!.Execution.CancellationToken);

		var bundle2 = FileSystem.Path.Join(bundleDir, "bundle2.yaml");
		var bundleContent2 = CreateResolvedBundleContent(bundleHeader, ("1755268140-second.yaml", changelog2));
		await FileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundle1 }, new BundleInput { BundleFile = bundle2 }],
			Output = outputDir,
			Title = "9.2.0"
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
			## 9.2.0 [elastic-9.2.0-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 9.2.0/deprecations.md ===
			## 9.2.0 [elastic-9.2.0-deprecations]
			_There are no deprecations associated with this release._
			=== 9.2.0/index.md ===
			## 9.2.0 [elastic-release-notes-9.2.0]
			### Features and enhancements [elastic-9.2.0-features-enhancements]
			* First feature. [#100](https://github.com/elastic/elastic/pull/100)
			* Second feature. [#200](https://github.com/elastic/elastic/pull/200)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}
}
