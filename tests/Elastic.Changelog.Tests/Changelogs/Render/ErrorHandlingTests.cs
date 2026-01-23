// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Rendering;
using Elastic.Documentation.Diagnostics;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Render;

public class ErrorHandlingTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithMissingBundleFile_ReturnsError()
	{
		// Arrange
		var missingBundle = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.yaml");

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = missingBundle }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Bundle file does not exist"));
	}

	[Fact]
	public async Task RenderChangelogs_WithMissingChangelogFile_ReturnsError()
	{
		// Arrange
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: nonexistent.yaml
			      checksum: abc123
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = bundleDir }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("does not exist"));
	}

	[Fact]
	public async Task RenderChangelogs_WithInvalidBundleStructure_ReturnsError()
	{
		// Arrange
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			"""
			invalid_field: value
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field") || d.Message.Contains("Failed to deserialize"));
	}

	[Fact]
	public async Task RenderChangelogs_WithInvalidChangelogFile_ReturnsError()
	{
		// Arrange
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create invalid changelog file (missing required fields)
		// language=yaml
		var invalidChangelog =
			"""
			title: Invalid feature
			# Missing type and products
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-invalid.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, invalidChangelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-invalid.yaml
			      checksum: {ComputeSha1(invalidChangelog)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field"));
	}

	[Fact]
	public async Task RenderChangelogs_WithResolvedEntry_ValidatesAndRenders()
	{
		// Arrange
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - type: feature
			    title: Resolved feature
			    products:
			      - product: elasticsearch
			        target: 9.2.0
			    pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var indexFile = FileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		FileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await FileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("Resolved feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithUnknownType_EmitsError()
	{
		// Arrange
		// When an unknown type string is encountered during YAML deserialization,
		// it should be parsed as Invalid and an error should be emitted.
		var changelogDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with an unknown type that will be marked as Invalid
		// language=yaml
		var changelog1 =
			"""
			title: Unknown type feature
			type: some-unknown-type
			products:
			  - product: elasticsearch
			    target: 9.2.0
			description: This has an unknown type
			""";

		var changelogFile = FileSystem.Path.Combine(changelogDir, "1755268130-unknown.yaml");
		await FileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		FileSystem.Directory.CreateDirectory(bundleDir);
		var bundleFile = FileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-unknown.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await FileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new RenderChangelogsArguments
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0"
		};

		// Act
		var result = await Service.RenderChangelogs(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("Unknown type feature") &&
			d.Message.Contains("invalid or unrecognized type"));
	}
}
