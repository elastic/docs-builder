// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class BundleValidationTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	// language=yaml
	private const string BundleHeader =
		"""
		products:
		  - product: elasticsearch
		    target: 9.2.0
		""";

	// language=yaml
	private const string ChangelogFeature1 =
		"""
		title: Feature one
		type: feature
		products:
		  - product: elasticsearch
		    target: 9.2.0
		prs:
		- "101"
		""";

	// language=yaml
	private const string ChangelogFeature2 =
		"""
		title: Feature two
		type: enhancement
		products:
		  - product: elasticsearch
		    target: 9.2.0
		prs:
		- "102"
		""";

	// language=yaml
	private const string ChangelogFeature3 =
		"""
		title: Feature three
		type: bug-fix
		products:
		  - product: elasticsearch
		    target: 9.2.0
		prs:
		- "103"
		""";

	[Fact]
	public async Task MultipleAmendFiles_AllEntriesMergedAndRendered()
	{
		// Arrange — main bundle with 1 entry + 2 amend files with 1 entry each
		var bundleDir = CreateBundleDir();

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile,
			CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1)));

		var amend1 = FileSystem.Path.Join(bundleDir, "bundle.amend-1.yaml");
		await WriteBundleAsync(amend1,
			CreateResolvedBundleContent(BundleHeader, ("1000000002-enhancement.yaml", ChangelogFeature2)));

		var amend2 = FileSystem.Path.Join(bundleDir, "bundle.amend-2.yaml");
		await WriteBundleAsync(amend2,
			CreateResolvedBundleContent(BundleHeader, ("1000000003-bugfix.yaml", ChangelogFeature3)));

		var input = CreateRenderInput(bundleFile);

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		// Verify all 3 entries were rendered
		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue("output should be rendered");
		var content = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		content.Should().Contain("Feature one");
		content.Should().Contain("Feature two");
		content.Should().Contain("Feature three");
	}

	[Fact]
	public async Task AmendFileEntry_StaleProvenanceChecksum_NoWarning()
	{
		// Arrange — the file block is provenance only: a checksum that no longer
		// matches the entry content must not produce a warning at render time
		var bundleDir = CreateBundleDir();

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile,
			CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1)));

		var amend1 = FileSystem.Path.Join(bundleDir, "bundle.amend-1.yaml");
		// language=yaml
		var amendContent =
			"""
			products: []
			entries:
			  - file:
			      name: 1000000002-enhancement.yaml
			      checksum: 0000000000000000000000000000000000000000
			    type: enhancement
			    title: Feature two
			    products:
			      - product: elasticsearch
			        target: 9.2.0
			    prs:
			    - "102"
			""";
		await FileSystem.File.WriteAllTextAsync(amend1, amendContent, TestContext.Current.CancellationToken);

		var input = CreateRenderInput(bundleFile);

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0, "provenance checksums are not verified at render time");

		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var content = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		content.Should().Contain("Feature two");
	}

	[Fact]
	public async Task AmendFileEntry_WithInlineContent_MergedAndRendered()
	{
		// Arrange — amend entry carries inline content without any file provenance
		var bundleDir = CreateBundleDir();

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile,
			CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1)));

		var amend1 = FileSystem.Path.Join(bundleDir, "bundle.amend-1.yaml");
		// language=yaml
		var amendContent =
			"""
			products: []
			entries:
			  - type: feature
			    title: Resolved amend feature
			    products:
			      - product: elasticsearch
			        target: 9.2.0
			    prs:
			    - "200"
			""";
		await FileSystem.File.WriteAllTextAsync(amend1, amendContent, TestContext.Current.CancellationToken);

		var input = CreateRenderInput(bundleFile);

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var content = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		content.Should().Contain("Resolved amend feature");
	}

	[Fact]
	public async Task ExcludeAmendFile_OmitsEntryFromRenderedOutput()
	{
		var bundleDir = CreateBundleDir();

		var file2 = "1000000002-enhancement.yaml";
		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile,
			CreateResolvedBundleContent(
				BundleHeader,
				("1000000001-feature.yaml", ChangelogFeature1),
				(file2, ChangelogFeature2)));

		var amend1 = FileSystem.Path.Join(bundleDir, "bundle.amend-1.yaml");
		await FileSystem.File.WriteAllTextAsync(
			amend1,
			// language=yaml
			$"""
			exclude-entries:
			  - file:
			      name: {file2}
			      checksum: {ComputeSha1(ChangelogFeature2)}
			""",
			TestContext.Current.CancellationToken);

		var input = CreateRenderInput(bundleFile);

		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
		var indexFile = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		var content = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		content.Should().Contain("Feature one");
		content.Should().NotContain("Feature two");
	}

	private string CreateBundleDir()
	{
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		return bundleDir;
	}

	private async Task WriteBundleAsync(string bundlePath, string content) =>
		await FileSystem.File.WriteAllTextAsync(bundlePath, content, TestContext.Current.CancellationToken);

	private RenderChangelogsArguments CreateRenderInput(string bundleFile) =>
		new()
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString()),
			Title = "9.2.0"
		};
}
