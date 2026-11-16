// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class TitleTargetTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_WithoutTitleAndNoTargets_EmitsWarning()
	{
		// Arrange
		// Create test changelog entry without target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			prs:
			- "100"
			""";

		// Create bundle file without target
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir
			// Note: Title is not set
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"warning  No --title option provided and bundle files do not contain 'target' values. Output folder and markdown titles will default to 'unknown'. Consider using --title to specify a custom title."
			);
	}

	[Test]
	public async Task RenderChangelogs_WithTitleAndNoTargets_NoWarning()
	{
		// Arrange
		// Create test changelog entry without target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			prs:
			- "100"
			""";

		// Create bundle file without target
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0" // Title is provided
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		// Should not have warning about missing title
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("");
	}

	[Test]
	public async Task RenderChangelogs_WithIsoDateTarget_FormatsDateInHeading()
	{
		// Arrange
		// Create test changelog entry with ISO date target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 2026-05-04
			prs:
			- "100"
			""";

		// Create bundle file with ISO date target
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 2026-05-04
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir
			// Note: Title is not set, should default to formatted date
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check that output directory uses raw date slug
		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 2026-05-04/breaking-changes.md ===
			## May 4, 2026 [elastic-2026-05-04-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 2026-05-04/deprecations.md ===
			## May 4, 2026 [elastic-2026-05-04-deprecations]
			_There are no deprecations associated with this release._
			=== 2026-05-04/index.md ===
			## May 4, 2026 [elastic-release-notes-2026-05-04]
			### Features and enhancements [elastic-2026-05-04-features-enhancements]
			* Test feature. [#100](https://github.com/elastic/elastic/pull/100)
			=== 2026-05-04/known-issues.md ===
			## May 4, 2026 [elastic-2026-05-04-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_WithYearMonthTarget_FormatsDateInHeading()
	{
		// Arrange
		// Create test changelog entry with year-month target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 2026-05
			prs:
			- "100"
			""";

		// Create bundle file with year-month target
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 2026-05
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir
			// Note: Title is not set, should default to formatted date
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check that output directory uses raw date slug
		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 2026-05/breaking-changes.md ===
			## May 2026 [elastic-2026-05-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 2026-05/deprecations.md ===
			## May 2026 [elastic-2026-05-deprecations]
			_There are no deprecations associated with this release._
			=== 2026-05/index.md ===
			## May 2026 [elastic-release-notes-2026-05]
			### Features and enhancements [elastic-2026-05-features-enhancements]
			* Test feature. [#100](https://github.com/elastic/elastic/pull/100)
			=== 2026-05/known-issues.md ===
			## May 2026 [elastic-2026-05-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_WithExplicitDateTitle_DoesNotFormatTitle()
	{
		// Arrange
		// Create test changelog entry with ISO date target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 2026-05-04
			prs:
			- "100"
			""";

		// Create bundle file with ISO date target
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 2026-05-04
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "2026-05-04" // Explicit title provided - should stay literal
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check that output directory uses title slug
		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 2026-05-04/breaking-changes.md ===
			## 2026-05-04 [elastic-2026-05-04-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 2026-05-04/deprecations.md ===
			## 2026-05-04 [elastic-2026-05-04-deprecations]
			_There are no deprecations associated with this release._
			=== 2026-05-04/index.md ===
			## 2026-05-04 [elastic-release-notes-2026-05-04]
			### Features and enhancements [elastic-2026-05-04-features-enhancements]
			* Test feature. [#100](https://github.com/elastic/elastic/pull/100)
			=== 2026-05-04/known-issues.md ===
			## 2026-05-04 [elastic-2026-05-04-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}
}
