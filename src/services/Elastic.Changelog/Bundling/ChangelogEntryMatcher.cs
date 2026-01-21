// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Changelog;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for parsing and filtering changelog entries
/// </summary>
public class ChangelogEntryMatcher(IFileSystem fileSystem, IDeserializer deserializer, ILogger logger)
{
	/// <summary>
	/// Parses and filters changelog files based on the provided filter criteria.
	/// </summary>
	public async Task<ChangelogMatchResult> MatchChangelogsAsync(
		IDiagnosticsCollector collector,
		IReadOnlyList<string> yamlFiles,
		ChangelogFilterCriteria criteria,
		Cancel ctx)
	{
		var changelogEntries = new List<ChangelogEntry>();
		var matchedPrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seenChangelogs = new HashSet<string>(); // For deduplication (using checksum)

		foreach (var filePath in yamlFiles)
		{
			var entry = await ProcessFileAsync(collector, filePath, criteria, seenChangelogs, matchedPrs, ctx);
			if (entry != null)
				changelogEntries.Add(entry);
		}

		// Warn about unmatched PRs if filtering by PRs
		if (criteria.PrsToMatch.Count > 0)
		{
			var unmatchedPrs = criteria.PrsToMatch.Where(pr => !matchedPrs.Contains(pr)).ToList();
			foreach (var unmatchedPr in unmatchedPrs)
				collector.EmitWarning(string.Empty, $"No changelog file found for PR: {unmatchedPr}");
		}

		return new ChangelogMatchResult
		{
			Entries = changelogEntries,
			MatchedPrs = matchedPrs
		};
	}

	private async Task<ChangelogEntry?> ProcessFileAsync(
		IDiagnosticsCollector collector,
		string filePath,
		ChangelogFilterCriteria criteria,
		HashSet<string> seenChangelogs,
		HashSet<string> matchedPrs,
		Cancel ctx)
	{
		try
		{
			var fileName = fileSystem.Path.GetFileName(filePath);
			var fileContent = await fileSystem.File.ReadAllTextAsync(filePath, ctx);

			// Compute checksum (SHA1)
			var checksum = ChangelogBundlingService.ComputeSha1(fileContent);

			// Deserialize YAML (skip comment lines)
			var yamlLines = fileContent.Split('\n');
			var yamlWithoutComments = string.Join('\n', yamlLines.Where(line => !line.TrimStart().StartsWith('#')));

			// Normalize "version:" to "target:" in products section for compatibility
			var normalizedYaml = ChangelogBundlingService.VersionToTargetRegex().Replace(yamlWithoutComments, "$1target:");

			var data = deserializer.Deserialize<ChangelogData>(normalizedYaml);

			if (data == null)
			{
				logger.LogWarning("Skipping file {FileName}: failed to deserialize", fileName);
				return null;
			}

			// Check for duplicates (using checksum)
			if (seenChangelogs.Contains(checksum))
			{
				logger.LogDebug("Skipping duplicate changelog: {FileName} (checksum: {Checksum})", fileName, checksum);
				return null;
			}

			// Apply filters
			if (!MatchesFilter(data, criteria, matchedPrs))
				return null;

			// Add to seen set
			_ = seenChangelogs.Add(checksum);

			return new ChangelogEntry
			{
				Data = data,
				FilePath = filePath,
				FileName = fileName,
				Checksum = checksum
			};
		}
		catch (YamlException ex)
		{
			logger.LogWarning(ex, "Failed to parse YAML file {FilePath}", filePath);
			collector.EmitError(filePath, $"Failed to parse YAML: {ex.Message}");
			return null;
		}
		catch (Exception ex) when (ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException))
		{
			logger.LogWarning(ex, "Error processing file {FilePath}", filePath);
			collector.EmitError(filePath, $"Error processing file: {ex.Message}");
			return null;
		}
	}

	private static bool MatchesFilter(
		ChangelogData data,
		ChangelogFilterCriteria criteria,
		HashSet<string> matchedPrs)
	{
		if (criteria.IncludeAll)
			return true;

		if (criteria.ProductFilters.Count > 0)
			return MatchesProductFilter(data, criteria.ProductFilters);

		if (criteria.PrsToMatch.Count > 0)
			return MatchesPrFilter(data, criteria, matchedPrs);

		return true;
	}

	private static bool MatchesProductFilter(
		ChangelogData data,
		IReadOnlyList<ProductFilter> productFilters)
	{
		foreach (var filter in productFilters)
		{
			// Check if any product in the changelog matches this filter
			foreach (var changelogProduct in data.Products)
			{
				var productMatches = MatchesPattern(changelogProduct.Product, filter.ProductPattern);
				var targetMatches = MatchesPattern(changelogProduct.Target, filter.TargetPattern);
				var lifecycleMatches = MatchesPattern(changelogProduct.Lifecycle, filter.LifecyclePattern);

				if (productMatches && targetMatches && lifecycleMatches)
					return true;
			}
		}

		return false;
	}

	private static bool MatchesPrFilter(
		ChangelogData data,
		ChangelogFilterCriteria criteria,
		HashSet<string> matchedPrs)
	{
		if (string.IsNullOrWhiteSpace(data.Pr))
			return false;

		// Normalize PR for comparison
		var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(data.Pr, criteria.DefaultOwner, criteria.DefaultRepo);
		foreach (var pr in criteria.PrsToMatch)
		{
			var normalizedPrToMatch = ChangelogBundlingService.NormalizePrForComparison(pr, criteria.DefaultOwner, criteria.DefaultRepo);
			if (normalizedPr == normalizedPrToMatch)
			{
				_ = matchedPrs.Add(pr);
				return true;
			}
		}

		return false;
	}

	private static bool MatchesPattern(string? value, string? pattern)
	{
		if (pattern == null)
			return true; // Wildcard matches anything (including null/empty)

		if (value == null)
			return false; // Non-wildcard pattern doesn't match null

		// If pattern ends with *, do prefix match
		if (pattern.EndsWith('*'))
		{
			var prefix = pattern[..^1];
			return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}

		// Exact match (case-insensitive)
		return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);
	}
}

/// <summary>
/// Filter criteria for matching changelog entries
/// </summary>
public record ChangelogFilterCriteria
{
	public required bool IncludeAll { get; init; }
	public required IReadOnlyList<ProductFilter> ProductFilters { get; init; }
	public required HashSet<string> PrsToMatch { get; init; }
	public string? DefaultOwner { get; init; }
	public string? DefaultRepo { get; init; }
}

/// <summary>
/// Product filter with wildcard support
/// </summary>
public record ProductFilter
{
	public string? ProductPattern { get; init; }
	public string? TargetPattern { get; init; }
	public string? LifecyclePattern { get; init; }
}

/// <summary>
/// A matched changelog entry
/// </summary>
public record ChangelogEntry
{
	public required ChangelogData Data { get; init; }
	public required string FilePath { get; init; }
	public required string FileName { get; init; }
	public required string Checksum { get; init; }
}

/// <summary>
/// Result of matching changelog entries
/// </summary>
public record ChangelogMatchResult
{
	public required IReadOnlyList<ChangelogEntry> Entries { get; init; }
	public required HashSet<string> MatchedPrs { get; init; }
}
