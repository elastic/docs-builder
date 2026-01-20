// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Services.Changelog;
using FluentAssertions;

namespace Elastic.Documentation.Services.Tests.Changelogs.Create;

public class BasicInputTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithBasicInput_CreatesValidYamlFile()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "Add new search feature",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Description = "This is a new search feature",
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
		files.Should().HaveCount(1);

		var yamlContent = await _fileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Add new search feature");
		yamlContent.Should().Contain("type: feature");
		yamlContent.Should().Contain("product: elasticsearch");
		yamlContent.Should().Contain("target: 9.2.0");
		yamlContent.Should().Contain("lifecycle: ga");
		yamlContent.Should().Contain("description: This is a new search feature");
	}

	[Fact]
	public async Task CreateChangelog_WithMultipleProducts_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "Multi-product feature",
			Type = "feature",
			Products =
			[
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductInfo { Product = "kibana", Target = "9.2.0", Lifecycle = "ga" }
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
		yamlContent.Should().Contain("products:");
		// Should contain both products
		var elasticsearchIndex = yamlContent.IndexOf("product: elasticsearch", StringComparison.Ordinal);
		var kibanaIndex = yamlContent.IndexOf("product: kibana", StringComparison.Ordinal);
		elasticsearchIndex.Should().BeGreaterThan(-1);
		kibanaIndex.Should().BeGreaterThan(-1);
	}

	[Fact]
	public async Task CreateChangelog_WithBreakingChangeAndSubtype_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new ChangelogInput
		{
			Title = "Breaking API change",
			Type = "breaking-change",
			Subtype = "api",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Impact = "API clients will need to update",
			Action = "Update your API client code",
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
		yamlContent.Should().Contain("type: breaking-change");
		yamlContent.Should().Contain("subtype: api");
		yamlContent.Should().Contain("impact: API clients will need to update");
		yamlContent.Should().Contain("action: Update your API client code");
	}
}
