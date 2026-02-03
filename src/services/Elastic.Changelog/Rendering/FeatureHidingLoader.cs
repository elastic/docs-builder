// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Rendering;

/// <summary>
/// Service for loading feature IDs that should be hidden/commented out in output
/// </summary>
public class FeatureHidingLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads feature IDs to hide from the provided input values.
	/// Values can be file paths (reads feature IDs from file, one per line) or direct feature IDs.
	/// </summary>
	public async Task<FeatureHidingResult> LoadFeatureIdsAsync(
		IDiagnosticsCollector collector,
		string[]? hideFeatures,
		Cancel ctx)
	{
		var featureIdsToHide = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (hideFeatures is not { Length: > 0 })
		{
			return new FeatureHidingResult
			{
				IsValid = true,
				FeatureIdsToHide = featureIdsToHide
			};
		}

		// If there's exactly one value, check if it's a file path
		if (hideFeatures.Length == 1)
		{
			var result = await ProcessSingleValueAsync(collector, hideFeatures[0], featureIdsToHide, ctx);
			return new FeatureHidingResult
			{
				IsValid = result,
				FeatureIdsToHide = featureIdsToHide
			};
		}

		// Multiple values - process all values first, then check for errors
		var result2 = await ProcessMultipleValuesAsync(collector, hideFeatures, featureIdsToHide, ctx);
		return new FeatureHidingResult
		{
			IsValid = result2,
			FeatureIdsToHide = featureIdsToHide
		};
	}

	private async Task<bool> ProcessSingleValueAsync(
		IDiagnosticsCollector collector,
		string singleValue,
		HashSet<string> featureIdsToHide,
		Cancel ctx)
	{
		if (fileSystem.File.Exists(singleValue))
		{
			// File exists, read feature IDs from it
			await ReadFeatureIdsFromFileAsync(singleValue, featureIdsToHide, ctx);
			return true;
		}

		// Check if it looks like a file path
		if (LooksLikeFilePath(singleValue))
		{
			// File path doesn't exist
			collector.EmitError(singleValue, $"File does not exist: {singleValue}");
			return false;
		}

		// Doesn't look like a file path, treat as feature ID
		_ = featureIdsToHide.Add(singleValue);
		return true;
	}

	private async Task<bool> ProcessMultipleValuesAsync(
		IDiagnosticsCollector collector,
		string[] values,
		HashSet<string> featureIdsToHide,
		Cancel ctx)
	{
		var nonExistentFiles = new List<string>();

		foreach (var value in values)
		{
			if (fileSystem.File.Exists(value))
			{
				// File exists, read feature IDs from it
				await ReadFeatureIdsFromFileAsync(value, featureIdsToHide, ctx);
			}
			else if (LooksLikeFilePath(value))
			{
				// Track non-existent files to check later
				nonExistentFiles.Add(value);
			}
			else
			{
				// Doesn't look like a file path, treat as feature ID
				_ = featureIdsToHide.Add(value);
			}
		}

		// Report errors for non-existent files
		if (nonExistentFiles.Count > 0)
		{
			foreach (var filePath in nonExistentFiles)
				collector.EmitError(filePath, $"File does not exist: {filePath}");
			return false;
		}

		return true;
	}

	private async Task ReadFeatureIdsFromFileAsync(
		string filePath,
		HashSet<string> featureIdsToHide,
		Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
		var featureIds = content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(f => !string.IsNullOrWhiteSpace(f));

		foreach (var featureId in featureIds)
			_ = featureIdsToHide.Add(featureId);
	}

	private bool LooksLikeFilePath(string value) =>
		value.Contains(fileSystem.Path.DirectorySeparatorChar) ||
		value.Contains(fileSystem.Path.AltDirectorySeparatorChar) ||
		fileSystem.Path.HasExtension(value);
}

/// <summary>
/// Result of loading feature IDs to hide
/// </summary>
public record FeatureHidingResult
{
	public required bool IsValid { get; init; }
	public required HashSet<string> FeatureIdsToHide { get; init; }
}
