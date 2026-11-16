// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Testing;
using FakeItEasy;
using Nullean.Snapshots;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class ValidationTests() : CreateChangelogTestBase()
{
	[Test]
	public async Task CreateChangelog_WithPrOptionButNoLabelMapping_ReturnsError()
	{
		// Arrange
		var prInfo = new GitHubPrInfo { Title = "Some PR", Labels = ["some-label"] };

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

		// Config without pivot.types mapping
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  Cannot derive type from PR https://github.com/elastic/elasticsearch/pull/12345 labels: no type mapping configured in changelog.yml. Please provide --type or configure pivot.types in changelog.yml."
			);
	}

	[Test]
	public async Task CreateChangelog_WithInvalidProduct_ReturnsError()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductArgument { Product = "invalid-product" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  Product 'invalid-product' is not in the list of available products from config/products.yml. Available products: cloud-hosted, cloud-serverless, elasticsearch, kibana, security"
			);
	}

	[Test]
	public async Task CreateChangelog_WithInvalidType_ReturnsError()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "invalid-type",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"error  Type 'invalid-type' is not in the list of available types. Available types: invalid, feature, enhancement, security, bug-fix, breaking-change, deprecation, known-issue, docs, regression, other"
			);
	}

	[Test]
	public async Task CreateChangelog_WithInvalidProductInAddBlockers_ReturnsError()
	{
		// Arrange
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - preview
			  - beta
			  - ga
			rules:
			  create:
			    products:
			      invalid-product:
			        exclude: "skip:releaseNotes"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector
			.Diagnostics
			.ToSnapshotText()
			.Should()
			.MatchInlineSnapshot(
				"""
			error {repo}/Guid_1/changelog.yml rules.create.products: 'invalid-product' not in available products. Available: cloud-hosted, cloud-serverless, elasticsearch, kibana, security
			error  Failed to load changelog configuration
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithRepoMatchingKnownProduct_InfersProduct()
	{
		// Arrange — no --products, but --repo matches a known product ID in products.yml
		var service = CreateService();

		var outputDir = CreateOutputDirectory();
		FileSystem.Directory.CreateDirectory(outputDir);

		var input = new CreateChangelogArguments
		{
			Title = "Test inference from repo name",
			Type = "feature",
			Products = [],
			Repo = "kibana",
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Output = outputDir
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
			  - product: kibana
			    lifecycle: ga
			title: Test inference from repo name
			"""
			);
	}

	[Test]
	public async Task CreateChangelog_WithUnknownRepo_ReturnsProductError()
	{
		// Arrange — no --products, --repo value does not match any known product ID
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test unknown repo",
			Type = "feature",
			Products = [],
			Repo = "my-unknown-repo",
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Diagnostics.ToSnapshotText().Should().MatchInlineSnapshot("error  At least one product is required");
	}

	[Test]
	public async Task CreateChangelog_WithValidProductInAddBlockers_Succeeds()
	{
		// Arrange
		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature:
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
			      cloud-hosted:
			        exclude: "ILM"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch" }],
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Config = configPath,
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
	}
}
