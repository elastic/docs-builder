// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.ReleaseNotes;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Service for resolving changelog entries from validated bundles.
/// Bundles are always self-contained, so entries convert directly from their inline content.
/// </summary>
public class BundleDataResolver
{
	private const string DefaultRepo = "elastic";

	/// <summary>
	/// Resolves all entries from validated bundles
	/// </summary>
	public ResolvedEntriesResult ResolveEntries(IReadOnlyList<ValidatedBundle> bundles)
	{
		var allResolvedEntries = new List<ResolvedEntry>();
		var allProducts = new HashSet<(string product, string target)>();

		foreach (var bundle in bundles)
			allResolvedEntries.AddRange(ResolveBundleEntries(bundle, allProducts));

		return new ResolvedEntriesResult
		{
			IsValid = allResolvedEntries.Count > 0,
			Entries = allResolvedEntries,
			AllProducts = allProducts
		};
	}

	private static List<ResolvedEntry> ResolveBundleEntries(
		ValidatedBundle bundle,
		HashSet<(string product, string target)> allProducts)
	{
		// Collect products from this bundle
		var bundleProductIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var product in bundle.Data.Products)
		{
			var target = product.Target ?? string.Empty;
			_ = allProducts.Add((product.ProductId, target));
			if (!string.IsNullOrWhiteSpace(product.ProductId))
				_ = bundleProductIds.Add(product.ProductId);
		}

		var repo = bundle.Input.Repo ?? DefaultRepo;
		var owner = bundle.Data.Products.Count > 0 && !string.IsNullOrWhiteSpace(bundle.Data.Products[0].Owner)
			? bundle.Data.Products[0].Owner!
			: "elastic";

		return bundle.Data.Entries
			.Select(entry => new ResolvedEntry
			{
				Entry = ReleaseNotesSerialization.ConvertBundledEntry(entry),
				Repo = repo,
				Owner = owner,
				BundleProductIds = bundleProductIds,
				HideLinks = bundle.Input.HideLinks
			})
			.ToList();
	}
}
