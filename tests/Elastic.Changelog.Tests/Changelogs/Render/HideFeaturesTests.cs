// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class HideFeaturesTests() : RenderChangelogTestBase()
{
	[Test]
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
			("1755268140-visible.yaml", changelog2)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-api"]
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
				"warning  Changelog entry 'Hidden feature' with feature-id 'feature:hidden-api' will be commented out in markdown output"
			);

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
			% * Hidden feature. [#100](https://github.com/elastic/elastic/pull/100)

			%   This feature should be hidden
			* Visible feature. [#101](https://github.com/elastic/elastic/pull/101)

			  This feature should be visible
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
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
			("1755268130-breaking.yaml", changelog)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-breaking"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();

		// The hidden entry is wrapped in a block comment <!-- -->
	}

	[Test]
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
			("1755268130-deprecation.yaml", changelog)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-deprecation"]
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();
	}

	[Test]
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
			("1755268150-visible.yaml", changelog3)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:first", "feature:second"]
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
			% * First hidden feature. [#100](https://github.com/elastic/elastic/pull/100)
			% * Second hidden feature. [#101](https://github.com/elastic/elastic/pull/101)
			* Visible feature. [#102](https://github.com/elastic/elastic/pull/102)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
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
			("1755268130-hidden.yaml", changelog)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		// Create feature IDs file
		var featureIdsFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "feature-ids.txt");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(featureIdsFile)!);
		await FileSystem.File.WriteAllTextAsync(
			featureIdsFile,
			"feature:from-file\nfeature:another",
			TestContext.Current!.Execution.CancellationToken
		);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = [featureIdsFile]
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
			% * Hidden feature. [#100](https://github.com/elastic/elastic/pull/100)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
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
			("1755268130-hidden.yaml", changelog)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:uppercase"] // Different case
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
			% * Hidden feature. [#100](https://github.com/elastic/elastic/pull/100)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
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
			("1755268140-visible.yaml", changelog2)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0"
			// No CLI --hide-features - relying on bundle metadata
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
			% * Hidden from bundle. [#100](https://github.com/elastic/elastic/pull/100)
			* Visible feature. [#101](https://github.com/elastic/elastic/pull/101)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
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
			("1755268150-visible.yaml", changelog3)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:cli-hidden"] // CLI hides different feature
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
			% * Hidden from CLI. [#100](https://github.com/elastic/elastic/pull/100)
			% * Hidden from bundle. [#101](https://github.com/elastic/elastic/pull/101)
			* Visible feature. [#102](https://github.com/elastic/elastic/pull/102)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}
}
