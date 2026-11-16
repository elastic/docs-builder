// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class BundleValidationTests() : RenderChangelogTestBase()
{
	// language=yaml
	private const string BundleHeader = """
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

	[Test]
	public async Task MultipleAmendFiles_AllEntriesMergedAndRendered()
	{
		// Arrange — main bundle with 1 entry + 2 amend files with 1 entry each
		var bundleDir = CreateBundleDir();

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile, CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1)));

		var amend1 = FileSystem.Path.Join(bundleDir, "bundle.amend-1.yaml");
		await WriteBundleAsync(amend1, CreateResolvedBundleContent(BundleHeader, ("1000000002-enhancement.yaml", ChangelogFeature2)));

		var amend2 = FileSystem.Path.Join(bundleDir, "bundle.amend-2.yaml");
		await WriteBundleAsync(amend2, CreateResolvedBundleContent(BundleHeader, ("1000000003-bugfix.yaml", ChangelogFeature3)));

		var input = CreateRenderInput(bundleFile);

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		// Verify all 3 entries were rendered
		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
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
			* Feature one. [#101](https://github.com/elastic/elastic/pull/101)
			* Feature two. [#102](https://github.com/elastic/elastic/pull/102)

			### Fixes [elastic-9.2.0-fixes]
			* Feature three. [#103](https://github.com/elastic/elastic/pull/103)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task AmendFileEntry_StaleProvenanceChecksum_NoWarning()
	{
		// Arrange — the file block is provenance only: a checksum that no longer
		// matches the entry content must not produce a warning at render time
		var bundleDir = CreateBundleDir();

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile, CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1)));

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
		await FileSystem.File.WriteAllTextAsync(amend1, amendContent, TestContext.Current!.Execution.CancellationToken);

		var input = CreateRenderInput(bundleFile);

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0, "provenance checksums are not verified at render time");

		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
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
			* Feature one. [#101](https://github.com/elastic/elastic/pull/101)
			* Feature two. [#102](https://github.com/elastic/elastic/pull/102)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task AmendFileEntry_WithInlineContent_MergedAndRendered()
	{
		// Arrange — amend entry carries inline content without any file provenance
		var bundleDir = CreateBundleDir();

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(bundleFile, CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1)));

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
		await FileSystem.File.WriteAllTextAsync(amend1, amendContent, TestContext.Current!.Execution.CancellationToken);

		var input = CreateRenderInput(bundleFile);

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
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
			* Feature one. [#101](https://github.com/elastic/elastic/pull/101)
			* Resolved amend feature. [#200](https://github.com/elastic/elastic/pull/200)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task ExcludeAmendFile_OmitsEntryFromRenderedOutput()
	{
		var bundleDir = CreateBundleDir();

		var file2 = "1000000002-enhancement.yaml";
		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		await WriteBundleAsync(
			bundleFile,
			CreateResolvedBundleContent(BundleHeader, ("1000000001-feature.yaml", ChangelogFeature1), (file2, ChangelogFeature2))
		);

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
			TestContext.Current!.Execution.CancellationToken
		);

		var input = CreateRenderInput(bundleFile);

		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? throw new InvalidOperationException("Output must be set");
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
			* Feature one. [#101](https://github.com/elastic/elastic/pull/101)
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	private string CreateBundleDir()
	{
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		return bundleDir;
	}

	private async Task WriteBundleAsync(string bundlePath, string content) =>
		await FileSystem.File.WriteAllTextAsync(bundlePath, content, TestContext.Current!.Execution.CancellationToken);

	private RenderChangelogsArguments CreateRenderInput(string bundleFile) =>
		new()
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString()),
			Title = "9.2.0"
		};
}
