// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using FakeItEasy;
using FluentAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class ValidationTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithPrOptionButNoLabelMapping_ReturnsError()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Some PR",
			Labels = ["some-label"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(
				A<string>._,
				A<string?>._,
				A<string?>._,
				A<CancellationToken>._))
			.Returns(prInfo);

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
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("Cannot derive type from PR"));
	}

	[Fact]
	public async Task CreateChangelog_WithInvalidProduct_ReturnsError()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductArgument { Product = "invalid-product", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("is not in the list of available products"));
	}

	[Fact]
	public async Task CreateChangelog_WithInvalidType_ReturnsError()
	{
		// Arrange
		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "invalid-type",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d => d.Message.Contains("is not in the list of available types"));
	}

	[Fact]
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
			block:
			  product:
			    invalid-product:
			      create: "skip:releaseNotes"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		Collector.Errors.Should().BeGreaterThan(0);
		Collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("Product 'invalid-product' in block.product") && d.Message.Contains("is not in the list of available products"));
	}

	[Fact]
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
			block:
			  product:
			    elasticsearch:
			      create: "skip:releaseNotes"
			    cloud-hosted:
			      create: "ILM"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductArgument { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
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
	}
}
