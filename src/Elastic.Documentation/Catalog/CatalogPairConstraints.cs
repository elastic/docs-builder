// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>The products of <c>products.yml</c> together with the versioning systems of <c>versions.yml</c> they point at.</summary>
public sealed record ProductCatalogInVersioningSystemCatalog(
	ProductCatalog Products,
	VersioningSystemCatalog VersioningSystems
) : IHaveConstraints<ProductCatalogInVersioningSystemCatalog>
{
	public static ConstraintSet<ProductCatalogInVersioningSystemCatalog> Constraints =>
		ProductCatalogInVersioningSystemCatalogConstraints.All;
}

/// <summary>The legacy url mappings of <c>legacy-url-mappings.yml</c> together with the products of <c>products.yml</c> they point at.</summary>
public sealed record LegacyUrlMappingCatalogInProductCatalog(
	LegacyUrlMappingCatalog LegacyUrlMappings,
	ProductCatalog Products
) : IHaveConstraints<LegacyUrlMappingCatalogInProductCatalog>
{
	public static ConstraintSet<LegacyUrlMappingCatalogInProductCatalog> Constraints =>
		LegacyUrlMappingCatalogInProductCatalogConstraints.All;
}

/// <summary>Every rule that spans the <see cref="ProductCatalog"/> and the <see cref="VersioningSystemCatalog"/> it refers to.</summary>
/// <remarks>The loader's map step fails on the same condition first, so a catalog read from files never reports it twice; the rule guards a catalog built by hand.</remarks>
// A catalog built by hand may hold a null where the types forbid one (a missing catalog, product or mapping); a loaded catalog never does, so
// the null guards below change no finding.
[Constraints<ProductCatalogInVersioningSystemCatalog>]
public static partial class ProductCatalogInVersioningSystemCatalogConstraints
{
	public static readonly Constraint<ProductCatalogInVersioningSystemCatalog> ProductVersioningSystemDefined = Constraints<ProductCatalogInVersioningSystemCatalog>.References(
		p => p.Products?.Products?.Values.Where(product => product?.VersioningSystem is { } vs && vs.Id != VersioningSystemId.None) ?? [],
		product => Enumerable.Repeat(product.VersioningSystem!.Id, 1),
		p => p.VersioningSystems?.VersioningSystems?.Keys ?? [],
		describe: (product, id) =>
			$"Product '{product.Id}' uses versioning system '{id.ToStringFast(true)}', which is not defined in versions.yml."
	).Describe("G80: A product's versioning system is defined in versions.yml.");
}

/// <summary>Every rule that spans the <see cref="LegacyUrlMappingCatalog"/> and the <see cref="ProductCatalog"/> it refers to.</summary>
/// <remarks>The loader's map step fails on the same condition first, so a catalog read from files never reports it twice; the rule guards a catalog built by hand.</remarks>
[Constraints<LegacyUrlMappingCatalogInProductCatalog>]
public static partial class LegacyUrlMappingCatalogInProductCatalogConstraints
{
	public static readonly Constraint<LegacyUrlMappingCatalogInProductCatalog> LegacyMappingProductDefined = Constraints<LegacyUrlMappingCatalogInProductCatalog>.References(
		p => p.LegacyUrlMappings?.Mappings?.Where(m => m?.Product is not null) ?? [],
		m => Enumerable.Repeat(m.Product.Id, 1),
		p => p.Products?.Products?.Keys ?? [],
		describe: (m, id) => $"Legacy url mapping '{m.BaseUrl}' refers to product '{id}', which is not defined in products.yml."
	).Describe("G93: A legacy url mapping's product is defined in products.yml.");
}
