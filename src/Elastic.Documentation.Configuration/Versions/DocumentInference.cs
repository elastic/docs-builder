// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Versions;

/// <summary>
/// Result of document inference containing product, versioning system, and repository information.
/// </summary>
public record DocumentInferenceResult
{
	/// <summary>
	/// The canonical/primary product from products.yml.
	/// Contains Id, DisplayName, VersioningSystem, and Repository.
	/// </summary>
	public Product? Product { get; init; }

	/// <summary>
	/// The current version from the versioning system (e.g., "9.2.4").
	/// Null for versionless products.
	/// Note: This is for runtime use only and is NOT indexed to ES.
	/// </summary>
	public string? ProductVersion { get; init; }

	/// <summary>
	/// The repository name validated against assembler.yml (e.g., "elasticsearch", "docs-content").
	/// </summary>
	public string? Repository { get; init; }

	/// <summary>
	/// All related products found during inference (from legacy mappings, applicability, repository, etc.)
	/// </summary>
	public IReadOnlyCollection<Product> RelatedProducts { get; init; } = [];
}

/// <summary>
/// Service for inferring product, version system, and repository information from document metadata.
/// </summary>
public interface IDocumentInferrerService
{
	/// <summary>
	/// Infers product, version system, and repository for a markdown page.
	/// </summary>
	/// <param name="repositoryName">The git repository name from GitCheckoutInformation</param>
	/// <param name="mappedPages">Legacy mapped page URLs from frontmatter</param>
	/// <param name="products">Products from frontmatter if available</param>
	/// <param name="applicableTo">ApplicableTo metadata from frontmatter</param>
	/// <returns>Inference result with product, version, repository, and related products</returns>
	DocumentInferenceResult InferForMarkdown(
		string repositoryName,
		IReadOnlyCollection<string>? mappedPages,
		IReadOnlyCollection<Product>? products,
		ApplicableTo? applicableTo);

	/// <summary>
	/// Infers product, version system, and repository for an OpenAPI endpoint.
	/// </summary>
	/// <param name="productSlug">The product slug (e.g., "elasticsearch", "kibana")</param>
	/// <returns>Inference result with product, version, repository, and related products</returns>
	DocumentInferenceResult InferForOpenApi(string productSlug);
}

/// <summary>
/// Implementation of document inference service that determines product, version, and repository
/// from various metadata sources with defined priority.
/// </summary>
public class DocumentInferrerService(
	ProductsConfiguration productsConfiguration,
	VersionsConfiguration versionsConfiguration,
	LegacyUrlMappingConfiguration legacyUrlMappings,
	AssemblyConfiguration? assemblyConfiguration = null) : IDocumentInferrerService
{
	private readonly IVersionInferrerService _versionInferrer = new ProductVersionInferrerService(productsConfiguration, versionsConfiguration);

	/// <inheritdoc />
	public DocumentInferenceResult InferForMarkdown(
		string repositoryName,
		IReadOnlyCollection<string>? mappedPages,
		IReadOnlyCollection<Product>? products,
		ApplicableTo? applicableTo)
	{
		var relatedProducts = new HashSet<Product>();

		// Collect all products from different sources
		var legacyProduct = InferProductFromLegacyMappings(mappedPages);
		var applicabilityProduct = InferProductFromApplicability(applicableTo);
		var repositoryProduct = InferProductFromRepository(repositoryName);

		// Add all found products to related products
		if (legacyProduct is not null)
			_ = relatedProducts.Add(legacyProduct);
		if (applicabilityProduct is not null)
			_ = relatedProducts.Add(applicabilityProduct);
		if (repositoryProduct is not null)
			_ = relatedProducts.Add(repositoryProduct);

		// Add products from frontmatter
		if (products is not null)
		{
			foreach (var p in products)
				_ = relatedProducts.Add(p);
		}

		// Determine canonical product using priority chain
		var canonicalProduct = legacyProduct ?? applicabilityProduct ?? repositoryProduct;

		// Map legacy pages to LegacyPageMapping for version inference
		var legacyPageMappings = MapLegacyPages(mappedPages);

		// Infer version system
		var versioningSystem = _versionInferrer.InferVersion(repositoryName, legacyPageMappings, products, applicableTo);

		// Determine repository (validate against assembler.yml if available)
		var repository = ValidateRepository(repositoryName);

		return new DocumentInferenceResult
		{
			Product = canonicalProduct,
			ProductVersion = versioningSystem.IsVersionless ? null : versioningSystem.Current.ToString(),
			Repository = repository,
			RelatedProducts = [.. relatedProducts]
		};
	}

	/// <inheritdoc />
	public DocumentInferenceResult InferForOpenApi(string productSlug)
	{
		var productId = productSlug.ToLowerInvariant();
		var product = productsConfiguration.Products.GetValueOrDefault(productId);

		var versioningSystem = product?.VersioningSystem
			?? versionsConfiguration.VersioningSystems[VersioningSystemId.Stack];

		// For OpenAPI, the product is always known
		var relatedProducts = new List<Product>();
		if (product is not null)
			relatedProducts.Add(product);

		return new DocumentInferenceResult
		{
			Product = product,
			ProductVersion = versioningSystem.IsVersionless ? null : versioningSystem.Current.ToString(),
			Repository = productId, // For OpenAPI, repository matches product slug
			RelatedProducts = relatedProducts
		};
	}

	/// <summary>
	/// Infers product from legacy URL mappings by matching mapped page URLs against LegacyUrlMappingConfiguration.
	/// </summary>
	private Product? InferProductFromLegacyMappings(IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null || mappedPages.Count == 0)
			return null;

		var mappedPage = mappedPages.First();

		// Find matching legacy URL mapping by BaseUrl
		var legacyMapping = legacyUrlMappings.Mappings
			.FirstOrDefault(x => mappedPage.Contains(x.BaseUrl, StringComparison.OrdinalIgnoreCase));

		return legacyMapping?.Product;
	}

	/// <summary>
	/// Infers product from ApplicableTo metadata using ProductApplicability conversion.
	/// </summary>
	private Product? InferProductFromApplicability(ApplicableTo? applicableTo)
	{
		if (applicableTo?.ProductApplicability is null)
			return null;

		var productId = ProductApplicabilityConversion.ProductApplicabilityToProductId(applicableTo.ProductApplicability);
		if (productId is null)
			return null;

		return productsConfiguration.Products.GetValueOrDefault(productId);
	}

	/// <summary>
	/// Infers product from repository name by direct match or repository configuration.
	/// </summary>
	private Product? InferProductFromRepository(string repositoryName)
	{
		// Priority 1: Direct product match by repository name
		if (productsConfiguration.Products.TryGetValue(repositoryName, out var directMatch))
			return directMatch;

		// Priority 2: Product by repository configuration
		return productsConfiguration.GetProductByRepositoryName(repositoryName);
	}

	/// <summary>
	/// Maps legacy page URLs to LegacyPageMapping for version inference compatibility.
	/// </summary>
	private IReadOnlyCollection<LegacyPageMapping>? MapLegacyPages(IReadOnlyCollection<string>? mappedPages)
	{
		if (mappedPages is null || mappedPages.Count == 0)
			return null;

		var mappedPage = mappedPages.First();

		// Find matching legacy URL mapping
		var legacyMapping = legacyUrlMappings.Mappings
			.FirstOrDefault(x => mappedPage.Contains(x.BaseUrl, StringComparison.OrdinalIgnoreCase));

		if (legacyMapping is null)
			return null;

		// Create a simple LegacyPageMapping for version inference
		// (we don't need the full version list, just the product)
		return [new LegacyPageMapping(legacyMapping.Product, mappedPage, "current", false)];
	}

	/// <summary>
	/// Validates repository name against assembler.yml available repositories.
	/// </summary>
	private string ValidateRepository(string repositoryName)
	{
		if (assemblyConfiguration is null)
			return repositoryName;

		// Check if repository exists in assembler.yml
		// Return the repository name regardless (validation is informational)
		return repositoryName;
	}
}

/// <summary>
/// No-op implementation of document inference service for testing or when inference is not needed.
/// </summary>
public class NoopDocumentInferrer : IDocumentInferrerService
{
	public DocumentInferenceResult InferForMarkdown(
		string repositoryName,
		IReadOnlyCollection<string>? mappedPages,
		IReadOnlyCollection<Product>? products,
		ApplicableTo? applicableTo) => new();

	public DocumentInferenceResult InferForOpenApi(string productSlug) => new();
}
