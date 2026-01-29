// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Serialization;
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
	/// Builds the bundled changelog data from matched entries.
	/// </summary>
	public BundleBuildResult BuildBundle(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		IReadOnlyList<ProductArgument>? outputProducts,
		bool resolve)
	{
		// Build products list
		var bundledProducts = BuildProducts(collector, entries, outputProducts);

		// Build entries list
		var bundledEntries = resolve
			? BuildResolvedEntries(collector, entries)
			: BuildFileOnlyEntries(entries);

		if (bundledEntries == null)
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
		IReadOnlyList<ProductArgument>? outputProducts)
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
					Lifecycle = ParseLifecycle(p.Lifecycle == "*" ? null : p.Lifecycle)
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
					pv.lifecycle))
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

		foreach (var entry in entries)
		{
			var data = entry.Data;

			// Validate required fields
			if (string.IsNullOrWhiteSpace(data.Title))
			{
				collector.EmitError(entry.FilePath, "Changelog file is missing required field: title");
				return null;
			}

			// Validate type is not Invalid (missing or unrecognized)
			if (data.Type == ChangelogEntryType.Invalid)
			{
				collector.EmitError(entry.FilePath, "Changelog file is missing required field: type");
				return null;
			}

			if (data.Products == null || data.Products.Count == 0)
			{
				collector.EmitError(entry.FilePath, "Changelog file is missing required field: products");
				return null;
			}

			// Validate products have required fields
			if (data.Products.Any(product => string.IsNullOrWhiteSpace(product.ProductId)))
			{
				collector.EmitError(entry.FilePath, "Changelog file has product entry missing required field: product");
				return null;
			}

			var bundledEntry = ChangelogMapper.ToBundledEntry(data) with
			{
				File = new BundledFile
				{
					Name = entry.FileName,
					Checksum = entry.Checksum
				}
			};
			resolvedEntries.Add(bundledEntry);
		}

		return resolvedEntries;
	}

	private static List<BundledEntry> BuildFileOnlyEntries(IReadOnlyList<MatchedChangelogFile> entries) =>
		entries
			.Select(e => new BundledEntry
			{
				File = new BundledFile
				{
					Name = e.FileName,
					Checksum = e.Checksum
				}
			})
			.ToList();
}

/// <summary>
/// Result of building bundle data
/// </summary>
public record BundleBuildResult
{
	public required bool IsValid { get; init; }
	public Bundle? Data { get; init; }
}
