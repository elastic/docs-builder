// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Render;

public class OutputFormatTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithCustomConfigPath_UsesSpecifiedConfigFile()
	{
		// Arrange
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile1 = _fileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);

		// Create config file in a custom location (not in docs/ subdirectory)
		var customConfigDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(customConfigDir);
		var customConfigPath = _fileSystem.Path.Combine(customConfigDir, "custom-changelog.yml");
		// language=yaml
		var configContent =
			"""
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await _fileSystem.File.WriteAllTextAsync(customConfigPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = _fileSystem.Path.Combine(bundleDir, "bundle.yaml");
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
			""";
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Don't change directory - use custom config path via Config property
		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Config = customConfigPath
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("Blocked feature") &&
			d.Message.Contains("render_blockers") &&
			d.Message.Contains("product 'elasticsearch'") &&
			d.Message.Contains("area 'search'"));

		var indexFile = _fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		_fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await _fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Blocked entry should be commented out with % prefix
		indexContent.Should().Contain("% * Blocked feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithAsciidocFileType_CreatesSingleAsciidocFile()
	{
		// Arrange
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This is a test feature
			""";

		var changelogFile = _fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		_fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = "asciidoc"
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Verify a single .asciidoc file is created (not multiple files like markdown)
		var asciidocFiles = _fileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1, "asciidoc render should create a single file");

		var asciidocFile = asciidocFiles[0];
		var asciidocContent = await _fileSystem.File.ReadAllTextAsync(asciidocFile, TestContext.Current.CancellationToken);

		// Verify valid asciidoc format elements
		asciidocContent.Should().Contain("[[release-notes-", "should contain anchor");
		asciidocContent.Should().Contain("== 9.2.0", "should contain section header");
		asciidocContent.Should().Contain("[[features-enhancements-", "should contain features section anchor");
		asciidocContent.Should().Contain("=== New features and enhancements", "should contain features section header");
		asciidocContent.Should().Contain("* Test feature", "should contain changelog entry");
		asciidocContent.Should().Contain("This is a test feature", "should contain description");

		// Verify no markdown files are created
		var markdownFiles = _fileSystem.Directory.GetFiles(outputDir, "*.md", SearchOption.AllDirectories);
		markdownFiles.Should().BeEmpty("asciidoc render should not create markdown files");
	}

	[Fact]
	public async Task RenderChangelogs_WithAsciidocFileType_ValidatesAsciidocFormat()
	{
		// Arrange
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with different types
		// language=yaml
		var featureChangelog =
			"""
			title: New search feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
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
			pr: https://github.com/elastic/elasticsearch/pull/200
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
			pr: https://github.com/elastic/elasticsearch/pull/300
			description: Changed API endpoint structure
			impact: Users need to update their API calls
			action: Update API client libraries
			""";

		var featureFile = _fileSystem.Path.Combine(changelogDir, "1755268130-feature.yaml");
		var bugFixFile = _fileSystem.Path.Combine(changelogDir, "1755268140-bugfix.yaml");
		var breakingFile = _fileSystem.Path.Combine(changelogDir, "1755268150-breaking.yaml");
		await _fileSystem.File.WriteAllTextAsync(featureFile, featureChangelog, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(bugFixFile, bugFixChangelog, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(breakingFile, breakingChangeChangelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		_fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-feature.yaml
			      checksum: {ComputeSha1(featureChangelog)}
			  - file:
			      name: 1755268140-bugfix.yaml
			      checksum: {ComputeSha1(bugFixChangelog)}
			  - file:
			      name: 1755268150-breaking.yaml
			      checksum: {ComputeSha1(breakingChangeChangelog)}
			""";
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = "asciidoc"
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var asciidocFiles = _fileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1);

		var asciidocContent = await _fileSystem.File.ReadAllTextAsync(asciidocFiles[0], TestContext.Current.CancellationToken);

		// Verify asciidoc structure
		asciidocContent.Should().Contain("[[release-notes-9.2.0]]", "should contain main anchor");
		asciidocContent.Should().Contain("== 9.2.0", "should contain main header");

		// Verify sections are present with proper asciidoc format
		asciidocContent.Should().Contain("[[bug-fixes-9.2.0]]", "should contain bug fixes anchor");
		asciidocContent.Should().Contain("[float]", "should contain float attribute");
		asciidocContent.Should().Contain("=== Bug fixes", "should contain bug fixes header");

		asciidocContent.Should().Contain("[[features-enhancements-9.2.0]]", "should contain features anchor");
		asciidocContent.Should().Contain("=== New features and enhancements", "should contain features header");

		asciidocContent.Should().Contain("[[breaking-changes-9.2.0]]", "should contain breaking changes anchor");
		asciidocContent.Should().Contain("=== Breaking changes", "should contain breaking changes header");

		// Verify entries are formatted correctly
		asciidocContent.Should().Contain("* New search feature", "should contain feature entry");
		asciidocContent.Should().Contain("* Fixed search bug", "should contain bug fix entry");
		asciidocContent.Should().Contain("* Breaking API change", "should contain breaking change entry");

		// Verify asciidoc list format (entries should start with *)
		var lines = asciidocContent.Split('\n');
		var entryLines = lines
			.Where(l => l.TrimStart().StartsWith("* ", StringComparison.Ordinal) && !l.TrimStart().StartsWith("* *", StringComparison.Ordinal)).ToList();
		entryLines.Should().HaveCountGreaterThanOrEqualTo(3, "should have at least 3 changelog entries");

		// Verify no invalid markdown syntax (like ##) is present
		asciidocContent.Should().NotContain("##", "should not contain markdown headers");
		asciidocContent.Should().NotContain("###", "should not contain markdown headers");
	}
}
