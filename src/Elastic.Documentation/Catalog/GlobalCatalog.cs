// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Catalog;

/// <summary>
/// Global reference data every documentation set, changelog and search document points at: the versioning systems,
/// the products that use them, and the legacy URL mappings that point at those products. Loaded once per process,
/// as one unit, from <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c>, in that order.
/// </summary>
/// <remarks>
/// Constructed with <c>CatalogLoader.Load</c>.
/// Optional members are <c>internal init</c>: only tests (<c>InternalsVisibleTo</c>) can set them.
/// </remarks>
public sealed record GlobalCatalog
{
	internal GlobalCatalog(VersioningSystemCatalog versioningSystems, ProductCatalog products, LegacyUrlMappingCatalog legacyUrlMappings)
	{
		VersioningSystems = versioningSystems;
		Products = products;
		LegacyUrlMappings = legacyUrlMappings;
	}

	public VersioningSystemCatalog VersioningSystems { get; internal init; }
	public ProductCatalog Products { get; internal init; }
	public LegacyUrlMappingCatalog LegacyUrlMappings { get; internal init; }

	public void Deconstruct(
		out VersioningSystemCatalog versioningSystems,
		out ProductCatalog products,
		out LegacyUrlMappingCatalog legacyUrlMappings
	)
	{
		versioningSystems = VersioningSystems;
		products = Products;
		legacyUrlMappings = LegacyUrlMappings;
	}
}
