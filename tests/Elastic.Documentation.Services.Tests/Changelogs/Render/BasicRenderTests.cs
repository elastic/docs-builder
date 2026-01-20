// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Render;

public class BasicRenderTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithValidBundle_CreatesMarkdownFiles()
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
			Title = "9.2.0"
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = _fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		_fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await _fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("## 9.2.0");
		indexContent.Should().Contain("Test feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithMultipleBundles_MergesAndRenders()
	{
		// Arrange
		var changelogDir1 = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var changelogDir2 = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir1);
		_fileSystem.Directory.CreateDirectory(changelogDir2);

		// Create test changelog files
		// language=yaml
		var changelog1 =
			"""
			title: First feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		// language=yaml
		var changelog2 =
			"""
			title: Second feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = _fileSystem.Path.Combine(changelogDir1, "1755268130-first.yaml");
		var file2 = _fileSystem.Path.Combine(changelogDir2, "1755268140-second.yaml");
		await _fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await _fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle files
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundle1 = _fileSystem.Path.Combine(bundleDir, "bundle1.yaml");
		// language=yaml
		var bundleContent1 =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-first.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await _fileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current.CancellationToken);

		var bundle2 = _fileSystem.Path.Combine(bundleDir, "bundle2.yaml");
		// language=yaml
		var bundleContent2 =
			$"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268140-second.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await _fileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current.CancellationToken);

		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles =
			[
				new BundleInput { BundleFile = bundle1, Directory = changelogDir1 },
				new BundleInput { BundleFile = bundle2, Directory = changelogDir2 }
			],
			Output = outputDir,
			Title = "9.2.0"
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = _fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		_fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await _fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("First feature");
		indexContent.Should().Contain("Second feature");
	}
}
