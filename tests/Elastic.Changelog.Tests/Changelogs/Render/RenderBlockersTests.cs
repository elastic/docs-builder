// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class RenderBlockersTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked (elasticsearch + search area)
		// language=yaml
		var changelog1 =
			"""
			title: Blocked feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked
			""";

		// Create changelog that should NOT be blocked (elasticsearch but different area)
		// language=yaml
		var changelog2 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - observability
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This feature should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-visible.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create config file with render_blockers in docs/ subdirectory
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

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

		// Set current directory to where config file is located so it can be found
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);
			Collector.Warnings.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("Blocked feature") &&
				d.Message.Contains("render_blockers") &&
				d.Message.Contains("product 'elasticsearch'") &&
				d.Message.Contains("area 'search'"));

			var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			FileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Blocked entry should be commented out with % prefix
			indexContent.Should().Contain("% * Blocked feature");
			// Visible entry should not be commented
			indexContent.Should().Contain("* Visible feature");
			indexContent.Should().NotContain("% * Visible feature");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_CommaSeparatedProducts_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with cloud-serverless product that should be blocked
		// language=yaml
		var changelog1 =
			"""
			title: Blocked cloud feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-02
			areas:
			  - security
			pr: https://github.com/elastic/cloud-serverless/pull/100
			description: This feature should be blocked
			""";

		// Create changelog with elasticsearch product that should also be blocked
		// language=yaml
		var changelog2 =
			"""
			title: Blocked elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - security
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This feature should also be blocked
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-cloud-blocked.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-es-blocked.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create config file with render_blockers using comma-separated products in docs/ subdirectory
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			available_lifecycles:
			  - ga
			render_blockers:
			  "elasticsearch, cloud-serverless":
			    areas:
			      - security
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

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
			  - product: cloud-serverless
			    target: 2025-12-02
			entries:
			  - file:
			      name: 1755268130-cloud-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-es-blocked.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);
			Collector.Warnings.Should().BeGreaterThan(0);

			var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			FileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Both entries should be commented out
			indexContent.Should().Contain("% * Blocked cloud feature");
			indexContent.Should().Contain("% * Blocked elasticsearch feature");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_MultipleProductsInEntry_ChecksAllProducts()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with multiple products - one matches render_blockers
		// language=yaml
		var changelog =
			"""
			title: Multi-product feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked because elasticsearch matches
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-multi-product.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create config file with render_blockers for elasticsearch only in docs/ subdirectory
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

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
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-multi-product.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);
			Collector.Warnings.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("Multi-product feature") &&
				d.Message.Contains("product 'elasticsearch'"));

			var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			FileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Should be blocked because elasticsearch matches, even though kibana doesn't
			indexContent.Should().Contain("% * Multi-product feature");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_TypeBlocking_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked (elasticsearch + feature type, blocked by type)
		// language=yaml
		var changelog1 =
			"""
			title: Blocked feature by type
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked by type
			""";

		// Create changelog that should NOT be blocked (elasticsearch but different type)
		// language=yaml
		var changelog2 =
			"""
			title: Visible enhancement
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This enhancement should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-visible.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create config file with render_blockers blocking docs type
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    enhancement:
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    types:
			      - feature
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

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

		// Set current directory to where config file is located so it can be found
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);
			Collector.Warnings.Should().BeGreaterThan(0);
			Collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("Blocked feature by type") &&
				d.Message.Contains("render_blockers") &&
				d.Message.Contains("product 'elasticsearch'") &&
				d.Message.Contains("type 'feature'"));

			var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			FileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Blocked entry should be commented out with % prefix
			indexContent.Should().Contain("% * Blocked feature by type");
			// Visible entry should not be commented
			indexContent.Should().Contain("* Visible enhancement");
			indexContent.Should().NotContain("% * Visible enhancement");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_AreasAndTypes_CommentsOutMatchingEntries()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked by area (elasticsearch + search area)
		// language=yaml
		var changelog1 =
			"""
			title: Blocked by area
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This should be blocked by area
			""";

		// Create changelog that should be blocked by type (elasticsearch + enhancement type, blocked by type)
		// language=yaml
		var changelog2 =
			"""
			title: Blocked by type
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This should be blocked by type
			""";

		// Create changelog that should NOT be blocked
		// language=yaml
		var changelog3 =
			"""
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - observability
			pr: https://github.com/elastic/elasticsearch/pull/102
			description: This should be visible
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-area-blocked.yaml");
		var changelogFile2 = FileSystem.Path.Combine(changelogDir, "1755268140-type-blocked.yaml");
		var changelogFile3 = FileSystem.Path.Combine(changelogDir, "1755268150-visible.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		// Create config file with render_blockers blocking both areas and types
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			    enhancement:
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			    types:
			      - enhancement
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

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
			      name: 1755268130-area-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-type-blocked.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-visible.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);
			Collector.Warnings.Should().BeGreaterThan(0);

			var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			FileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Both blocked entries should be commented out
			indexContent.Should().Contain("% * Blocked by area");
			indexContent.Should().Contain("% * Blocked by type");
			// Visible entry should not be commented
			indexContent.Should().Contain("* Visible feature");
			indexContent.Should().NotContain("% * Visible feature");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_UsesBundleProductsNotEntryProducts()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with elasticsearch product and search area
		// But bundle has kibana product - should NOT be blocked because render_blockers matches against bundle products
		// language=yaml
		var changelog1 =
			"""
			title: Entry with elasticsearch but bundle has kibana
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This should NOT be blocked because bundle product is kibana
			""";

		var changelogFile1 = FileSystem.Path.Combine(changelogDir, "1755268130-test.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);

		// Create config file with render_blockers blocking elasticsearch
		var configDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = FileSystem.Path.Combine(configDir, "docs");
		FileSystem.Directory.CreateDirectory(docsDir);
		var configPath = FileSystem.Path.Combine(docsDir, "changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await FileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file with kibana product (not elasticsearch)
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
			      name: 1755268130-test.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = FileSystem.Directory.GetCurrentDirectory();
		try
		{
			FileSystem.Directory.SetCurrentDirectory(configDir);

			var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);
			// Should have no warnings because entry is NOT blocked (bundle product is kibana, not elasticsearch)
			Collector.Warnings.Should().Be(0);

			var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			FileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Entry should NOT be commented out because bundle product is kibana, not elasticsearch
			indexContent.Should().Contain("* Entry with elasticsearch but bundle has kibana");
			indexContent.Should().NotContain("% * Entry with elasticsearch but bundle has kibana");
		}
		finally
		{
			FileSystem.Directory.SetCurrentDirectory(originalDir);
		}
	}
}
