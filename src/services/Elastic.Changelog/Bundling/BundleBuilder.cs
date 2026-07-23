// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for building bundle data from matched changelog entries
/// </summary>
public class BundleBuilder
{
	/// <summary>
	/// Builds the bundled changelog data from matched entries. Entry contents are always
	/// resolved (inlined) — reference-only bundles are not a supported format.
	/// </summary>
	/// <param name="collector">The diagnostics collector.</param>
	/// <param name="entries">Matched changelog files to bundle.</param>
	/// <param name="outputProducts">Optional explicit products to set in the output.</param>
	/// <param name="repo">Optional GitHub repository name to set on products for link generation.</param>
	/// <param name="owner">Optional GitHub owner to set on products for link generation.</param>
	/// <param name="hideFeatures">Optional feature IDs to mark as hidden in the bundle.</param>
	public BundleBuildResult BuildBundle(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		IReadOnlyList<ProductArgument>? outputProducts,
		string? repo = null,
		string? owner = null,
		HashSet<string>? hideFeatures = null)
	{
		// Build products list
		var bundledProducts = BuildProducts(collector, entries, outputProducts, repo, owner);

		// Build entries list
		var bundledEntries = BuildResolvedEntries(collector, entries);

		if (bundledEntries == null)
		{
			return new BundleBuildResult
			{
				IsValid = false,
				Data = null
			};
		}

		// Guard against a bundle-level target that disagrees with the targets its own entries declare
		// for the same product (for example a release version typed as 2027-07-20 while every entry says
		// 2026-07-20). The bundle-level target drives the rendered release date, so a mismatch silently
		// publishes entries under the wrong version/date. Refuse to build the bundle in that case.
		if (!ValidateProductTargetConsistency(collector, bundledProducts, entries))
		{
			return new BundleBuildResult
			{
				IsValid = false,
				Data = null
			};
		}

		var bundledData = new Bundle
		{
			Products = bundledProducts,
			HideFeatures = hideFeatures?.Count > 0 ? hideFeatures.ToList() : [],
			Entries = bundledEntries
		};

		return new BundleBuildResult
		{
			IsValid = true,
			Data = bundledData
		};
	}

	private static List<BundledProduct> BuildProducts(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		IReadOnlyList<ProductArgument>? outputProducts,
		string? repo,
		string? owner)
	{
		List<BundledProduct> bundledProducts;

		if (outputProducts is { Count: > 0 })
		{
			bundledProducts = outputProducts
				.OrderBy(p => p.Product)
				.ThenBy(p => p.Target ?? string.Empty)
				.ThenBy(p => p.Lifecycle ?? string.Empty)
				.Select(p => new BundledProduct
				{
					ProductId = p.Product ?? "",
					Target = p.Target == "*" ? null : p.Target,
					Lifecycle = ParseLifecycle(p.Lifecycle == "*" ? null : p.Lifecycle),
					Repo = repo,
					Owner = owner
				})
				.ToList();
		}
		else if (entries.Count > 0)
		{
			var productVersions = new HashSet<(string product, string version, Lifecycle? lifecycle)>();
			foreach (var entry in entries)
			{
				if (entry.Data.Products == null)
					continue;
				foreach (var product in entry.Data.Products)
				{
					var version = product.Target ?? string.Empty;
					_ = productVersions.Add((product.ProductId, version, product.Lifecycle));
				}
			}

			bundledProducts = productVersions
				.OrderBy(pv => pv.product)
				.ThenBy(pv => pv.version)
				.ThenBy(pv => pv.lifecycle?.ToStringFast(true) ?? string.Empty)
				.Select(pv => new BundledProduct(
					pv.product,
					string.IsNullOrWhiteSpace(pv.version) ? null : pv.version,
					pv.lifecycle,
					repo,
					owner))
				.ToList();
		}
		else
			bundledProducts = [];

		// Check for products with same product ID but different versions
		var productsByProductId = bundledProducts.GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase)
			.Where(g => g.Count() > 1)
			.ToList();

		foreach (var productGroup in productsByProductId)
		{
			var targets = productGroup.Select(p =>
			{
				var target = string.IsNullOrWhiteSpace(p.Target) ? "(no target)" : p.Target;
				if (p.Lifecycle != null)
					target = $"{target} {p.Lifecycle.Value.ToStringFast(true)}";
				return target;
			}).ToList();
			collector.EmitWarning(string.Empty, $"Product '{productGroup.Key}' has multiple targets in bundle: {string.Join(", ", targets)}");
		}

		return bundledProducts;
	}

	/// <summary>
	/// Verifies that, for every bundle-level product that declares a target, each entry declaring the
	/// same product declares a compatible target. A coarser bundle target may be a component-prefix of a
	/// finer entry target (for example a monthly rollup <c>2026-05</c> covering an entry dated
	/// <c>2026-05-15</c>), but genuinely divergent targets (for example <c>2027-07-20</c> vs
	/// <c>2026-07-20</c>, or <c>9.5.0</c> vs <c>9.6.0</c>) are rejected. Entries whose product carries no
	/// target, or a product the bundle does not declare, are not compared.
	/// </summary>
	/// <returns><c>true</c> when all entry targets are consistent with the bundle-level targets.</returns>
	private static bool ValidateProductTargetConsistency(
		IDiagnosticsCollector collector,
		IReadOnlyList<BundledProduct> bundledProducts,
		IReadOnlyList<MatchedChangelogFile> entries)
	{
		// Index the bundle-level targets by product id, keeping only products that declare a target.
		var bundleTargetsByProduct = bundledProducts
			.Where(p => !string.IsNullOrWhiteSpace(p.ProductId) && !string.IsNullOrWhiteSpace(p.Target))
			.GroupBy(p => p.ProductId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.Select(p => p.Target!).ToList(), StringComparer.OrdinalIgnoreCase);

		if (bundleTargetsByProduct.Count == 0)
			return true;

		var isValid = true;

		foreach (var entry in entries)
		{
			if (entry.Data.Products == null)
				continue;

			foreach (var entryProduct in entry.Data.Products)
			{
				if (string.IsNullOrWhiteSpace(entryProduct.ProductId) || string.IsNullOrWhiteSpace(entryProduct.Target))
					continue;

				if (!bundleTargetsByProduct.TryGetValue(entryProduct.ProductId, out var bundleTargets))
					continue;

				if (bundleTargets.Any(bundleTarget => AreTargetsCompatible(bundleTarget, entryProduct.Target!)))
					continue;

				collector.EmitError(entry.FilePath,
					$"Changelog entry '{entry.FileName}' declares target '{entryProduct.Target}' for product '{entryProduct.ProductId}', " +
					$"but the bundle target for that product is '{string.Join("', '", bundleTargets)}'. " +
					"A bundle target and its entries' targets for the same product must match (a coarser bundle target may be a prefix of a finer entry target). " +
					"Check the release version passed to 'changelog bundle' / 'changelog gh-release'.");
				isValid = false;
			}
		}

		return isValid;
	}

	/// <summary>
	/// Two targets are compatible when they are equal or when one is a component-wise prefix of the other.
	/// Components are the dot- and dash-delimited parts of a version or date (for example <c>9.5.0</c> or
	/// <c>2026-07-20</c>), so <c>2026-05</c> is compatible with <c>2026-05-15</c> but <c>2027-07-20</c> is
	/// not compatible with <c>2026-07-20</c>.
	/// </summary>
	private static bool AreTargetsCompatible(string a, string b)
	{
		if (string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase))
			return true;

		var componentsA = SplitTargetComponents(a);
		var componentsB = SplitTargetComponents(b);
		var shared = Math.Min(componentsA.Length, componentsB.Length);

		if (shared == 0)
			return false;

		for (var i = 0; i < shared; i++)
		{
			if (!string.Equals(componentsA[i], componentsB[i], StringComparison.OrdinalIgnoreCase))
				return false;
		}

		// Every shared leading component matched; one target is a prefix of the other.
		return true;
	}

	private static string[] SplitTargetComponents(string target) =>
		target.Split(['.', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

	private static Lifecycle? ParseLifecycle(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return null;

		return LifecycleExtensions.TryParse(value, out var result, ignoreCase: true, allowMatchingMetadataAttribute: true)
			? result
			: null;
	}

	private static List<BundledEntry>? BuildResolvedEntries(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries)
	{
		var resolvedEntries = new List<BundledEntry>();
		var hasInvalidEntries = false;

		foreach (var entry in entries)
		{
			if (!IsResolvedEntryValid(collector, entry))
			{
				hasInvalidEntries = true;
				continue;
			}

			var bundledEntry = entry.Data.ToBundledEntry() with
			{
				File = new BundledFile
				{
					Name = entry.FileName,
					Checksum = entry.Checksum
				}
			};
			resolvedEntries.Add(bundledEntry);
		}

		// Report every invalid entry in a single pass instead of aborting on the first,
		// so a release with several broken changelogs surfaces them all at once.
		return hasInvalidEntries ? null : resolvedEntries;
	}

	private static bool IsResolvedEntryValid(IDiagnosticsCollector collector, MatchedChangelogFile entry)
	{
		var data = entry.Data;

		if (string.IsNullOrWhiteSpace(data.Title))
		{
			collector.EmitError(entry.FilePath, "Changelog file is missing required field: title");
			return false;
		}

		// Validate type is not Invalid (missing or unrecognized)
		if (data.Type == ChangelogEntryType.Invalid)
		{
			collector.EmitError(entry.FilePath, "Changelog file is missing required field: type");
			return false;
		}

		if (data.Products == null || data.Products.Count == 0)
		{
			collector.EmitError(entry.FilePath, "Changelog file is missing required field: products");
			return false;
		}

		// Validate products have required fields
		if (data.Products.Any(product => string.IsNullOrWhiteSpace(product.ProductId)))
		{
			collector.EmitError(entry.FilePath, "Changelog file has product entry missing required field: product");
			return false;
		}

		return true;
	}
}

/// <summary>
/// Result of building bundle data
/// </summary>
public record BundleBuildResult
{
	public required bool IsValid { get; init; }
	public Bundle? Data { get; init; }
}
