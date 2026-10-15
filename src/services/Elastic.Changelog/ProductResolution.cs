// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog;

/// <summary>
/// Resolves the products of an entry from GitHub labels the same way in every command: <c>changelog add</c>,
/// the PR evaluation, label validation and commit-range bundles.
/// </summary>
public static class ProductResolution
{
	/// <summary>
	/// The products that the labels map to (<c>pivot.products</c>), followed by the products that each of them
	/// implies (<c>products.implies</c>). An implied product carries the product ID only, and a product that a label
	/// already maps to is not added again. Empty when no label maps to a product.
	/// </summary>
	public static List<ProductArgument> FromLabels(string[] labels, ChangelogConfiguration config)
	{
		if (config.LabelToProducts is not { Count: > 0 })
			return [];

		var products = PrInfoProcessor.MapLabelsToProducts(labels, config.LabelToProducts);
		if (products.Count == 0 || config.ProductsConfiguration?.Implies is not { Count: > 0 } implies)
			return products;

		var present = products.Select(p => Normalize(p.Product)).ToHashSet(StringComparer.OrdinalIgnoreCase);
		foreach (var product in products.ToList())
		{
			if (!implies.TryGetValue(Normalize(product.Product), out var implied))
				continue;

			foreach (var impliedId in implied)
			{
				if (present.Add(impliedId))
					products.Add(new ProductArgument { Product = impliedId });
			}
		}

		return products;
	}

	/// <summary>The <c>products.default</c> entries with their lifecycle. Empty when none are configured.</summary>
	public static List<ProductArgument> Defaults(ChangelogConfiguration config) =>
		config.ProductsConfiguration?.Default is { Count: > 0 } defaults
			? defaults.Select(d => new ProductArgument { Product = d.Product, Lifecycle = d.Lifecycle }).ToList()
			: [];

	private static string Normalize(string? productId) => (productId ?? string.Empty).Replace('_', '-');
}
