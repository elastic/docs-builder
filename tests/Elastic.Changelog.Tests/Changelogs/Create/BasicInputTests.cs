// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class BasicInputTests() : CreateChangelogTestBase()
{
	[Test]
	public async Task CreateChangelog_WithBasicInput_CreatesValidYamlFile()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Add new search feature",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Lifecycle = "ga" }],
			Description = "This is a new search feature",
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				TestContext.Current?.Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		yamlContent
			.WithoutSchemaComments()
			.Should()
			.MatchInlineSnapshot(
				"""
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/12345"
			type: feature
			products:
			  - product: elasticsearch
			    lifecycle: ga
			title: Add new search feature
			description: This is a new search feature
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithMultipleProducts_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Multi-product feature",
			Type = "feature",
			Products =
			[
				new ProductArgument { Product = "elasticsearch", Lifecycle = "ga" },
				new ProductArgument { Product = "kibana", Lifecycle = "ga" }
			],
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				TestContext.Current?.Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		yamlContent
			.WithoutSchemaComments()
			.Should()
			.MatchInlineSnapshot(
				"""
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/12345"
			type: feature
			products:
			  - product: elasticsearch
			    lifecycle: ga
			  - product: kibana
			    lifecycle: ga
			title: Multi-product feature
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithBreakingChangeAndSubtype_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Breaking API change",
			Type = "breaking-change",
			Subtype = "api",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Impact = "API clients will need to update",
			Action = "Update your API client code",
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				TestContext.Current?.Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		yamlContent
			.WithoutSchemaComments()
			.Should()
			.MatchInlineSnapshot(
				"""
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/12345"
			type: breaking-change
			subtype: api
			products:
			  - product: elasticsearch
			title: Breaking API change
			impact: API clients will need to update
			action: Update your API client code
			"""
			);
	}
}
