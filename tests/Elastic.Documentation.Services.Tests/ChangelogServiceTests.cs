// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services.Changelog;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Services.Tests;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method names with underscores are standard in xUnit")]
public class ChangelogServiceTests : IDisposable
{
	private readonly MockFileSystem _fileSystem;
	private readonly IConfigurationContext _configurationContext;
	private readonly TestDiagnosticsCollector _collector;
	private readonly ILoggerFactory _loggerFactory;
	private readonly ITestOutputHelper _output;

	public ChangelogServiceTests(ITestOutputHelper output)
	{
		_output = output;
		_fileSystem = new MockFileSystem();
		_collector = new TestDiagnosticsCollector(output);
		_loggerFactory = new TestLoggerFactory(output);

		var versionsConfiguration = new VersionsConfiguration
		{
			VersioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>
			{
				{
					VersioningSystemId.Stack, new VersioningSystem
					{
						Id = VersioningSystemId.Stack,
						Current = new SemVersion(9, 2, 0),
						Base = new SemVersion(9, 2, 0)
					}
				}
			},
		};

		var products = new Dictionary<string, Product>
		{
			{
				"elasticsearch", new Product
				{
					Id = "elasticsearch",
					DisplayName = "Elasticsearch",
					VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
				}
			},
			{
				"kibana", new Product
				{
					Id = "kibana",
					DisplayName = "Kibana",
					VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
				}
			},
			{
				"cloud-hosted", new Product
				{
					Id = "cloud-hosted",
					DisplayName = "Elastic Cloud Hosted",
					VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
				}
			},
			{
				"cloud-serverless", new Product
				{
					Id = "cloud-serverless",
					DisplayName = "Elastic Cloud Serverless",
					VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
				}
			}
		};
		var productsConfiguration = new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};

		_configurationContext = new ConfigurationContext
		{
			Endpoints = new DocumentationEndpoints
			{
				Elasticsearch = ElasticsearchEndpoint.Default,
			},
			ConfigurationFileProvider = new ConfigurationFileProvider(NullLoggerFactory.Instance, _fileSystem),
			VersionsConfiguration = versionsConfiguration,
			ProductsConfiguration = productsConfiguration,
			SearchConfiguration = new SearchConfiguration { Synonyms = new Dictionary<string, string[]>(), Rules = [], DiminishTerms = [] },
			LegacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] },
		};
	}

	public void Dispose()
	{
		_loggerFactory?.Dispose();
		GC.SuppressFinalize(this);
	}

	[Fact]
	public async Task CreateChangelog_WithBasicInput_CreatesValidYamlFile()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Add new search feature",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Description = "This is a new search feature",
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Add new search feature");
		yamlContent.Should().Contain("type: feature");
		yamlContent.Should().Contain("product: elasticsearch");
		yamlContent.Should().Contain("target: 9.2.0");
		yamlContent.Should().Contain("lifecycle: ga");
		yamlContent.Should().Contain("description: This is a new search feature");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOption_FetchesPrInfoAndDerivesTitle()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		// Create a config file with label mappings
		// Note: ChangelogService uses real FileSystem, so we need to use the real file system
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		yamlContent.Should().Contain("type: feature");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/12345");
	}

	[Fact]
	public async Task CreateChangelog_WithStripTitlePrefix_RemovesSquareBracketsAndColon()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "[ES|QL]: Update Vector Similarity To Support BFLOAT16",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		// Create a config file with label mappings
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			StripTitlePrefix = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Update Vector Similarity To Support BFLOAT16");
		yamlContent.Should().NotContain("[ES|QL]");
		yamlContent.Should().NotContain("[ES|QL]:");
	}

	[Fact]
	public async Task CreateChangelog_WithStripTitlePrefix_RemovesSquareBracketsWithoutColon()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "[Security] Improve authentication handling",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		// Create a config file with label mappings
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			StripTitlePrefix = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Improve authentication handling");
		yamlContent.Should().NotContain("[Security]");
	}

	[Fact]
	public async Task CreateChangelog_WithUsePrNumber_CreatesFileWithPrNumberAsFilename()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Fix memory leak in search",
			Labels = ["type:bug"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/140034",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		// Create a config file with label mappings
		// Note: ChangelogService uses real FileSystem, so we need to use the real file system
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:bug": bug-fix
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/140034"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			UsePrNumber = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		// Verify the filename is the PR number, not a timestamp-based name
		var fileName = Path.GetFileName(files[0]);
		fileName.Should().Be("140034.yaml", "the filename should be the PR number when UsePrNumber is true");

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("type: bug-fix");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/140034");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionAndLabelMapping_MapsLabelsToType()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Fix memory leak in search",
			Labels = ["type:bug"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fs = new FileSystem();
		var configDir = fs.Path.Combine(fs.Path.GetTempPath(), Guid.NewGuid().ToString());
		fs.Directory.CreateDirectory(configDir);
		var configPath = fs.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			  - enhancement
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:bug": bug-fix
			  "type:feature": feature
			""";
		await fs.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = fs.Path.Combine(fs.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("type: bug-fix");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionAndAreaMapping_MapsLabelsToAreas()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Add security enhancements",
			Labels = ["type:enhancement", "area:security", "area:search"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - enhancement
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:enhancement": enhancement
			label_to_areas:
			  "area:security": security
			  "area:search": search
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("areas:");
		yamlContent.Should().Contain("- security");
		yamlContent.Should().Contain("- search");
	}

	[Fact]
	public async Task CreateChangelog_WithPrNumberAndOwnerRepo_FetchesPrInfo()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Update documentation",
			Labels = []
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"12345",
			"elastic",
			"elasticsearch",
			A<CancellationToken>._))
			.Returns(prInfo);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Prs = ["12345"],
			Owner = "elastic",
			Repo = "elasticsearch",
			Title = "Update documentation",
			Type = "docs",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"12345",
			"elastic",
			"elasticsearch",
			A<CancellationToken>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task CreateChangelog_WithExplicitTitle_OverridesPrTitle()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "PR Title from GitHub",
			Labels = []
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Title = "Custom Title Override",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Custom Title Override");
		yamlContent.Should().NotContain("PR Title from GitHub");
	}

	[Fact]
	public async Task CreateChangelog_WithMultipleProducts_CreatesValidYaml()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Multi-product feature",
			Type = "feature",
			Products = [
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductInfo { Product = "kibana", Target = "9.2.0", Lifecycle = "ga" }
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("products:");
		// Should contain both products
		var elasticsearchIndex = yamlContent.IndexOf("product: elasticsearch", StringComparison.Ordinal);
		var kibanaIndex = yamlContent.IndexOf("product: kibana", StringComparison.Ordinal);
		elasticsearchIndex.Should().BeGreaterThan(-1);
		kibanaIndex.Should().BeGreaterThan(-1);
	}

	[Fact]
	public async Task CreateChangelog_WithBreakingChangeAndSubtype_CreatesValidYaml()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Breaking API change",
			Type = "breaking-change",
			Subtype = "api",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Impact = "API clients will need to update",
			Action = "Update your API client code",
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("type: breaking-change");
		yamlContent.Should().Contain("subtype: api");
		yamlContent.Should().Contain("impact: API clients will need to update");
		yamlContent.Should().Contain("action: Update your API client code");
	}

	[Fact]
	public async Task CreateChangelog_WithIssues_CreatesValidYaml()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Fix multiple issues",
			Type = "bug-fix",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Issues = [
				"https://github.com/elastic/elasticsearch/issues/123",
				"https://github.com/elastic/elasticsearch/issues/456"
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("issues:");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/123");
		yamlContent.Should().Contain("- https://github.com/elastic/elasticsearch/issues/456");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionButNoLabelMapping_ReturnsError()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Some PR",
			Labels = ["some-label"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		// Config without label_to_type mapping
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Cannot derive type from PR"));
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionButPrFetchFails_WithTitleAndType_CreatesChangelog()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Title = "Manual title provided",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Failed to fetch PR information") && d.Severity == Severity.Warning);

		// Verify changelog file was created with provided values
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Manual title provided");
		yamlContent.Should().Contain("type: feature");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/12345");
	}

	[Fact]
	public async Task CreateChangelog_WithPrOptionButPrFetchFails_WithoutTitleAndType_CreatesChangelogWithCommentedFields()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Failed to fetch PR information") && d.Severity == Severity.Warning);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Title is missing") && d.Severity == Severity.Warning);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Type is missing") && d.Severity == Severity.Warning);

		// Verify changelog file was created with commented title/type
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("# title: # TODO: Add title");
		yamlContent.Should().Contain("# type: # TODO: Add type");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/12345");
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
		var mockGitHubService = A.Fake<IGitHubPrService>();

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns((GitHubPrInfo?)null);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345", "https://github.com/elastic/elasticsearch/pull/67890"],
			Title = "Shared title",
			Type = "bug-fix",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		// Verify that warnings were emitted for both PRs (may be multiple warnings per PR)
		var prWarnings = _collector.Diagnostics.Where(d => d.Message.Contains("Failed to fetch PR information")).ToList();
		prWarnings.Should().HaveCountGreaterThanOrEqualTo(2);
		// Verify both PR URLs are mentioned in warnings
		prWarnings.Should().Contain(d => d.Message.Contains("12345"));
		prWarnings.Should().Contain(d => d.Message.Contains("67890"));

		// Verify changelog file was created (may be 1 file if both PRs have same title/type, which is expected)
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCountGreaterThanOrEqualTo(1);

		// Verify the file contains the provided title/type and at least one PR reference
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Shared title");
		yamlContent.Should().Contain("type: bug-fix");
		// Should reference at least one of the PRs (when filenames collide, the last one wins)
		yamlContent.Should().MatchRegex(@"pr:\s*(https://github\.com/elastic/elasticsearch/pull/12345|https://github\.com/elastic/elasticsearch/pull/67890)");
	}

	[Fact]
	public async Task CreateChangelog_WithInvalidProduct_ReturnsError()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductInfo { Product = "invalid-product", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("is not in the list of available products"));
	}

	[Fact]
	public async Task CreateChangelog_WithInvalidType_ReturnsError()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Test",
			Type = "invalid-type",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("is not in the list of available types"));
	}

	[Fact]
	public async Task CreateChangelog_WithInvalidProductInAddBlockers_ReturnsError()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			add_blockers:
			  invalid-product:
			    - "skip:releaseNotes"
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Product 'invalid-product' in add_blockers") && d.Message.Contains("is not in the list of available products"));
	}

	[Fact]
	public async Task CreateChangelog_WithValidProductInAddBlockers_Succeeds()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			add_blockers:
			  elasticsearch:
			    - "skip:releaseNotes"
			  cloud-hosted:
			    - "ILM"
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Title = "Test",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
	}

	[Fact]
	public async Task CreateChangelog_WithHighlightFlag_CreatesValidYaml()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "Important feature",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Highlight = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("highlight: true");
	}

	[Fact]
	public async Task CreateChangelog_WithFeatureId_CreatesValidYaml()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);
		var fileSystem = new FileSystem();

		var input = new ChangelogInput
		{
			Title = "New feature with flag",
			Type = "feature",
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			FeatureId = "feature:new-search-api",
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		if (!result)
		{
			foreach (var diagnostic in _collector.Diagnostics)
			{
				_output.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
			}
		}
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Note: ChangelogService uses real FileSystem, so we need to check the actual file system
		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("feature-id: feature:new-search-api");
	}

	[Fact]
	public async Task CreateChangelog_WithMultiplePrs_CreatesOneFilePerPr()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var pr1Info = new GitHubPrInfo
		{
			Title = "First PR feature",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "Second PR bug fix",
			Labels = ["type:bug"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("1234"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("5678"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr2Info);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			  "type:bug": bug-fix
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234", "https://github.com/elastic/elasticsearch/pull/5678"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(2);

		var yamlContents = new List<string>();
		foreach (var file in files)
		{
			yamlContents.Add(await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));
		}

		// Verify both PRs were processed
		yamlContents.Should().Contain(c => c.Contains("title: First PR feature"));
		yamlContents.Should().Contain(c => c.Contains("title: Second PR bug fix"));
	}

	[Fact]
	public async Task BundleChangelogs_WithAllOption_CreatesValidBundle()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: First changelog
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Second changelog
			type: enhancement
			products:
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/kibana/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-first-changelog.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-second-changelog.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("products:");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("product: kibana");
		bundleContent.Should().Contain("entries:");
		bundleContent.Should().Contain("file:");
		bundleContent.Should().Contain("name: 1755268130-first-changelog.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-changelog.yaml");
		bundleContent.Should().Contain("checksum:");
	}

	[Fact]
	public async Task BundleChangelogs_WithProductsFilter_FiltersCorrectly()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/kibana/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-kibana-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().NotContain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFilter_FiltersCorrectly()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: First PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Second PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";
		var changelog3 = """
			title: Third PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-first-pr.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-second-pr.yaml");
		var file3 = fileSystem.Path.Combine(changelogDir, "1755268150-third-pr.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/100", "https://github.com/elastic/elasticsearch/pull/200"],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-first-pr.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-pr.yaml");
		bundleContent.Should().NotContain("name: 1755268150-third-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFilterAndUnmatchedPrs_EmitsWarnings()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file for only one PR
		var changelog1 = """
			title: First PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-first-pr.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = [
				"https://github.com/elastic/elasticsearch/pull/100",
				"https://github.com/elastic/elasticsearch/pull/200",
				"https://github.com/elastic/elasticsearch/pull/300"
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().Be(2); // Two unmatched PRs
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No changelog file found for PR: https://github.com/elastic/elasticsearch/pull/200"));
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No changelog file found for PR: https://github.com/elastic/elasticsearch/pull/300"));
	}

	[Fact]
	public async Task BundleChangelogs_WithPrsFileFilter_FiltersCorrectly()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: First PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Second PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-first-pr.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-second-pr.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create PRs file
		var prsFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "prs.txt");
		fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(prsFile)!);
		await fileSystem.File.WriteAllTextAsync(prsFile, """
			https://github.com/elastic/elasticsearch/pull/100
			https://github.com/elastic/elasticsearch/pull/200
			""", TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = [prsFile],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-first-pr.yaml");
		bundleContent.Should().Contain("name: 1755268140-second-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrNumberAndOwnerRepo_FiltersCorrectly()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		var changelog1 = """
			title: PR with number
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-pr-number.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = ["100"],
			Owner = "elastic",
			Repo = "elasticsearch",
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-pr-number.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithShortPrFormat_FiltersCorrectly()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		var changelog1 = """
			title: PR with short format
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/133609
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-short-format.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = ["elastic/elasticsearch#133609"],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-short-format.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNoMatchingFiles_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("No YAML files found") || d.Message.Contains("No changelog entries matched"));
	}

	[Fact]
	public async Task BundleChangelogs_WithInvalidDirectory_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var invalidDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

		var input = new ChangelogBundleInput
		{
			Directory = invalidDir,
			All = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Directory does not exist"));
	}

	[Fact]
	public async Task BundleChangelogs_WithNoFilterOption_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: First changelog
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Second changelog
			type: enhancement
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/kibana/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-first-changelog.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-second-changelog.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("At least one filter option must be specified"));
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleFilterOptions_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Multiple filter options cannot be specified together"));
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleProducts_CreatesValidBundle()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Cloud serverless feature 1
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-02
			pr: https://github.com/elastic/cloud-serverless/pull/100
			""";
		var changelog2 = """
			title: Cloud serverless feature 2
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-06
			pr: https://github.com/elastic/cloud-serverless/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-cloud-feature1.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-cloud-feature2.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "*" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "*" }
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("target: 2025-12-02");
		bundleContent.Should().Contain("target: 2025-12-06");
		bundleContent.Should().Contain("name: 1755268130-cloud-feature1.yaml");
		bundleContent.Should().Contain("name: 1755268140-cloud-feature2.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithWildcardProductFilter_MatchesAllProducts()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/kibana/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-kibana-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [new ProductInfo { Product = "*", Target = "9.2.0", Lifecycle = "ga" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().Contain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithWildcardAllParts_EquivalentToAll()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.3.0
			    lifecycle: beta
			pr: https://github.com/elastic/kibana/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-kibana-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [new ProductInfo { Product = "*", Target = "*", Lifecycle = "*" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().Contain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithPrefixWildcardTarget_MatchesCorrectly()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Elasticsearch 9.3.0 feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Elasticsearch 9.3.1 feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.1
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";
		var changelog3 = """
			title: Elasticsearch 9.2.0 feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-es-9.3.0.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-es-9.3.1.yaml");
		var file3 = fileSystem.Path.Combine(changelogDir, "1755268150-es-9.2.0.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.3.*", Lifecycle = "*" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-es-9.3.0.yaml");
		bundleContent.Should().Contain("name: 1755268140-es-9.3.1.yaml");
		bundleContent.Should().NotContain("name: 1755268150-es-9.2.0.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNonExistentFileAsPrs_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Provide a non-existent file path - should return error since there are no other PRs
		var nonexistentFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt");
		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = [nonexistentFile],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		// File doesn't exist and there are no other PRs, so should return error
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("File does not exist"));
	}

	[Fact]
	public async Task BundleChangelogs_WithUrlAsPrs_TreatsAsPrIdentifier()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create a changelog file for a specific PR
		var changelog = """
			title: Test PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/123
			""";
		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-test-pr.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Provide a URL - should be treated as a PR identifier, not a file path
		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = ["https://github.com/elastic/elasticsearch/pull/123"],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		// URL should be treated as PR identifier and match the changelog
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-test-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithNonExistentFileAndOtherPrs_EmitsWarning()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create a changelog file for a specific PR
		var changelog = """
			title: Test PR
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/123
			""";
		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-test-pr.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Provide a non-existent file path along with a valid PR - should emit warning for file but continue with PR
		var nonexistentFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt");
		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = [nonexistentFile, "https://github.com/elastic/elasticsearch/pull/123"],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		// Should succeed because we have a valid PR, but should emit warning for the non-existent file
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		// Check that we have a warning about the file not existing
		var fileWarning = _collector.Diagnostics.FirstOrDefault(d => d.Message.Contains("File does not exist, skipping"));
		fileWarning.Should().NotBeNull("Expected a warning about the non-existent file being skipped");

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("name: 1755268130-test-pr.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_OverridesChangelogProducts()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with different products
		var changelog1 = """
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/kibana/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-feature.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-kibana-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			OutputProducts = [
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "beta" }
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		// Output products should override changelog products
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("target: 2025-12-02");
		bundleContent.Should().Contain("target: 2025-12-06");
		// Lifecycle values should be included in products array
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
		// Should not contain products from changelogs
		bundleContent.Should().NotContain("product: elasticsearch");
		bundleContent.Should().NotContain("product: kibana");
		// But should still contain the entries
		bundleContent.Should().Contain("name: 1755268130-elasticsearch-feature.yaml");
		bundleContent.Should().Contain("name: 1755268140-kibana-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleProducts_IncludesAllProducts()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with different products
		var changelog1 = """
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Kibana feature
			type: feature
			products:
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/kibana/pull/200
			""";
		var changelog3 = """
			title: Multi-product feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-kibana.yaml");
		var file3 = fileSystem.Path.Combine(changelogDir, "1755268150-multi-product.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("product: kibana");
		bundleContent.Should().Contain("target: 9.2.0");
		// Should have 3 entries
		var entryCount = bundleContent.Split("file:", StringSplitOptions.RemoveEmptyEntries).Length - 1;
		entryCount.Should().Be(3);
	}

	[Fact]
	public async Task BundleChangelogs_WithInputProducts_IncludesLifecycleInProductsArray()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Elasticsearch GA feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Elasticsearch Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: beta
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-elasticsearch-beta.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductInfo { Product = "elasticsearch", Target = "9.3.0", Lifecycle = "beta" }
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		// Verify lifecycle is included in products array (extracted from changelog entries, not filter)
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("target: 9.3.0");
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
	}

	[Fact]
	public async Task BundleChangelogs_WithOutputProducts_IncludesLifecycleInProductsArray()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files
		var changelog1 = """
			title: Elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			OutputProducts = [
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02", Lifecycle = "ga" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06", Lifecycle = "beta" }
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		// Verify lifecycle is included in products array from --output-products
		bundleContent.Should().Contain("product: cloud-serverless");
		bundleContent.Should().Contain("target: 2025-12-02");
		bundleContent.Should().Contain("target: 2025-12-06");
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
	}

	[Fact]
	public async Task BundleChangelogs_ExtractsLifecycleFromChangelogEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with lifecycle
		var changelog1 = """
			title: Elasticsearch GA feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Elasticsearch Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.3.0
			    lifecycle: beta
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-elasticsearch-beta.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		// Verify lifecycle is included in products array extracted from changelog entries
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("target: 9.3.0");
		bundleContent.Should().Contain("lifecycle: ga");
		bundleContent.Should().Contain("lifecycle: beta");
	}

	[Fact]
	public async Task BundleChangelogs_WithInputProductsWildcardLifecycle_ExtractsActualLifecycleFromChangelogs()
	{
		// Arrange - Test the scenario where --input-products uses "*" for lifecycle,
		// but the actual lifecycle value should be extracted from the changelog entries
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with lifecycle
		var changelog1 = """
			title: A new feature was added
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			InputProducts = [
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "*" }
			],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		// Verify that the actual lifecycle value "ga" from the changelog is included in products array,
		// not the wildcard "*" from the filter
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("lifecycle: ga");
		// Verify wildcard "*" is not included in the products array
		bundleContent.Should().NotContain("lifecycle: *");
		bundleContent.Should().NotContain("lifecycle: '*\"");
	}

	[Fact]
	public async Task BundleChangelogs_WithMultipleTargets_WarningIncludesLifecycle()
	{
		// Arrange - Test that warning message includes lifecycle when multiple products
		// have the same target but different lifecycles
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with same target but different lifecycles
		var changelog1 = """
			title: Elasticsearch GA feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: ga
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Elasticsearch Beta feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			    lifecycle: beta
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";
		var changelog3 = """
			title: Elasticsearch feature without lifecycle
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/300
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-elasticsearch-ga.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir, "1755268140-elasticsearch-beta.yaml");
		var file3 = fileSystem.Path.Combine(changelogDir, "1755268150-elasticsearch-no-lifecycle.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file3, changelog3, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		// Verify warning message includes lifecycle values
		_collector.Diagnostics.Should().Contain(d =>
			d.Message.Contains("Product 'elasticsearch' has multiple targets in bundle") &&
			d.Message.Contains("9.2.0") &&
			d.Message.Contains("9.2.0 beta") &&
			d.Message.Contains("9.2.0 ga"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolve_CopiesChangelogContents()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			areas:
			  - Search
			description: This is a test feature
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Resolve = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var bundleContent = await fileSystem.File.ReadAllTextAsync(input.Output!, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("file:");
		bundleContent.Should().Contain("name: 1755268130-test-feature.yaml");
		bundleContent.Should().Contain("checksum:");
		bundleContent.Should().Contain("type: feature");
		bundleContent.Should().Contain("title: Test feature");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("target: 9.2.0");
		bundleContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/100");
		bundleContent.Should().Contain("areas:");
		bundleContent.Should().Contain("- Search");
		bundleContent.Should().Contain("description: This is a test feature");
	}

	[Fact]
	public async Task BundleChangelogs_WithDirectoryOutputPath_CreatesDefaultFilename()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		// Use a directory path with default filename (simulating command layer processing)
		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var outputPath = fileSystem.Path.Combine(outputDir, "changelog-bundle.yaml");

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Output = outputPath
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		fileSystem.File.Exists(outputPath).Should().BeTrue("Output file should be created");

		var bundleContent = await fileSystem.File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
		bundleContent.Should().Contain("products:");
		bundleContent.Should().Contain("product: elasticsearch");
		bundleContent.Should().Contain("entries:");
		bundleContent.Should().Contain("name: 1755268130-test-feature.yaml");
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndMissingTitle_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without title
		var changelog1 = """
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Resolve = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: title"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndMissingType_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without type
		var changelog1 = """
			title: Test feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Resolve = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: type"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndMissingProducts_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without products
		var changelog1 = """
			title: Test feature
			type: feature
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Resolve = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field: products"));
	}

	[Fact]
	public async Task BundleChangelogs_WithResolveAndInvalidProduct_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file with invalid product (missing product field)
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - target: 9.2.0
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			All = true,
			Resolve = true,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("product entry missing required field: product"));
	}

	[Fact]
	public async Task RenderChangelogs_WithValidBundle_CreatesMarkdownFiles()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This is a test feature
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0"
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("## 9.2.0");
		indexContent.Should().Contain("Test feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithMultipleBundles_MergesAndRenders()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir1 = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var changelogDir2 = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir1);
		fileSystem.Directory.CreateDirectory(changelogDir2);

		// Create test changelog files
		var changelog1 = """
			title: First feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Second feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
			""";

		var file1 = fileSystem.Path.Combine(changelogDir1, "1755268130-first.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir2, "1755268140-second.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle files
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundle1 = fileSystem.Path.Combine(bundleDir, "bundle1.yaml");
		var bundleContent1 = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-first.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current.CancellationToken);

		var bundle2 = fileSystem.Path.Combine(bundleDir, "bundle2.yaml");
		var bundleContent2 = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268140-second.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [
				new BundleInput { BundleFile = bundle1, Directory = changelogDir1 },
				new BundleInput { BundleFile = bundle2, Directory = changelogDir2 }
			],
			Output = outputDir,
			Title = "9.2.0"
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("First feature");
		indexContent.Should().Contain("Second feature");
	}

	[Fact]
	public async Task CreateChangelog_WithBlockingLabel_SkipsChangelogCreation()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with blocking label",
			Labels = ["type:feature", "skip:releaseNotes"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			add_blockers:
			  elasticsearch:
			    - "skip:releaseNotes"
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains("skip:releaseNotes"));

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}

	[Fact]
	public async Task CreateChangelog_WithBlockingLabelForSpecificProduct_OnlyBlocksForThatProduct()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with blocking label",
			Labels = ["type:feature", "ILM"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			add_blockers:
			  cloud-serverless:
			    - "ILM"
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234"],
			Products = [
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-08-05" }
			],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog due to cloud-serverless blocker
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains("ILM"));

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created because cloud-serverless blocks it
	}

	[Fact]
	public async Task RenderChangelogs_WithMissingBundleFile_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var missingBundle = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.yaml");

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = missingBundle }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Bundle file does not exist"));
	}

	[Fact]
	public async Task RenderChangelogs_WithMissingChangelogFile_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: nonexistent.yaml
			      checksum: abc123
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = bundleDir }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("does not exist"));
	}

	[Fact]
	public async Task RenderChangelogs_WithInvalidBundleStructure_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = """
			invalid_field: value
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field") || d.Message.Contains("Failed to deserialize"));
	}

	[Fact]
	public async Task RenderChangelogs_WithDuplicateFileName_EmitsWarning()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir1 = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var changelogDir2 = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir1);
		fileSystem.Directory.CreateDirectory(changelogDir2);

		// Create same changelog file in both directories
		var changelog = """
			title: Duplicate feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var fileName = "1755268130-duplicate.yaml";
		var file1 = fileSystem.Path.Combine(changelogDir1, fileName);
		var file2 = fileSystem.Path.Combine(changelogDir2, fileName);
		await fileSystem.File.WriteAllTextAsync(file1, changelog, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog, TestContext.Current.CancellationToken);

		// Create bundle files
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundle1 = fileSystem.Path.Combine(bundleDir, "bundle1.yaml");
		var bundleContent1 = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: {fileName}
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current.CancellationToken);

		var bundle2 = fileSystem.Path.Combine(bundleDir, "bundle2.yaml");
		var bundleContent2 = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: {fileName}
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [
				new BundleInput { BundleFile = bundle1, Directory = changelogDir1 },
				new BundleInput { BundleFile = bundle2, Directory = changelogDir2 }
			],
			Output = outputDir
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("appears in multiple bundles"));
	}

	[Fact]
	public async Task RenderChangelogs_WithDuplicateFileNameInSameBundle_EmitsWarning()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog file
		var changelog = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var fileName = "1755268130-test-feature.yaml";
		var changelogFile = fileSystem.Path.Combine(changelogDir, fileName);
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create bundle file with the same file referenced twice
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: {fileName}
			      checksum: {ComputeSha1(changelog)}
			  - file:
			      name: {fileName}
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [
				new BundleInput { BundleFile = bundleFile, Directory = changelogDir }
			],
			Output = outputDir
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("appears multiple times in the same bundle") &&
			d.File == bundleFile);
	}

	[Fact]
	public async Task RenderChangelogs_WithDuplicatePr_EmitsWarning()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir1 = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var changelogDir2 = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir1);
		fileSystem.Directory.CreateDirectory(changelogDir2);

		// Create changelog files with same PR
		var changelog1 = """
			title: First feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		var changelog2 = """
			title: Second feature
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir1, "1755268130-first.yaml");
		var file2 = fileSystem.Path.Combine(changelogDir2, "1755268140-second.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(file2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle files
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundle1 = fileSystem.Path.Combine(bundleDir, "bundle1.yaml");
		var bundleContent1 = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-first.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundle1, bundleContent1, TestContext.Current.CancellationToken);

		var bundle2 = fileSystem.Path.Combine(bundleDir, "bundle2.yaml");
		var bundleContent2 = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268140-second.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundle2, bundleContent2, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [
				new BundleInput { BundleFile = bundle1, Directory = changelogDir1 },
				new BundleInput { BundleFile = bundle2, Directory = changelogDir2 }
			],
			Output = outputDir
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("appears in multiple bundles"));
	}

	[Fact]
	public async Task RenderChangelogs_WithInvalidChangelogFile_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create invalid changelog file (missing required fields)
		var invalidChangelog = """
			title: Invalid feature
			# Missing type and products
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-invalid.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, invalidChangelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-invalid.yaml
			      checksum: {ComputeSha1(invalidChangelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("missing required field"));
	}

	[Fact]
	public async Task RenderChangelogs_WithResolvedEntry_ValidatesAndRenders()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = """
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - type: feature
			    title: Resolved feature
			    products:
			      - product: elasticsearch
			        target: 9.2.0
			    pr: https://github.com/elastic/elasticsearch/pull/100
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile }],
			Output = outputDir,
			Title = "9.2.0"
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("Resolved feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithoutTitleAndNoTargets_EmitsWarning()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without target
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file without target
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir
			// Note: Title is not set
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided") &&
			d.Message.Contains("default to 'unknown'"));
	}

	[Fact]
	public async Task RenderChangelogs_WithTitleAndNoTargets_NoWarning()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file without target
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file without target
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0" // Title is provided
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		// Should not have warning about missing title
		_collector.Diagnostics.Should().NotContain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("No --title option provided"));
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with feature-id
		var changelog1 = """
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-api
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be hidden
			""";

		// Create changelog without feature-id (should not be hidden)
		var changelog2 = """
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This feature should be visible
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-hidden.yaml");
		var changelogFile2 = fileSystem.Path.Combine(changelogDir, "1755268140-visible.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-hidden.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-api"]
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("Hidden feature") &&
			d.Message.Contains("feature:hidden-api") &&
			d.Message.Contains("will be commented out"));

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Hidden entry should be commented out with % prefix
		indexContent.Should().Contain("% * Hidden feature");
		// Visible entry should not be commented
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_BreakingChange_UsesBlockComments()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var changelog = """
			title: Hidden breaking change
			type: breaking-change
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-breaking
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This breaking change should be hidden
			impact: Users will be affected
			action: Update your code
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-breaking.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-breaking.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-breaking"]
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var breakingFile = fileSystem.Path.Combine(outputDir, "9.2.0", "breaking-changes.md");
		fileSystem.File.Exists(breakingFile).Should().BeTrue();

		var breakingContent = await fileSystem.File.ReadAllTextAsync(breakingFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- -->
		breakingContent.Should().Contain("<!--");
		breakingContent.Should().Contain("-->");
		breakingContent.Should().Contain("Hidden breaking change");
		// Entry should be between comment markers
		var commentStart = breakingContent.IndexOf("<!--", StringComparison.Ordinal);
		var commentEnd = breakingContent.IndexOf("-->", StringComparison.Ordinal);
		commentStart.Should().BeLessThan(commentEnd);
		breakingContent.Substring(commentStart, commentEnd - commentStart).Should().Contain("Hidden breaking change");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_Deprecation_UsesBlockComments()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var changelog = """
			title: Hidden deprecation
			type: deprecation
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:hidden-deprecation
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This deprecation should be hidden
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-deprecation.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-deprecation.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:hidden-deprecation"]
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var deprecationsFile = fileSystem.Path.Combine(outputDir, "9.2.0", "deprecations.md");
		fileSystem.File.Exists(deprecationsFile).Should().BeTrue();

		var deprecationsContent = await fileSystem.File.ReadAllTextAsync(deprecationsFile, TestContext.Current.CancellationToken);
		// Should use block comments <!-- -->
		deprecationsContent.Should().Contain("<!--");
		deprecationsContent.Should().Contain("-->");
		deprecationsContent.Should().Contain("Hidden deprecation");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CommaSeparated_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var changelog1 = """
			title: First hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:first
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelog2 = """
			title: Second hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:second
			pr: https://github.com/elastic/elasticsearch/pull/101
			""";

		var changelog3 = """
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/102
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-first.yaml");
		var changelogFile2 = fileSystem.Path.Combine(changelogDir, "1755268140-second.yaml");
		var changelogFile3 = fileSystem.Path.Combine(changelogDir, "1755268150-visible.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-first.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-second.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-visible.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:first", "feature:second"]
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("% * First hidden feature");
		indexContent.Should().Contain("% * Second hidden feature");
		indexContent.Should().Contain("* Visible feature");
		indexContent.Should().NotContain("% * Visible feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_FromFile_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var changelog = """
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: feature:from-file
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-hidden.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-hidden.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Create feature IDs file
		var featureIdsFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "feature-ids.txt");
		fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(featureIdsFile)!);
		await fileSystem.File.WriteAllTextAsync(featureIdsFile, "feature:from-file\nfeature:another", TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = [featureIdsFile]
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		indexContent.Should().Contain("% * Hidden feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithHideFeatures_CaseInsensitive_MatchesFeatureIds()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var changelog = """
			title: Hidden feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			feature-id: Feature:UpperCase
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-hidden.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-hidden.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			HideFeatures = ["feature:uppercase"] // Different case
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Should match case-insensitively
		indexContent.Should().Contain("% * Hidden feature");
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked (elasticsearch + search area)
		var changelog1 = """
			title: Blocked feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked
			""";

		// Create changelog that should NOT be blocked (elasticsearch but different area)
		var changelog2 = """
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - observability
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This feature should be visible
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		var changelogFile2 = fileSystem.Path.Combine(changelogDir, "1755268140-visible.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create config file with render_blockers in docs/ subdirectory
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			_collector.Warnings.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("Blocked feature") &&
				d.Message.Contains("render_blockers") &&
				d.Message.Contains("product 'elasticsearch'") &&
				d.Message.Contains("area 'search'"));

			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Blocked entry should be commented out with % prefix
			indexContent.Should().Contain("% * Blocked feature");
			// Visible entry should not be commented
			indexContent.Should().Contain("* Visible feature");
			indexContent.Should().NotContain("% * Visible feature");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_CommaSeparatedProducts_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with cloud-serverless product that should be blocked
		var changelog1 = """
			title: Blocked cloud feature
			type: feature
			products:
			  - product: cloud-serverless
			    target: 2025-12-02
			areas:
			  - security
			pr: https://github.com/elastic/cloud-serverless/pull/100
			description: This feature should be blocked
			""";

		// Create changelog with elasticsearch product that should also be blocked
		var changelog2 = """
			title: Blocked elasticsearch feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - security
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This feature should also be blocked
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-cloud-blocked.yaml");
		var changelogFile2 = fileSystem.Path.Combine(changelogDir, "1755268140-es-blocked.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create config file with render_blockers using comma-separated products in docs/ subdirectory
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  "elasticsearch, cloud-serverless":
			    areas:
			      - security
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: cloud-serverless
			    target: 2025-12-02
			entries:
			  - file:
			      name: 1755268130-cloud-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-es-blocked.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			_collector.Warnings.Should().BeGreaterThan(0);

			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Both entries should be commented out
			indexContent.Should().Contain("% * Blocked cloud feature");
			indexContent.Should().Contain("% * Blocked elasticsearch feature");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_MultipleProductsInEntry_ChecksAllProducts()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with multiple products - one matches render_blockers
		var changelog = """
			title: Multi-product feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked because elasticsearch matches
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-multi-product.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog, TestContext.Current.CancellationToken);

		// Create config file with render_blockers for elasticsearch only in docs/ subdirectory
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-multi-product.yaml
			      checksum: {ComputeSha1(changelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			_collector.Warnings.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("Multi-product feature") &&
				d.Message.Contains("product 'elasticsearch'"));

			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Should be blocked because elasticsearch matches, even though kibana doesn't
			indexContent.Should().Contain("% * Multi-product feature");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_TypeBlocking_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked (elasticsearch + feature type, blocked by type)
		var changelog1 = """
			title: Blocked feature by type
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked by type
			""";

		// Create changelog that should NOT be blocked (elasticsearch but different type)
		var changelog2 = """
			title: Visible enhancement
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This enhancement should be visible
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		var changelogFile2 = fileSystem.Path.Combine(changelogDir, "1755268140-visible.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);

		// Create config file with render_blockers blocking docs type
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - enhancement
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    types:
			      - feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-visible.yaml
			      checksum: {ComputeSha1(changelog2)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			_collector.Warnings.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("Blocked feature by type") &&
				d.Message.Contains("render_blockers") &&
				d.Message.Contains("product 'elasticsearch'") &&
				d.Message.Contains("type 'feature'"));

			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Blocked entry should be commented out with % prefix
			indexContent.Should().Contain("% * Blocked feature by type");
			// Visible entry should not be commented
			indexContent.Should().Contain("* Visible enhancement");
			indexContent.Should().NotContain("% * Visible enhancement");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_AreasAndTypes_CommentsOutMatchingEntries()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked by area (elasticsearch + search area)
		var changelog1 = """
			title: Blocked by area
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This should be blocked by area
			""";

		// Create changelog that should be blocked by type (elasticsearch + enhancement type, blocked by type)
		var changelog2 = """
			title: Blocked by type
			type: enhancement
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/101
			description: This should be blocked by type
			""";

		// Create changelog that should NOT be blocked
		var changelog3 = """
			title: Visible feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - observability
			pr: https://github.com/elastic/elasticsearch/pull/102
			description: This should be visible
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-area-blocked.yaml");
		var changelogFile2 = fileSystem.Path.Combine(changelogDir, "1755268140-type-blocked.yaml");
		var changelogFile3 = fileSystem.Path.Combine(changelogDir, "1755268150-visible.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile2, changelog2, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(changelogFile3, changelog3, TestContext.Current.CancellationToken);

		// Create config file with render_blockers blocking both areas and types
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - enhancement
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			    types:
			      - enhancement
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-area-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			  - file:
			      name: 1755268140-type-blocked.yaml
			      checksum: {ComputeSha1(changelog2)}
			  - file:
			      name: 1755268150-visible.yaml
			      checksum: {ComputeSha1(changelog3)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			_collector.Warnings.Should().BeGreaterThan(0);

			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Both blocked entries should be commented out
			indexContent.Should().Contain("% * Blocked by area");
			indexContent.Should().Contain("% * Blocked by type");
			// Visible entry should not be commented
			indexContent.Should().Contain("* Visible feature");
			indexContent.Should().NotContain("% * Visible feature");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithRenderBlockers_UsesBundleProductsNotEntryProducts()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog with elasticsearch product and search area
		// But bundle has kibana product - should NOT be blocked because render_blockers matches against bundle products
		var changelog1 = """
			title: Entry with elasticsearch but bundle has kibana
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This should NOT be blocked because bundle product is kibana
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-test.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);

		// Create config file with render_blockers blocking elasticsearch
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file with kibana product (not elasticsearch)
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: kibana
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-test.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Set current directory to where config file is located so it can be found
		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			// Should have no warnings because entry is NOT blocked (bundle product is kibana, not elasticsearch)
			_collector.Warnings.Should().Be(0);

			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			// Entry should NOT be commented out because bundle product is kibana, not elasticsearch
			indexContent.Should().Contain("* Entry with elasticsearch but bundle has kibana");
			indexContent.Should().NotContain("% * Entry with elasticsearch but bundle has kibana");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithCustomConfigPath_UsesSpecifiedConfigFile()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create changelog that should be blocked (elasticsearch + search area)
		var changelog1 = """
			title: Blocked feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			areas:
			  - search
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This feature should be blocked
			""";

		var changelogFile1 = fileSystem.Path.Combine(changelogDir, "1755268130-blocked.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile1, changelog1, TestContext.Current.CancellationToken);

		// Create config file in a custom location (not in docs/ subdirectory)
		var customConfigDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(customConfigDir);
		var customConfigPath = fileSystem.Path.Combine(customConfigDir, "custom-changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    areas:
			      - search
			""";
		await fileSystem.File.WriteAllTextAsync(customConfigPath, configContent, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(bundleDir);

		var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-blocked.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		// Don't change directory - use custom config path via Config property
		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			Config = customConfigPath
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d =>
			d.Severity == Severity.Warning &&
			d.Message.Contains("Blocked feature") &&
			d.Message.Contains("render_blockers") &&
			d.Message.Contains("product 'elasticsearch'") &&
			d.Message.Contains("area 'search'"));

		var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
		fileSystem.File.Exists(indexFile).Should().BeTrue();

		var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
		// Blocked entry should be commented out with % prefix
		indexContent.Should().Contain("% * Blocked feature");
	}

	[Fact]
	public async Task CreateChangelog_WithCommaSeparatedProductIdsInAddBlockers_ExpandsCorrectly()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "PR with blocking label",
			Labels = ["type:feature", ">non-issue"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>._,
			A<string?>._,
			A<string?>._,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			add_blockers:
			  elasticsearch, cloud-serverless:
			    - ">non-issue"
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/1234"],
			Products = [
				new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-08-05" }
			],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue(); // Should succeed but skip creating changelog due to blocker
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains(">non-issue"));

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}

	[Fact]
	public async Task CreateChangelog_WithPrsFromFile_ProcessesAllPrsFromFile()
	{
		// Arrange - Simulate what ChangelogCommand does: read PRs from a file
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var pr1Info = new GitHubPrInfo
		{
			Title = "First PR from file",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "Second PR from file",
			Labels = ["type:bug"]
		};
		var pr3Info = new GitHubPrInfo
		{
			Title = "Third PR from file",
			Labels = ["type:enhancement"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("1111"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("2222"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr2Info);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("3333"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr3Info);

		var fileSystem = new FileSystem();
		var tempDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(tempDir);

		// Create a file with newline-delimited PRs (simulating what ChangelogCommand would read)
		var prsFile = fileSystem.Path.Combine(tempDir, "prs.txt");
		var prsFileContent = """
			https://github.com/elastic/elasticsearch/pull/1111
			https://github.com/elastic/elasticsearch/pull/2222
			https://github.com/elastic/elasticsearch/pull/3333
			""";
		await fileSystem.File.WriteAllTextAsync(prsFile, prsFileContent, TestContext.Current.CancellationToken);

		// Read PRs from file (simulating ChangelogCommand behavior)
		var prsFromFile = await fileSystem.File.ReadAllLinesAsync(prsFile, TestContext.Current.CancellationToken);
		var parsedPrs = prsFromFile
			.Where(line => !string.IsNullOrWhiteSpace(line))
			.Select(line => line.Trim())
			.ToArray();

		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			  - enhancement
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			  "type:bug": bug-fix
			  "type:enhancement": enhancement
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = parsedPrs, // PRs read from file
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(3); // One file per PR

		var yamlContents = new List<string>();
		foreach (var file in files)
		{
			yamlContents.Add(await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken));
		}

		// Verify all PRs were processed
		yamlContents.Should().Contain(c => c.Contains("title: First PR from file"));
		yamlContents.Should().Contain(c => c.Contains("title: Second PR from file"));
		yamlContents.Should().Contain(c => c.Contains("title: Third PR from file"));
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableTypes_UsesDefaults()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_types - should use defaults
		var configContent = """
			available_subtypes: []
			available_lifecycles:
			  - ga
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			_collector.Errors.Should().Be(0);
			// Should have default types
			config!.AvailableTypes.Should().Contain("feature");
			config.AvailableTypes.Should().Contain("bug-fix");
			config.AvailableTypes.Should().Contain("docs");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableSubtypes_UsesDefaults()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_subtypes - should use defaults
		var configContent = """
			available_types:
			  - feature
			available_lifecycles:
			  - ga
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			_collector.Errors.Should().Be(0);
			// Should have default subtypes
			config!.AvailableSubtypes.Should().Contain("api");
			config.AvailableSubtypes.Should().Contain("behavioral");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithoutAvailableLifecycles_UsesDefaults()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config without available_lifecycles - should use defaults
		var configContent = """
			available_types:
			  - feature
			available_subtypes: []
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().NotBeNull();
			_collector.Errors.Should().Be(0);
			// Should have default lifecycles
			config!.AvailableLifecycles.Should().Contain("preview");
			config.AvailableLifecycles.Should().Contain("beta");
			config.AvailableLifecycles.Should().Contain("ga");
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task LoadChangelogConfiguration_WithInvalidRenderBlockersType_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		var docsDir = fileSystem.Path.Combine(configDir, "docs");
		fileSystem.Directory.CreateDirectory(docsDir);
		var configPath = fileSystem.Path.Combine(docsDir, "changelog.yml");
		// Config with invalid type in render_blockers
		var configContent = """
			available_types:
			  - feature
			  - docs
			available_subtypes: []
			available_lifecycles:
			  - ga
			render_blockers:
			  elasticsearch:
			    types:
			      - invalid-type
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var originalDir = Directory.GetCurrentDirectory();
		try
		{
			Directory.SetCurrentDirectory(configDir);

			// Act
			var config = await service.LoadChangelogConfiguration(_collector, null, TestContext.Current.CancellationToken);

			// Assert
			config.Should().BeNull();
			_collector.Errors.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Error &&
				d.Message.Contains("Type 'invalid-type' in render_blockers") &&
				d.Message.Contains("is not in the list of available types"));
		}
		finally
		{
			Directory.SetCurrentDirectory(originalDir);
		}
	}

	[Fact]
	public async Task RenderChangelogs_WithUnhandledType_EmitsWarning()
	{
		// Arrange
		// This test simulates the scenario where a new type is added to ChangelogConfiguration.cs
		// but the rendering code hasn't been updated to handle it yet.
		// We use reflection to temporarily add "experimental-feature" to the defaults for testing.
		var defaultConfig = ChangelogConfiguration.Default;
		var originalTypes = defaultConfig.AvailableTypes.ToList();
		var testType = "experimental-feature";

		// Temporarily add the test type to defaults to simulate it being added to ChangelogConfiguration.cs
		defaultConfig.AvailableTypes.Add(testType);

		try
		{
			var service = new ChangelogService(_loggerFactory, _configurationContext, null);
			var fileSystem = new FileSystem();
			var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
			fileSystem.Directory.CreateDirectory(changelogDir);

			// Create changelog with an unhandled type
			var changelog1 = """
				title: Experimental feature
				type: experimental-feature
				products:
				  - product: elasticsearch
				    target: 9.2.0
				description: This is an experimental feature
				""";

			var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-experimental.yaml");
			await fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

			// Create bundle file
			var bundleDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
			fileSystem.Directory.CreateDirectory(bundleDir);
			var bundleFile = fileSystem.Path.Combine(bundleDir, "bundle.yaml");
			var bundleContent = $"""
				products:
				  - product: elasticsearch
				    target: 9.2.0
				entries:
				  - file:
				      name: 1755268130-experimental.yaml
				      checksum: {ComputeSha1(changelog1)}
				""";
			await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

			var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

			var input = new ChangelogRenderInput
			{
				Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
				Output = outputDir,
				Title = "9.2.0"
			};

			// Act
			var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

			// Assert
			result.Should().BeTrue();
			_collector.Errors.Should().Be(0);
			_collector.Warnings.Should().BeGreaterThan(0);
			_collector.Diagnostics.Should().Contain(d =>
				d.Severity == Severity.Warning &&
				d.Message.Contains("experimental-feature") &&
				d.Message.Contains("is valid according to configuration but is not handled in rendering output") &&
				d.Message.Contains("1 entry/entries of this type will not be included"));

			// Verify that the entry is not included in the output
			var indexFile = fileSystem.Path.Combine(outputDir, "9.2.0", "index.md");
			fileSystem.File.Exists(indexFile).Should().BeTrue();

			var indexContent = await fileSystem.File.ReadAllTextAsync(indexFile, TestContext.Current.CancellationToken);
			indexContent.Should().NotContain("Experimental feature");
		}
		finally
		{
			// Restore original types
			defaultConfig.AvailableTypes.Clear();
			defaultConfig.AvailableTypes.AddRange(originalTypes);
		}
	}

	[Fact]
	public async Task CreateChangelog_WithMixedPrsFromFileAndCommaSeparated_ProcessesAllPrs()
	{
		// Arrange - Simulate ChangelogCommand handling both file paths and comma-separated PRs
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var pr1Info = new GitHubPrInfo
		{
			Title = "PR from comma-separated",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "PR from file",
			Labels = ["type:bug"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("1111"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			A<string>.That.Contains("2222"),
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr2Info);

		var fileSystem = new FileSystem();
		var tempDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(tempDir);

		// Create a file with PRs
		var prsFile = fileSystem.Path.Combine(tempDir, "prs.txt");
		var prsFileContent = """
			https://github.com/elastic/elasticsearch/pull/2222
			""";
		await fileSystem.File.WriteAllTextAsync(prsFile, prsFileContent, TestContext.Current.CancellationToken);

		// Simulate ChangelogCommand processing: comma-separated PRs + file path
		var allPrs = new List<string>();

		// Add comma-separated PRs
		var commaSeparatedPrs = "https://github.com/elastic/elasticsearch/pull/1111".Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		allPrs.AddRange(commaSeparatedPrs);

		// Add PRs from file
		var prsFromFile = await fileSystem.File.ReadAllLinesAsync(prsFile, TestContext.Current.CancellationToken);
		allPrs.AddRange(
			prsFromFile
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.Select(line => line.Trim())
		);

		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			  "type:bug": bug-fix
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = allPrs.ToArray(), // Mixed PRs from comma-separated and file
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString())
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(2); // One file per PR

		var yamlContents = new List<string>();
		foreach (var file in files)
		{
			var content = await File.ReadAllTextAsync(file, TestContext.Current.CancellationToken);
			yamlContents.Add(content);
		}

		// Verify both PRs were processed
		yamlContents.Should().Contain(c => c.Contains("title: PR from comma-separated"));
		yamlContents.Should().Contain(c => c.Contains("title: PR from file"));
		yamlContents.Should().Contain(c => c.Contains("pr: https://github.com/elastic/elasticsearch/pull/1111"));
		yamlContents.Should().Contain(c => c.Contains("pr: https://github.com/elastic/elasticsearch/pull/2222"));
	}

	[Fact]
	public async Task RenderChangelogs_WithAsciidocFileType_CreatesSingleAsciidocFile()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog file
		var changelog1 = """
			title: Test feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: This is a test feature
			""";

		var changelogFile = fileSystem.Path.Combine(changelogDir, "1755268130-test-feature.yaml");
		await fileSystem.File.WriteAllTextAsync(changelogFile, changelog1, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-test-feature.yaml
			      checksum: {ComputeSha1(changelog1)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = "asciidoc"
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		// Verify a single .asciidoc file is created (not multiple files like markdown)
		var asciidocFiles = fileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1, "asciidoc render should create a single file");

		var asciidocFile = asciidocFiles[0];
		var asciidocContent = await fileSystem.File.ReadAllTextAsync(asciidocFile, TestContext.Current.CancellationToken);

		// Verify valid asciidoc format elements
		asciidocContent.Should().Contain("[[release-notes-", "should contain anchor");
		asciidocContent.Should().Contain("== 9.2.0", "should contain section header");
		asciidocContent.Should().Contain("[[features-enhancements-", "should contain features section anchor");
		asciidocContent.Should().Contain("=== New features and enhancements", "should contain features section header");
		asciidocContent.Should().Contain("* Test feature", "should contain changelog entry");
		asciidocContent.Should().Contain("This is a test feature", "should contain description");

		// Verify no markdown files are created
		var markdownFiles = fileSystem.Directory.GetFiles(outputDir, "*.md", SearchOption.AllDirectories);
		markdownFiles.Should().BeEmpty("asciidoc render should not create markdown files");
	}

	[Fact]
	public async Task RenderChangelogs_WithAsciidocFileType_ValidatesAsciidocFormat()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		// Create test changelog files with different types
		var featureChangelog = """
			title: New search feature
			type: feature
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/100
			description: Added new search capabilities
			""";

		var bugFixChangelog = """
			title: Fixed search bug
			type: bug-fix
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/200
			description: Fixed a critical search issue
			""";

		var breakingChangeChangelog = """
			title: Breaking API change
			type: breaking-change
			subtype: api
			products:
			  - product: elasticsearch
			    target: 9.2.0
			pr: https://github.com/elastic/elasticsearch/pull/300
			description: Changed API endpoint structure
			impact: Users need to update their API calls
			action: Update API client libraries
			""";

		var featureFile = fileSystem.Path.Combine(changelogDir, "1755268130-feature.yaml");
		var bugFixFile = fileSystem.Path.Combine(changelogDir, "1755268140-bugfix.yaml");
		var breakingFile = fileSystem.Path.Combine(changelogDir, "1755268150-breaking.yaml");
		await fileSystem.File.WriteAllTextAsync(featureFile, featureChangelog, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(bugFixFile, bugFixChangelog, TestContext.Current.CancellationToken);
		await fileSystem.File.WriteAllTextAsync(breakingFile, breakingChangeChangelog, TestContext.Current.CancellationToken);

		// Create bundle file
		var bundleFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml");
		fileSystem.Directory.CreateDirectory(fileSystem.Path.GetDirectoryName(bundleFile)!);

		var bundleContent = $"""
			products:
			  - product: elasticsearch
			    target: 9.2.0
			entries:
			  - file:
			      name: 1755268130-feature.yaml
			      checksum: {ComputeSha1(featureChangelog)}
			  - file:
			      name: 1755268140-bugfix.yaml
			      checksum: {ComputeSha1(bugFixChangelog)}
			  - file:
			      name: 1755268150-breaking.yaml
			      checksum: {ComputeSha1(breakingChangeChangelog)}
			""";
		await fileSystem.File.WriteAllTextAsync(bundleFile, bundleContent, TestContext.Current.CancellationToken);

		var outputDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());

		var input = new ChangelogRenderInput
		{
			Bundles = [new BundleInput { BundleFile = bundleFile, Directory = changelogDir }],
			Output = outputDir,
			Title = "9.2.0",
			FileType = "asciidoc"
		};

		// Act
		var result = await service.RenderChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var asciidocFiles = fileSystem.Directory.GetFiles(outputDir, "*.asciidoc", SearchOption.AllDirectories);
		asciidocFiles.Should().HaveCount(1);

		var asciidocContent = await fileSystem.File.ReadAllTextAsync(asciidocFiles[0], TestContext.Current.CancellationToken);

		// Verify asciidoc structure
		asciidocContent.Should().Contain("[[release-notes-9.2.0]]", "should contain main anchor");
		asciidocContent.Should().Contain("== 9.2.0", "should contain main header");

		// Verify sections are present with proper asciidoc format
		asciidocContent.Should().Contain("[[bug-fixes-9.2.0]]", "should contain bug fixes anchor");
		asciidocContent.Should().Contain("[float]", "should contain float attribute");
		asciidocContent.Should().Contain("=== Bug fixes", "should contain bug fixes header");

		asciidocContent.Should().Contain("[[features-enhancements-9.2.0]]", "should contain features anchor");
		asciidocContent.Should().Contain("=== New features and enhancements", "should contain features header");

		asciidocContent.Should().Contain("[[breaking-changes-9.2.0]]", "should contain breaking changes anchor");
		asciidocContent.Should().Contain("=== Breaking changes", "should contain breaking changes header");

		// Verify entries are formatted correctly
		asciidocContent.Should().Contain("* New search feature", "should contain feature entry");
		asciidocContent.Should().Contain("* Fixed search bug", "should contain bug fix entry");
		asciidocContent.Should().Contain("* Breaking API change", "should contain breaking change entry");

		// Verify asciidoc list format (entries should start with *)
		var lines = asciidocContent.Split('\n');
		var entryLines = lines.Where(l => l.TrimStart().StartsWith("* ", StringComparison.Ordinal) && !l.TrimStart().StartsWith("* *", StringComparison.Ordinal)).ToList();
		entryLines.Should().HaveCountGreaterThanOrEqualTo(3, "should have at least 3 changelog entries");

		// Verify no invalid markdown syntax (like ##) is present
		asciidocContent.Should().NotContain("##", "should not contain markdown headers");
		asciidocContent.Should().NotContain("###", "should not contain markdown headers");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_ShortReleaseNote_UsesAsTitle()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = "## Summary\n\nThis PR adds a new feature.\n\nRelease Notes: Adds support for new aggregation types",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			ExtractReleaseNotes = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Adds support for new aggregation types");
		// Description should not be set when release note is used as title
		if (yamlContent.Contains("description:"))
		{
			// If description field exists, it should be empty or commented out
			var descriptionLine = yamlContent.Split('\n').FirstOrDefault(l => l.Contains("description:"));
			descriptionLine.Should().MatchRegex(@"description:\s*(#|$)");
		}
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_LongReleaseNote_UsesAsDescription()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var longReleaseNote = "Adds support for new aggregation types including date histogram, range aggregations, and nested aggregations with improved performance";
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = $"## Summary\n\nThis PR adds a new feature.\n\nRelease Notes: {longReleaseNote}",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			ExtractReleaseNotes = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		yamlContent.Should().Contain($"description: {longReleaseNote}");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_MultiLineReleaseNote_UsesAsDescription()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		// The regex stops at double newline, so we need a release note that spans multiple lines without double newline
		var multiLineReleaseNote = "Adds support for new aggregation types\nThis includes date histogram and range aggregations\nwith improved performance";
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = $"## Summary\n\nThis PR adds a new feature.\n\nRelease Notes: {multiLineReleaseNote}",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			ExtractReleaseNotes = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		yamlContent.Should().Contain("description:");
		yamlContent.Should().Contain("Adds support for new aggregation types");
		yamlContent.Should().Contain("date histogram");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_NoReleaseNote_UsesPrTitle()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = "## Summary\n\nThis PR adds a new feature but has no release notes section.",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			ExtractReleaseNotes = true
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Implement new aggregation API");
		// Description should not be set when no release note is found
		if (yamlContent.Contains("description:"))
		{
			// If description field exists, it should be empty or commented out
			var descriptionLine = yamlContent.Split('\n').FirstOrDefault(l => l.Contains("description:"));
			descriptionLine.Should().MatchRegex(@"description:\s*(#|$)");
		}
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_ExplicitTitle_TakesPrecedence()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = "Release Notes: Adds support for new aggregation types",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			ExtractReleaseNotes = true,
			Title = "Custom title"
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: Custom title");
		yamlContent.Should().NotContain("Adds support for new aggregation types");
	}

	[Fact]
	public async Task CreateChangelog_WithExtractReleaseNotes_ExplicitDescription_TakesPrecedence()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var longReleaseNote = "Adds support for new aggregation types including date histogram, range aggregations, and nested aggregations with improved performance";
		var prInfo = new GitHubPrInfo
		{
			Title = "Implement new aggregation API",
			Body = $"Release Notes: {longReleaseNote}",
			Labels = ["type:feature"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"https://github.com/elastic/elasticsearch/pull/12345",
			null,
			null,
			A<CancellationToken>._))
			.Returns(prInfo);

		var fileSystem = new FileSystem();
		var configDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(configDir);
		var configPath = fileSystem.Path.Combine(configDir, "changelog.yml");
		var configContent = """
			available_types:
			  - feature
			  - bug-fix
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			""";
		await fileSystem.File.WriteAllTextAsync(configPath, configContent, TestContext.Current.CancellationToken);

		var service = new ChangelogService(_loggerFactory, _configurationContext, mockGitHubService);

		var input = new ChangelogInput
		{
			Prs = ["https://github.com/elastic/elasticsearch/pull/12345"],
			Products = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0", Lifecycle = "ga" }],
			Config = configPath,
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString()),
			ExtractReleaseNotes = true,
			Description = "Custom description"
		};

		// Act
		var result = await service.CreateChangelog(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeTrue();
		_collector.Errors.Should().Be(0);

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1);

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("description: Custom description");
		yamlContent.Should().NotContain(longReleaseNote);
	}

	[SuppressMessage("Security", "CA5350:Do not use insecure cryptographic algorithm SHA1", Justification = "SHA1 is required for compatibility with existing changelog bundle format")]
	private static string ComputeSha1(string content)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(content);
		var hash = System.Security.Cryptography.SHA1.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}

