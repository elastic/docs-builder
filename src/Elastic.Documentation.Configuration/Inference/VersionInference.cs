// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;
using Elastic.Documentation.Configuration.Versions;

namespace Elastic.Documentation.Configuration.Inference;

public interface IVersionInferrerService
{
	VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages, IReadOnlyCollection<Product>? products, ApplicableTo? applicableTo);
}

public class ProductVersionInferrerService(ProductsConfiguration productsConfiguration, VersionsConfiguration versionsConfiguration) : IVersionInferrerService
{
	private ProductsConfiguration ProductsConfiguration { get; } = productsConfiguration;
	private VersionsConfiguration VersionsConfiguration { get; } = versionsConfiguration;
	public VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages, IReadOnlyCollection<Product>? products, ApplicableTo? applicableTo)
	{
		if (legacyPages is { Count: > 0 })
			return legacyPages.ElementAt(0).Product.VersioningSystem!; // If the page has legacy mappings, use the versioning system of the first mapping's product

		if (applicableTo is not null)
		{
			var versioningFromApplicability = VersioningFromApplicability(applicableTo); // Try to infer the versioning system from the applicability metadata
			if (versioningFromApplicability is not null)
				return versioningFromApplicability;
		}

		var versioning = ProductsConfiguration.Products.TryGetValue(repositoryName, out var belonging)
				? belonging.VersioningSystem! //If the page's docset has a name with a direct product match, use the versioning system of the product
				: ProductsConfiguration.Products.Values.SingleOrDefault(p =>
					p.Repository is not null && p.Repository.Equals(repositoryName, StringComparison.OrdinalIgnoreCase)) is { } repositoryMatch
					? repositoryMatch.VersioningSystem! // Verify if the page belongs to a repository linked to a product, and if so, use the versioning system of the product
					: VersionsConfiguration.VersioningSystems[VersioningSystemId.Stack]; // Fallback to the stack versioning system

		return versioning;
	}

	private VersioningSystem? VersioningFromApplicability(ApplicableTo applicableTo)
	{
		// Priority 1: Product applicability
		var product = ProductFromApplicability(applicableTo.ProductApplicability);
		if (product?.VersioningSystem is not null)
			return product.VersioningSystem;

		// Priority 2: Stack applicability
		if (applicableTo.Stack is not null)
			return VersionsConfiguration.VersioningSystems[VersioningSystemId.Stack];
		// Priority 3: Deployment applicability
		if (applicableTo.Deployment is not null)
		{
			var versioning = applicableTo.Deployment switch
			{
				{ Ece: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Ece],
				{ Eck: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Eck],
				{ Ess: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Ess],
				{ Self: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Self],
				_ => null
			};
			if (versioning is not null)
				return versioning;
		}

		// Priority 4: Serverless applicability
		if (applicableTo.Serverless is not null)
		{
			var versioning = applicableTo.Serverless switch
			{
				{ Elasticsearch: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ElasticsearchProject],
				{ Observability: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ObservabilityProject],
				{ Security: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.SecurityProject],
				_ => null
			};
			if (versioning is not null)
				return versioning;
		}

		return null;
	}
	private Product? ProductFromApplicability(ProductApplicability? productApplicability)
	{
		if (productApplicability is null)
			return null;

		var productId = ProductApplicabilityConversion.ProductApplicabilityToProductId(productApplicability);

		return productId is null ? null : ProductsConfiguration.Products.GetValueOrDefault(productId);
	}
}

public class NoopVersionInferrer : IVersionInferrerService
{
	public VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages, IReadOnlyCollection<Product>? products, ApplicableTo? applicableTo) => new()
	{
		Id = VersioningSystemId.Stack,
		Base = ZeroVersion.Instance,
		Current = ZeroVersion.Instance
	};
}
