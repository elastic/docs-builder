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
/// <para><b>Built by:</b> <c>CatalogLoader.Load</c> in <c>Elastic.Documentation.Configuration</c>, which reads <c>versions.yml</c>, <c>products.yml</c> and <c>legacy-url-mappings.yml</c> as one unit and returns a <c>Result&lt;GlobalCatalog&gt;</c>.</para>
/// <para><b>Why it cannot be built from outside:</b> Its members are <c>internal init</c>: only <c>Elastic.Documentation</c>, <c>Elastic.Documentation.Configuration</c> and the test projects (<c>InternalsVisibleTo</c>) can set them, so a record that reaches a service has been through a loader or a checked operation.</para>
/// <para><b>In a test:</b> build it with an object initializer through <c>InternalsVisibleTo</c> (<c>new GlobalCatalog { ... }</c>) and <c>with</c> for variations.</para>
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
