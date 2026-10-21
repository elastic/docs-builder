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
	/// <param name="repo">GitHub repository for link generation.</param>
	/// <param name="owner">GitHub owner for link generation.</param>
	/// <param name="hideFeatures">Optional feature IDs to mark as hidden in the bundle.</param>
	public BundleBuildResult BuildBundle(
		IDiagnosticsCollector collector,
		IReadOnlyList<MatchedChangelogFile> entries,
		IReadOnlyList<ProductArgument>? outputProducts,
		string? repo = null,
		string? owner = null,
		HashSet<string>? hideFeatures = null
	)
	{
		var sources = ResolveSources(collector, entries);
		if (sources == null)
			return new BundleBuildResult { IsValid = false, Data = null };

		var assembled = Bundle.Assemble(
			sources,
			outputProducts?.Select(p => new BundleProductSpec(p.Product, p.Target, p.Lifecycle)).ToList(),
			repo,
			owner,
			hideFeatures
		);

		// every finding is forwarded as the collector received it before (a product with several targets is a warning)
		return new BundleBuildResult { IsValid = !assembled.HasErrors, Data = assembled.EmitTo(collector) };
	}

	private static List<BundleSource>? ResolveSources(IDiagnosticsCollector collector, IReadOnlyList<MatchedChangelogFile> entries)
	{
		var sources = new List<BundleSource>();
		var hasInvalidEntries = false;

		foreach (var entry in entries)
		{
			if (!IsResolvedEntryValid(collector, entry))
			{
				hasInvalidEntries = true;
				continue;
			}

			sources.Add(new BundleSource(entry.FileName, entry.Checksum, entry.Data));
		}

		// Report every invalid entry in a single pass instead of aborting on the first,
		// so a release with several broken changelogs surfaces them all at once.
		return hasInvalidEntries ? null : sources;
	}

	private static bool IsResolvedEntryValid(IDiagnosticsCollector collector, MatchedChangelogFile entry)
	{
		var data = entry.Data;

		if (string.IsNullOrWhiteSpace(data.Title))
		{
			collector.EmitError(entry.FilePath, "Changelog file is missing required field: title");
			return false;
		}

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
