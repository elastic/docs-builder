// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class HighlightsRenderTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithHighlightedEntries_CreatesHighlightsFile()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with highlight
		// language=yaml
		var changelog1 =
			"""
			title: New Cloud Connect UI for self-managed installations
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: ga
			description: Adds Cloud Connect functionality to Kibana
			highlight: true
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-highlight-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - file:
			      name: 1755268130-highlight-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsFile = FileSystem.Path.Combine(outputDir, "9.3.0", "highlights.md");
		FileSystem.File.Exists(highlightsFile).Should().BeTrue("highlights.md should be created when entries have highlight: true");

		var highlightsContent = await FileSystem.File.ReadAllTextAsync(highlightsFile, TestContext.Current.CancellationToken);
		highlightsContent.Should().Contain("## 9.3.0");
		highlightsContent.Should().Contain("New Cloud Connect UI");
		highlightsContent.Should().Contain("Adds Cloud Connect functionality");

		// Verify the entry also appears in index.md
		var indexFile = FileSystem.Path.Combine(outputDir, "9.3.0", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("New Cloud Connect UI");
		indexContent.Should().Contain("[Highlights]");
	}

	[Fact]
	public async Task RenderChangelogs_WithoutHighlightedEntries_DoesNotCreateHighlightsFile()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without highlight
		// language=yaml
		var changelog1 =
			"""
			title: Regular feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-regular-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - file:
			      name: 1755268130-regular-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsFile = FileSystem.Path.Combine(outputDir, "9.3.0", "highlights.md");
		FileSystem.File.Exists(highlightsFile).Should().BeFalse("highlights.md should NOT be created when no entries have highlight: true");
	}

	[Fact]
	public async Task RenderChangelogs_WithHighlightedEntries_IncludesHighlightsInAsciidoc()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with highlight
		// language=yaml
		var changelog1 =
			"""
			title: Highlighted enhancement
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.3.0
			description: This is a highlighted enhancement
			highlight: true
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-highlight-enhancement.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - file:
			      name: 1755268130-highlight-enhancement.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.3.0",
			FileType = ChangelogFileType.Asciidoc
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var asciidocFile = FileSystem.Path.Combine(outputDir, "9.3.0.asciidoc");
		FileSystem.File.Exists(asciidocFile).Should().BeTrue();

		var asciidocContent = await FileSystem.File.ReadAllTextAsync(asciidocFile, TestContext.Current.CancellationToken);
		asciidocContent.Should().Contain("[[highlights-9.3.0]]");
		asciidocContent.Should().Contain("=== Highlights");
		asciidocContent.Should().Contain("Highlighted enhancement");
		asciidocContent.Should().Contain("This is a highlighted enhancement");
	}

	[Fact]
	public async Task RenderChangelogs_WithMultipleHighlightedEntries_GroupsByArea()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with highlights
		// language=yaml
		var changelog1 =
			"""
			title: Search highlight
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			areas:
			  - Search
			highlight: true
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		// language=yaml
		var changelog2 =
			"""
			title: Indexing highlight
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.3.0
			areas:
			  - Indexing
			highlight: true
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = FileSystem.Path.Combine(changelogDir, "1755268130-search.yaml");
		var file2 = FileSystem.Path.Combine(changelogDir, "1755268140-indexing.yaml");
		await FileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await FileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			entries:
			  - file:
			      name: 1755268130-search.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-indexing.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.3.0",
			Subsections = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsFile = FileSystem.Path.Combine(outputDir, "9.3.0", "highlights.md");
		FileSystem.File.Exists(highlightsFile).Should().BeTrue();

		var highlightsContent = await FileSystem.File.ReadAllTextAsync(highlightsFile, TestContext.Current.CancellationToken);
		highlightsContent.Should().Contain("Search highlight");
		highlightsContent.Should().Contain("Indexing highlight");
		highlightsContent.Should().Contain("**Search**");
		highlightsContent.Should().Contain("**Indexing**");
	}
}
