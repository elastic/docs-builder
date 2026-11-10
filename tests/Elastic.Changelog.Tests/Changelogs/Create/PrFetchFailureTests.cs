// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Testing;
using FakeItEasy;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class PrFetchFailureTests() : CreateChangelogTestBase()
{
	[Test]
	public async Task CreateChangelog_WithPrOptionAndTitleAndType_SkipsApiFetch()
	{
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Title = "Manual title provided",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Output = CreateOutputDirectory()
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Warnings.Should().Be(0);

		A.CallTo(
			() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)
		).MustNotHaveHappened();

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
			title: Manual title provided
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithPrOptionButPrFetchFails_WithoutTitleAndType_CreatesChangelogWithCommentedFields()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubPrInfo?)null
		);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/12345. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			"""
			);

		// Verify changelog file was created with commented title/type
		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		// a text snapshot: title and type are written as commented fields, and a YAML snapshot ignores comments
		yamlContent
			.WithoutSchemaComments()
			.Should()
			.MatchInlineSnapshot(
				"""
			# title: # TODO: Add title
			# type: # TODO: Add type (e.g., feature, enhancement, bug-fix, breaking-change)
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/12345"
			products:
			  - product: elasticsearch
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithMultiplePrsButPrFetchFails_GeneratesBasicChangelogs()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubPrInfo?)null
		);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345", "https://github.com/elastic/elasticsearch/pull/67890"],
			Title = "Shared title",
			Type = "bug-fix",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

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
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current!.Execution.CancellationToken);
		yamlContent
			.WithoutSchemaComments()
			.Should()
			.MatchInlineSnapshot(
				"""
			prs:
			  - "https://github.com/elastic/elasticsearch/pull/12345"
			type: bug-fix
			products:
			  - product: elasticsearch
			title: Shared title
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithMultiplePrsFetchFails_EmitsAggregateWarningSummary()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubPrInfo?)null
		);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345", "https://github.com/elastic/elasticsearch/pull/67890"],
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert: by default the bulk fetch failure is a single, loud summary warning (not an error).
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/12345. Generating basic changelog with provided values.
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/12345. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/67890. Generating basic changelog with provided values.
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/67890. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			warning  2 of 2 pull request(s) could not be fetched from GitHub. Their changelogs were created without rules.create label filtering and may be missing title or type, which will cause 'changelog bundle' to fail. Verify GITHUB_TOKEN is set and can access the referenced repositories, then delete the generated changelog files and re-run.
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithMultiplePrsFetchFailsAndStrictFetch_EmitsError()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubPrInfo?)null
		);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345", "https://github.com/elastic/elasticsearch/pull/67890"],
			Products = [new ProductArgument { Product = "elasticsearch" }],
			StrictFetch = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert: under --strict-fetch the bulk fetch failure escalates to an error (non-zero exit),
		// but the best-effort files are still written so they can be inspected.
		result.Should().BeTrue();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/12345. Generating basic changelog with provided values.
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/12345. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/67890. Generating basic changelog with provided values.
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/67890. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			error  2 of 2 pull request(s) could not be fetched from GitHub. Their changelogs were created without rules.create label filtering and may be missing title or type, which will cause 'changelog bundle' to fail. Verify GITHUB_TOKEN is set and can access the referenced repositories, then delete the generated changelog files and re-run.
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithMultipleIssuesFetchFailsAndStrictFetch_EmitsError()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchIssueInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubIssueInfo?)null
		);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Issues = ["https://github.com/elastic/elasticsearch/issues/12345", "https://github.com/elastic/elasticsearch/issues/67890"],
			Products = [new ProductArgument { Product = "elasticsearch" }],
			StrictFetch = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert: under --strict-fetch the bulk fetch failure escalates to an error.
		// No files are written because filename derivation requires a PR number;
		// the caller must use 'changelog note' for issue-only entries. The aggregate
		// "changelogs were created" fetch summary is omitted because nothing was written.
		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning  Failed to fetch issue information from GitHub for issue: https://github.com/elastic/elasticsearch/issues/12345. Generating basic changelog with provided values.
			warning  Failed to fetch issue information from GitHub for issue: https://github.com/elastic/elasticsearch/issues/12345. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			error  Could not derive a PR number from the provided --prs values. Changelog entries must be anchored to a PR number. For items with no PR use 'changelog note' instead.
			warning  Failed to fetch issue information from GitHub for issue: https://github.com/elastic/elasticsearch/issues/67890. Generating basic changelog with provided values.
			warning  Failed to fetch issue information from GitHub for issue: https://github.com/elastic/elasticsearch/issues/67890. Generating basic changelog with provided values.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			error  Could not derive a PR number from the provided --prs values. Changelog entries must be anchored to a PR number. For items with no PR use 'changelog note' instead.
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithSinglePrFetchFailsAndStrictFetch_EmitsError()
	{
		// Arrange
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubPrInfo?)null
		);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductArgument { Product = "elasticsearch" }],
			StrictFetch = true,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			warning  Failed to fetch PR information from GitHub for PR: https://github.com/elastic/elasticsearch/pull/12345. Generating basic changelog with provided values.
			error  Could not fetch pull request 'https://github.com/elastic/elasticsearch/pull/12345' from GitHub and --strict-fetch is set. Verify GITHUB_TOKEN is set and can access the repository, then re-run.
			warning  Title is missing. The changelog will be created with title commented out. Please manually update the title field.
			warning  Type is missing. The changelog will be created with type commented out. Please manually update the type field.
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithVersionedProductsOnBulkAdd_EmitsErrorWithoutFetchOrFiles()
	{
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(
			(GitHubPrInfo?)null
		);

		var service = CreateService();
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345", "https://github.com/elastic/elasticsearch/pull/67890"],
			Products =
			[
				new ProductArgument { Product = "cloud-serverless", Target = "2026-08-27" },
				new ProductArgument { Product = "kibana" }
			],
			Output = outputDir
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  Product 'cloud-serverless' specifies version(s) '2026-08-27'. 'changelog add' does not require or allow product versions."
			);
		A.CallTo(
			() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)
		).MustNotHaveHappened();
		if (FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.GetFiles(outputDir, "*.yaml").Should().BeEmpty();
	}
}
