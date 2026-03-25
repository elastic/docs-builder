// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Changelog.GitHub;
using FakeItEasy;
using AwesomeAssertions;

namespace Elastic.Changelog.Tests.Changelogs.Create;

public class LabelMappingTests(ITestOutputHelper output) : CreateChangelogTestBase(output)
{
	[Fact]
	public async Task CreateChangelog_WithPrOptionAndLabelMapping_MapsLabelsToType()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Fix memory leak in search",
			Labels = ["type:bug"]
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
			    feature:
			    bug-fix: "type:bug"
			    breaking-change:
			    enhancement:
			  subtypes:
			    api:
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
		yamlContent.Should().Contain("type: bug-fix");
	}

	[Fact]
	public void MapLabelsToProducts_WithProductIdOnly_ParsesCorrectly()
	{
		// Arrange
		var labelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[":stack/elasticsearch"] = "elasticsearch",
			[":stack/kibana"] = "kibana"
		};

		// Act
		var result = PrInfoProcessor.MapLabelsToProducts(
			[":stack/elasticsearch", ":stack/kibana", "unrelated-label"],
			labelToProducts
		);

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain(p => p.Product == "elasticsearch" && p.Target == null && p.Lifecycle == null);
		result.Should().Contain(p => p.Product == "kibana" && p.Target == null && p.Lifecycle == null);
	}

	[Fact]
	public void MapLabelsToProducts_WithProductAndTarget_ParsesCorrectly()
	{
		// Arrange
		var labelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[":feature/new"] = "elasticsearch 9.2.0"
		};

		// Act
		var result = PrInfoProcessor.MapLabelsToProducts([":feature/new"], labelToProducts);

		// Assert
		result.Should().HaveCount(1);
		result[0].Product.Should().Be("elasticsearch");
		result[0].Target.Should().Be("9.2.0");
		result[0].Lifecycle.Should().BeNull();
	}

	[Fact]
	public void MapLabelsToProducts_WithFullSpec_ParsesCorrectly()
	{
		// Arrange
		var labelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[":cloud/serverless"] = "cloud-serverless 2025-06 ga"
		};

		// Act
		var result = PrInfoProcessor.MapLabelsToProducts([":cloud/serverless"], labelToProducts);

		// Assert
		result.Should().HaveCount(1);
		result[0].Product.Should().Be("cloud-serverless");
		result[0].Target.Should().Be("2025-06");
		result[0].Lifecycle.Should().Be("ga");
	}

	[Fact]
	public void MapLabelsToProducts_WithDuplicateMatchingLabels_DeduplicatesProducts()
	{
		// Arrange
		var labelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[":label-a"] = "elasticsearch",
			[":label-b"] = "elasticsearch"
		};

		// Act
		var result = PrInfoProcessor.MapLabelsToProducts([":label-a", ":label-b"], labelToProducts);

		// Assert — same product spec only included once
		result.Should().HaveCount(1);
		result[0].Product.Should().Be("elasticsearch");
	}

	[Fact]
	public void MapLabelsToProducts_WithNoMatchingLabels_ReturnsEmpty()
	{
		// Arrange
		var labelToProducts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[":stack/elasticsearch"] = "elasticsearch"
		};

		// Act
		var result = PrInfoProcessor.MapLabelsToProducts(["unrelated-label", ">bug"], labelToProducts);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task CreateChangelog_WithLabelProductMapping_DerviesProductsFromLabels()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Add new search feature",
			Labels = ["type:feature", ":stack/elasticsearch"]
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
			  products:
			    'elasticsearch':
			      - ":stack/elasticsearch"
			lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		// No --products provided: should be derived from PR labels
		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [],
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

		var outputDir = input.Output;
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("- product: elasticsearch");
	}

	[Fact]
	public async Task CreateChangelog_WithLabelProductMapping_MultipleMatchingLabels_AddsAllProducts()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Cross-product change",
			Labels = ["type:feature", ":stack/elasticsearch", ":stack/kibana"]
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
			  products:
			    'elasticsearch':
			      - ":stack/elasticsearch"
			    'kibana':
			      - ":stack/kibana"
			lifecycles:
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output;
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("- product: elasticsearch");
		yamlContent.Should().Contain("- product: kibana");
	}

	[Fact]
	public async Task CreateChangelog_WithLabelProductMapping_ExplicitProductsOverrideLabels()
	{
		// Arrange — PR has label for elasticsearch but --products specifies kibana
		var prInfo = new GitHubPrInfo
		{
			Title = "Fix something",
			Labels = ["type:bug", ":stack/elasticsearch"]
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
			    feature:
			    bug-fix: "type:bug"
			    breaking-change:
			  products:
			    'elasticsearch':
			      - ":stack/elasticsearch"
			lifecycles:
			  - ga
			""";
		var configPath = await CreateConfigDirectory(configContent);

		var service = CreateService();

		var input = new CreateChangelogArguments
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			// Explicit product takes precedence over label mapping
			Products = [new ProductArgument { Product = "kibana", Target = "9.2.0" }],
			Config = configPath,
			Output = CreateOutputDirectory()
		};

		// Act
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output;
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("- product: kibana");
		yamlContent.Should().NotContain("- product: elasticsearch");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionAndAreaMapping_MapsLabelsToAreas()
	{
		// Arrange
		var prInfo = new GitHubPrInfo
		{
			Title = "Add security enhancements",
			Labels = ["type:enhancement", "area:security", "area:search"]
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
			    feature:
			    bug-fix:
			    breaking-change:
			    enhancement: "type:enhancement"
			  subtypes:
			    api:
			  areas:
			    security: "area:security"
			    search: "area:search"
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
		yamlContent.Should().Contain("areas:");
		yamlContent.Should().Contain("- security");
		yamlContent.Should().Contain("- search");
	}

	[Fact]
	public void MapLabelsToAreas_WithAreaNameContainingCommas_PresservesFullName()
	{
		// Arrange
		var labelToAreas = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["Team:Alerting Services"] = ["Alerting, connectors, and reporting"],
			["Team:Search"] = ["Search"]
		};

		// Act
		var result = PrInfoProcessor.MapLabelsToAreas(
			["Team:Alerting Services", "Team:Search"],
			labelToAreas
		);

		// Assert
		result.Should().HaveCount(2);
		result.Should().Contain("Alerting, connectors, and reporting");  // Not split
		result.Should().Contain("Search");
	}

	[Fact]
	public async Task CreateChangelog_WithAreaNameContainingCommas_PreservesAreaName()
	{
		// Arrange: Area name contains commas
		var prInfo = new GitHubPrInfo
		{
			Title = "Fix alerting issues",
			Labels = ["type:bug", "Team:Alerting Services"]
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
			    bug-fix: "type:bug"
			    feature:
			    breaking-change:
			  areas:
			    "Alerting, connectors, and reporting":
			      - "Team:Alerting Services"
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
		if (!result)
		{
			foreach (var diagnostic in Collector.Diagnostics)
				Output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
		}

		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? FileSystem.Directory.GetCurrentDirectory();
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("- Alerting, connectors, and reporting");  // Full name, not split
	}

	[Fact]
	public async Task CreateChangelog_WithOneLabelMappedToMultipleAreas_AddsAllAreas()
	{
		// Arrange: Same label under multiple areas
		var prInfo = new GitHubPrInfo
		{
			Title = "Cross-area change",
			Labels = ["type:enhancement", "Team:Search"]
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
			    enhancement: "type:enhancement"
			    feature:
			    bug-fix:
			    breaking-change:
			  areas:
			    "Search":
			      - "Team:Search"
			    "Observability":
			      - "Team:Search"
			lifecycles:
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
		var result = await service.CreateChangelog(Collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		Collector.Errors.Should().Be(0);

		var outputDir = input.Output;
		if (!FileSystem.Directory.Exists(outputDir))
			FileSystem.Directory.CreateDirectory(outputDir);
		var files = FileSystem.Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await FileSystem.File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("- Search");
		yamlContent.Should().Contain("- Observability");
	}
}
