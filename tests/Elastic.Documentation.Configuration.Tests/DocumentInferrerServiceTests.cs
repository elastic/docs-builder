// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;
using FluentAssertions;

namespace Elastic.Documentation.Configuration.Tests;

public class DocumentInferrerServiceTests
{
	private static VersionsConfiguration CreateVersionsConfiguration()
	{
		var versioningSystems = new Dictionary<VersioningSystemId, VersioningSystem>();

		foreach (var id in Enum.GetValues<VersioningSystemId>())
		{
			// Create a versionless system for "All" type IDs
			var isVersionless = id is VersioningSystemId.All or VersioningSystemId.Serverless
				or VersioningSystemId.Ess or VersioningSystemId.Ech
				or VersioningSystemId.ElasticsearchProject or VersioningSystemId.ObservabilityProject
				or VersioningSystemId.SecurityProject;

			var version = isVersionless
				? new SemVersion(VersioningSystem.VersionlessSentinel, 0, 0)
				: new SemVersion(9, 2, 0);

			versioningSystems[id] = new VersioningSystem
			{
				Id = id,
				Current = version,
				Base = isVersionless ? version : new SemVersion(9, 0, 0)
			};
		}

		return new VersionsConfiguration { VersioningSystems = versioningSystems };
	}

	private static ProductsConfiguration CreateProductsConfiguration(VersionsConfiguration versionsConfiguration)
	{
		var products = new Dictionary<string, Product>
		{
			["elasticsearch"] = new Product
			{
				Id = "elasticsearch",
				DisplayName = "Elasticsearch",
				VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
			},
			["kibana"] = new Product
			{
				Id = "kibana",
				DisplayName = "Kibana",
				VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Stack)
			},
			["apm-agent-java"] = new Product
			{
				Id = "apm-agent-java",
				DisplayName = "APM Java Agent",
				VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.ApmAgentJava),
				Repository = "elastic-otel-java"
			},
			["curator"] = new Product
			{
				Id = "curator",
				DisplayName = "Curator",
				VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Curator)
			},
			["cloud-control-ecctl"] = new Product
			{
				Id = "cloud-control-ecctl",
				DisplayName = "Elastic Cloud Control",
				VersioningSystem = versionsConfiguration.GetVersioningSystem(VersioningSystemId.Ecctl)
			}
		};

		return new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};
	}

	private static LegacyUrlMappingConfiguration CreateLegacyUrlMappings(ProductsConfiguration productsConfig)
	{
		var mappings = new List<LegacyUrlMapping>
		{
			new()
			{
				BaseUrl = "www.elastic.co/guide/en/elasticsearch/reference",
				Product = productsConfig.Products["elasticsearch"],
				LegacyVersions = ["8.17", "8.16", "8.15"]
			},
			new()
			{
				BaseUrl = "www.elastic.co/guide/en/kibana",
				Product = productsConfig.Products["kibana"],
				LegacyVersions = ["8.17", "8.16", "8.15"]
			},
			new()
			{
				BaseUrl = "www.elastic.co/guide/en/apm/agent/java",
				Product = productsConfig.Products["apm-agent-java"],
				LegacyVersions = ["1.55", "1.54", "1.53"]
			}
		};

		return new LegacyUrlMappingConfiguration { Mappings = mappings };
	}

	[Fact]
	public void InferForMarkdownWithDirectRepositoryMatch_ReturnsProduct()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForMarkdown(
			repositoryName: "elasticsearch",
			mappedPages: null,
			products: null,
			applicableTo: null);

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("elasticsearch");
		result.Repository.Should().Be("elasticsearch");
		result.ProductVersion.Should().Be("9.2.0");
	}

	[Fact]
	public void InferForMarkdownWithProductRepositoryReturnsProductByRepositoryField()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		// "elastic-otel-java" is the Repository field of "apm-agent-java" product
		var result = inferrer.InferForMarkdown(
			repositoryName: "elastic-otel-java",
			mappedPages: null,
			products: null,
			applicableTo: null);

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("apm-agent-java");
		result.Repository.Should().Be("elastic-otel-java");
	}

	[Fact]
	public void InferForMarkdownWithLegacyMappedPagesReturnsProductFromLegacyMapping()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var mappedPages = new[] { "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html" };

		var result = inferrer.InferForMarkdown(
			repositoryName: "docs-content",
			mappedPages: mappedPages,
			products: null,
			applicableTo: null);

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("elasticsearch");
	}

	[Fact]
	public void InferForMarkdownWithProductApplicabilityReturnsProductFromApplicability()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability { Curator = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferForMarkdown(
			repositoryName: "docs-content",
			mappedPages: null,
			products: null,
			applicableTo: applicableTo);

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("curator");
	}

	[Fact]
	public void InferForMarkdownLegacyMappingTakesPriorityOverApplicability()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var mappedPages = new[] { "https://www.elastic.co/guide/en/kibana/current/index.html" };
		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability { Curator = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferForMarkdown(
			repositoryName: "docs-content",
			mappedPages: mappedPages,
			products: null,
			applicableTo: applicableTo);

		// Legacy mapping should take priority
		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("kibana");
	}

	[Fact]
	public void InferForMarkdownApplicabilityTakesPriorityOverRepository()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability { Curator = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferForMarkdown(
			repositoryName: "elasticsearch",
			mappedPages: null,
			products: null,
			applicableTo: applicableTo);

		// Applicability should take priority over repository match
		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("curator");
	}

	[Fact]
	public void InferForMarkdownCollectsAllRelatedProducts()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var mappedPages = new[] { "https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html" };
		var applicableTo = new ApplicableTo
		{
			ProductApplicability = new ProductApplicability { Curator = AppliesCollection.GenerallyAvailable }
		};

		var result = inferrer.InferForMarkdown(
			repositoryName: "kibana",
			mappedPages: mappedPages,
			products: null,
			applicableTo: applicableTo);

		// Should collect all products: elasticsearch (from legacy), curator (from applicability), kibana (from repo)
		result.RelatedProducts.Should().HaveCount(3);
		result.RelatedProducts.Select(p => p.Id).Should().Contain("elasticsearch");
		result.RelatedProducts.Select(p => p.Id).Should().Contain("curator");
		result.RelatedProducts.Select(p => p.Id).Should().Contain("kibana");
	}

	[Fact]
	public void InferForMarkdownIncludesFrontmatterProductsInRelatedProducts()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var frontmatterProducts = new[] { productsConfig.Products["elasticsearch"], productsConfig.Products["kibana"] };

		var result = inferrer.InferForMarkdown(
			repositoryName: "docs-content",
			mappedPages: null,
			products: frontmatterProducts,
			applicableTo: null);

		result.RelatedProducts.Should().HaveCount(2);
		result.RelatedProducts.Select(p => p.Id).Should().Contain("elasticsearch");
		result.RelatedProducts.Select(p => p.Id).Should().Contain("kibana");
	}

	[Fact]
	public void InferForMarkdownWithUnknownRepositoryReturnsNullProduct()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForMarkdown(
			repositoryName: "unknown-repo",
			mappedPages: null,
			products: null,
			applicableTo: null);

		result.Product.Should().BeNull();
		result.Repository.Should().Be("unknown-repo");
		result.RelatedProducts.Should().BeEmpty();
	}

	[Fact]
	public void InferForMarkdownWithVersionlessProductReturnsNullVersion()
	{
		var versionsConfig = CreateVersionsConfiguration();

		// Add a versionless product
		var products = new Dictionary<string, Product>
		{
			["serverless-es"] = new Product
			{
				Id = "serverless-es",
				DisplayName = "Serverless Elasticsearch",
				VersioningSystem = versionsConfig.GetVersioningSystem(VersioningSystemId.ElasticsearchProject)
			}
		};

		var productsConfig = new ProductsConfiguration
		{
			Products = products.ToFrozenDictionary(),
			ProductDisplayNames = products.ToDictionary(p => p.Key, p => p.Value.DisplayName).ToFrozenDictionary()
		};
		var legacyUrlMappings = new LegacyUrlMappingConfiguration { Mappings = [] };

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForMarkdown(
			repositoryName: "serverless-es",
			mappedPages: null,
			products: null,
			applicableTo: null);

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("serverless-es");
		result.ProductVersion.Should().BeNull("versionless products should return null version");
	}

	[Fact]
	public void InferForOpenApiWithElasticsearchReturnsCorrectProduct()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForOpenApi("elasticsearch");

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("elasticsearch");
		result.Repository.Should().Be("elasticsearch");
		result.ProductVersion.Should().Be("9.2.0");
		result.RelatedProducts.Should().HaveCount(1);
		result.RelatedProducts.First().Id.Should().Be("elasticsearch");
	}

	[Fact]
	public void InferForOpenApiWithKibanaReturnsCorrectProduct()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForOpenApi("kibana");

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("kibana");
		result.Repository.Should().Be("kibana");
		result.ProductVersion.Should().Be("9.2.0");
	}

	[Fact]
	public void InferForOpenApiWithUnknownProductReturnsNullProductWithStackVersion()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForOpenApi("unknown-product");

		result.Product.Should().BeNull();
		result.Repository.Should().Be("unknown-product");
		result.ProductVersion.Should().Be("9.2.0"); // Falls back to Stack version
		result.RelatedProducts.Should().BeEmpty();
	}

	[Fact]
	public void InferForOpenApiIsCaseInsensitive()
	{
		var versionsConfig = CreateVersionsConfiguration();
		var productsConfig = CreateProductsConfiguration(versionsConfig);
		var legacyUrlMappings = CreateLegacyUrlMappings(productsConfig);

		var inferrer = new DocumentInferrerService(productsConfig, versionsConfig, legacyUrlMappings);

		var result = inferrer.InferForOpenApi("ELASTICSEARCH");

		result.Product.Should().NotBeNull();
		result.Product!.Id.Should().Be("elasticsearch");
	}

	[Fact]
	public void NoopDocumentInferrerReturnsEmptyResult()
	{
		var inferrer = new NoopDocumentInferrer();

		var markdownResult = inferrer.InferForMarkdown("repo", null, null, null);
		var openApiResult = inferrer.InferForOpenApi("product");

		markdownResult.Product.Should().BeNull();
		markdownResult.ProductVersion.Should().BeNull();
		markdownResult.Repository.Should().BeNull();
		markdownResult.RelatedProducts.Should().BeEmpty();

		openApiResult.Product.Should().BeNull();
		openApiResult.ProductVersion.Should().BeNull();
		openApiResult.Repository.Should().BeNull();
		openApiResult.RelatedProducts.Should().BeEmpty();
	}
}
