// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class GfmRenderTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithGfmFileType_CreatesSingleGfmFile()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
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

		var changelogFile = FileSystem.Path.Join(changelogDir, "test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: test-feature.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Should create only changelog.md file, not multiple files like regular markdown
		var outputFile = FileSystem.Path.Join(outputDir, "9.2.0", "changelog.md");
		FileSystem.File.Exists(outputFile).Should().BeTrue();

		// Should NOT create separate files
		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var breakingChangesFile = FileSystem.Path.Join(outputDir, "9.2.0", "breaking-changes.md");
		FileSystem.File.Exists(indexFile).Should().BeFalse();
		FileSystem.File.Exists(breakingChangesFile).Should().BeFalse();

		var content = await FileSystem.File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
		content.Should().Contain("## 9.2.0");
		content.Should().Contain("### Features and enhancements");
		content.Should().Contain("Test feature");
		content.Should().Contain("[#100](https://github.com/elastic/elasticsearch/pull/100)");

		// Should NOT contain anchor brackets in headings
		content.Should().NotContain("## 9.2.0 [");
		content.Should().NotContain("### Features and enhancements [");
	}

	[Fact]
	public async Task RenderChangelogs_WithGfmFileType_IncludesAllSectionTypes()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files for different types
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

		var featureFile = FileSystem.Path.Join(changelogDir, "feature.yaml");
		var breakingFile = FileSystem.Path.Join(changelogDir, "breaking.yaml");
		var deprecationFile = FileSystem.Path.Join(changelogDir, "deprecation.yaml");
		var bugFixFile = FileSystem.Path.Join(changelogDir, "bugfix.yaml");
		var knownIssueFile = FileSystem.Path.Join(changelogDir, "known-issue.yaml");

		await FileSystem.File.WriteAllTextAsync(featureFile, feature, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(breakingFile, breakingChange, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(deprecationFile, deprecation, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(bugFixFile, bugFix, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(knownIssueFile, knownIssue, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: feature.yaml
			      checksum: {ComputeSha1(feature)}
			  - file:
			      name: breaking.yaml
			      checksum: {ComputeSha1(breakingChange)}
			  - file:
			      name: deprecation.yaml
			      checksum: {ComputeSha1(deprecation)}
			  - file:
			      name: bugfix.yaml
			      checksum: {ComputeSha1(bugFix)}
			  - file:
			      name: known-issue.yaml
			      checksum: {ComputeSha1(knownIssue)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputChangelogPath = FileSystem.Path.Join(outputDir, "9.2.0", "changelog.md");
		var content = await FileSystem.File.ReadAllTextAsync(outputChangelogPath, TestContext.Current.CancellationToken);

		// Should include all section types in the proper order
		content.Should().Contain("### Features and enhancements");
		content.Should().Contain("### Breaking changes");
		content.Should().Contain("### Deprecations");
		content.Should().Contain("### Bug fixes");
		content.Should().Contain("### Known issues");

		// Should contain the entry titles
		content.Should().Contain("New feature");
		content.Should().Contain("Breaking change");
		content.Should().Contain("Deprecated API");
		content.Should().Contain("Bug fix");
		content.Should().Contain("Known issue");

		// Check section ordering (features should come before breaking changes)
		var featuresIndex = content.IndexOf("### Features and enhancements", StringComparison.Ordinal);
		var breakingIndex = content.IndexOf("### Breaking changes", StringComparison.Ordinal);
		var deprecationIndex = content.IndexOf("### Deprecations", StringComparison.Ordinal);
		var bugFixIndex = content.IndexOf("### Bug fixes", StringComparison.Ordinal);
		var knownIssueIndex = content.IndexOf("### Known issues", StringComparison.Ordinal);

		featuresIndex.Should().BeLessThan(breakingIndex);
		breakingIndex.Should().BeLessThan(deprecationIndex);
		deprecationIndex.Should().BeLessThan(bugFixIndex);
		bugFixIndex.Should().BeLessThan(knownIssueIndex);
	}

	[Fact]
	public async Task RenderChangelogs_WithGfmFileType_HandlesHighlights()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

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

		var highlightFile = FileSystem.Path.Join(changelogDir, "highlight.yaml");
		var normalFile = FileSystem.Path.Join(changelogDir, "normal.yaml");

		await FileSystem.File.WriteAllTextAsync(highlightFile, highlightedFeature, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(normalFile, normalFeature, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: highlight.yaml
			      checksum: {ComputeSha1(highlightedFeature)}
			  - file:
			      name: normal.yaml
			      checksum: {ComputeSha1(normalFeature)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsOutputFile = FileSystem.Path.Join(outputDir, "9.2.0", "changelog.md");
		var content = await FileSystem.File.ReadAllTextAsync(highlightsOutputFile, TestContext.Current.CancellationToken);

		// Should include highlights section first
		content.Should().Contain("### Highlights");
		content.Should().Contain("### Features and enhancements");

		// Highlights should come first
		var highlightsIndex = content.IndexOf("### Highlights", StringComparison.Ordinal);
		var featuresIndex = content.IndexOf("### Features and enhancements", StringComparison.Ordinal);
		highlightsIndex.Should().BeLessThan(featuresIndex);

		// Both features should be present
		content.Should().Contain("Important new feature");
		content.Should().Contain("Regular feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithGfmFileType_HandlesDescriptionsAndHideDescriptions()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile = FileSystem.Path.Join(changelogDir, "feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: feature.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		// Test with descriptions shown (default)
		var inputWithDescriptions = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm,
			HideDescriptions = false
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, inputWithDescriptions, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputChangelogFile = FileSystem.Path.Join(outputDir, "9.2.0", "changelog.md");
		var content = await FileSystem.File.ReadAllTextAsync(outputChangelogFile, TestContext.Current.CancellationToken);

		content.Should().Contain("Feature with description");
		content.Should().Contain("This is a detailed description of the feature.");

		// Test with descriptions hidden
		var outputDir2 = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		var inputWithoutDescriptions = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir2,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm,
			HideDescriptions = true
		};

		var result2 = await Service.RenderChangelogs(Collector, inputWithoutDescriptions, TestContext.Current.CancellationToken);

		result2.Should().BeTrue();
		var changelogFile2 = FileSystem.Path.Join(outputDir2, "9.2.0", "changelog.md");
		var content2 = await FileSystem.File.ReadAllTextAsync(changelogFile2, TestContext.Current.CancellationToken);

		content2.Should().Contain("Feature with description");
		content2.Should().NotContain("This is a detailed description of the feature.");
	}

	[Fact]
	public async Task RenderChangelogs_WithGfmFileType_HandlesBundleDescriptionAndReleaseDate()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile = FileSystem.Path.Join(changelogDir, "feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create bundle file with description and release date
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: "This is a major release with many improvements."
			release-date: "2024-03-15"
			entries:
			  - file:
			      name: feature.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Gfm
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var finalChangelogFile = FileSystem.Path.Join(outputDir, "9.2.0", "changelog.md");
		var content = await FileSystem.File.ReadAllTextAsync(finalChangelogFile, TestContext.Current.CancellationToken);

		// Should include the bundle description and release date
		content.Should().Contain("## 9.2.0");
		content.Should().Contain("_Released: March 15, 2024_");
		content.Should().Contain("This is a major release with many improvements.");
	}
}
