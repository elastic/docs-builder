// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>Every rule a <see cref="ProductCatalog"/> must satisfy, in the order they are reported.</summary>
[Constraints<ProductCatalog>]
public static partial class ProductCatalogConstraints
{
	/// <remarks>The loader's map step throws the same text first, so a catalog read from <c>products.yml</c> never reports it twice; the rule guards a catalog built by hand.</remarks>
	public static readonly Constraint<ProductCatalog> PublicReferenceProductHasVersioning = Constraints<ProductCatalog>.Each(
		ProductsOf,
		p => p.Features is not { PublicReference: true } ||
			(p.VersioningSystem is not null && !ReferenceEquals(p.VersioningSystem, VersioningSystem.None)),
		p => $"Product '{p.Id}' has invalid or missing versioning while 'public-reference' is enabled."
	).Describe("G81: A public-reference product has a versioning system.");

	// A catalog built by hand may hold a null where the types forbid one; a loaded catalog never does, so this changes no finding.
	private static IEnumerable<Product> ProductsOf(ProductCatalog catalog) => catalog.Products?.Values.Where(p => p is not null) ?? [];
}
