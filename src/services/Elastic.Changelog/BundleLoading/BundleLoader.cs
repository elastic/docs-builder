// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Bundling;
using Elastic.Changelog.Configuration;
using Elastic.Changelog.Serialization;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Changelog;
using YamlDotNet.Core;

namespace Elastic.Changelog.BundleLoading;

/// <summary>
/// Service for loading, resolving, filtering, and merging changelog bundles.
/// </summary>
public class BundleLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads all changelog bundles from a folder.
	/// </summary>
	/// <param name="bundlesFolderPath">The absolute path to the bundles folder.</param>
	/// <param name="emitWarning">Callback to emit warnings during loading.</param>
	/// <returns>A list of successfully loaded bundles.</returns>
	public IReadOnlyList<LoadedBundle> LoadBundles(
		string bundlesFolderPath,
		Action<string> emitWarning)
	{
		var yamlFiles = fileSystem.Directory
			.EnumerateFiles(bundlesFolderPath, "*.yaml")
			.Concat(fileSystem.Directory.EnumerateFiles(bundlesFolderPath, "*.yml"))
			.ToList();

		var loadedBundles = new List<LoadedBundle>();

		foreach (var bundleFile in yamlFiles)
		{
			var bundleData = LoadBundle(bundleFile, emitWarning);
			if (bundleData == null)
				continue;

			var version = GetVersionFromBundle(bundleData) ?? fileSystem.Path.GetFileNameWithoutExtension(bundleFile);
			var repo = bundleData.Products.Count > 0
				? bundleData.Products[0].ProductId
				: "elastic";

			// Bundle directory is the directory containing the bundle file
			var bundleDirectory = fileSystem.Path.GetDirectoryName(bundleFile) ?? bundlesFolderPath;
			// Default changelog directory is parent of bundles folder
			var changelogDirectory = fileSystem.Path.GetDirectoryName(bundleDirectory) ?? bundlesFolderPath;

			var entries = ResolveEntries(bundleData, changelogDirectory, emitWarning);

			loadedBundles.Add(new LoadedBundle(version, repo, bundleData, bundleFile, entries));
		}

		return loadedBundles;
	}

	/// <summary>
	/// Resolves entries from a bundle, loading from file references if needed.
	/// </summary>
	/// <param name="bundledData">The parsed bundle data.</param>
	/// <param name="changelogDirectory">The changelog directory (parent of bundles folder).</param>
	/// <param name="emitWarning">Callback to emit warnings during resolution.</param>
	/// <returns>A list of resolved changelog entries.</returns>
	public List<ChangelogEntry> ResolveEntries(
		Bundle bundledData,
		string changelogDirectory,
		Action<string> emitWarning)
	{
		var entries = new List<ChangelogEntry>();

		foreach (var entry in bundledData.Entries)
		{
			ChangelogEntry? entryData = null;

			// If entry has resolved/inline data, use it directly
			if (!string.IsNullOrWhiteSpace(entry.Title) && entry.Type != null)
				entryData = ChangelogYamlSerialization.ConvertBundledEntry(entry);
			else if (!string.IsNullOrWhiteSpace(entry.File?.Name))
			{
				// Load from file reference - look in changelog directory (parent of bundles)
				var filePath = fileSystem.Path.Combine(changelogDirectory, entry.File.Name);

				if (!fileSystem.File.Exists(filePath))
				{
					emitWarning($"Referenced changelog file '{entry.File.Name}' not found at '{filePath}'.");
					continue;
				}

				try
				{
					var fileContent = fileSystem.File.ReadAllText(filePath);

					// Skip comment lines and normalize version to target
					var yamlLines = fileContent.Split('\n');
					var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));
					// Reuse the regex from ChangelogBundlingService
					var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

					entryData = ChangelogYamlSerialization.DeserializeEntry(normalizedYaml);
				}
				catch (YamlException e)
				{
					emitWarning($"Failed to parse changelog file '{entry.File.Name}': {e.Message}");
					continue;
				}
			}

			if (entryData != null)
				entries.Add(entryData);
		}

		return entries;
	}

	/// <summary>
	/// Filters entries based on publish blocker configuration and feature IDs to hide.
	/// Uses PublishBlockerExtensions.ShouldBlock() for publish blocker filtering.
	/// </summary>
	/// <param name="entries">The entries to filter.</param>
	/// <param name="publishBlocker">Optional publish blocker configuration.</param>
	/// <param name="featureIdsToHide">Set of feature IDs to hide.</param>
	/// <returns>Filtered list of entries.</returns>
	public IReadOnlyList<ChangelogEntry> FilterEntries(
		IReadOnlyList<ChangelogEntry> entries,
		PublishBlocker? publishBlocker,
		HashSet<string> featureIdsToHide)
	{
		var hasPublishBlocker = publishBlocker is { HasBlockingRules: true };
		var hasFeaturesToHide = featureIdsToHide.Count > 0;

		if (!hasPublishBlocker && !hasFeaturesToHide)
			return entries;

		return entries.Where(e => !ShouldHideEntry(e, publishBlocker, featureIdsToHide)).ToList();
	}

	/// <summary>
	/// Merges bundles that share the same target version/date into a single bundle.
	/// </summary>
	/// <param name="bundles">The sorted list of bundles to merge.</param>
	/// <returns>A list of bundles where same-target bundles are merged.</returns>
	public IReadOnlyList<LoadedBundle> MergeBundlesByTarget(IReadOnlyList<LoadedBundle> bundles)
	{
		if (bundles.Count <= 1)
			return bundles;

		return bundles
			.GroupBy(b => b.Version)
			.Select(MergeBundleGroup)
			.OrderByDescending(b => VersionOrDate.Parse(b.Version))
			.ToList();
	}

	/// <summary>
	/// Loads a single bundle from a file.
	/// </summary>
	private Bundle? LoadBundle(string filePath, Action<string> emitWarning)
	{
		try
		{
			var bundleContent = fileSystem.File.ReadAllText(filePath);
			return ChangelogYamlSerialization.DeserializeBundle(bundleContent);
		}
		catch (YamlException e)
		{
			var fileName = fileSystem.Path.GetFileName(filePath);
			emitWarning($"Failed to parse changelog bundle '{fileName}': {e.Message}");
			return null;
		}
	}

	/// <summary>
	/// Gets the version from a bundle's first product.
	/// </summary>
	private static string? GetVersionFromBundle(Bundle bundledData) =>
		bundledData.Products.Count > 0 ? bundledData.Products[0].Target : null;

	/// <summary>
	/// Merges a group of bundles with the same target version into a single bundle.
	/// </summary>
	private static LoadedBundle MergeBundleGroup(IGrouping<string, LoadedBundle> group)
	{
		var bundlesList = group.ToList();

		if (bundlesList.Count == 1)
			return bundlesList[0];

		// Merge entries from all bundles
		var mergedEntries = bundlesList.SelectMany(b => b.Entries).ToList();

		// Combine repo names from all contributing bundles
		var combinedRepo = string.Join("+", bundlesList.Select(b => b.Repo).Distinct().OrderBy(r => r));

		// Use the first bundle's metadata as the base
		var first = bundlesList[0];

		return new LoadedBundle(
			first.Version,
			combinedRepo,
			first.Data,
			first.FilePath,
			mergedEntries
		);
	}

	/// <summary>
	/// Determines if an entry should be hidden based on feature IDs or publish blocker configuration.
	/// </summary>
	private static bool ShouldHideEntry(
		ChangelogEntry entry,
		PublishBlocker? publishBlocker,
		HashSet<string> featureIdsToHide)
	{
		// Check feature IDs first
		if (!string.IsNullOrWhiteSpace(entry.FeatureId) && featureIdsToHide.Contains(entry.FeatureId))
			return true;

		// Check publish blocker
		return publishBlocker?.ShouldBlock(entry) == true;
	}
}
