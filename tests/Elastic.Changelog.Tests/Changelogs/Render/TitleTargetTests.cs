// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class TitleTargetTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
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
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir
			// Note: Title is not set
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided") &&
			d.Message.Contains("default to 'unknown'"));
	}

	[Fact]
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
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0" // Title is provided
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		// Should not have warning about missing title
		Collector.Diagnostics.Should().NotContain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided"));
	}

	[Fact]
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
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 2026-05-04
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir
			// Note: Title is not set, should default to formatted date
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check that output directory uses raw date slug
		var indexFile = FileSystem.Path.Join(outputDir, "2026-05-04", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		// Check that heading uses formatted date but anchor uses raw date
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("## May 4, 2026 [elastic-release-notes-2026-05-04]");
	}

	[Fact]
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
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 2026-05
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir
			// Note: Title is not set, should default to formatted date
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check that output directory uses raw date slug
		var indexFile = FileSystem.Path.Join(outputDir, "2026-05", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		// Check that heading uses formatted date but anchor uses raw date
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("## May 2026 [elastic-release-notes-2026-05]");
	}

	[Fact]
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
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 2026-05-04
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "2026-05-04" // Explicit title provided - should stay literal
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check that output directory uses title slug
		var indexFile = FileSystem.Path.Join(outputDir, "2026-05-04", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		// Check that heading uses literal title (no formatting applied)
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("## 2026-05-04 [elastic-release-notes-2026-05-04]");
	}
}
