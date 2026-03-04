// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class PrFetchFailureTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithPrOptionButPrFetchFails_WithTitleAndType_CreatesChangelog()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Title = "Manual title provided",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Failed to fetch PR information") && d.Severity == Severity.Warning);

		// Verify changelog file was created with provided values
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Manual title provided");
		yamlContent.Should().Contain("type: feature");
		yamlContent.Should().Contain("prs:");
		yamlContent.Should().Contain("https://github.com/elastic/elasticsearch/pull/12345");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionButPrFetchFails_WithoutTitleAndType_CreatesChangelogWithCommentedFields()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Failed to fetch PR information") && d.Severity == Severity.Warning);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Title is missing") && d.Severity == Severity.Warning);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Type is missing") && d.Severity == Severity.Warning);

		// Verify changelog file was created with commented title/type
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("# title: # TODO: Add title");
		yamlContent.Should().Contain("# type: # TODO: Add type");
		yamlContent.Should().Contain("prs:");
		yamlContent.Should().Contain("https://github.com/elastic/elasticsearch/pull/12345");
		yamlContent.Should().Contain("products:");
		// Should not contain uncommented title/type
		var lines = yamlContent.Split('\n');
		lines.Should().NotContain(l => l.Trim().StartsWith("title:", StringComparison.Ordinal) && !l.Trim().StartsWith('#'));
		lines.Should().NotContain(l => l.Trim().StartsWith("type:", StringComparison.Ordinal) && !l.Trim().StartsWith('#'));
	}

	[Fact]
	public async Task CreateChangelog_WithMultiplePrsButPrFetchFails_GeneratesBasicChangelogs()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345", "https://github.com/elastic/elasticsearch/pull/67890"],
			Title = "Shared title",
			Type = "bug-fix",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().BeGreaterThan(0);
		// Verify that warnings were emitted for both PRs (may be multiple warnings per PR)
		var prWarnings = Collector.Diagnostics.Where(d => d.Message.Contains("Failed to fetch PR information")).ToList();
		prWarnings.Should().HaveCountGreaterThanOrEqualTo(2);
		// Verify both PR URLs are mentioned in warnings
		prWarnings.Should().Contain(d => d.Message.Contains("12345"));
		prWarnings.Should().Contain(d => d.Message.Contains("67890"));

		// Verify changelog file was created (may be 1 file if both PRs have same title/type, which is expected)
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCountGreaterThanOrEqualTo(1);

		// Verify the file contains the provided title/type and at least one PR reference
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Shared title");
		yamlContent.Should().Contain("type: bug-fix");
		// Should reference at least one of the PRs (when filenames collide, the last one wins)
		yamlContent.Should().Contain("prs:");
		yamlContent.Should().MatchRegex(@"(https://github\.com/elastic/elasticsearch/pull/12345|https://github\.com/elastic/elasticsearch/pull/67890)");
	}
}
