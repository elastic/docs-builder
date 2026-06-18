// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class PrFetchFailureTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithPrOptionAndTitleAndType_SkipsApiFetch()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Title = "Manual title provided",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.MustNotHaveHappened();

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

	[Fact]
	public async Task CreateChangelog_WithMultiplePrsFetchFails_EmitsAggregateWarningSummary()
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
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert: by default the bulk fetch failure is a single, loud summary warning (not an error).
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("2 of 2") &&
			d.Message.Contains("could not be fetched from GitHub"));
	}

	[Fact]
	public async Task CreateChangelog_WithMultiplePrsFetchFailsAndStrictFetch_EmitsError()
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
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			StrictFetch = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert: under --strict-fetch the bulk fetch failure escalates to an error (non-zero exit),
		// but the best-effort files are still written so they can be inspected.
		result.Should().BeTrue();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("could not be fetched from GitHub"));
	}

	[Fact]
	public async Task CreateChangelog_WithMultipleIssuesFetchFailsAndStrictFetch_EmitsError()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchIssueInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns((GitHubIssueInfo?)null);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Issues = ["https://github.com/elastic/elasticsearch/issues/12345", "https://github.com/elastic/elasticsearch/issues/67890"],
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			StrictFetch = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert: mirrors the PR path — under --strict-fetch the bulk fetch failure escalates to an error
		// (non-zero exit), but the best-effort files are still written so they can be inspected.
		result.Should().BeTrue();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("could not be fetched from GitHub"));
	}

	[Fact]
	public async Task CreateChangelog_WithSinglePrFetchFailsAndStrictFetch_EmitsError()
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
			StrictFetch = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Error &&
			d.Message.Contains("--strict-fetch"));
	}
}
