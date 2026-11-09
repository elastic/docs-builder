// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class OutputFormatTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_WithCustomConfigPath_UsesSpecifiedConfigFile()
	{
		// Arrange
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			description: This is a test feature
			""";

		// Create config file in a custom location (not in docs/ subdirectory)
		var customConfigDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(customConfigDir);
		var customConfigPath = FileSystem.Path.Join(customConfigDir, "custom-changelog.yml");
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - ga
			""";
		await FileSystem.File.WriteAllTextAsync(customConfigPath, configContent, TestContext.Current!.Execution.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Join(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		// Don't change directory - use custom config path via Config property
		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			Config = customConfigPath
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
			* Test feature. [#100](https://github.com/elastic/elastic/pull/100)

			  This is a test feature
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_WithAsciidocFileType_CreatesSingleAsciidocFile()
	{
		// Arrange
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			description: This is a test feature
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("1755268130-test-feature.yaml", changelog1));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Asciidoc
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Verify a single .asciidoc file is created (not multiple files like markdown)
		var asciidocFiles = FileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1, "asciidoc render should create a single file");

		MockFileSystemDump
			.ToText(FileSystem, outputDir)
			.Should()
			.MatchInlineSnapshot(
				"""
			=== 9.2.0.asciidoc ===
			[[release-notes-9.2.0]]
			== 9.2.0

			[[features-enhancements-9.2.0]]
			[float]
			=== New features and enhancements

			* Test feature. {elastic-pull}100[#100] 
			+
			This is a test feature
			"""
			);

		// Verify no markdown files are created
		var markdownFiles = FileSystem.Directory.GetFiles(outputDir, "*.md", SearchOption.AllDirectories);
		markdownFiles.Should().BeEmpty("asciidoc render should not create markdown files");
	}

	[Test]
	public async Task RenderChangelogs_WithAsciidocFileType_ValidatesAsciidocFormat()
	{
		// Arrange
		// Create test changelog entries with different types
		// language=yaml
		var featureChangelog =
			"""
			title: New search feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			description: Added new search capabilities
			""";

		// language=yaml
		var bugFixChangelog =
			"""
			title: Fixed search bug
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "200"
			description: Fixed a critical search issue
			""";

		// language=yaml
		var breakingChangeChangelog =
			"""
			title: Breaking API change
			type: breaking-change
			subtype: api
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "300"
			description: Changed API endpoint structure
			impact: Users need to update their API calls
			action: Update API client libraries
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(
			bundleHeader,
			("1755268130-feature.yaml", featureChangelog),
			("1755268140-bugfix.yaml", bugFixChangelog),
			("1755268150-breaking.yaml", breakingChangeChangelog)
		);
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = ChangelogFileType.Asciidoc
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var asciidocFiles = FileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1);

		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();

		// The snapshot shows the asciidoc list format: entries start with *
		// Verify no invalid markdown syntax (like ##) is present
	}
}
