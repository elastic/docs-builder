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

	/// <summary>
	/// True when a pull request or issue without a product label is an error: <c>pivot.products</c> maps two or more
	/// distinct products and <c>products.default</c> is not set. A repository that maps a single product, or none, keeps
	/// its fallbacks: the repository name for <c>changelog add</c>, the profile's output products for a range bundle.
	/// </summary>
	public static bool RequiresProductLabel(ChangelogConfiguration config) =>
		config.ProductsConfiguration?.Default is not { Count: > 0 }
			&& config.LabelToProducts is { Count: > 0 } mapping
			&& mapping.Values.Select(ProductIdOf).Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2;

	/// <summary>
	/// The error for <paramref name="subject"/> without a product label, for example <c>PR https://github.com/elastic/kibana/pull/1</c>.
	/// It lists the labels of <c>pivot.products</c>, as the PR evaluation does.
	/// </summary>
	public static string MissingProductLabelMessage(string subject, ChangelogConfiguration config) =>
		$"No matching product label found on {subject}. Add one of the following labels: {string.Join(", ", config.LabelToProducts?.Keys ?? [])}.";

	private static string ProductIdOf(string spec) =>
		Normalize(spec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault());

	private static string Normalize(string? productId) => (productId ?? string.Empty).Replace('_', '-');
}
