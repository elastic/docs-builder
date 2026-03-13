// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class BlockConfigurationTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithBlockedArea_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with blocked area
		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This feature should be blocked
			""";

		// Create changelog with non-blocked area
		// language=yaml
		var changelog2 =
			"""
			title: Visible Search feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Search
			prs:
			- "101"
			description: This feature should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
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
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Allocation:
			    Search:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Blocked entry should be commented out with % prefix
		indexContent.Should().Contain("% * Blocked Allocation feature");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible Search feature");
		indexContent.Should().NotContain("% * Visible Search feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithBlockedType_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with blocked type (blocked by area)
		// language=yaml
		var changelog1 =
			"""
			title: Blocked deprecation
			type: deprecation
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This deprecation should be blocked
			""";

		// Create visible deprecation (not blocked - different area)
		// language=yaml
		var changelog2 =
			"""
			title: Visible deprecation
			type: deprecation
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Search
			prs:
			- "101"
			description: This deprecation should be visible
			""";

		// Create changelog with non-blocked type
		// language=yaml
		var changelog3 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			prs:
			- "102"
			description: This feature should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-visible-deprecation.yaml");
		var changelogFile3 = FileSystem.Path.Combine(changelogDir, "1755268150-visible-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible-deprecation.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-visible-feature.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration - block only Allocation area (not all deprecations)
		// This will block the deprecation in Allocation area but not the one in Search area
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    deprecation:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var deprecationsFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "deprecations.md");
		FileSystem.File.Exists(deprecationsFile).Should().BeTrue();

		var deprecationsContent = await FileSystem.File.ReadAllTextAsync(deprecationsFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- --> for blocked entry
		deprecationsContent.Should().Contain("<!--");
		deprecationsContent.Should().Contain("-->");
		deprecationsContent.Should().Contain("Blocked deprecation");
		// Visible entry should not be commented
		deprecationsContent.Should().Contain("Visible deprecation");
		deprecationsContent.Should().NotContain("<!--Visible deprecation");
		// Entry should be between comment markers
		var commentStart = deprecationsContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = deprecationsContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		deprecationsContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Blocked deprecation");

		var indexFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithGlobalBlockedArea_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with blocked area
		// language=yaml
		var changelog1 =
			"""
			title: Blocked Internal feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "100"
			description: This feature should be blocked globally
			""";

		// Create changelog with non-blocked area
		// language=yaml
		var changelog2 =
			"""
			title: Visible Search feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - Search
			prs:
			- "101"
			description: This feature should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
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
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with global block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Internal:
			    Search:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Blocked entry should be commented out with % prefix
		indexContent.Should().Contain("% * Blocked Internal feature");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible Search feature");
		indexContent.Should().NotContain("% * Visible Search feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithProductSpecificOverride_OverridesGlobalBlock()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with area that's blocked globally but not for this product
		// language=yaml
		var changelog1 =
			"""
			title: Visible Internal feature for cloud-serverless
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Internal
			prs:
			- "100"
			description: This should be visible for cloud-serverless
			""";

		// Create changelog with area that's blocked globally
		// language=yaml
		var changelog2 =
			"""
			title: Blocked Internal feature for elasticsearch
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "101"
			description: This should be blocked for elasticsearch
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-cloud.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-es.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle files
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile1 = FileSystem.Path.Combine(bundleDir, "bundle-cloud.yaml");
		// language=yaml
		var bundleContent1 =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-cloud.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile1, bundleContent1, TestContext.Current.CancellationToken);

		var bundleFile2 = FileSystem.Path.Combine(bundleDir, "bundle-es.yaml");
		// language=yaml
		var bundleContent2 =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268140-es.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile2, bundleContent2, TestContext.Current.CancellationToken);

		// Create config with global block and product-specific override
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Internal:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			    products:
			      cloud-serverless:
			        exclude_areas: []
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [
				new BundleInput { BundleFile = bundleFile1, Directory = changelogDir },
				new BundleInput { BundleFile = bundleFile2, Directory = changelogDir }
			],
			Output = outputDir,
			Title = "mixed",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Check cloud-serverless output (should be visible)
		var cloudOutputDir = FileSystem.Path.Combine(outputDir, "2026-01-26");
		if (FileSystem.Directory.Exists(cloudOutputDir))
		{
			var cloudIndexFile = FileSystem.Path.Combine(cloudOutputDir, "index.md");
			if (FileSystem.File.Exists(cloudIndexFile))
			{
				var cloudContent = await FileSystem.File.ReadAllTextAsync(cloudIndexFile, TestContext.Current.CancellationToken);
				cloudContent.Should().Contain("* Visible Internal feature for cloud-serverless");
				cloudContent.Should().NotContain("% * Visible Internal feature for cloud-serverless");
			}
		}

		// Check elasticsearch output (should be blocked)
		var esOutputDir = FileSystem.Path.Combine(outputDir, "9.2.0");
		if (FileSystem.Directory.Exists(esOutputDir))
		{
			var esIndexFile = FileSystem.Path.Combine(esOutputDir, "index.md");
			if (FileSystem.File.Exists(esIndexFile))
			{
				var esContent = await FileSystem.File.ReadAllTextAsync(esIndexFile, TestContext.Current.CancellationToken);
				esContent.Should().Contain("% * Blocked Internal feature for elasticsearch");
			}
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithBlockedArea_BreakingChange_UsesBlockComments()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation breaking change
			type: breaking-change
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This breaking change should be blocked
			impact: Users will be affected
			action: Update your code
			""";

		// Create visible breaking change (not blocked)
		// language=yaml
		var changelog2 =
			"""
			title: Visible Search breaking change
			type: breaking-change
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Search
			prs:
			- "101"
			description: This breaking change should be visible
			impact: Users will be affected
			action: Update your code
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked-breaking.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-visible-breaking.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-blocked-breaking.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible-breaking.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Allocation:
			    Search:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "breaking-changes.md");
		FileSystem.File.Exists(breakingFile).Should().BeTrue();

		var breakingContent = await FileSystem.File.ReadAllTextAsync(breakingFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- --> for blocked entry
		breakingContent.Should().Contain("<!--");
		breakingContent.Should().Contain("-->");
		breakingContent.Should().Contain("Blocked Allocation breaking change");
		// Entry should be between comment markers
		var commentStart = breakingContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = breakingContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		breakingContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Blocked Allocation breaking change");
		// Visible entry should not be commented
		breakingContent.Should().Contain("Visible Search breaking change");
		breakingContent.Should().NotContain("<!--Visible Search breaking change");
	}

	[Fact]
	public async Task RenderChangelogs_WithBlockedArea_KnownIssue_UsesBlockComments()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation known issue
			type: known-issue
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This known issue should be blocked
			impact: Users may experience issues
			action: Workaround available
			""";

		// Create visible known issue (not blocked)
		// language=yaml
		var changelog2 =
			"""
			title: Visible Search known issue
			type: known-issue
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Search
			prs:
			- "101"
			description: This known issue should be visible
			impact: Users may experience issues
			action: Workaround available
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked-known.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-visible-known.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-blocked-known.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible-known.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    known-issue:
			  areas:
			    Allocation:
			    Search:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var knownIssuesFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "known-issues.md");
		FileSystem.File.Exists(knownIssuesFile).Should().BeTrue();

		var knownIssuesContent = await FileSystem.File.ReadAllTextAsync(knownIssuesFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- --> for blocked entry
		knownIssuesContent.Should().Contain("<!--");
		knownIssuesContent.Should().Contain("-->");
		knownIssuesContent.Should().Contain("Blocked Allocation known issue");
		// Entry should be between comment markers
		var commentStart = knownIssuesContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = knownIssuesContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		knownIssuesContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Blocked Allocation known issue");
		// Visible entry should not be commented
		knownIssuesContent.Should().Contain("Visible Search known issue");
		knownIssuesContent.Should().NotContain("<!--Visible Search known issue");
	}

	[Fact]
	public async Task RenderChangelogs_WithMultipleBlockedAreas_CommentsOutAllMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Blocked Internal feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Internal
			prs:
			- "101"
			""";

		// language=yaml
		var changelog3 =
			"""
			title: Visible Search feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Search
			prs:
			- "102"
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-allocation.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-internal.yaml");
		var changelogFile3 = FileSystem.Path.Combine(changelogDir, "1755268150-search.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-allocation.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-internal.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-search.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with multiple blocked areas
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Allocation:
			    Internal:
			    Search:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      'cloud-serverless':
			        exclude_areas:
			          - Allocation
			          - Internal
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result || Collector.Errors > 0)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Both blocked entries should be commented out
		indexContent.Should().Contain("% * Blocked Allocation feature");
		indexContent.Should().Contain("% * Blocked Internal feature");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible Search feature");
		indexContent.Should().NotContain("% * Visible Search feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithSubsections_CommentsOutEmptySubsectionHeaders()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with blocked area
		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This feature should be blocked
			""";

		// Create changelog with non-blocked area
		// language=yaml
		var changelog2 =
			"""
			title: Visible Search feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Search
			prs:
			- "101"
			description: This feature should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
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
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Allocation:
			    Search:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile,
			Subsections = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Allocation subsection header should be commented out (all entries are blocked)
		indexContent.Should().Contain("% **Allocation**");
		// Search subsection header should not be commented out (has visible entries)
		indexContent.Should().Contain("**Search**");
		indexContent.Should().NotContain("% **Search**");
		// Blocked entry should be commented out
		indexContent.Should().Contain("% * Blocked Allocation feature");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible Search feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithAllEntriesBlocked_ShowsNoItemsMessage()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with blocked area
		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This feature should be blocked
			""";

		// Create another changelog with blocked area
		// language=yaml
		var changelog2 =
			"""
			title: Blocked Allocation enhancement
			type: enhancement
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "101"
			description: This enhancement should be blocked
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-feature.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-enhancement.yaml");
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
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-enhancement.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    enhancement:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Should show "no items" message since all entries are blocked
		indexContent.Should().Contain("_There are no new features, enhancements, or fixes associated with this release._");
		// Should still contain commented-out entry titles
		indexContent.Should().Contain("% * Blocked Allocation feature");
		indexContent.Should().Contain("% * Blocked Allocation enhancement");
	}

	[Fact]
	public async Task RenderChangelogs_WithAllBreakingChangesBlocked_ShowsNoBreakingChangesMessage()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Blocked Allocation breaking change
			type: breaking-change
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This breaking change should be blocked
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
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-breaking.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Allocation:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "breaking-changes.md");
		FileSystem.File.Exists(breakingFile).Should().BeTrue();

		var breakingContent = await FileSystem.File.ReadAllTextAsync(breakingFile, TestContext.Current.CancellationToken);
		// Should show "no breaking changes" message since all entries are blocked
		breakingContent.Should().Contain("_There are no breaking changes associated with this release._");
		// Should still contain commented-out entry content
		breakingContent.Should().Contain("Blocked Allocation breaking change");
		breakingContent.Should().Contain("<!--");
	}

	[Fact]
	public async Task RenderChangelogs_WithAllDeprecationsBlocked_ShowsNoDeprecationsMessage()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Blocked Allocation deprecation
			type: deprecation
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This deprecation should be blocked
			impact: Users will be affected
			action: Update your code
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
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-deprecation.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    deprecation:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var deprecationsFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "deprecations.md");
		FileSystem.File.Exists(deprecationsFile).Should().BeTrue();

		var deprecationsContent = await FileSystem.File.ReadAllTextAsync(deprecationsFile, TestContext.Current.CancellationToken);
		// Should show "no deprecations" message since all entries are blocked
		deprecationsContent.Should().Contain("_There are no deprecations associated with this release._");
		// Should still contain commented-out entry content
		deprecationsContent.Should().Contain("Blocked Allocation deprecation");
		deprecationsContent.Should().Contain("<!--");
	}

	[Fact]
	public async Task RenderChangelogs_WithAllKnownIssuesBlocked_ShowsNoKnownIssuesMessage()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var changelog =
			"""
			title: Blocked Allocation known issue
			type: known-issue
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This known issue should be blocked
			impact: Users may experience issues
			action: Workaround available
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-known.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-known.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    known-issue:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_areas:
			          - Allocation
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var knownIssuesFile = FileSystem.Path.Combine(outputDir, "2026-01-26", "known-issues.md");
		FileSystem.File.Exists(knownIssuesFile).Should().BeTrue();

		var knownIssuesContent = await FileSystem.File.ReadAllTextAsync(knownIssuesFile, TestContext.Current.CancellationToken);
		// Should show "no known issues" message since all entries are blocked
		knownIssuesContent.Should().Contain("_There are no known issues associated with this release._");
		// Should still contain commented-out entry content
		knownIssuesContent.Should().Contain("Blocked Allocation known issue");
		knownIssuesContent.Should().Contain("<!--");
	}

	[Fact]
	public async Task RenderChangelogs_WithMultipleProducts_SharedEntry_UsesAlphabeticalFirstMatch()
	{
		// Arrange
		// Bundle has [elasticsearch, kibana]. Entry belongs to both (shared).
		// elasticsearch rule: exclude_areas: "Internal" (e < k alphabetically → wins)
		// kibana rule: no area filter
		// Expected: elasticsearch rule fires → shared entry with area "Internal" is commented out.
		// A kibana-only entry with area "Internal" uses the kibana rule (no filter) → visible.
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var sharedEntry =
			"""
			title: Shared internal feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "200"
			""";

		// language=yaml
		var kibanaOnlyEntry =
			"""
			title: Kibana internal feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "201"
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268200-shared.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268201-kibana-only.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, sharedEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, kibanaOnlyEntry, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268200-shared.yaml
			      checksum: {ComputeSha1(sharedEntry)}
			  - file:
			      name: 1755268201-kibana-only.yaml
			      checksum: {ComputeSha1(kibanaOnlyEntry)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Internal:
			    Search:
			lifecycles:
			  - ga
			rules:
			  publish:
			    products:
			      elasticsearch:
			        exclude_areas:
			          - Internal
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);

		// Shared entry: elasticsearch rule wins (e < k) → blocked
		indexContent.Should().Contain("% * Shared internal feature",
			"elasticsearch rule is alphabetically first and excludes Internal area for the shared entry");

		// Kibana-only entry: no elasticsearch rule applies; kibana has no area rule → visible
		indexContent.Should().Contain("* Kibana internal feature",
			"kibana-only entry has no matching rule and should be visible");
		indexContent.Should().NotContain("% * Kibana internal feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithMultipleProducts_SingleProductEntries_UseTheirOwnRules()
	{
		// Arrange
		// Bundle has [elasticsearch, kibana].
		// elasticsearch-only entry uses elasticsearch rule (exclude Internal).
		// kibana-only entry uses kibana rule (no area filter) → visible even with Internal area.
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var esEntry =
			"""
			title: Elasticsearch internal feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "300"
			""";

		// language=yaml
		var kibanaEntry =
			"""
			title: Kibana internal feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "301"
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268300-es.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268301-kibana.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, esEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, kibanaEntry, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268300-es.yaml
			      checksum: {ComputeSha1(esEntry)}
			  - file:
			      name: 1755268301-kibana.yaml
			      checksum: {ComputeSha1(kibanaEntry)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Internal:
			lifecycles:
			  - ga
			rules:
			  publish:
			    products:
			      elasticsearch:
			        exclude_areas:
			          - Internal
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);

		// elasticsearch entry: elasticsearch rule applies → blocked
		indexContent.Should().Contain("% * Elasticsearch internal feature",
			"elasticsearch rule excludes Internal area");

		// kibana entry: kibana has no per-product rule; falls through to global (none) → visible
		indexContent.Should().Contain("* Kibana internal feature",
			"kibana entry has no matching per-product rule and should be visible");
		indexContent.Should().NotContain("% * Kibana internal feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithDisjointBundleAndEntryProducts_FallsBackToGlobalBlocker()
	{
		// Arrange
		// Bundle has [kibana]. Entry belongs to [elasticsearch] only (disjoint).
		// Intersection is empty → falls back to entry's own products ([elasticsearch]).
		// No per-product rule for elasticsearch → global blocker (exclude_areas: Internal) applies.
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// language=yaml
		var esEntry =
			"""
			title: Elasticsearch internal entry
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - Internal
			prs:
			- "400"
			""";

		// language=yaml
		var esSearchEntry =
			"""
			title: Elasticsearch search entry
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - Search
			prs:
			- "401"
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268400-es-internal.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268401-es-search.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, esEntry, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, esSearchEntry, TestContext.Current.CancellationToken);

		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268400-es-internal.yaml
			      checksum: {ComputeSha1(esEntry)}
			  - file:
			      name: 1755268401-es-search.yaml
			      checksum: {ComputeSha1(esSearchEntry)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    Internal:
			    Search:
			lifecycles:
			  - ga
			rules:
			  publish:
			    exclude_areas:
			      - Internal
			    products:
			      kibana:
			        exclude_types:
			          - feature
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue($"Errors: {string.Join("; ", Collector.Diagnostics.Select(d => d.Message))}");
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);

		// elasticsearch Internal entry: disjoint intersection → falls back to entry's own [elasticsearch],
		// no elasticsearch per-product rule → global blocker (exclude Internal) applies → blocked
		indexContent.Should().Contain("% * Elasticsearch internal entry",
			"global exclude_areas rule should apply via fallback to entry's own product");

		// elasticsearch Search entry: global blocker does not match Search → visible
		indexContent.Should().Contain("* Elasticsearch search entry",
			"Search area is not excluded globally and kibana rule should not bleed across to elasticsearch entries");
		indexContent.Should().NotContain("% * Elasticsearch search entry");
	}

	[Fact]
	public async Task RenderChangelogs_WithBlockedEntries_EmitsWarnings()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with blocked area
		// language=yaml
		var changelog1 =
			"""
			title: Blocked Allocation feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			prs:
			- "100"
			description: This feature should be blocked
			""";

		// Create changelog with blocked type
		// language=yaml
		var changelog2 =
			"""
			title: Blocked deprecation
			type: deprecation
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			prs:
			- "101"
			description: This deprecation should be blocked
			""";

		// Create changelog with multiple blocked areas
		// language=yaml
		var changelog3 =
			"""
			title: Blocked Internal feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			  - Internal
			prs:
			- "102"
			description: This feature should be blocked
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-allocation.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-deprecation.yaml");
		var changelogFile3 = FileSystem.Path.Combine(changelogDir, "1755268150-internal.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			entries:
			  - file:
			      name: 1755268130-allocation.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-deprecation.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-internal.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create config with block configuration
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(configDir);
		var configFile = FileSystem.Path.Combine(configDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    deprecation:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  publish:
			    products:
			      cloud-serverless:
			        exclude_types:
			          - deprecation
			        exclude_areas:
			          - Allocation
			          - Internal
			""";
		await FileSystem.File.WriteAllTextAsync(configFile, configContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "2026-01-26",
			Config = configFile
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);

		// Verify warnings for blocked entries
		var warnings = Collector.Diagnostics.Where(d => d.Severity == Documentation.Diagnostics.Severity.Warning).ToList();

		// Should have warning for Allocation feature (blocked by area) - PR 100
		warnings.Should().Contain(w =>
			w.Message.Contains("for PR 100") &&
			w.Message.Contains("will be commented out") &&
			w.Message.Contains("Allocation"));

		// Should have warning for deprecation (blocked by type) - PR 101
		warnings.Should().Contain(w =>
			w.Message.Contains("for PR 101") &&
			w.Message.Contains("will be commented out") &&
			w.Message.Contains("deprecation"));

		// Should have warning for Internal feature (blocked by areas) - PR 102
		warnings.Should().Contain(w =>
			w.Message.Contains("for PR 102") &&
			w.Message.Contains("will be commented out") &&
			w.Message.Contains("Allocation"));
	}
}
