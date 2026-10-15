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
	/// Creates the function that decides, from the labels of a pull request, whether the routing leaves it out of the bundle.
	/// It returns the first label that maps to another related product, or <c>null</c> to keep the pull request: when no such
	/// label is present, or when a label maps it to <see cref="Product"/>. Labels compare case-insensitively, as GitHub treats them.
	/// Returns <c>null</c> when no label of <paramref name="labelToProducts"/> routes anything.
	/// </summary>
	public Func<IReadOnlyCollection<string>, string?>? CreateLabelRouter(IReadOnlyDictionary<string, string>? labelToProducts)
	{
		if (labelToProducts is not { Count: > 0 })
			return null;

		var exclude = new List<string>();
		var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (label, spec) in labelToProducts)
		{
			var productId = ProductId(spec);
			if (productId == null || !RelatedProducts.Any(related => SameProduct(related, productId)))
				continue;

			if (SameProduct(Product, productId))
				_ = keep.Add(label);
			else
				exclude.Add(label);
		}

		if (exclude.Count == 0)
			return null;

		return labels =>
		{
			var present = labels.ToHashSet(StringComparer.OrdinalIgnoreCase);
			return keep.Any(present.Contains) ? null : exclude.FirstOrDefault(present.Contains);
		};
	}

	/// <summary>The product ID of a <c>pivot.products</c> spec such as <c>cloud-serverless 2026-10 ga</c>.</summary>
	private static string? ProductId(string spec) =>
		spec.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

	private static bool SameProduct(string left, string right) =>
		string.Equals(left.Replace('_', '-'), right.Replace('_', '-'), StringComparison.OrdinalIgnoreCase);
}
