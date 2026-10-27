// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using Elastic.Documentation.Diagnostics;
using FakeItEasy;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class BlockingLabelTests() : CreateChangelogTestBase()
{
	[Test]
	public async Task CreateChangelog_WithBlockingLabel_SkipsChangelogCreation()
	{
		// Arrange
		var prInfo = new GitHubPrInfo { Title = "PR with blocking label", Labels = ["type:feature", "skip:releaseNotes"] };

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

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
			Products = [new ProductArgument { Product = "elasticsearch", Lifecycle = "ga" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog
		Collector.Errors.Should().Be(0);
		Logged.Should().Contain(l => l.Contains("entries: exclude"));
		Logged.Should().Contain(l => l.Contains("entries: exclude") && l.Contains("skip:releaseNotes"));

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}

	[Test]
	public async Task CreateChangelog_WithBlockingLabelForSpecificProduct_OnlyBlocksForThatProduct()
	{
		// Arrange
		var prInfo = new GitHubPrInfo { Title = "PR with blocking label", Labels = ["type:feature", "ILM"] };

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

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
				new ProductArgument { Product = "elasticsearch", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-serverless", Lifecycle = "ga" }
			],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog due to cloud-serverless blocker
		Collector.Errors.Should().Be(0);
		Logged.Should().Contain(l => l.Contains("entries: exclude"));
		Logged.Should().Contain(l => l.Contains("entries: exclude") && l.Contains("ILM"));

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created because cloud-serverless blocks it
	}

	[Test]
	public async Task CreateChangelog_WithCommaSeparatedProductIdsInAddBlockers_ExpandsCorrectly()
	{
		// Arrange
		var prInfo = new GitHubPrInfo { Title = "PR with blocking label", Labels = ["type:feature", ">non-issue"] };

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

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
				new ProductArgument { Product = "elasticsearch", Lifecycle = "ga" },
				new ProductArgument { Product = "cloud-serverless", Lifecycle = "ga" }
			],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog due to blocker
		Collector.Errors.Should().Be(0);
		Logged.Should().Contain(l => l.Contains("entries: exclude"));
		Logged.Should().Contain(l => l.Contains("entries: exclude") && l.Contains(">non-issue"));

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}

	[Test]
	public async Task CreateChangelog_WithLabelDerivedProduct_AppliesProductSpecificCreateRule()
	{
		// This test verifies the timing fix: products derived from pivot.products label mapping
		// are available before rules.create.products is evaluated (the blocker check).
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with product label and blocking label",
			Labels = ["type:feature", ":stack/elasticsearch", "skip:releaseNotes"]
		};

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			  products:
			    'elasticsearch':
			      - ":stack/elasticsearch"
			lifecycles:
			  - ga
			rules:
			  create:
			    products:
			      elasticsearch:
			        exclude: "skip:releaseNotes"
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		// No --products provided; products must be derived from labels to trigger the product-specific rule
		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		// Assert — product-specific rule should have blocked creation
		result.Should().BeTrue(); // Succeed but skip
		Collector.Errors.Should().Be(0);
		Logged.Should().Contain(l => l.Contains("entries: exclude") && l.Contains("skip:releaseNotes"));

		var outputDir = input.Output;
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0);
	}

	[Test]
	public async Task CreateChangelog_WithDefaultProductThatHasAnOverride_GlobalSkipLabelDoesNotApply()
	{
		// The entry gets products.default, so the blocker check must see that product's override, which replaces the
		// global rules: the global skip label does not skip the pull request.
		var prInfo = new GitHubPrInfo { Title = "PR with the global skip label", Labels = ["type:feature", "changelog:skip"] };
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			products:
			  default:
			    - product: kibana
			lifecycles:
			  - ga
			rules:
			  create:
			    exclude: "changelog:skip"
			    products:
			      kibana:
			        exclude: "kibana:skip"
			""";
		var configPath = await CreateConfigDirectory(configContent);
		var service = CreateService();
		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/kibana/pull/12345"],
			Products = [],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Logged.Should().NotContain(l => l.Contains("entries: exclude"));
		FileSystem.Directory.GetFiles(input.Output!, "*.yaml").Should().HaveCount(1);
	}

	[Test]
	public async Task CreateChangelog_WithNoResolvedProductAndProductOverrides_GlobalSkipLabelApplies()
	{
		// No explicit, label-derived or default product: the per-product overrides name nothing that applies, so the
		// global skip label decides and the pull request is skipped.
		var prInfo = new GitHubPrInfo { Title = "PR with the global skip label", Labels = ["type:feature", "changelog:skip"] };
		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - ga
			rules:
			  create:
			    exclude: "changelog:skip"
			    products:
			      kibana:
			        exclude: "kibana:skip"
			""";
		var configPath = await CreateConfigDirectory(configContent);
		var service = CreateService();
		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/kibana/pull/12345"],
			Products = [],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Logged.Should().Contain(l => l.Contains("entries: exclude") && l.Contains("changelog:skip"));
		if (!FileSystem.Directory.Exists(input.Output!))
			FileSystem.Directory.CreateDirectory(input.Output!);
		FileSystem.Directory.GetFiles(input.Output!, "*.yaml").Should().HaveCount(0);
	}

	[Test]
	public async Task CreateChangelog_WithAllPrsSkippedByCreateRules_ReturnsSuccessWithoutErrors()
	{
		var prInfo = new GitHubPrInfo { Title = "PR with blocking label", Labels = ["type:feature", "skip:releaseNotes"] };

		A.CallTo(() => MockGitHubService.FetchPrInfoAsync(A<string>._, A<string?>._, A<string?>._, A<CancellationToken>._)).Returns(prInfo);

		// language=yaml
		var configContent =
			"""
			pivot:
			  types:
			    feature: "type:feature"
			    bug-fix:
			    breaking-change:
			lifecycles:
			  - ga
			rules:
			  create:
			    products:
			      elasticsearch:
			        exclude: "skip:releaseNotes"
			""";
		var configPath = await CreateConfigDirectory(configContent);
		var service = CreateService();
		var outputDir = CreateOutputDirectory();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/111", "https://github.com/elastic/elasticsearch/pull/222"],
			Products = [new ProductArgument { Product = "elasticsearch", Lifecycle = "ga" }],
			Config = configPath,
			Output = outputDir
		};

		var result = await service.CreateChangelog(Collector, input, TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);
		Collector.Diagnostics.Should().NotContain(d => d.Message.Contains("returned false without emitting errors"));
		Collector.Diagnostics.Should().OnlyContain(d => d.Severity != Severity.Error);
		Logged.Should().Contain(l => l.Contains("entries: exclude"));
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		FileSystem.Directory.GetFiles(outputDir, "*.yaml").Should().BeEmpty();
	}
}
