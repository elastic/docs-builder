// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Documentation.ReleaseNotes;
using YamlDotNet.Core;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Service for loading, resolving, filtering, and merging changelog bundles.
/// </summary>
public partial class BundleLoader(IFileSystem fileSystem)
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
			var repo = GetRepoFromBundle(bundleData);

			// Bundle directory is the directory containing the bundle file
			var bundleDirectory = fileSystem.Path.GetDirectoryName(bundleFile) ?? bundlesFolderPath;
			// Default changelog directory is parent of bundles folder
			var changelogDirectory = fileSystem.Path.GetDirectoryName(bundleDirectory) ?? bundlesFolderPath;

			var entries = ResolveEntries(bundleData, changelogDirectory, emitWarning);

			loadedBundles.Add(new LoadedBundle(version, repo, bundleData, bundleFile, entries));
		}

		// Merge amend files with their parent bundles
		loadedBundles = MergeAmendFiles(loadedBundles);

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
				entryData = ReleaseNotesSerialization.ConvertBundledEntry(entry);
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
					var normalizedYaml = ReleaseNotesSerialization.NormalizeYaml(fileContent);
					entryData = ReleaseNotesSerialization.DeserializeEntry(normalizedYaml);
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
	/// Filters entries based on publish blocker configuration.
	/// Uses PublishBlockerExtensions.ShouldBlock() for publish blocker filtering.
	/// </summary>
	/// <param name="entries">The entries to filter.</param>
	/// <param name="publishBlocker">Optional publish blocker configuration.</param>
	/// <returns>Filtered list of entries.</returns>
	public IReadOnlyList<ChangelogEntry> FilterEntries(
		IReadOnlyList<ChangelogEntry> entries,
		PublishBlocker? publishBlocker)
	{
		if (publishBlocker is not { HasBlockingRules: true })
			return entries;

		return entries.Where(e => !publishBlocker.ShouldBlock(e)).ToList();
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
			return ReleaseNotesSerialization.DeserializeBundle(bundleContent);
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
	/// Gets the repository name from a bundle's first product.
	/// Uses the explicit Repo field if set, otherwise falls back to ProductId.
	/// </summary>
	private static string GetRepoFromBundle(Bundle bundledData)
	{
		if (bundledData.Products.Count == 0)
			return "elastic";

		var firstProduct = bundledData.Products[0];
		// Use explicit Repo if provided, otherwise fall back to ProductId
		return !string.IsNullOrWhiteSpace(firstProduct.Repo)
			? firstProduct.Repo
			: firstProduct.ProductId;
	}

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
	/// Merges amend files with their parent bundles.
	/// Amend files follow the naming pattern: {baseName}.amend-{N}.yaml
	/// </summary>
	/// <param name="bundles">The list of loaded bundles including amend files.</param>
	/// <returns>A list of bundles with amend file entries merged into their parent bundles.</returns>
	private List<LoadedBundle> MergeAmendFiles(List<LoadedBundle> bundles)
	{
		if (bundles.Count <= 1)
			return bundles;

		// Build a lookup of bundles by their file path for quick access
		var bundlesByPath = bundles.ToDictionary(b => b.FilePath, StringComparer.OrdinalIgnoreCase);

		// Identify amend files and their parent bundles
		var amendBundles = bundles.Where(b => IsAmendFile(b.FilePath)).ToList();

		if (amendBundles.Count == 0)
			return bundles;

		// Track which bundles to remove (amend files that were merged)
		var mergedAmendPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Track parent bundles with their merged entries
		var mergedParents = new Dictionary<string, LoadedBundle>(StringComparer.OrdinalIgnoreCase);

		foreach (var amendBundle in amendBundles)
		{
			var parentPath = GetParentBundlePath(amendBundle.FilePath);

			if (parentPath == null || !bundlesByPath.TryGetValue(parentPath, out var parentBundle))
				continue; // No parent found, amend file will remain standalone

			// Get or create the merged parent entry
			if (!mergedParents.TryGetValue(parentPath, out var mergedParent))
				mergedParent = parentBundle;

			// Merge the amend entries into the parent
			var combinedEntries = mergedParent.Entries.Concat(amendBundle.Entries).ToList();

			mergedParents[parentPath] = new LoadedBundle(
				mergedParent.Version,
				mergedParent.Repo,
				mergedParent.Data,
				mergedParent.FilePath,
				combinedEntries
			);

			_ = mergedAmendPaths.Add(amendBundle.FilePath);
		}

		// Build the final result: replace parent bundles with merged versions, exclude merged amend files
		var result = new List<LoadedBundle>();
		foreach (var bundle in bundles)
		{
			// Skip amend files that were merged into a parent
			if (mergedAmendPaths.Contains(bundle.FilePath))
				continue;

			// Use merged version if this is a parent that had amend files
			if (mergedParents.TryGetValue(bundle.FilePath, out var mergedBundle))
				result.Add(mergedBundle);
			else
				result.Add(bundle);
		}

		return result;
	}

	/// <summary>
	/// Determines if a file path represents an amend file.
	/// </summary>
	/// <param name="filePath">The file path to check.</param>
	/// <returns>True if the file is an amend file.</returns>
	private static bool IsAmendFile(string filePath) =>
		AmendFileRegex().IsMatch(filePath);

	/// <summary>
	/// Gets the parent bundle path from an amend file path.
	/// </summary>
	/// <param name="amendFilePath">The amend file path.</param>
	/// <returns>The parent bundle path, or null if not an amend file.</returns>
	private string? GetParentBundlePath(string amendFilePath)
	{
		var match = AmendFileRegex().Match(amendFilePath);
		if (!match.Success)
			return null;

		// Replace the ".amend-N" part with just the extension
		var directory = fileSystem.Path.GetDirectoryName(amendFilePath) ?? string.Empty;
		var fileName = fileSystem.Path.GetFileName(amendFilePath);
		var extension = fileSystem.Path.GetExtension(amendFilePath);

		// Remove the .amend-N part from the filename
		var parentFileName = AmendFileRegex().Replace(fileName, extension);

		return fileSystem.Path.Combine(directory, parentFileName);
	}

	[GeneratedRegex(@"\.amend-\d+\.ya?ml$", RegexOptions.IgnoreCase)]
	private static partial Regex AmendFileRegex();

}
