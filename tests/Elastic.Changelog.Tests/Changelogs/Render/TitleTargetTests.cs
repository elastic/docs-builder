// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class TitleTargetTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithoutTitleAndNoTargets_EmitsWarning()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file without target
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir
			// Note: Title is not set
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided") &&
			d.Message.Contains("default to 'unknown'"));
	}

	[Fact]
	public async Task RenderChangelogs_WithTitleAndNoTargets_NoWarning()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without target
		// language=yaml
		var changelog1 =
			"""
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file without target
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0" // Title is provided
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		// Should not have warning about missing title
		Collector.Diagnostics.Should().NotContain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided"));
	}
}
