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
			PrsFile = prsFile,
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
	public async Task BundleChangelogs_WithInvalidPrsFile_ReturnsError()
	{
		// Arrange
		var service = new ChangelogService(_loggerFactory, _configurationContext, null);
		var fileSystem = new FileSystem();
		var changelogDir = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString());
		fileSystem.Directory.CreateDirectory(changelogDir);

		var input = new ChangelogBundleInput
		{
			Directory = changelogDir,
			PrsFile = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.txt"),
			Output = fileSystem.Path.Combine(fileSystem.Path.GetTempPath(), Guid.NewGuid().ToString(), "bundle.yaml")
		};

		// Act
		var result = await service.BundleChangelogs(_collector, input, TestContext.Current.CancellationToken);

		// Assert
		result.Should().BeFalse();
		_collector.Errors.Should().BeGreaterThan(0);
		_collector.Diagnostics.Should().Contain(d => d.Message.Contains("PRs file does not exist"));
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
}

