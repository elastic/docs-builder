// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Render;

/// <summary>
/// Tests that CLI rendering does not produce incomplete "For more information, check." sentences
/// when entries have only PRIVATE PR/issue references.
/// </summary>
public class PrivateLinkBugTests() : RenderChangelogTestBase()
{
	[Test]
	public async Task RenderChangelogs_WithOnlyPrivateLinks_DoesNotRenderIncompleteForMoreInformationSentence()
	{
		// Arrange
		// Create deprecation entry with only private links (matching Cloud scenario)
		// language=yaml
		var changelog =
			"""
			title: The v1 Costs API has been deprecated. Customers should migrate to the v2 Costs API.
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- '# PRIVATE: https://github.com/elastic/cloud/pull/153728'
			description: This API will be removed in a future version.
			impact: Users must update their integration.
			action: Follow the migration guide.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("153728-deprecate-costs-api-v1.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.3.0"
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
			=== 9.3.0/breaking-changes.md ===
			## 9.3.0 [elasticsearch-9.3.0-breaking-changes]
			_There are no breaking changes associated with this release._
			=== 9.3.0/deprecations.md ===
			## 9.3.0 [elasticsearch-9.3.0-deprecations]

			* The v1 Costs API has been deprecated. Customers should migrate to the v2 Costs API.
			  This API will be removed in a future version.

			  **Impact:** Users must update their integration.

			  **Action:** Follow the migration guide.

			=== 9.3.0/index.md ===
			## 9.3.0 [elasticsearch-release-notes-9.3.0]
			_There are no new features, enhancements, or fixes associated with this release._
			=== 9.3.0/known-issues.md ===
			## 9.3.0 [elasticsearch-9.3.0-known-issues]
			_There are no known issues associated with this release._
			"""
			);
	}

	[Test]
	public async Task RenderChangelogs_WithMixedPrivateAndPublicLinks_RendersOnlyPublicLinks()
	{
		// Arrange
		// Create entry with mix of private and public links
		// language=yaml
		var changelog =
			"""
			title: Mixed links breaking change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.3.0
			prs:
			- '# PRIVATE: https://github.com/elastic/cloud/pull/153728'
			- "123456"
			issues:
			- '# PRIVATE: https://github.com/elastic/cloud/issues/789'
			- "654321"
			description: This change breaks compatibility.
			impact: Users must update.
			action: Follow upgrade guide.
			""";

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleHeader =
			"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			""";
		var bundleContent = CreateResolvedBundleContent(bundleHeader, ("123456-mixed-links.yaml", changelog));
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current!.Execution.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		MockFileSystemDump.ToText(FileSystem, outputDir).Should().MatchSnapshot();
	}
}
