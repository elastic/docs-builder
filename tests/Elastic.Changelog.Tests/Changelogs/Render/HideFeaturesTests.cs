// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class HideFeaturesTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with feature-id
		// language=yaml
		var changelog1 =
			"""
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-api
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be hidden
			""";

		// Create changelog without feature-id (should not be hidden)
		// language=yaml
		var changelog2 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This feature should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-hidden.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-visible.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-hidden.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-api"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("Hidden feature") &&
			d.Message.Contains("feature:hidden-api") &&
			d.Message.Contains("will be commented out"));

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Hidden entry should be commented out with % prefix
		indexContent.Should().Contain("% * Hidden feature");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_BreakingChange_UsesBlockComments()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Hidden breaking change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-breaking
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This breaking change should be hidden
			impact: Users will be affected
			action: Update your code
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-breaking.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-breaking.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-breaking"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingFile = FileSystem.Path.Combine(outputDir, "9.2.0", "breaking-changes.md");
		FileSystem.File.Exists(breakingFile).Should().BeTrue();

		var breakingContent = await FileSystem.File.ReadAllTextAsync(breakingFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- -->
		breakingContent.Should().Contain("<!--");
		breakingContent.Should().Contain("-->");
		breakingContent.Should().Contain("Hidden breaking change");
		// Entry should be between comment markers
		var commentStart = breakingContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = breakingContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		breakingContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Hidden breaking change");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_Deprecation_UsesBlockComments()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Hidden deprecation
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-deprecation
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This deprecation should be hidden
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-deprecation.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-deprecation.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-deprecation"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var deprecationsFile = FileSystem.Path.Combine(outputDir, "9.2.0", "deprecations.md");
		FileSystem.File.Exists(deprecationsFile).Should().BeTrue();

		var deprecationsContent = await FileSystem.File.ReadAllTextAsync(deprecationsFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- -->
		deprecationsContent.Should().Contain("<!--");
		deprecationsContent.Should().Contain("-->");
		deprecationsContent.Should().Contain("Hidden deprecation");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CommaSeparated_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog1 =
			"""
			title: First hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:first
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Second hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:second
			pr: https://github.com/elastic/elasticsearch/pull/101
			""";

		// language=yaml
		var changelog3 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/102
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-first.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-second.yaml");
		var changelogFile3 = FileSystem.Path.Combine(changelogDir, "1755268150-visible.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-first.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-second.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-visible.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:first", "feature:second"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("% * First hidden feature");
		indexContent.Should().Contain("% * Second hidden feature");
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_FromFile_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:from-file
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-hidden.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-hidden.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create feature IDs file
		var featureIdsFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "feature-ids.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(featureIdsFile)!);
		await FileSystem.File.WriteAllTextAsync(featureIdsFile, "feature:from-file\nfeature:another", TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = [featureIdsFile]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("% * Hidden feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CaseInsensitive_MatchesFeatureIds()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: Feature:UpperCase
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-hidden.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-hidden.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:uppercase"] // Different case
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Should match case-insensitively
		indexContent.Should().Contain("% * Hidden feature");
	}
}
