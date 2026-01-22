// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Changelog;
using YamlDotNet.Serialization;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Service for resolving changelog entries from validated bundles
/// </summary>
public class BundleDataResolver(IFileSystem fileSystem, IDeserializer deserializer)
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
			_ = allProducts.Add((product.Product, target));
			if (!string.IsNullOrWhiteSpace(product.Product))
				_ = bundleProductIds.Add(product.Product);
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

	private async Task<ChangelogData> ResolveEntryAsync(
		BundledEntry entry,
		string bundleDirectory,
		Cancel ctx)
	{
		// If entry has resolved data, use it
		if (!string.IsNullOrWhiteSpace(entry.Title) && !string.IsNullOrWhiteSpace(entry.Type))
		{
			var entryType = ChangelogEntryTypeExtensions.TryParse(entry.Type, out var parsed, ignoreCase: true, allowMatchingMetadataAttribute: true)
				? parsed
				: ChangelogEntryType.Other;

			return new ChangelogData
			{
				Title = entry.Title,
				Type = entryType,
				Subtype = entry.Subtype,
				Description = entry.Description,
				Impact = entry.Impact,
				Action = entry.Action,
				FeatureId = entry.FeatureId,
				Highlight = entry.Highlight,
				Pr = entry.Pr,
				Products = entry.Products ?? [],
				Areas = entry.Areas,
				Issues = entry.Issues
			};
		}

		// Load from file (already validated to exist)
		var filePath = fileSystem.Path.Combine(bundleDirectory, entry.File!.Name);
		var fileContent = await fileSystem.File.ReadAllTextAsync(filePath, ctx);

		// Deserialize YAML (skip comment lines)
		var yamlLines = fileContent.Split('\n');
		var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

		// Normalize "version:" to "target:" in products section
		var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

		return deserializer.Deserialize<ChangelogData>(normalizedYaml);
	}
}
