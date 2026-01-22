// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog;
using Elastic.Documentation.Changelog;
using Elastic.Documentation.Diagnostics;

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
		IReadOnlyList<ChangelogEntry> entries,
		IReadOnlyList<ProductInfo>? outputProducts,
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

		var bundledData = new BundledChangelogData
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
		IReadOnlyList<ChangelogEntry> entries,
		IReadOnlyList<ProductInfo>? outputProducts)
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
					Product = p.Product,
					Target = p.Target == "*" ? null : p.Target,
					Lifecycle = p.Lifecycle == "*" ? null : p.Lifecycle
				})
				.ToList();
		}
		else if (entries.Count > 0)
		{
			var productVersions = new HashSet<(string product, string version, string? lifecycle)>();
			foreach (var entry in entries)
			{
				foreach (var product in entry.Data.Products)
				{
					var version = product.Target ?? string.Empty;
					_ = productVersions.Add((product.Product, version, product.Lifecycle));
				}
			}

			bundledProducts = productVersions
				.OrderBy(pv => pv.product)
				.ThenBy(pv => pv.version)
				.ThenBy(pv => pv.lifecycle ?? string.Empty)
				.Select(pv => new BundledProduct
				{
					Product = pv.product,
					Target = string.IsNullOrWhiteSpace(pv.version) ? null : pv.version,
					Lifecycle = pv.lifecycle
				})
				.ToList();
		}
		else
		{
			bundledProducts = [];
		}

		// Check for products with same product ID but different versions
		var productsByProductId = bundledProducts.GroupBy(p => p.Product, StringComparer.OrdinalIgnoreCase)
			.Where(g => g.Count() > 1)
			.ToList();

		foreach (var productGroup in productsByProductId)
		{
			var targets = productGroup.Select(p =>
			{
				var target = string.IsNullOrWhiteSpace(p.Target) ? "(no target)" : p.Target;
				if (!string.IsNullOrWhiteSpace(p.Lifecycle))
					target = $"{target} {p.Lifecycle}";
				return target;
			}).ToList();
			collector.EmitWarning(string.Empty, $"Product '{productGroup.Key}' has multiple targets in bundle: {string.Join(", ", targets)}");
		}

		return bundledProducts;
	}

	private static List<BundledEntry>? BuildResolvedEntries(
		IDiagnosticsCollector collector,
		IReadOnlyList<ChangelogEntry> entries)
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
			if (data.Products.Any(product => string.IsNullOrWhiteSpace(product.Product)))
			{
				collector.EmitError(entry.FilePath, "Changelog file has product entry missing required field: product");
				return null;
			}

			resolvedEntries.Add(new BundledEntry
			{
				File = new BundledFile
				{
					Name = entry.FileName,
					Checksum = entry.Checksum
				},
				Type = data.Type.ToStringFast(true),
				Title = data.Title,
				Products = data.Products.ToList(),
				Description = data.Description,
				Impact = data.Impact,
				Action = data.Action,
				FeatureId = data.FeatureId,
				Highlight = data.Highlight,
				Subtype = data.Subtype,
				Areas = data.Areas?.ToList(),
				Pr = data.Pr,
				Issues = data.Issues?.ToList()
			});
		}

		return resolvedEntries;
	}

	private static List<BundledEntry> BuildFileOnlyEntries(IReadOnlyList<ChangelogEntry> entries) =>
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
	public BundledChangelogData? Data { get; init; }
}
