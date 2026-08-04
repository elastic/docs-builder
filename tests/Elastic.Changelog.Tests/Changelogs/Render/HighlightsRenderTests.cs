// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class HighlightsRenderTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithHighlightedEntries_CreatesHighlightsFile()
	{
		// Arrange
		// Changelog entry with highlight
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
			prs:
			- "100"
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			""",
			("1755268130-highlight-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsFile = FileSystem.Path.Join(outputDir, "9.3.0", "highlights.md");
		FileSystem.File.Exists(highlightsFile).Should().BeTrue("highlights.md should be created when entries have highlight: true");

		var highlightsContent = await FileSystem.File.ReadAllTextAsync(highlightsFile, TestContext.Current.CancellationToken);
		highlightsContent.Should().Contain("## 9.3.0");
		highlightsContent.Should().Contain("New Cloud Connect UI");
		highlightsContent.Should().Contain("Adds Cloud Connect functionality");

		// Verify the entry also appears in index.md
		var indexFile = FileSystem.Path.Join(outputDir, "9.3.0", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();
		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("New Cloud Connect UI");
		// Note: Cross-file links like "[Highlights]" have been removed from index.md
	}

	[Fact]
	public async Task RenderChangelogs_WithoutHighlightedEntries_DoesNotCreateHighlightsFile()
	{
		// Arrange
		// Changelog entry without highlight
		// language=yaml
		var changelog1 =
			"""
			title: Regular feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- "100"
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			""",
			("1755268130-regular-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsFile = FileSystem.Path.Join(outputDir, "9.3.0", "highlights.md");
		FileSystem.File.Exists(highlightsFile).Should().BeFalse("highlights.md should NOT be created when no entries have highlight: true");
	}

	[Fact]
	public async Task RenderChangelogs_WithHighlightedEntries_IncludesHighlightsInAsciidoc()
	{
		// Arrange
		// Changelog entry with highlight
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
			prs:
			- "200"
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			""",
			("1755268130-highlight-enhancement.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.3.0",
			FileType = ChangelogFileType.Asciidoc
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var asciidocFile = FileSystem.Path.Join(outputDir, "9.3.0.asciidoc");
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
		// Changelog entries with highlights in different areas
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
			prs:
			- "100"
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
			prs:
			- "200"
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = CreateResolvedBundleContent(
			// language=yaml
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			""",
			("1755268130-search.yaml", changelog1),
			("1755268140-indexing.yaml", changelog2));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.3.0",
			Subsections = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var highlightsFile = FileSystem.Path.Join(outputDir, "9.3.0", "highlights.md");
		FileSystem.File.Exists(highlightsFile).Should().BeTrue();

		var highlightsContent = await FileSystem.File.ReadAllTextAsync(highlightsFile, TestContext.Current.CancellationToken);
		highlightsContent.Should().Contain("Search highlight");
		highlightsContent.Should().Contain("Indexing highlight");
		highlightsContent.Should().Contain("**Search**");
		highlightsContent.Should().Contain("**Indexing**");
	}
}
