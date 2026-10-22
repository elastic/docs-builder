// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Creation;
using Elastic.Documentation.Configuration.Changelog;

namespace Elastic.Changelog.Evaluation;

public enum ProductAssignment
{
	/// <summary>No product was assigned: either nothing is mapped by label, or a product label is missing.</summary>
	None,

	/// <summary>A label on the PR maps to the products (plus the products they imply).</summary>
	FromLabels,

	/// <summary>No label matched, but <c>pivot.products</c> maps a single distinct product, so it is assigned without a label.</summary>
	SingleConfigured
}

/// <summary>The type and products that a PR's labels resolve to, and what is missing for a valid changelog entry.</summary>
/// <param name="Type">The type that the single matching type label maps to; null when none matched or the match is ambiguous.</param>
/// <param name="AmbiguousTypeLabels">Comma-separated type labels when more than one matched; null otherwise.</param>
/// <param name="Products">The formatted product specs, or null when no product was resolved.</param>
/// <param name="ProductAssignment">How <paramref name="Products"/> was resolved.</param>
/// <param name="ProductLabelTable">The product label table to show when a product label could be added; null otherwise.</param>
public sealed record PrLabelResolution(
	string? Type,
	string? AmbiguousTypeLabels,
	string? Products,
	ProductAssignment ProductAssignment,
	string? ProductLabelTable
)
{
	public bool IsAmbiguousType => AmbiguousTypeLabels != null;
	public bool MissingType => Type == null && AmbiguousTypeLabels == null;
}

/// <summary>
/// Resolves the type and products of a PR from its labels. Shared by <c>validate-labels</c> and <c>evaluate-pr</c>
/// so that both gates reach the same verdict.
/// </summary>
public static class PrLabelResolver
{
	public static PrLabelResolution Resolve(string[] labels, ChangelogConfiguration config)
	{
		var (type, ambiguous) = ResolveType(labels, config);
		var (products, assignment, productLabelTable) = ResolveProducts(labels, config);
		return new PrLabelResolution(type, ambiguous, products, assignment, productLabelTable);
	}

	// Detects multiple matching labels before picking one.
	private static (string? Type, string? Ambiguous) ResolveType(string[] labels, ChangelogConfiguration config)
	{
		if (config.LabelToType is not { Count: > 0 })
			return (null, null);

		var matching = PrInfoProcessor.MatchingTypeLabels(labels, config.LabelToType);
		if (matching.Count > 1)
			return (null, string.Join(",", matching));

		return (matching.Count == 1 ? config.LabelToType[matching[0]] : null, null);
	}

	private static (string? Products, ProductAssignment Assignment, string? LabelTable) ResolveProducts(
		string[] labels,
		ChangelogConfiguration config
	)
	{
		if (config.LabelToProducts is not { Count: > 0 } labelToProducts)
			return (null, ProductAssignment.None, null);

		var products = ProductResolution.FromLabels(labels, config);
		if (products.Count > 0)
			return (ProductArgument.FormatProductSpecs(products), ProductAssignment.FromLabels, null);

		// When only one distinct product is configured, assigning it implicitly is
		// unambiguous — no point requiring contributors to add a redundant label.
		var distinctSpecs = labelToProducts.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (distinctSpecs.Count == 1)
			return (ProductArgument.FormatProductSpecs(
				ProductArgument.ParseProductSpecs(distinctSpecs[0])
			), ProductAssignment.SingleConfigured, null);

		return (null, ProductAssignment.None, ChangelogTableRenderers.BuildProductLabelTable(labelToProducts));
	}
}
