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
			if (!await ValidateBundleInputAsync(collector, bundleInput, ctx))
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

			// Determine directory for resolving file references
			var bundleDirectory = bundleInput.Directory ?? fileSystem.Path.GetDirectoryName(bundleInput.BundleFile) ?? fileSystem.Directory.GetCurrentDirectory();

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
			var result = await ValidateBundleEntriesAsync(collector, bundleInput, bundledData, bundleDirectory, seenFileNames, seenPrs, ctx);
			if (!result)
				return CreateInvalidResult(bundleDataList, seenFileNames, seenPrs);

			bundleDataList.Add(new ValidatedBundle
			{
				Data = bundledData,
				Input = bundleInput,
				Directory = bundleDirectory
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
		var mergedEntries = new List<BundledEntry>(mainBundle.Entries);

		foreach (var amendFile in amendFiles)
		{
			try
			{
				var amendContent = await fileSystem.File.ReadAllTextAsync(amendFile, ctx);
				var amendBundle = ReleaseNotesSerialization.DeserializeBundle(amendContent);

				_logger.LogInformation("Merging {Count} entries from amend file {AmendFile}", amendBundle.Entries.Count, amendFile);
				mergedEntries.AddRange(amendBundle.Entries);
			}
			catch (YamlException yamlEx)
			{
				collector.EmitError(amendFile, $"Failed to deserialize amend file: {yamlEx.Message}", yamlEx);
				return null;
			}
		}

		return new Bundle
		{
			Products = mainBundle.Products,
			Entries = mergedEntries
		};
	}

	private async Task<bool> ValidateBundleInputAsync(
		IDiagnosticsCollector collector,
		BundleInput bundleInput,
		Cancel ctx)
	{
		_ = ctx; // Unused but kept for consistency

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

		return await Task.FromResult(true);
	}

	private async Task<bool> ValidateBundleEntriesAsync(
		IDiagnosticsCollector collector,
		BundleInput bundleInput,
		Bundle bundledData,
		string bundleDirectory,
		Dictionary<string, List<string>> seenFileNames,
		Dictionary<string, List<string>> seenPrs,
		Cancel ctx)
	{
		var fileNamesInThisBundle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

			// If entry has resolved data, validate it
			if (!string.IsNullOrWhiteSpace(entry.Title) && entry.Type != null)
			{
				if (!ValidateResolvedEntry(collector, bundleInput.BundleFile, entry, seenPrs))
					return false;
			}
			else
			{
				// Entry only has file reference - validate file exists
				if (!await ValidateFileReferenceEntryAsync(collector, bundleInput.BundleFile, entry, bundleDirectory, seenPrs, ctx))
					return false;
			}
		}

		return true;
	}

	private static bool ValidateResolvedEntry(
		IDiagnosticsCollector collector,
		string bundleFile,
		BundledEntry entry,
		Dictionary<string, List<string>> seenPrs)
	{
		if (entry.Products == null || entry.Products.Count == 0)
		{
			collector.EmitError(bundleFile, $"Entry '{entry.Title}' in bundle is missing required field: products");
			return false;
		}

		// Track PRs for duplicate detection
		if (!string.IsNullOrWhiteSpace(entry.Pr))
		{
			var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(entry.Pr, null, null);
			if (!seenPrs.TryGetValue(normalizedPr, out var prBundleList))
			{
				prBundleList = [];
				seenPrs[normalizedPr] = prBundleList;
			}
			prBundleList.Add(bundleFile);
		}

		return true;
	}

	private async Task<bool> ValidateFileReferenceEntryAsync(
		IDiagnosticsCollector collector,
		string bundleFile,
		BundledEntry entry,
		string bundleDirectory,
		Dictionary<string, List<string>> seenPrs,
		Cancel ctx)
	{
		if (string.IsNullOrWhiteSpace(entry.File?.Name))
		{
			collector.EmitError(bundleFile, "Entry in bundle is missing required field: file.name");
			return false;
		}

		if (string.IsNullOrWhiteSpace(entry.File.Checksum))
		{
			collector.EmitError(bundleFile, $"Entry for file '{entry.File.Name}' in bundle is missing required field: file.checksum");
			return false;
		}

		var filePath = fileSystem.Path.Combine(bundleDirectory, entry.File.Name);
		if (!fileSystem.File.Exists(filePath))
		{
			collector.EmitError(bundleFile, $"Referenced changelog file '{entry.File.Name}' does not exist at path: {filePath}");
			return false;
		}

		// Validate the changelog file can be deserialized
		try
		{
			var fileContent = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
			var checksum = ChangelogBundlingService.ComputeSha1(fileContent);
			if (checksum != entry.File.Checksum)
				collector.EmitWarning(bundleFile, $"Checksum mismatch for file {entry.File.Name}. Expected {entry.File.Checksum}, got {checksum}");

			// Deserialize YAML to validate structure
			var normalizedYaml = ReleaseNotesSerialization.NormalizeYaml(fileContent);
			var entryData = ReleaseNotesSerialization.DeserializeEntry(normalizedYaml);

			// Validate required fields in changelog file
			if (string.IsNullOrWhiteSpace(entryData.Title))
			{
				collector.EmitError(filePath, "Changelog file is missing required field: title");
				return false;
			}

			// Type is an enum with a default value, so it's always valid

			if (entryData.Products == null || entryData.Products.Count == 0)
			{
				collector.EmitError(filePath, "Changelog file is missing required field: products");
				return false;
			}

			// Track PRs for duplicate detection
			if (!string.IsNullOrWhiteSpace(entryData.Pr))
			{
				var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(entryData.Pr, null, null);
				if (!seenPrs.TryGetValue(normalizedPr, out var prBundleList))
				{
					prBundleList = [];
					seenPrs[normalizedPr] = prBundleList;
				}
				prBundleList.Add(bundleFile);
			}
		}
		catch (YamlException yamlEx)
		{
			collector.EmitError(filePath, $"Failed to parse changelog file: {yamlEx.Message}", yamlEx);
			return false;
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
