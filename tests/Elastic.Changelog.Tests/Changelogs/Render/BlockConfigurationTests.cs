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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/101
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
			block:
			  product:
			    cloud-serverless:
			      publish:
			        areas:
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

		// Create changelog with blocked type
		// language=yaml
		var changelog1 =
			"""
			title: Blocked deprecation
			type: deprecation
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This deprecation should be blocked
			""";

		// Create changelog with non-blocked type
		// language=yaml
		var changelog2 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			pr: https://github.com/elastic/elasticsearch/pull/101
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
			    deprecation:
			lifecycles:
			  - preview
			  - beta
			  - ga
			block:
			  product:
			    cloud-serverless:
			      publish:
			        types:
			          - deprecation
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
		// Should use block comments <!-- -->
		deprecationsContent.Should().Contain("<!--");
		deprecationsContent.Should().Contain("-->");
		deprecationsContent.Should().Contain("Blocked deprecation");
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/101
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
			block:
			  publish:
			    areas:
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/101
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
			block:
			  publish:
			    areas:
			      - Internal
			  product:
			    cloud-serverless:
			      publish:
			        areas: []
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
		var changelog =
			"""
			title: Blocked Allocation breaking change
			type: breaking-change
			products:
			  - product: cloud-serverless
			    target: 2026-01-26
			areas:
			  - Allocation
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			block:
			  product:
			    cloud-serverless:
			      publish:
			        areas:
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
		// Should use block comments <!-- -->
		breakingContent.Should().Contain("<!--");
		breakingContent.Should().Contain("-->");
		breakingContent.Should().Contain("Blocked Allocation breaking change");
		// Entry should be between comment markers
		var commentStart = breakingContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = breakingContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		breakingContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Blocked Allocation breaking change");
	}

	[Fact]
	public async Task RenderChangelogs_WithBlockedArea_KnownIssue_UsesBlockComments()
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			  areas:
			    Allocation:
			lifecycles:
			  - preview
			  - beta
			  - ga
			block:
			  product:
			    cloud-serverless:
			      publish:
			        areas:
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
		// Should use block comments <!-- -->
		knownIssuesContent.Should().Contain("<!--");
		knownIssuesContent.Should().Contain("-->");
		knownIssuesContent.Should().Contain("Blocked Allocation known issue");
		// Entry should be between comment markers
		var commentStart = knownIssuesContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = knownIssuesContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		knownIssuesContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Blocked Allocation known issue");
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
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/101
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
			pr: https://github.com/elastic/elasticsearch/pull/102
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
			block:
			  product:
			    cloud-serverless:
			      publish:
			        areas:
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
}
