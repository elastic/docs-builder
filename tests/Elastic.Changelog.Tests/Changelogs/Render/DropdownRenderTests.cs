// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class DropdownRenderTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithDropdownsTrue_RendersDropdownFormat()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create breaking change changelog
		// language=yaml
		var breakingChange =
			"""
			title: Breaking API change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "123"
			description: API has been changed to improve performance
			impact: Existing API calls will fail
			action: Update your code to use the new API endpoints
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "breaking-change.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, breakingChange, TestContext.Current.CancellationToken);

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
			      checksum: {ComputeSha1(breakingChange)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Dropdowns = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingChangesFile = FileSystem.Path.Join(outputDir, "9.2.0", "breaking-changes.md");
		FileSystem.File.Exists(breakingChangesFile).Should().BeTrue();

		var content = await FileSystem.File.ReadAllTextAsync(breakingChangesFile, TestContext.Current.CancellationToken);

		// Verify dropdown format
		content.Should().Contain("::::{dropdown} Breaking API change");
		content.Should().Contain("API has been changed to improve performance");
		content.Should().Contain("**Impact**<br>Existing API calls will fail");
		content.Should().Contain("**Action**<br>Update your code to use the new API endpoints");
		content.Should().Contain("::::");
	}

	[Fact]
	public async Task RenderChangelogs_WithDropdownsFalse_RendersFlattendFormat()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create deprecation changelog
		// language=yaml
		var deprecation =
			"""
			title: Deprecated old API
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "456"
			issues:
			- "789"
			description: The old API is deprecated
			impact: API will be removed in future version
			action: Migrate to the new API
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "deprecation.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, deprecation, TestContext.Current.CancellationToken);

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
			      name: deprecation.yaml
			      checksum: {ComputeSha1(deprecation)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Dropdowns = false // Explicitly set to false for clarity
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var deprecationsFile = FileSystem.Path.Join(outputDir, "9.2.0", "deprecations.md");
		FileSystem.File.Exists(deprecationsFile).Should().BeTrue();

		var content = await FileSystem.File.ReadAllTextAsync(deprecationsFile, TestContext.Current.CancellationToken);

		// Verify flattened format
		content.Should().Contain("* Deprecated old API");
		content.Should().Contain("The old API is deprecated");
		content.Should().Contain("  For more information, check"); // Indented for list continuation
		content.Should().Contain("#456");
		content.Should().Contain("#789");
		content.Should().Contain("  **Impact:** API will be removed in future version"); // Indented for list continuation
		content.Should().Contain("  **Action:** Migrate to the new API"); // Indented for list continuation

		// Should NOT contain dropdown syntax
		content.Should().NotContain("::::{dropdown}");
		content.Should().NotContain("::::");
	}

	[Fact]
	public async Task RenderChangelogs_DefaultDropdownsFalse_RendersFlattedFormat()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create known issue changelog
		// language=yaml
		var knownIssue =
			"""
			title: Known issue with search
			type: known-issue
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "999"
			description: Search results are incomplete under certain conditions
			impact: Some search results may be missing
			action: Use the workaround provided in the documentation
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "known-issue.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, knownIssue, TestContext.Current.CancellationToken);

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
			      name: known-issue.yaml
			      checksum: {ComputeSha1(knownIssue)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0"
			// Note: Dropdowns not set, should default to false
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var knownIssuesFile = FileSystem.Path.Join(outputDir, "9.2.0", "known-issues.md");
		FileSystem.File.Exists(knownIssuesFile).Should().BeTrue();

		var content = await FileSystem.File.ReadAllTextAsync(knownIssuesFile, TestContext.Current.CancellationToken);

		// Verify flattened format (default behavior)
		content.Should().Contain("* Known issue with search");
		content.Should().Contain("Search results are incomplete under certain conditions");
		content.Should().Contain("  For more information, check"); // Indented for list continuation
		content.Should().Contain("#999");
		content.Should().Contain("  **Impact:** Some search results may be missing"); // Indented for list continuation
		content.Should().Contain("  **Action:** Use the workaround provided in the documentation"); // Indented for list continuation

		// Should NOT contain dropdown syntax
		content.Should().NotContain("::::{dropdown}");
		content.Should().NotContain("::::");
	}

	[Fact]
	public async Task RenderChangelogs_HighlightsWithDropdowns_RendersCorrectFormat()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create highlight feature
		// language=yaml
		var highlight =
			"""
			title: Amazing new feature
			type: feature
			highlight: true
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "555"
			description: This feature revolutionizes how you work with data
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "highlight.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, highlight, TestContext.Current.CancellationToken);

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
			      name: highlight.yaml
			      checksum: {ComputeSha1(highlight)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		// Test both dropdown and flattened modes
		var testCases = new[]
		{
			new { Dropdowns = true, ExpectDropdown = true },
			new { Dropdowns = false, ExpectDropdown = false }
		};

		foreach (var testCase in testCases)
		{
			var subOutputDir = FileSystem.Path.Join(outputDir, testCase.Dropdowns.ToString());

			var input = new RenderChangelogsArguments
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = subOutputDir,
				Title = "9.2.0",
				Dropdowns = testCase.Dropdowns
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);

			var highlightsFile = FileSystem.Path.Join(subOutputDir, "9.2.0", "highlights.md");
			FileSystem.File.Exists(highlightsFile).Should().BeTrue();

			var content = await FileSystem.File.ReadAllTextAsync(highlightsFile, TestContext.Current.CancellationToken);

			if (testCase.ExpectDropdown)
			{
				// Verify dropdown format
				content.Should().Contain("::::{dropdown} Amazing new feature");
				content.Should().Contain("This feature revolutionizes how you work with data");
				content.Should().Contain("::::");
			}
			else
			{
				// Verify flattened format
				content.Should().Contain("* Amazing new feature");
				content.Should().Contain("This feature revolutionizes how you work with data");
				content.Should().Contain("  For more information, check"); // Indented for list continuation
				content.Should().Contain("#555");

				// Should NOT contain dropdown syntax
				content.Should().NotContain("::::{dropdown}");
				content.Should().NotContain("::::");
			}
		}
	}

	[Fact]
	public async Task RenderChangelogs_AsciidocFormat_IgnoresDropdownsFlag()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create breaking change
		// language=yaml
		var breakingChange =
			"""
			title: Breaking API change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			prs:
			- "123"
			description: API has been changed
			impact: Existing API calls will fail
			action: Update your code
			""";

		var changelogFile = FileSystem.Path.Join(changelogDir, "breaking-change.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, breakingChange, TestContext.Current.CancellationToken);

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
			      checksum: {ComputeSha1(breakingChange)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		// Test both dropdown values with AsciiDoc format
		var testCases = new[] { true, false };

		foreach (var dropdowns in testCases)
		{
			var subOutputDir = FileSystem.Path.Join(outputDir, dropdowns.ToString());

			var input = new RenderChangelogsArguments
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = subOutputDir,
				Title = "9.2.0",
				Dropdowns = dropdowns,
				FileType = ChangelogFileType.Asciidoc
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);

			// Find the AsciiDoc file (path structure differs from Markdown)
			var asciidocFiles = FileSystem.Directory.GetFiles(subOutputDir, "*.asciidoc", SearchOption.AllDirectories);
			asciidocFiles.Should().HaveCount(1, "should create exactly one AsciiDoc file");

			var asciidocFile = asciidocFiles[0];
			var content = await FileSystem.File.ReadAllTextAsync(asciidocFile, TestContext.Current.CancellationToken);

			// AsciiDoc should always use bullet format regardless of dropdowns flag
			content.Should().Contain("* Breaking API change");
			content.Should().Contain("*Impact:* Existing API calls will fail");
			content.Should().Contain("*Action:* Update your code");

			// Should never contain MyST dropdown syntax
			content.Should().NotContain("::::{dropdown}");
			content.Should().NotContain("::::");
		}
	}
}
