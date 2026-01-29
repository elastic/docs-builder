// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Serialization;
using Elastic.Documentation.ReleaseNotes;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Service for resolving changelog entries from validated bundles
/// </summary>
public class BundleDataResolver(IFileSystem fileSystem)
{
	private const string DefaultRepo = "elastic";

	/// <summary>
	/// Resolves all entries from validated bundles
	/// </summary>
	public async Task<ResolvedEntriesResult> ResolveEntriesAsync(IReadOnlyList<ValidatedBundle> bundles, Cancel ctx)
	{
		var allResolvedEntries = new List<ResolvedEntry>();
		var allProducts = new HashSet<(string product, string target)>();

		foreach (var bundle in bundles)
		{
			var resolvedFromBundle = await ResolveBundleEntriesAsync(bundle, allProducts, ctx);
			allResolvedEntries.AddRange(resolvedFromBundle);
		}

		return new ResolvedEntriesResult
		{
			IsValid = allResolvedEntries.Count > 0,
			Entries = allResolvedEntries,
			AllProducts = allProducts
		};
	}

	private async Task<List<ResolvedEntry>> ResolveBundleEntriesAsync(
		ValidatedBundle bundle,
		HashSet<(string product, string target)> allProducts,
		Cancel ctx)
	{
		var resolvedEntries = new List<ResolvedEntry>();

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

		// Resolve entries
		foreach (var entry in bundle.Data.Entries)
		{
			var entryData = await ResolveEntryAsync(entry, bundle.Directory, ctx);

			resolvedEntries.Add(new ResolvedEntry
			{
				Entry = entryData,
				Repo = repo,
				BundleProductIds = bundleProductIds,
				HideLinks = bundle.Input.HideLinks
			});
		}

		return resolvedEntries;
	}

	private async Task<ChangelogEntry> ResolveEntryAsync(
		BundledEntry entry,
		string bundleDirectory,
		Cancel ctx)
	{
		// If entry has resolved data, use Mapperly to convert
		if (!string.IsNullOrWhiteSpace(entry.Title) && entry.Type != null)
			return ChangelogMapper.ToEntry(entry);

		// Load from file (already validated to exist)
		var filePath = fileSystem.Path.Combine(bundleDirectory, entry.File!.Name);
		var fileContent = await fileSystem.File.ReadAllTextAsync(filePath, ctx);

		// Deserialize YAML (skip comment lines)
		var yamlLines = fileContent.Split('\n');
		var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

		// Normalize "version:" to "target:" in products section
		var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

		return ChangelogYamlSerialization.DeserializeEntry(normalizedYaml);
	}
}
