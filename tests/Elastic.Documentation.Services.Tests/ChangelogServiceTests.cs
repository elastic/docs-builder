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
			Pr = "https://github.com/elastic/elasticsearch/pull/12345",
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
			Pr = "https://github.com/elastic/elasticsearch/pull/12345",
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
			Pr = "https://github.com/elastic/elasticsearch/pull/12345",
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
			Pr = "12345",
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
			Pr = "https://github.com/elastic/elasticsearch/pull/12345",
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
			Pr = "https://github.com/elastic/elasticsearch/pull/12345",
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
			Pr = "https://github.com/elastic/elasticsearch/pull/12345",
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
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
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
			Prs = new[] { prsFile },
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
			pr: https://github.com/elastic/elasticsearch/pull/100
			""";

		var file1 = fileSystem.Path.Combine(changelogDir, "1755268130-short-format.yaml");
		await fileSystem.File.WriteAllTextAsync(file1, changelog1, TestContext.Current.CancellationToken);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			Prs = ["elastic/elasticsearch#100"],
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
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
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
			InputProducts = [new ProductInfo { Product = "elasticsearch", Target = "9.2.0" }],
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("Only one filter option can be specified"));
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
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06" }
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
			Prs = new[] { nonexistentFile },
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
			Prs = new[] { "https://github.com/elastic/elasticsearch/pull/123" },
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
			Prs = new[] { nonexistentFile, "https://github.com/elastic/elasticsearch/pull/123" },
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
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-02" },
				new ProductInfo { Product = "cloud-serverless", Target = "2025-12-06" }
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

	private static string ComputeSha1(string content)
	{
		var bytes = System.Text.Encoding.UTF8.GetBytes(content);
		var hash = System.Security.Cryptography.SHA1.HashData(bytes);
		return System.Convert.ToHexString(hash).ToLowerInvariant();
	}
}

