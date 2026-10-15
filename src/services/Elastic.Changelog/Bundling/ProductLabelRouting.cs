// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Routes the pull requests of a commit range by the products their labels map to (<c>pivot.products</c>).
/// A pull request stays out of the bundle when a label maps it to one of <see cref="RelatedProducts"/>
/// and no label maps it to <see cref="Product"/>. A pull request with no label that maps to a related
/// product stays in: its labels say nothing about it.
/// </summary>
/// <param name="Product">The product the bundle is for, for example <c>cloud-serverless</c>.</param>
/// <param name="RelatedProducts">The products that decide the routing, for example <c>cloud-serverless</c> and <c>elasticsearch</c>.
/// A label that maps to any other product, such as a team label, does not.</param>
public sealed record ProductLabelRouting(string Product, IReadOnlyList<string> RelatedProducts)
{
	/// <summary>
	/// Splits the labels of <c>pivot.products</c> into those that leave a pull request out of the bundle and those that
	/// keep it in. A label that maps to <see cref="Product"/> keeps. A label that maps to another related product
	/// excludes, unless the pull request also has a keeping label. Returns <c>null</c> when no label routes anything.
	/// </summary>
	public (IReadOnlyList<string> Exclude, IReadOnlyList<string> Keep)? DeriveLabels(IReadOnlyDictionary<string, string>? labelToProducts)
	{
		if (labelToProducts is not { Count: > 0 })
			return null;

		var exclude = new List<string>();
		var keep = new List<string>();
		foreach (var (label, spec) in labelToProducts)
		{
			var productId = ProductId(spec);
			if (productId == null || !RelatedProducts.Any(related => SameProduct(related, productId)))
				continue;

			if (SameProduct(Product, productId))
				keep.Add(label);
			else
				exclude.Add(label);
		}

		return exclude.Count == 0 ? null : (exclude, keep);
	}

	/// <summary>The product ID of a <c>pivot.products</c> spec such as <c>cloud-serverless 2026-10 ga</c>.</summary>
	private static string? ProductId(string spec) =>
		spec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

	private static bool SameProduct(string left, string right) =>
		string.Equals(left.Replace('_', '-'), right.Replace('_', '-'), StringComparison.OrdinalIgnoreCase);
}
