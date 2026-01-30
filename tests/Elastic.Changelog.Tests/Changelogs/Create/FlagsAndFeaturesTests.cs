// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class FlagsAndFeaturesTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithHighlightFlag_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Important feature",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Highlight = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("highlight: true");
	}

	[Fact]
	public async Task CreateChangelog_WithFeatureId_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "New feature with flag",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			FeatureId = "feature:new-search-api",
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("feature-id: feature:new-search-api");
	}

	[Fact]
	public async Task CreateChangelog_WithIssues_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Fix multiple issues",
			Type = "bug-fix",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Issues =
			[
				"https://github.com/elastic/elasticsearch/issues/123",
				"https://github.com/elastic/elasticsearch/issues/456"
			],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("issues:");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/123");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/456");
	}
}
