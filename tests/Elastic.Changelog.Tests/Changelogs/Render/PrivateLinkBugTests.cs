// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Configuration;

namespace Elastic.Changelog.Tests.Changelogs.Render;

/// <summary>
/// Tests that CLI rendering does not produce incomplete "For more information, check." sentences
/// when entries have only PRIVATE PR/issue references.
/// </summary>
public class PrivateLinkBugTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithOnlyPrivateLinks_DoesNotRenderIncompleteForMoreInformationSentence()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile = FileSystem.Path.Join(changelogDir, "153728-deprecate-costs-api-v1.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			entries:
			  - file:
			      name: 153728-deprecate-costs-api-v1.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var deprecationsFile = FileSystem.Path.Join(outputDir, "9.3.0", "deprecations.md");
		FileSystem.File.Exists(deprecationsFile).Should().BeTrue();

		var content = await FileSystem.File.ReadAllTextAsync(deprecationsFile, TestContext.Current.CancellationToken);

		// Should not contain the incomplete sentence
		content.Should().NotContain("For more information, check.");
		content.Should().NotContain("check .");
		content.Should().NotContain("check.");

		// Should still contain the entry content
		content.Should().Contain("The v1 Costs API has been deprecated");
		content.Should().Contain("This API will be removed");
		content.Should().Contain("Users must update");
		content.Should().Contain("Follow the migration guide");
	}

	[Fact]
	public async Task RenderChangelogs_WithMixedPrivateAndPublicLinks_RendersOnlyPublicLinks()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile = FileSystem.Path.Join(changelogDir, "123456-mixed-links.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString(), "bundle.yaml");
		FileSystem.Directory.CreateDirectory(FileSystem.Path.GetDirectoryName(bundleFile)!);

		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    repo: elasticsearch
			    owner: elastic
			entries:
			  - file:
			      name: 123456-mixed-links.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Join(Paths.WorkingDirectoryRoot.FullName, Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir, Repo = "elasticsearch" }],
			Output = outputDir,
			Title = "9.3.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var breakingChangesFile = FileSystem.Path.Join(outputDir, "9.3.0", "breaking-changes.md");
		FileSystem.File.Exists(breakingChangesFile).Should().BeTrue();

		var content = await FileSystem.File.ReadAllTextAsync(breakingChangesFile, TestContext.Current.CancellationToken);

		// Should contain proper "For more information" with only public links
		content.Should().Contain("For more information, check");
		content.Should().Contain("#123456");
		content.Should().Contain("#654321");

		// Should not contain incomplete sentence
		content.Should().NotContain("For more information, check.");

		// Should end properly after the links
		content.Should().Contain("654321](https://github.com/elastic/elasticsearch/issues/654321).");
	}
}
