// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.AppliesTo;
using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Versions;

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
		// docs-content handles content from multiple products which should preferably be inferred through frontmatter metadata
		if (repositoryName.Equals("docs-content", StringComparison.OrdinalIgnoreCase))
		{
			if (products is { Count: > 0 }) // If the page is from multiple products, use the versioning system of the first product
				return products.First().VersioningSystem!;
			if (applicableTo is not null)
			{
				var versioningFromApplicability = VersioningFromApplicability(applicableTo); // Try to infer the versioning system from the applicability metadata
				if (versioningFromApplicability is not null)
					return versioningFromApplicability;
			}
		}
		if (legacyPages is { Count: > 0 })
			return legacyPages.ElementAt(0).Product.VersioningSystem!; // If the page has a legacy page mapping, use the versioning system of the legacy page

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
		VersioningSystem? versioning = null;
		if (applicableTo.ProductApplicability is not null)
		{
			versioning = applicableTo.ProductApplicability switch
			{
				{ ApmAgentAndroid: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentAndroid],
				{ ApmAgentIos: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentIos],
				{ ApmAgentJava: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentJava],
				{ ApmAgentDotnet: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentDotnet],
				{ ApmAgentGo: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentGo],
				{ ApmAgentNode: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentNode],
				{ ApmAgentPhp: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentPhp],
				{ ApmAgentPython: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentPython],
				{ ApmAgentRuby: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentRuby],
				{ ApmAgentRumJs: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.ApmAgentRumJs],
				{ Curator: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Curator],
				{ Ecctl: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Ecctl],
				{ EdotAndroid: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotAndroid],
				{ EdotCfAws: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotCfAws],
				{ EdotCfAzure: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotCfAzure],
				{ EdotCollector: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotCollector],
				{ EdotIos: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotIos],
				{ EdotJava: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotJava],
				{ EdotNode: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotNode],
				{ EdotDotnet: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotDotnet],
				{ EdotPhp: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotPhp],
				{ EdotPython: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.EdotPython],
				_ => null
			};
		}
		if (versioning is not null)
			return versioning;
		if (applicableTo.Deployment is not null)
		{
			versioning = applicableTo.Deployment switch
			{
				{ Ece: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Ece],
				{ Eck: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Eck],
				{ Ess: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Ess],
				{ Self: not null } => VersionsConfiguration.VersioningSystems[VersioningSystemId.Self],
				_ => null
			};
		}
		return versioning;
	}
}

public class NoopVersionInferrer : IVersionInferrerService
{
	public VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages, IReadOnlyCollection<Product>? products, ApplicableTo? applicableTo) => new()
	{
		Id = VersioningSystemId.Stack,
		Base = new SemVersion(0, 0, 0),
		Current = new SemVersion(0, 0, 0)
	};
}
