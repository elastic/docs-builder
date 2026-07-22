// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Service for validating changelog bundles before rendering
/// </summary>
public class BundleValidationService(ILoggerFactory logFactory, IFileSystem fileSystem)
{
	private readonly ILogger _logger = logFactory.CreateLogger<BundleValidationService>();
	/// <summary>
	/// Validates all bundles and returns validation result with loaded bundle data
	/// </summary>
	public async Task<BundleValidationResult> ValidateBundlesAsync(
		IDiagnosticsCollector collector,
		IReadOnlyCollection<BundleInput> bundles,
		Cancel ctx)
	{
		var bundleDataList = new List<ValidatedBundle>();
		var seenFileNames = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		var seenPrs = new Dictionary<string, List<string>>();

		foreach (var bundleInput in bundles)
		{
			if (!ValidateBundleInput(collector, bundleInput))
				return CreateInvalidResult(bundleDataList, seenFileNames, seenPrs);

			// Load bundle file
			var bundleContent = await fileSystem.File.ReadAllTextAsync(bundleInput.BundleFile, ctx);

			// Validate bundle structure
			Bundle? bundledData;
			try
			{
				bundledData = ReleaseNotesSerialization.DeserializeBundle(bundleContent);
			}
			catch (YamlException yamlEx)
			{
				collector.EmitError(bundleInput.BundleFile, $"Failed to deserialize bundle file: {yamlEx.Message}", yamlEx);
				return CreateInvalidResult(bundleDataList, seenFileNames, seenPrs);
			}

			// Auto-discover and merge amend files
			var amendFiles = ChangelogBundleAmendService.DiscoverAmendFiles(fileSystem, bundleInput.BundleFile);
			if (amendFiles.Count > 0)
			{
				_logger.LogInformation("Found {Count} amend file(s) for bundle {BundleFile}", amendFiles.Count, bundleInput.BundleFile);
				var mergedData = await MergeAmendFilesAsync(collector, bundledData, amendFiles, ctx);
				if (mergedData == null)
					return CreateInvalidResult(bundleDataList, seenFileNames, seenPrs);
				bundledData = mergedData;
			}

			// Validate all entries in this bundle
			var result = ValidateBundleEntries(collector, bundleInput, bundledData, seenFileNames, seenPrs);
			if (!result)
				return CreateInvalidResult(bundleDataList, seenFileNames, seenPrs);

			bundleDataList.Add(new ValidatedBundle
			{
				Data = bundledData,
				Input = bundleInput
			});
		}

		// Check for duplicate file names across bundles
		EmitDuplicateWarnings(collector, seenFileNames, seenPrs);

		return new BundleValidationResult
		{
			IsValid = collector.Errors == 0,
			Bundles = bundleDataList,
			SeenFileNames = seenFileNames,
			SeenPrs = seenPrs
		};
	}

	private async Task<Bundle?> MergeAmendFilesAsync(
		IDiagnosticsCollector collector,
		Bundle mainBundle,
		IReadOnlyList<string> amendFiles,
		Cancel ctx)
	{
		var amendBundles = new List<Bundle>();

		foreach (var amendFile in amendFiles)
		{
			try
			{
				var amendContent = await fileSystem.File.ReadAllTextAsync(amendFile, ctx);
				var amendBundle = ReleaseNotesSerialization.DeserializeBundle(amendContent);
				amendBundles.Add(amendBundle);
				_logger.LogInformation(
					"Merging amend file {AmendFile} ({AddCount} additions, {ExcludeCount} exclusions)",
					amendFile,
					amendBundle.Entries.Count,
					amendBundle.ExcludeEntries.Count);
			}
			catch (YamlException yamlEx)
			{
				collector.EmitError(amendFile, $"Failed to deserialize amend file: {yamlEx.Message}", yamlEx);
				return null;
			}
		}

		var mergedEntries = BundleAmendMerger.MergeEntries(mainBundle.Entries, amendBundles);

		return new Bundle
		{
			Products = mainBundle.Products,
			Description = mainBundle.Description,
			ReleaseDate = mainBundle.ReleaseDate,
			HideFeatures = mainBundle.HideFeatures,
			Entries = mergedEntries
		};
	}

	private bool ValidateBundleInput(IDiagnosticsCollector collector, BundleInput bundleInput)
	{
		if (string.IsNullOrWhiteSpace(bundleInput.BundleFile))
		{
			collector.EmitError(string.Empty, "Bundle file path is required for each --input");
			return false;
		}

		if (!fileSystem.File.Exists(bundleInput.BundleFile))
		{
			collector.EmitError(bundleInput.BundleFile, "Bundle file does not exist");
			return false;
		}

		return true;
	}

	private static bool ValidateBundleEntries(
		IDiagnosticsCollector collector,
		BundleInput bundleInput,
		Bundle bundledData,
		Dictionary<string, List<string>> seenFileNames,
		Dictionary<string, List<string>> seenPrs)
	{
		var fileNamesInThisBundle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var allValid = true;

		foreach (var entry in bundledData.Entries)
		{
			// Track file names for duplicate detection
			if (!string.IsNullOrWhiteSpace(entry.File?.Name))
			{
				var fileName = entry.File.Name;

				// Check for duplicates within the same bundle
				if (!fileNamesInThisBundle.Add(fileName))
					collector.EmitWarning(bundleInput.BundleFile, $"Changelog file '{fileName}' appears multiple times in the same bundle");

				// Track across bundles
				if (!seenFileNames.TryGetValue(fileName, out var bundleList))
				{
					bundleList = [];
					seenFileNames[fileName] = bundleList;
				}
				bundleList.Add(bundleInput.BundleFile);
			}

			// Continue past invalid entries so every problem in the bundle is reported in one pass.
			if (!ValidateResolvedEntry(collector, bundleInput.BundleFile, entry, seenPrs))
				allValid = false;
		}

		return allValid;
	}

	private static bool ValidateResolvedEntry(
		IDiagnosticsCollector collector,
		string bundleFile,
		BundledEntry entry,
		Dictionary<string, List<string>> seenPrs)
	{
		// Bundles are always self-contained: an entry without inline content is invalid.
		if (string.IsNullOrWhiteSpace(entry.Title) || entry.Type == null)
		{
			var entryName = !string.IsNullOrWhiteSpace(entry.File?.Name) ? entry.File.Name : entry.Title ?? "<unnamed>";
			collector.EmitError(bundleFile,
				$"Entry '{entryName}' in bundle has no inline content: title and type are required. " +
				"Re-create the bundle with 'changelog bundle'.");
			return false;
		}

		if (entry.Products == null || entry.Products.Count == 0)
		{
			collector.EmitError(bundleFile, $"Entry '{entry.Title}' in bundle is missing required field: products");
			return false;
		}

		// Track PRs for duplicate detection
		foreach (var pr in entry.Prs ?? [])
		{
			if (string.IsNullOrWhiteSpace(pr))
				continue;
			var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(pr, null, null);
			if (!seenPrs.TryGetValue(normalizedPr, out var prBundleList))
			{
				prBundleList = [];
				seenPrs[normalizedPr] = prBundleList;
			}
			prBundleList.Add(bundleFile);
		}

		return true;
	}

	private static void EmitDuplicateWarnings(
		IDiagnosticsCollector collector,
		Dictionary<string, List<string>> seenFileNames,
		Dictionary<string, List<string>> seenPrs)
	{
		// Check for duplicate file names across bundles
		foreach (var (fileName, bundleFiles) in seenFileNames.Where(kvp => kvp.Value.Count > 1))
		{
			var uniqueBundles = bundleFiles.Distinct().ToList();
			if (uniqueBundles.Count > 1)
				collector.EmitWarning(string.Empty, $"Changelog file '{fileName}' appears in multiple bundles: {string.Join(", ", uniqueBundles)}");
		}

		// Check for duplicate PRs
		foreach (var (pr, bundleFiles) in seenPrs.Where(kvp => kvp.Value.Count > 1))
		{
			var uniqueBundles = bundleFiles.Distinct().ToList();
			if (uniqueBundles.Count > 1)
				collector.EmitWarning(string.Empty, $"PR '{pr}' appears in multiple bundles: {string.Join(", ", uniqueBundles)}");
		}
	}

	private static BundleValidationResult CreateInvalidResult(
		List<ValidatedBundle> bundles,
		Dictionary<string, List<string>> seenFileNames,
		Dictionary<string, List<string>> seenPrs) =>
		new()
		{
			IsValid = false,
			Bundles = bundles,
			SeenFileNames = seenFileNames,
			SeenPrs = seenPrs
		};
}
