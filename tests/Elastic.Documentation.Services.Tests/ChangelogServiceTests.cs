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

		var productsConfiguration = new ProductsConfiguration
		{
			Products = new Dictionary<string, Product>
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
				}
			}.ToFrozenDictionary()
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
	public async Task CreateChangelog_WithPrOptionButPrFetchFails_ReturnsError()
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
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Failed to fetch PR information"));
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
	public async Task CreateChangelog_WithInvalidProductInProductLabelBlockers_ReturnsError()
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
			product_label_blockers:
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
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Product 'invalid-product' in product_label_blockers") && d.Message.Contains("is not in the list of available products"));
	}

	[Fact]
	public async Task CreateChangelog_WithValidProductInProductLabelBlockers_Succeeds()
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
			product_label_blockers:
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
		yamlContent.Should().Contain("feature_id: feature:new-search-api");
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
			"1234",
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"5678",
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

		var yamlContent1 = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		var yamlContent2 = await File.ReadAllTextAsync(files[1], TestContext.Current.CancellationToken);

		// One file should contain first PR title, the other should contain second PR title
		var contents = new[] { yamlContent1, yamlContent2 };
		contents.Should().Contain(c => c.Contains("title: First PR feature"));
		contents.Should().Contain(c => c.Contains("title: Second PR bug fix"));
		contents.Should().Contain(c => c.Contains("pr: https://github.com/elastic/elasticsearch/pull/1234"));
		contents.Should().Contain(c => c.Contains("pr: https://github.com/elastic/elasticsearch/pull/5678"));
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
			product_label_blockers:
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
			product_label_blockers:
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
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains("ILM") && d.Message.Contains("cloud-serverless"));

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(0); // No files should be created
	}

	[Fact]
	public async Task CreateChangelog_WithMultiplePrsAndSomeBlocked_CreatesFilesForNonBlockedPrs()
	{
		// Arrange
		var mockGitHubService = A.Fake<IGitHubPrService>();
		var pr1Info = new GitHubPrInfo
		{
			Title = "First PR without blocker",
			Labels = ["type:feature"]
		};
		var pr2Info = new GitHubPrInfo
		{
			Title = "Second PR with blocker",
			Labels = ["type:feature", "skip:releaseNotes"]
		};

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"1234",
			null,
			null,
			A<CancellationToken>._))
			.Returns(pr1Info);

		A.CallTo(() => mockGitHubService.FetchPrInfoAsync(
			"5678",
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
			available_subtypes: []
			available_lifecycles:
			  - preview
			  - beta
			  - ga
			label_to_type:
			  "type:feature": feature
			product_label_blockers:
			  elasticsearch:
			    - "skip:releaseNotes"
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
		_collector.Warnings.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Skipping changelog creation") && d.Message.Contains("5678"));

		var outputDir = input.Output ?? Directory.GetCurrentDirectory();
		if (!Directory.Exists(outputDir))
			Directory.CreateDirectory(outputDir);
		var files = Directory.GetFiles(outputDir, "*.yaml");
		files.Should().HaveCount(1); // Only one file should be created (for PR 1234)

		var yamlContent = await File.ReadAllTextAsync(files[0], TestContext.Current.CancellationToken);
		yamlContent.Should().Contain("title: First PR without blocker");
		yamlContent.Should().Contain("pr: https://github.com/elastic/elasticsearch/pull/1234");
		yamlContent.Should().NotContain("Second PR with blocker");
	}
}

