// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Service for loading, resolving, filtering, and merging changelog bundles.
/// </summary>
public class BundleLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads all changelog bundles from a folder: each file is parsed, mapped and checked against <see cref="BundleReadConstraints"/> by
	/// <see cref="ReleaseNotesLoader.LoadBundle"/>, and the bundles that were loaded (amend files already merged) are checked once against
	/// <see cref="LoadedBundleConstraints"/>. Every finding rides in the <see cref="Result{T}"/>: a bundle that cannot be parsed or an entry
	/// without inline content is an error diagnostic (the bundle or entry is skipped), a broken rule is reported at its own severity. The value is
	/// always the bundles that could be loaded, so a caller reports the diagnostics and carries on.
	/// </summary>
	/// <param name="bundlesFolderPath">The absolute path to the bundles folder.</param>
	public Result<IReadOnlyList<LoadedBundle>> LoadBundles(string bundlesFolderPath)
	{
		var yamlFiles = fileSystem
			.Directory
			.EnumerateFiles(bundlesFolderPath, "*.yaml")
			.Concat(fileSystem.Directory.EnumerateFiles(bundlesFolderPath, "*.yml"))
			.ToList();

		var diagnostics = new List<Diagnostic>();
		var loadedBundles = new List<LoadedBundle>();

		foreach (var bundleFile in yamlFiles)
		{
			var fileName = fileSystem.Path.GetFileName(bundleFile);
			var text = fileSystem.File.ReadAllText(bundleFile);

			if (LoadOne(text, bundleFile, fileName, diagnostics) is { } loaded)
				loadedBundles.Add(loaded);
		}

		return Finish(loadedBundles, diagnostics, LoadedBundleConstraints.All);
	}

	/// <summary>
	/// Loads bundles from in-memory YAML content rather than a folder, with the same checks as <see cref="LoadBundles"/>. Used by the <c>changelog</c>
	/// directive in <c>cdn:</c> mode, where bundle files are fetched over HTTP.
	/// Amend sidecars (numbered and <c>.amend-notes</c>) are still merged by file name.
	/// </summary>
	/// <param name="bundles">Bundle file name and raw YAML content pairs.</param>
	public Result<IReadOnlyList<LoadedBundle>> LoadBundlesFromContent(IReadOnlyList<(string FileName, string Content)> bundles)
	{
		var diagnostics = new List<Diagnostic>();
		var loadedBundles = new List<LoadedBundle>(bundles.Count);

		foreach (var (fileName, content) in bundles)
		{
			if (LoadOne(content, fileName, fileName, diagnostics) is { } loaded)
				loadedBundles.Add(loaded);
		}

		return Finish(loadedBundles, diagnostics, LoadedBundleReadConstraints.FromContent);
	}

	// One file: parse + map + check by the loader, then the version/repo/owner every consumer derives from it.
	private LoadedBundle? LoadOne(string text, string path, string fileName, List<Diagnostic> diagnostics)
	{
		var result = ReleaseNotesLoader.LoadBundle(text, path);
		if (result.Value is not { } bundleData)
		{
			foreach (var failure in result.Diagnostics)
				diagnostics.Add(failure with { Message = $"Failed to parse changelog bundle '{fileName}': {failure.Message}" });
			return null;
		}

		diagnostics.AddRange(result.Diagnostics);
		var version = GetVersionFromBundle(bundleData) ?? fileSystem.Path.GetFileNameWithoutExtension(path);
		var repo = GetRepoFromBundle(bundleData);
		var owner = GetOwnerFromBundle(bundleData);
		var entries = ResolveEntries(bundleData, fileName, message => diagnostics.Add(Error(fileName, message)));
		return new LoadedBundle(version, repo, owner, bundleData, path, entries);
	}

	private Result<IReadOnlyList<LoadedBundle>> Finish(
		List<LoadedBundle> loadedBundles,
		List<Diagnostic> diagnostics,
		Nullean.Entities.ConstraintSet<LoadedBundle> rules
	)
	{
		// Merge amend files with their parent bundles, then check what was loaded
		var merged = MergeAmendFiles(loadedBundles, message => diagnostics.Add(Error(string.Empty, message)));
		foreach (var loaded in merged)
			diagnostics.AddRange(Result.From(rules.Report(loaded), loaded.FilePath).Diagnostics);
		return Result.Success<IReadOnlyList<LoadedBundle>>(merged, diagnostics);
	}

	private static Diagnostic Error(string file, string message) => new() { Severity = Severity.Error, File = file, Message = message };

	/// <summary>
	/// Resolves entries from a bundle. Bundles are always self-contained: an entry without inline
	/// content (<c>title</c> + <c>type</c>) is invalid and is skipped with a warning — entry files
	/// are never read from disk.
	/// </summary>
	/// <param name="bundledData">The parsed bundle data.</param>
	/// <param name="bundleName">The bundle file name, used in diagnostics.</param>
	/// <param name="emitWarning">Callback to emit warnings during resolution.</param>
	/// <returns>A list of resolved changelog entries.</returns>
	public static List<ChangelogEntry> ResolveEntries(Bundle bundledData, string bundleName, Action<string> emitWarning)
	{
		var entries = new List<ChangelogEntry>(bundledData.Entries.Count);

		foreach (var entry in bundledData.Entries)
		{
			if (!string.IsNullOrWhiteSpace(entry.Title) && entry.Type != null)
			{
				entries.Add(ReleaseNotesSerialization.ConvertBundledEntry(entry));
				continue;
			}

			var entryName = !string.IsNullOrWhiteSpace(entry.File?.Name) ? entry.File.Name : entry.Title ?? "<unnamed>";
			emitWarning(
				$"Bundle '{bundleName}' entry '{entryName}' has no inline content (title and type are required); bundles must inline their entries. Skipping."
			);
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
	public IReadOnlyList<ChangelogEntry> FilterEntries(IReadOnlyList<ChangelogEntry> entries, PublishBlocker? publishBlocker)
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
	public static IReadOnlyList<LoadedBundle> MergeBundlesByTarget(IReadOnlyList<LoadedBundle> bundles)
	// the domain operation; a merge breaks no rule the sources did not, so there is no finding to forward
	=> LoadedBundle.MergeByTarget(bundles).Value ?? bundles;

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
		return !string.IsNullOrWhiteSpace(firstProduct.Repo) ? firstProduct.Repo : firstProduct.ProductId;
	}

	/// <summary>
	/// Gets the GitHub owner from a bundle's first product.
	/// Uses the explicit Owner field if set, otherwise falls back to "elastic".
	/// </summary>
	private static string GetOwnerFromBundle(Bundle bundledData)
	{
		if (bundledData.Products.Count == 0)
			return "elastic";

		var firstProduct = bundledData.Products[0];
		return !string.IsNullOrWhiteSpace(firstProduct.Owner) ? firstProduct.Owner : "elastic";
	}

	/// <summary>
	/// Merges amend files with their parent bundles.
	/// Numbered sidecars (<c>{baseName}.amend-{N}.yaml</c>) apply in numeric order, then the
	/// reconciler-owned <c>{baseName}.amend-notes.yaml</c> sidecar.
	/// </summary>
	/// <param name="bundles">The list of loaded bundles including amend files.</param>
	/// <param name="emitWarning">Callback to emit warnings during entry resolution.</param>
	/// <returns>A list of bundles with amend file entries merged into their parent bundles.</returns>
	private List<LoadedBundle> MergeAmendFiles(List<LoadedBundle> bundles, Action<string> emitWarning)
	{
		if (bundles.Count <= 1)
			return bundles;

		var bundlesByPath = bundles.ToDictionary(b => b.FilePath, StringComparer.OrdinalIgnoreCase);
		var amendBundles = bundles.Where(b => BundleAmendMerger.IsAmendFile(b.FilePath)).ToList();

		if (amendBundles.Count == 0)
			return bundles;

		var mergedAmendPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var mergedParents = new Dictionary<string, LoadedBundle>(StringComparer.OrdinalIgnoreCase);

		var amendsByParent = amendBundles.GroupBy(a => BundleAmendMerger.GetParentBundlePath(a.FilePath)).Where(group => group.Key != null);

		foreach (var group in amendsByParent)
		{
			var parentPath = group.Key!;
			if (!bundlesByPath.TryGetValue(parentPath, out var parentBundle))
				continue;

			var orderedAmends = group.OrderBy(a => BundleAmendMerger.GetAmendMergeOrder(a.FilePath)).ToList();
			var amends = orderedAmends.Select(
				a => new BundleAmend(a.Data, !BundleAmendMerger.IsNotesAmendFile(a.FilePath), a.FilePath)
			).ToList();

			var mergedBundleData = parentBundle.Data.ApplyAmends(amends).Value ?? parentBundle.Data;
			var resolvedEntries = ResolveEntries(mergedBundleData, fileSystem.Path.GetFileName(parentPath), emitWarning);

			mergedParents[parentPath] = new LoadedBundle(
				parentBundle.Version,
				parentBundle.Repo,
				parentBundle.Owner,
				mergedBundleData,
				parentPath,
				resolvedEntries
			);

			foreach (var amend in group)
				_ = mergedAmendPaths.Add(amend.FilePath);
		}

		return bundles
			.Where(bundle => !mergedAmendPaths.Contains(bundle.FilePath))
			.Select(bundle => mergedParents.TryGetValue(bundle.FilePath, out var mergedBundle) ? mergedBundle : bundle)
			.ToList();
	}
}
