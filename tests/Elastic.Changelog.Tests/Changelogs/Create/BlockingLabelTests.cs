// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class BlockingLabelTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithBlockingLabel_SkipsChangelogCreation()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with blocking label",
			Labels = ["type:feature", "skip:releaseNotes"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  create:
			    products:
			      elasticsearch:
			        exclude: "skip:releaseNotes"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234"],
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains("skip:releaseNotes"));

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}

	[Fact]
	public async Task CreateChangelog_WithBlockingLabelForSpecificProduct_OnlyBlocksForThatProduct()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with blocking label",
			Labels = ["type:feature", "ILM"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  create:
			    products:
			      cloud-serverless:
			        exclude: "ILM"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234"],
			Products =
			[
				new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-serverless", Target = "2025-08-05" }
			],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog due to cloud-serverless blocker
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains("ILM"));

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created because cloud-serverless blocks it
	}

	[Fact]
	public async Task CreateChangelog_WithCommaSeparatedProductIdsInAddBlockers_ExpandsCorrectly()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with blocking label",
			Labels = ["type:feature", ">non-issue"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  create:
			    products:
			      'elasticsearch, cloud-serverless':
			        exclude: ">non-issue"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234"],
			Products =
			[
				new ProductArgument { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-serverless", Target = "2025-08-05" }
			],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog due to blocker
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains(">non-issue"));

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}
}
