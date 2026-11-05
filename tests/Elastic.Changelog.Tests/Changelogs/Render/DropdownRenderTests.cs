// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using AwesomeAssertions;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class DropdownRenderTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_WithDropdownsTrue_RendersDropdownFormat()
	{
		// Arrange
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

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("breaking-change.yaml", breakingChange));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			Dropdowns = true
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();
	}

	[Test]
	public async Task RenderChangelogs_WithDropdownsFalse_RendersFlattendFormat()
	{
		// Arrange
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

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("deprecation.yaml", deprecation));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0",
			Dropdowns = false // Explicitly set to false for clarity
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();
	}

	[Test]
	public async Task RenderChangelogs_DefaultDropdownsFalse_RendersFlattedFormat()
	{
		// Arrange
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

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("known-issue.yaml", knownIssue));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0"
			// Note: Dropdowns not set, should default to false
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
			_There are no new features, enhancements, or fixes associated with this release._
			=== 9.2.0/known-issues.md ===
			## 9.2.0 [elastic-9.2.0-known-issues]

			* Known issue with search.
			  Search results are incomplete under certain conditions

			  For more information, check [#999](https://github.com/elastic/elastic/pull/999).

			  **Impact:** Some search results may be missing

			  **Action:** Use the workaround provided in the documentation
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_HighlightsWithDropdowns_RendersCorrectFormat()
	{
		// Arrange
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

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("highlight.yaml", highlight));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		// Test both dropdown and flattened modes
		var testCases = new[] { new { Dropdowns = true, ExpectDropdown = true }, new { Dropdowns = false, ExpectDropdown = false } };

		foreach (var testCase in testCases)
		{
			var subOutputDir = FileSystem.Path.Join(outputDir, testCase.Dropdowns.ToString());

			var input = new RenderChangelogsArguments
			{
				Bundles = [new BundleInput { BundleFile = bundleFile }],
				Output = subOutputDir,
				Title = "9.2.0",
				Dropdowns = testCase.Dropdowns
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);

			MockFileSystemDump
				.ToText(FileSystem, subOutputDir)
				.Should()
				.MatchSnapshot(name: testCase.ExpectDropdown ? "dropdown" : "flattened");
		}
	}

	[Test]
	public async Task RenderChangelogs_AsciidocFormat_IgnoresDropdownsFlag()
	{
		// Arrange
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

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("breaking-change.yaml", breakingChange));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		// Test both dropdown values with AsciiDoc format
		var testCases = new[] { true, false };

		foreach (var dropdowns in testCases)
		{
			var subOutputDir = FileSystem.Path.Join(outputDir, dropdowns.ToString());

			var input = new RenderChangelogsArguments
			{
				Bundles = [new BundleInput { BundleFile = bundleFile }],
				Output = subOutputDir,
				Title = "9.2.0",
				Dropdowns = dropdowns,
				FileType = ChangelogFileType.Asciidoc
			};

			// Act
			var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

			// Assert
			result.Should().BeTrue();
			Collector.Errors.Should().Be(0);

			// AsciiDoc always uses the bullet format regardless of the dropdowns flag, and never MyST dropdown syntax
			MockFileSystemDump.ToText(FileSystem, subOutputDir).Should().MatchSnapshot(name: dropdowns ? "dropdowns" : "flattened");
		}
	}
}
