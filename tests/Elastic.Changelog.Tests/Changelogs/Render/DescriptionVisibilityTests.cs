// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class DescriptionVisibilityTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_DefaultBehavior_IncludesDescriptionsInMarkdown()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature with description
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "100"
			description: This is a detailed description of the test feature that should be visible by default.
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = false // Default behavior
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexMarkdown = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexMarkdown).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexMarkdown, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("Test feature with description");
		indexContent.Should().Contain("This is a detailed description of the test feature that should be visible by default.");
		indexContent.Should().Contain("[#100](https://github.com/elastic/elasticsearch/pull/100)");
	}

	[Fact]
	public async Task RenderChangelogs_NoDescriptionsFlag_HidesDescriptionsInMarkdown()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature with hidden description
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "200"
			description: This description should be hidden when --no-descriptions flag is used.
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true // Hide descriptions
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexMarkdown = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexMarkdown).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexMarkdown, TestContext.Current.CancellationToken);

		// Title and links should still be present
		indexContent.Should().Contain("Test feature with hidden description");
		indexContent.Should().Contain("[#200](https://github.com/elastic/elasticsearch/pull/200)");

		// Description should be hidden
		indexContent.Should().NotContain("This description should be hidden when --no-descriptions flag is used.");
	}

	[Fact]
	public async Task RenderChangelogs_NoDescriptionsFlag_HidesDescriptionsInAsciidoc()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature for asciidoc
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "300"
			description: This description should be hidden in asciidoc format when --no-descriptions is used.
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Asciidoc,
			HideDescriptions = true // Hide descriptions
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var asciidocFiles = FileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1);

		var asciidocContent = await FileSystem.File.ReadAllTextAsync(asciidocFiles[0], TestContext.Current.CancellationToken);

		// Title and links should still be present
		asciidocContent.Should().Contain("Test feature for asciidoc");
		asciidocContent.Should().Contain("{es-pull}300[#300]");

		// Description should be hidden
		asciidocContent.Should().NotContain("This description should be hidden in asciidoc format when --no-descriptions is used.");
	}

	[Fact]
	public async Task RenderChangelogs_NoDescriptionsFlag_PreservesImpactAndActionForBreakingChanges()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create breaking change with description, impact, and action
		// language=yaml
		var changelog1 =
			"""
			title: Breaking change test
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "400"
			description: This description should be hidden but Impact and Action should remain.
			impact: This is the impact section that should always be visible.
			action: This is the action section that should always be visible.
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "breaking-change.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: breaking-change.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true,
			Dropdowns = false // Test flattened mode
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingChangesMarkdown = FileSystem.Path.Join(outputDir, "9.2.0", "breaking-changes.md");
		FileSystem.File.Exists(breakingChangesMarkdown).Should().BeTrue();

		var breakingChangesContent = await FileSystem.File.ReadAllTextAsync(breakingChangesMarkdown, TestContext.Current.CancellationToken);

		// Title and links should be present
		breakingChangesContent.Should().Contain("Breaking change test");
		breakingChangesContent.Should().Contain("[#400](https://github.com/elastic/elasticsearch/pull/400)");

		// Description should be hidden
		breakingChangesContent.Should().NotContain("This description should be hidden but Impact and Action should remain.");

		// Impact and Action should still be visible
		breakingChangesContent.Should().Contain("**Impact:** This is the impact section that should always be visible.");
		breakingChangesContent.Should().Contain("**Action:** This is the action section that should always be visible.");
	}

	[Fact]
	public async Task RenderChangelogs_NoDescriptionsFlag_WorksWithDropdownsMode()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create breaking change with description
		// language=yaml
		var changelog1 =
			"""
			title: Breaking change for dropdown test
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "500"
			description: This description should be hidden in dropdown mode.
			impact: Impact visible in dropdown mode.
			action: Action visible in dropdown mode.
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "breaking-change.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: breaking-change.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true,
			Dropdowns = true // Test dropdown mode
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingChangesMarkdown = FileSystem.Path.Join(outputDir, "9.2.0", "breaking-changes.md");
		FileSystem.File.Exists(breakingChangesMarkdown).Should().BeTrue();

		var breakingChangesContent = await FileSystem.File.ReadAllTextAsync(breakingChangesMarkdown, TestContext.Current.CancellationToken);

		// Should have dropdown structure
		breakingChangesContent.Should().Contain("::::{dropdown} Breaking change for dropdown test");
		breakingChangesContent.Should().Contain("::::");

		// Links should be present
		breakingChangesContent.Should().Contain("[#500](https://github.com/elastic/elasticsearch/pull/500)");

		// Description should be hidden (no placeholder text either)
		breakingChangesContent.Should().NotContain("This description should be hidden in dropdown mode.");
		breakingChangesContent.Should().NotContain("% Describe the functionality that changed");

		// Impact and Action should still be visible
		breakingChangesContent.Should().Contain("**Impact**<br>Impact visible in dropdown mode.");
		breakingChangesContent.Should().Contain("**Action**<br>Action visible in dropdown mode.");
	}

	[Fact]
	public async Task RenderChangelogs_NoDescriptionsFlag_PreservesBundleDescription()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with description
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: This entry description should be hidden.
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file with bundle-level description
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: |
			  This is the bundle-level description that should always be visible
			  regardless of the --no-descriptions flag.
			entries:
			  - file:
			      name: test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			FileType = ChangelogFileType.Markdown,
			HideDescriptions = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexMarkdown = FileSystem.Path.Join(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexMarkdown).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexMarkdown, TestContext.Current.CancellationToken);

		// Bundle description should be visible
		indexContent.Should().Contain("This is the bundle-level description that should always be visible");
		indexContent.Should().Contain("regardless of the --no-descriptions flag.");

		// Entry title should be present
		indexContent.Should().Contain("Test feature");

		// Entry description should be hidden
		indexContent.Should().NotContain("This entry description should be hidden.");
	}
}
