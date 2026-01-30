// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Inference;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Configuration.Tests;

public class VersionInferenceTests
{
	/// <summary>
	/// All ProductApplicability product names that should be mapped.
	/// </summary>
	private static Dictionary<string, string> ProductApplicabilityOptions => new()
	{
		{ nameof(ProductApplicability.Ecctl), "cloud-control-ecctl" },
		{ nameof(ProductApplicability.Curator), "curator" },
		{ nameof(ProductApplicability.ApmAgentAndroid), "edot-android" },
		{ nameof(ProductApplicability.ApmAgentDotnet), "apm-agent-dotnet" },
		{ nameof(ProductApplicability.ApmAgentGo), "apm-agent-go" },
		{ nameof(ProductApplicability.ApmAgentIos), "edot-ios" },
		{ nameof(ProductApplicability.ApmAgentJava), "apm-agent-java" },
		{ nameof(ProductApplicability.ApmAgentNode), "apm-agent-node" },
		{ nameof(ProductApplicability.ApmAgentPhp), "apm-agent-php" },
		{ nameof(ProductApplicability.ApmAgentPython), "apm-agent-python" },
		{ nameof(ProductApplicability.ApmAgentRuby), "apm-agent-ruby" },
		{ nameof(ProductApplicability.ApmAgentRumJs), "apm-agent-rum-js" },
		{ nameof(ProductApplicability.EdotIos), "edot-ios" },
		{ nameof(ProductApplicability.EdotAndroid), "edot-android" },
		{ nameof(ProductApplicability.EdotDotnet), "edot-dotnet" },
		{ nameof(ProductApplicability.EdotJava), "edot-java" },
		{ nameof(ProductApplicability.EdotNode), "edot-node" },
		{ nameof(ProductApplicability.EdotPhp), "edot-php" },
		{ nameof(ProductApplicability.EdotPython), "edot-python" },
		{ nameof(ProductApplicability.EdotCfAws), "edot-cf-aws" },
		{ nameof(ProductApplicability.EdotCfAzure), "edot-cf-azure" },
		{ nameof(ProductApplicability.EdotCfGcp), "edot-cf-gcp" },
		{ nameof(ProductApplicability.EdotCollector), "edot-collector" }
	};

	public static TheoryData<string, string> ProductApplicabilityOptionsAsList => [.. ProductApplicabilityOptions.Select(kvp => (kvp.Key, kvp.Value))];

	private static VersionsConfiguration CreateVersionsConfiguration()
	{
		var versioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>();

		foreach (var id in Enum.GetValues<VersioningSystemId>())
		{
			versioningSystems[id] = new VersioningSystem
			{
				Id = id,
				Current = new SemVersion(1, 0, 0),
				Base = new SemVersion(1, 0, 0)
			};
		}

		return new VersionsConfiguration { VersioningSystems = versioningSystems };
	}

	/// <summary>
	/// Creates a ProductsConfiguration with all products needed for testing.
	/// </summary>
	private static ProductsConfiguration CreateProductsConfiguration(VersionsConfiguration versionsConfiguration)
	{
		var products = new Dictionary<string, Product>();

		var productIdToVersioningSystem = new Dictionary<string, VersioningSystemId>
		{
			{ "cloud-control-ecctl", VersioningSystemId.Ecctl },
			{ "curator", VersioningSystemId.Curator },
			{ "apm-agent-dotnet", VersioningSystemId.ApmAgentDotnet },
			{ "apm-agent-go", VersioningSystemId.ApmAgentGo },
			{ "apm-agent-java", VersioningSystemId.ApmAgentJava },
			{ "apm-agent-node", VersioningSystemId.ApmAgentNode },
			{ "apm-agent-php", VersioningSystemId.ApmAgentPhp },
			{ "apm-agent-python", VersioningSystemId.ApmAgentPython },
			{ "apm-agent-ruby", VersioningSystemId.ApmAgentRuby },
			{ "apm-agent-rum-js", VersioningSystemId.ApmAgentRumJs },
			{ "edot-ios", VersioningSystemId.EdotIos },
			{ "edot-android", VersioningSystemId.EdotAndroid },
			{ "edot-dotnet", VersioningSystemId.EdotDotnet },
			{ "edot-java", VersioningSystemId.EdotJava },
			{ "edot-node", VersioningSystemId.EdotNode },
			{ "edot-php", VersioningSystemId.EdotPhp },
			{ "edot-python", VersioningSystemId.EdotPython },
			{ "edot-cf-aws", VersioningSystemId.EdotCfAws },
			{ "edot-cf-azure", VersioningSystemId.EdotCfAzure },
			{ "edot-cf-gcp", VersioningSystemId.EdotCfGcp },
			{ "edot-collector", VersioningSystemId.EdotCollector },
		};

		foreach (var (productId, versioningSystemId) in productIdToVersioningSystem)
		{
			products[productId] = new Product
			{
				Id = productId,
				DisplayName = $"Test {productId}",
				VersioningSystem = versionsConfiguration.GetVersioningSystem(versioningSystemId)
			};
		}

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};
	}

	private static ProductApplicability CreateProductApplicabilityByName(string productName)
	{
		var applicability = new ProductApplicability();
		var value = AppliesCollection.GenerallyAvailable;

		switch (productName)
		{
			case nameof(ProductApplicability.Ecctl):
				applicability.Ecctl = value;
				break;
			case nameof(ProductApplicability.Curator):
				applicability.Curator = value;
				break;
			case nameof(ProductApplicability.ApmAgentAndroid):
				applicability.ApmAgentAndroid = value;
				break;
			case nameof(ProductApplicability.ApmAgentDotnet):
				applicability.ApmAgentDotnet = value;
				break;
			case nameof(ProductApplicability.ApmAgentGo):
				applicability.ApmAgentGo = value;
				break;
			case nameof(ProductApplicability.ApmAgentIos):
				applicability.ApmAgentIos = value;
				break;
			case nameof(ProductApplicability.ApmAgentJava):
				applicability.ApmAgentJava = value;
				break;
			case nameof(ProductApplicability.ApmAgentNode):
				applicability.ApmAgentNode = value;
				break;
			case nameof(ProductApplicability.ApmAgentPhp):
				applicability.ApmAgentPhp = value;
				break;
			case nameof(ProductApplicability.ApmAgentPython):
				applicability.ApmAgentPython = value;
				break;
			case nameof(ProductApplicability.ApmAgentRuby):
				applicability.ApmAgentRuby = value;
				break;
			case nameof(ProductApplicability.ApmAgentRumJs):
				applicability.ApmAgentRumJs = value;
				break;
			case nameof(ProductApplicability.EdotIos):
				applicability.EdotIos = value;
				break;
			case nameof(ProductApplicability.EdotAndroid):
				applicability.EdotAndroid = value;
				break;
			case nameof(ProductApplicability.EdotDotnet):
				applicability.EdotDotnet = value;
				break;
			case nameof(ProductApplicability.EdotJava):
				applicability.EdotJava = value;
				break;
			case nameof(ProductApplicability.EdotNode):
				applicability.EdotNode = value;
				break;
			case nameof(ProductApplicability.EdotPhp):
				applicability.EdotPhp = value;
				break;
			case nameof(ProductApplicability.EdotPython):
				applicability.EdotPython = value;
				break;
			case nameof(ProductApplicability.EdotCfAws):
				applicability.EdotCfAws = value;
				break;
			case nameof(ProductApplicability.EdotCfAzure):
				applicability.EdotCfAzure = value;
				break;
			case nameof(ProductApplicability.EdotCfGcp):
				applicability.EdotCfGcp = value;
				break;
			case nameof(ProductApplicability.EdotCollector):
				applicability.EdotCollector = value;
				break;
			default:
				throw new ArgumentException($"Unknown product: {productName}");
		}

		return applicability;
	}

	[Theory(DisplayName = "ProductApplicabilityToProductId returns correct product ID for product {0}"), MemberData(nameof(ProductApplicabilityOptionsAsList))]
	public void InferVersionReturnsCorrectVersioningForAllProductApplicabilityProperties(string productApplicabilityEntry, string targetProductId)
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var productApplicability = CreateProductApplicabilityByName(productApplicabilityEntry);
		var applicableTo = new ApplicableTo { ProductApplicability = productApplicability };

		var result = inferrer.InferVersion(
			repositoryName: "any-repo",
			legacyPages: null,
			products: null,
			applicableTo: applicableTo);

		result.Should().NotBeNull($"Product {productApplicabilityEntry} should return a valid VersioningSystem via InferVersion");

		var resultingProductId = ProductApplicabilityConversion.ProductApplicabilityToProductId(productApplicability);
		resultingProductId.Should().NotBeNull($"Product {productApplicabilityEntry} should return a valid product ID via ProductApplicabilityToProductId");

		if (productsConfiguration.Products.TryGetValue(resultingProductId, out var expectedProduct))
		{
			result.Id.Should().Be(expectedProduct.VersioningSystem!.Id,
				$"Product {productApplicabilityEntry} should return versioning system {expectedProduct.VersioningSystem.Id} via InferVersion");
		}

		resultingProductId.Should().Be(targetProductId, $"Product {productApplicabilityEntry} should return '{targetProductId}'");
	}

	[Fact]
	public void InferVersionPrioritizesLegacyPagesOverAppliesTo()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var legacyProduct = new Product
		{
			Id = "legacy-product",
			DisplayName = "Legacy Product",
			VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Ece)
		};

		var legacyPages = new[]
		{
			new LegacyUrlMappings.LegacyPageMapping(legacyProduct, "/test/url", "8.0", true)
		};

		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability { Curator = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferVersion(
			repositoryName: "any-repo",
			legacyPages: legacyPages,
			products: null,
			applicableTo: applicableTo);

		result.Id.Should().Be(VersioningSystemId.Ece);
	}

	[Fact]
	public void InferVersionPrioritizesProductApplicabilityOverStack()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability { Curator = AppliesCollection.GenerallyAvailable },
			Stack = AppliesCollection.GenerallyAvailable
		};

		var result = inferrer.InferVersion(
			repositoryName: "unknown-repo",
			legacyPages: null,
			products: null,
			applicableTo: applicableTo);

		result.Id.Should().Be(VersioningSystemId.Curator);
	}

	[Fact]
	public void InferVersionPrioritizesStackOverDeployment()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var applicableTo = new ApplicableTo
		{
			Stack = AppliesCollection.GenerallyAvailable,
			Deployment = new DeploymentApplicability { Ece = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferVersion(
			repositoryName: "unknown-repo",
			legacyPages: null,
			products: null,
			applicableTo: applicableTo);

		result.Id.Should().Be(VersioningSystemId.Stack);
	}

	[Fact]
	public void InferVersionPrioritizesDeploymentOverServerless()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var applicableTo = new ApplicableTo
		{
			Deployment = new DeploymentApplicability { Eck = AppliesCollection.GenerallyAvailable },
			Serverless = new ServerlessProjectApplicability { Elasticsearch = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferVersion(
			repositoryName: "unknown-repo",
			legacyPages: null,
			products: null,
			applicableTo: applicableTo);

		result.Id.Should().Be(VersioningSystemId.Eck);
	}

	[Fact]
	public void InferVersionReturnsServerlessVersioningWhenOnlyServerlessSet()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var testCases = new (ServerlessProjectApplicability serverless, VersioningSystemId expectedId)[]
		{
			(new ServerlessProjectApplicability { Elasticsearch = AppliesCollection.GenerallyAvailable }, VersioningSystemId.ElasticsearchProject),
			(new ServerlessProjectApplicability { Observability = AppliesCollection.GenerallyAvailable }, VersioningSystemId.ObservabilityProject),
			(new ServerlessProjectApplicability { Security = AppliesCollection.GenerallyAvailable }, VersioningSystemId.SecurityProject),
		};

		foreach (var (serverless, expectedId) in testCases)
		{
			var applicableTo = new ApplicableTo { Serverless = serverless };

			var result = inferrer.InferVersion(
				repositoryName: "unknown-repo",
				legacyPages: null,
				products: null,
				applicableTo: applicableTo);

			result.Id.Should().Be(expectedId);
		}
	}

	[Fact]
	public void InferVersionReturnsDeploymentVersioningForAllDeploymentTypes()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var testCases = new (DeploymentApplicability deployment, VersioningSystemId expectedId)[]
		{
			(new DeploymentApplicability { Ece = AppliesCollection.GenerallyAvailable }, VersioningSystemId.Ece),
			(new DeploymentApplicability { Eck = AppliesCollection.GenerallyAvailable }, VersioningSystemId.Eck),
			(new DeploymentApplicability { Ess = AppliesCollection.GenerallyAvailable }, VersioningSystemId.Ess),
			(new DeploymentApplicability { Self = AppliesCollection.GenerallyAvailable }, VersioningSystemId.Self),
		};

		foreach (var (deployment, expectedId) in testCases)
		{
			var applicableTo = new ApplicableTo { Deployment = deployment };

			var result = inferrer.InferVersion(
				repositoryName: "unknown-repo",
				legacyPages: null,
				products: null,
				applicableTo: applicableTo);

			result.Id.Should().Be(expectedId);
		}
	}

	[Fact]
	public void InferVersionFallsBackToRepositoryNameWhenNoAppliesTo()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var result = inferrer.InferVersion(
			repositoryName: "curator",
			legacyPages: null,
			products: null,
			applicableTo: null);

		result.Id.Should().Be(VersioningSystemId.Curator);
	}

	[Fact]
	public void InferVersionFallsBackToStackWhenNoMatchFound()
	{
		var versionsConfiguration = CreateVersionsConfiguration();
		var productsConfiguration = CreateProductsConfiguration(versionsConfiguration);
		var inferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

		var result = inferrer.InferVersion(
			repositoryName: "unknown-repo",
			legacyPages: null,
			products: null,
			applicableTo: null);

		result.Id.Should().Be(VersioningSystemId.Stack);
	}

	[Fact]
	public void ProductApplicabilityToProductIdReturnsNullForEmptyApplicability()
	{
		var result = ProductApplicabilityConversion.ProductApplicabilityToProductId(new ProductApplicability());
		result.Should().BeNull();
	}

	[Theory(DisplayName = "IsVersionless returns true for versionless products")]
	[InlineData(VersioningSystemId.All)]
	[InlineData(VersioningSystemId.Serverless)]
	[InlineData(VersioningSystemId.Ech)]
	[InlineData(VersioningSystemId.Ess)]
	[InlineData(VersioningSystemId.ElasticsearchProject)]
	[InlineData(VersioningSystemId.ObservabilityProject)]
	[InlineData(VersioningSystemId.SecurityProject)]
	public void IsVersionlessReturnsTrueForVersionlessProducts(VersioningSystemId id)
	{
		var versioningSystem = new VersioningSystem
		{
			Id = id,
			Current = new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0),
			Base = new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0)
		};

		versioningSystem.IsVersionless.Should().BeTrue(
			$"Versioning system {id} with version {VersioningSystem.VersionlessSentinel} should be marked as versionless");
	}

	[Theory(DisplayName = "IsVersionless returns false for versioned products")]
	[InlineData(VersioningSystemId.Stack, 9, 2, 0)]
	[InlineData(VersioningSystemId.Ece, 4, 0, 3)]
	[InlineData(VersioningSystemId.Eck, 3, 2, 0)]
	[InlineData(VersioningSystemId.ApmAgentJava, 1, 55, 2)]
	[InlineData(VersioningSystemId.EdotCollector, 9, 2, 2)]
	public void IsVersionlessReturnsFalseForVersionedProducts(VersioningSystemId id, int major, int minor, int patch)
	{
		var versioningSystem = new VersioningSystem
		{
			Id = id,
			Current = new SemVersion(major, minor, patch),
			Base = new SemVersion(major, 0, 0)
		};

		versioningSystem.IsVersionless.Should().BeFalse(
			$"Versioning system {id} with version {major}.{minor}.{patch} should not be marked as versionless");
	}

	[Fact(DisplayName = "VersionlessSentinel constant matches versions.yml value")]
	public void VersionlessSentinelMatchesConfigValue()
	{
		// This test ensures the sentinel value matches what's used in config/versions.yml
		// If this test fails, update VersioningSystem.VersionlessSentinel to match versions.yml
		VersioningSystem.VersionlessSentinel.Should().Be(99999,
			"VersionlessSentinel should match the value used in config/versions.yml for 'all' versioning system");
	}

	/// <summary>
	/// These are the versioning system IDs that use the 'all' alias in versions.yml,
	/// meaning they have version 99999 and should be marked as versionless.
	/// </summary>
	private static readonly HashSet<VersioningSystemId> ExpectedVersionlessIds =
	[
		VersioningSystemId.All,
		VersioningSystemId.Ech,
		VersioningSystemId.Ess,
		VersioningSystemId.Serverless,
		VersioningSystemId.ElasticsearchProject,
		VersioningSystemId.ObservabilityProject,
		VersioningSystemId.SecurityProject
	];

	[Fact(DisplayName = "IsVersionless correctly identifies all versionless systems from actual config")]
	public void IsVersionlessCorrectlyIdentifiesAllVersionlessSystemsFromActualConfig()
	{
		// Load the actual versions.yml configuration
		var fileSystem = new FileSystem();
		var versionsPath = fileSystem.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "config", "versions.yml");
		File.Exists(versionsPath).Should().BeTrue($"Expected versions file to exist at {versionsPath}");

		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), fileSystem);
		var versionsConfig = provider.CreateVersionConfiguration();

		// Verify all expected versionless systems are marked as versionless
		foreach (var id in ExpectedVersionlessIds)
		{
			if (versionsConfig.VersioningSystems.TryGetValue(id, out var versioningSystem))
			{
				versioningSystem.IsVersionless.Should().BeTrue(
					$"Versioning system {id} uses 'all' alias in versions.yml and should be marked as versionless");
			}
		}

		// Verify all other systems are NOT marked as versionless
		foreach (var (id, versioningSystem) in versionsConfig.VersioningSystems)
		{
			if (!ExpectedVersionlessIds.Contains(id))
			{
				versioningSystem.IsVersionless.Should().BeFalse(
					$"Versioning system {id} has version {versioningSystem.Current} and should NOT be marked as versionless");
			}
		}
	}

	[Fact(DisplayName = "All versioning systems in config are accounted for in IsVersionless logic")]
	public void AllVersioningSystemsInConfigAreAccountedFor()
	{
		// Load the actual versions.yml configuration
		var fileSystem = new FileSystem();
		var versionsPath = fileSystem.Path.Combine(Paths.WorkingDirectoryRoot.FullName, "config", "versions.yml");
		File.Exists(versionsPath).Should().BeTrue($"Expected versions file to exist at {versionsPath}");

		var provider = new ConfigurationFileProvider(new NullLoggerFactory(), fileSystem);
		var versionsConfig = provider.CreateVersionConfiguration();

		// Count how many are versionless vs versioned
		var versionlessSystems = versionsConfig.VersioningSystems.Values.Where(v => v.IsVersionless).ToList();
		var versionedSystems = versionsConfig.VersioningSystems.Values.Where(v => !v.IsVersionless).ToList();

		// The versionless systems should match our expected list
		versionlessSystems.Select(v => v.Id).Should().BeEquivalentTo(ExpectedVersionlessIds,
			"The versioning systems marked as versionless should match the expected list from versions.yml");

		// All versioned systems should have version < 99999
		foreach (var system in versionedSystems)
		{
			system.Current.Major.Should().BeLessThan(VersioningSystem.VersionlessSentinel,
				$"Versioned system {system.Id} should have major version less than {VersioningSystem.VersionlessSentinel}");
		}
	}
}
