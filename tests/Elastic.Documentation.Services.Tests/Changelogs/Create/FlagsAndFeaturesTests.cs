// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Create;

public class FlagsAndFeaturesTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithHighlightFlag_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "Important feature",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Highlight = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("highlight: true");
	}

	[Fact]
	public async Task CreateChangelog_WithFeatureId_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "New feature with flag",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			FeatureId = "feature:new-search-api",
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("feature-id: feature:new-search-api");
	}

	[Fact]
	public async Task CreateChangelog_WithIssues_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "Fix multiple issues",
			Type = "bug-fix",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Issues =
			[
				"https://github.com/elastic/elasticsearch/issues/123",
				"https://github.com/elastic/elasticsearch/issues/456"
			],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? _fileSystem.Directory.GetCurrentDirectory();
		if (!_fileSystem.Directory.Exists(outputDir))
			_fileSystem.Directory.CreateDirectory(outputDir);
		var files = _fileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("issues:");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/123");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/456");
	}
}
