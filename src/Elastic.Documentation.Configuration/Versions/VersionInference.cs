// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.LegacyUrlMappings;
using Elastic.Documentation.Configuration.Products;

namespace Elastic.Documentation.Configuration.Versions;

public interface IVersionInferrerService
{
	VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages);
}

public class ProductVersionInferrerService(ProductsConfiguration productsConfiguration, VersionsConfiguration versionsConfiguration) : IVersionInferrerService
{
	private ProductsConfiguration ProductsConfiguration { get; } = productsConfiguration;
	private VersionsConfiguration VersionsConfiguration { get; } = versionsConfiguration;
	public VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages)
	{
		var versioning = legacyPages is not null && legacyPages.Count > 0
			? legacyPages.ElementAt(0).Product.VersioningSystem! // If the page has a legacy page mapping, use the versioning system of the legacy page
			: ProductsConfiguration.Products.TryGetValue(repositoryName, out var belonging)
				? belonging.VersioningSystem! //If the page's docset has a name with a direct product match, use the versioning system of the product
				: ProductsConfiguration.Products.Values.SingleOrDefault(p =>
					p.Repository is not null && p.Repository.Equals(repositoryName, StringComparison.OrdinalIgnoreCase)) is { } repositoryMatch
					? repositoryMatch.VersioningSystem! // Verify if the page belongs to a repository linked to a product, and if so, use the versioning system of the product
					: VersionsConfiguration.VersioningSystems[VersioningSystemId.Stack]; // Fallback to the stack versioning system

		return versioning;
	}
}

public class NoopVersionInferrer : IVersionInferrerService
{
	public VersioningSystem InferVersion(string repositoryName, IReadOnlyCollection<LegacyPageMapping>? legacyPages) => new()
	{
		Id = VersioningSystemId.Stack,
		Base = new SemVersion(0, 0, 0),
		Current = new SemVersion(0, 0, 0)
	};
}
