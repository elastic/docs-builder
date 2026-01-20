// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Render;

public class ErrorHandlingTests(ITestOutputHelper output) : RenderChangelogTestBase(output)
{
	[Fact]
	public async Task RenderChangelogs_WithMissingBundleFile_ReturnsError()
	{
		// Arrange
		var missingBundle = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.yaml");

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = missingBundle }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Bundle file does not exist"));
	}

	[Fact]
	public async Task RenderChangelogs_WithMissingChangelogFile_ReturnsError()
	{
		// Arrange
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = _fileSystem.Path.Combine(bundleDir, "bundle.yaml");
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
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = bundleDir }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("does not exist"));
	}

	[Fact]
	public async Task RenderChangelogs_WithInvalidBundleStructure_ReturnsError()
	{
		// Arrange
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = _fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		// language=yaml
		var bundleContent =
			"""
			invalid_field: value
			""";
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field") || d.Message.Contains("Failed to deserialize"));
	}

	[Fact]
	public async Task RenderChangelogs_WithInvalidChangelogFile_ReturnsError()
	{
		// Arrange
		var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(changelogDir);

		// Create invalid changelog file (missing required fields)
		// language=yaml
		var invalidChangelog =
			"""
			title: Invalid feature
			# Missing type and products
			""";

		var changelogFile = _fileSystem.Path.Combine(changelogDir, "1755268130-invalid.yaml");
		await _fileSystem.File.WriteAllTextAsync(changelogFile, invalidChangelog, TestContext.Current.CancellationToken);

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
			      name: 1755268130-invalid.yaml
			      checksum: {ComputeSha1(invalidChangelog)}
			""";
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await Service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field"));
	}

	[Fact]
	public async Task RenderChangelogs_WithResolvedEntry_ValidatesAndRenders()
	{
		// Arrange
		var bundleDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		_fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = _fileSystem.Path.Combine(bundleDir, "bundle.yaml");
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
		await _fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
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
		indexContent.Should().Contain("Resolved feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithUnhandledType_EmitsWarning()
	{
		// Arrange
		// This test simulates the scenario where a new type is added to ChangelogConfiguration.cs
		// but the rendering code hasn't been updated to handle it yet.
		// We use reflection to temporarily add "experimental-feature" to the defaults for testing.
		var defaultConfig = ChangelogConfiguration.Default;
		var originalTypes = defaultConfig.AvailableTypes.ToList();
		var testType = "experimental-feature";

		// Temporarily add the test type to defaults to simulate it being added to ChangelogConfiguration.cs
		defaultConfig.AvailableTypes.Add(testType);

		try
		{
			var changelogDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
			_fileSystem.Directory.CreateDirectory(changelogDir);

			// Create changelog with an unhandled type
			// language=yaml
			var changelog1 =
				"""
				title: Experimental feature
				type: experimental-feature
				products:
				  - product: elasticsearch
				    target: 9.2.0
				description: This is an experimental feature
				""";

			var changelogFile = _fileSystem.Path.Combine(changelogDir, "1755268130-experimental.yaml");
			await _fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

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
				      name: 1755268130-experimental.yaml
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
			_collector.Warnings.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("experimental-feature") &&
				d.Message.Contains("is valid according to configuration but is not handled in rendering output") &&
				d.Message.Contains("1 entry/entries of this type will not be included"));

			// Verify that the entry is not included in the output
			var indexFile = _fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			_fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await _fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			indexContent.Should().NotContain("Experimental feature");
		}
		finally
		{
			// Restore original types
			defaultConfig.AvailableTypes.Clear();
			defaultConfig.AvailableTypes.AddRange(originalTypes);
		}
	}
}
