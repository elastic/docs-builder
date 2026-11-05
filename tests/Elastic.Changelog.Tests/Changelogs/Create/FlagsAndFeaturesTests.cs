// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Documentation.Testing;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class FlagsAndFeaturesTests() : CreateChangelogTestBase()
{
	[Test]
	public async Task CreateChangelog_WithHighlightFlag_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Important feature",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Highlight = true,
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
			title: Important feature
			highlight: true
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithFeatureId_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "New feature with flag",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			FeatureId = "feature:new-search-api",
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
			title: New feature with flag
			feature-id: "feature:new-search-api"
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithIssues_CreatesValidYaml()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Fix multiple issues",
			Type = "bug-fix",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Issues = ["https://github.com/elastic/elasticsearch/issues/123", "https://github.com/elastic/elasticsearch/issues/456"],
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
			issues:
			  - "https://github.com/elastic/elasticsearch/issues/123"
			  - "https://github.com/elastic/elasticsearch/issues/456"
			type: bug-fix
			products:
			  - product: elasticsearch
			title: Fix multiple issues
			"""
			);
	}
}
