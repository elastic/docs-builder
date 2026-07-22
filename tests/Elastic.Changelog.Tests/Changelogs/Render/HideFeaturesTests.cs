// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class HideFeaturesTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CommentsOutMatchingEntries()
	{
		// Arrange
		// Changelog with feature-id
		// language=yaml
		var changelog1 =
			"""
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-api
			prs:
			- "100"
			description: This feature should be hidden
			""";

		// Changelog without feature-id (should not be hidden)
		// language=yaml
		var changelog2 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "101"
			description: This feature should be visible
			""";

		// Create bundle file
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""",
			("1755268130-hidden.yaml", changelog1),
			("1755268140-visible.yaml", changelog2));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
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

		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
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
		// language=yaml
		var changelog =
			"""
			title: Hidden breaking change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-breaking
			prs:
			- "100"
			description: This breaking change should be hidden
			impact: Users will be affected
			action: Update your code
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""",
			("1755268130-breaking.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-breaking"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingFile = FileSystem.Path.Join(outputDir, "9.2.0", "breaking-changes.md");
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
		// language=yaml
		var changelog =
			"""
			title: Hidden deprecation
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-deprecation
			prs:
			- "100"
			description: This deprecation should be hidden
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""",
			("1755268130-deprecation.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-deprecation"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var deprecationsFile = FileSystem.Path.Join(outputDir, "9.2.0", "deprecations.md");
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
		// language=yaml
		var changelog1 =
			"""
			title: First hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:first
			prs:
			- "100"
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
			prs:
			- "101"
			""";

		// language=yaml
		var changelog3 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "102"
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""",
			("1755268130-first.yaml", changelog1),
			("1755268140-second.yaml", changelog2),
			("1755268150-visible.yaml", changelog3));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:first", "feature:second"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
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
		// language=yaml
		var changelog =
			"""
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:from-file
			prs:
			- "100"
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""",
			("1755268130-hidden.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create feature IDs file
		var featureIdsFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "feature-ids.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(featureIdsFile)!);
		await FileSystem.File.WriteAllTextAsync(featureIdsFile, "feature:from-file\nfeature:another", TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = [featureIdsFile]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("% * Hidden feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CaseInsensitive_MatchesFeatureIds()
	{
		// Arrange
		// language=yaml
		var changelog =
			"""
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: Feature:UpperCase
			prs:
			- "100"
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""",
			("1755268130-hidden.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:uppercase"] // Different case
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Should match case-insensitively
		indexContent.Should().Contain("% * Hidden feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithBundleHideFeatures_CommentsOutMatchingEntries()
	{
		// Arrange - Test that hide-features from bundle metadata are used to hide entries
		// language=yaml
		var changelog1 =
			"""
			title: Hidden from bundle
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:from-bundle
			prs:
			- "100"
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "101"
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// Bundle with hide-features field
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			hide-features:
			  - feature:from-bundle
			""",
			("1755268130-hidden.yaml", changelog1),
			("1755268140-visible.yaml", changelog2));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0"
			// No CLI --hide-features - relying on bundle metadata
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Entry from bundle hide-features should be commented out
		indexContent.Should().Contain("% * Hidden from bundle");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}

	[Fact]
	public async Task RenderChangelogs_MergesCLIAndBundleHideFeatures()
	{
		// Arrange - Test that CLI and bundle hide-features are merged
		// language=yaml
		var changelog1 =
			"""
			title: Hidden from CLI
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:cli-hidden
			prs:
			- "100"
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Hidden from bundle
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:bundle-hidden
			prs:
			- "101"
			""";

		// language=yaml
		var changelog3 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "102"
			""";

		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// Bundle with hide-features for one entry
		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			hide-features:
			  - feature:bundle-hidden
			""",
			("1755268130-cli.yaml", changelog1),
			("1755268140-bundle.yaml", changelog2),
			("1755268150-visible.yaml", changelog3));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:cli-hidden"] // CLI hides different feature
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Both CLI and bundle hidden entries should be commented
		indexContent.Should().Contain("% * Hidden from CLI");
		indexContent.Should().Contain("% * Hidden from bundle");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}
}
