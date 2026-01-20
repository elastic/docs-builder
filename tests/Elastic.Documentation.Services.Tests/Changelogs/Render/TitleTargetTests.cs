// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Render;

public class TitleTargetTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithoutTitleAndNoTargets_EmitsWarning()
	{
		// Arrange
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile = _fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file without target
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = _fileSystem.Path.Combine(bundleDir, "bundle.yaml");
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
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir
			// Note: Title is not set
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided") &&
			d.Message.Contains("default to 'unknown'"));
	}

	[Fact]
	public async Task RenderChangelogs_WithTitleAndNoTargets_NoWarning()
	{
		// Arrange
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);

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

		var changelogFile = _fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file without target
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = _fileSystem.Path.Combine(bundleDir, "bundle.yaml");
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
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0" // Title is provided
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		// Should not have warning about missing title
		_collector.Diagnostics.Should().NotContain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided"));
	}
}
