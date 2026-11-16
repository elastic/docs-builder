// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Entities;

namespace Elastic.Documentation.Catalog;

/// <summary>Every rule a <see cref="ProductCatalog"/> must satisfy, in the order they are reported.</summary>
[Constraints]
public static partial class ProductCatalogConstraints
{
	public static Constraint<ProductCatalog> ProductIdMatchesKey { get; } = Constraints<ProductCatalog>
		.KeysMatch(c => c.Products, p => p.Id)
		.WithSeverity(Severity.Warning)
		.Describe("G77: A product's id is its key in products.yml, compared case-sensitively; the two never differ.");

	public static Constraint<ProductCatalog> ProductHasDisplayName { get; } = Constraints<ProductCatalog>
		.Each(
			c => c.Products.Values,
			p => !string.IsNullOrWhiteSpace(p.DisplayName),
			p => $"Product '{p.Id}' has no 'display' name; it defaults to an empty string."
		)
		.WithSeverity(Severity.Warning)
		.Describe("G78: Every product has a display name; 'display' is not required and defaults to an empty string.");

	public static Constraint<ProductCatalog> PublicReferenceProductHasVersioning { get; } = Constraints<ProductCatalog>.Each(
		c => c.Products.Values,
		p => !p.Features.PublicReference || (p.VersioningSystem is not null && p.VersioningSystem.Id != VersioningSystemId.None),
		p => $"Product '{p.Id}' has invalid or missing versioning while 'public-reference' is enabled."
	).Describe(
		"G81: A public-reference product has a versioning system; without one the loader throws, and for other products it silently becomes none."
	);
}
