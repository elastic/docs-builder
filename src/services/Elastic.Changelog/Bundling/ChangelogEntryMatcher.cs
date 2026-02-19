// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using System.Linq;

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
		var changelogEntries = new List<MatchedChangelogFile>();
		var matchedPrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var matchedIssues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var seenChangelogs = new HashSet<string>();

		foreach (var filePath in yamlFiles)
		{
			var entry = await ProcessFileAsync(collector, filePath, criteria, seenChangelogs, matchedPrs, matchedIssues, ctx);
			if (entry != null)
				changelogEntries.Add(entry);
		}

		if (criteria.PrsToMatch.Count > 0)
		{
			foreach (var pr in criteria.PrsToMatch.Where(pr => !matchedPrs.Contains(pr)))
				collector.EmitWarning(string.Empty, $"No changelog file found for PR: {pr}");
		}

		if (criteria.IssuesToMatch.Count > 0)
		{
			foreach (var issue in criteria.IssuesToMatch.Where(issue => !matchedIssues.Contains(issue)))
				collector.EmitWarning(string.Empty, $"No changelog file found for issue: {issue}");
		}

		return new ChangelogMatchResult
		{
			Entries = changelogEntries,
			MatchedPrs = matchedPrs,
			MatchedIssues = matchedIssues
		};
	}

	private async Task<MatchedChangelogFile?> ProcessFileAsync(
		IDiagnosticsCollector collector,
		string filePath,
		ChangelogFilterCriteria criteria,
		HashSet<string> seenChangelogs,
		HashSet<string> matchedPrs,
		HashSet<string> matchedIssues,
		Cancel ctx)
	{
		try
		{
			var fileName = fileSystem.Path.GetFileName(filePath);
			var fileContent = await fileSystem.File.ReadAllTextAsync(filePath, ctx);

			// Compute checksum (SHA1)
			var checksum = ChangelogBundlingService.ComputeSha1(fileContent);

			// Deserialize YAML
			var normalizedYaml = ReleaseNotesSerialization.NormalizeYaml(fileContent);
			var yamlDto = deserializer.Deserialize<ChangelogEntryDto>(normalizedYaml);

			// Check for duplicates (using checksum)
			if (seenChangelogs.Contains(checksum))
			{
				logger.LogDebug("Skipping duplicate changelog: {FileName} (checksum: {Checksum})", fileName, checksum);
				return null;
			}

			if (!MatchesFilter(yamlDto, criteria, matchedPrs, matchedIssues))
				return null;

			// Add to seen set
			_ = seenChangelogs.Add(checksum);

			// Convert to domain type
			var data = ReleaseNotesSerialization.ConvertEntry(yamlDto);

			return new MatchedChangelogFile
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
		ChangelogEntryDto data,
		ChangelogFilterCriteria criteria,
		HashSet<string> matchedPrs,
		HashSet<string> matchedIssues)
	{
		if (criteria.IncludeAll)
			return true;

		if (criteria.ProductFilters.Count > 0)
			return MatchesProductFilter(data, criteria.ProductFilters);

		if (criteria.PrsToMatch.Count > 0)
			return MatchesPrFilter(data, criteria, matchedPrs);

		if (criteria.IssuesToMatch.Count > 0)
			return MatchesIssueFilter(data, criteria, matchedIssues);

		return true;
	}

	private static bool MatchesProductFilter(
		ChangelogEntryDto data,
		IReadOnlyList<ProductFilter> productFilters)
	{
		if (data.Products == null || data.Products.Count == 0)
			return false;

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
		ChangelogEntryDto data,
		ChangelogFilterCriteria criteria,
		HashSet<string> matchedPrs)
	{
		var prs = data.Prs ?? (data.Pr != null ? [data.Pr] : null);
		if (prs is not { Count: > 0 })
			return false;

		foreach (var dataPr in prs.Where(pr => !string.IsNullOrWhiteSpace(pr)))
		{
			var normalizedPr = ChangelogBundlingService.NormalizePrForComparison(dataPr, criteria.DefaultOwner, criteria.DefaultRepo);
			foreach (var pr in criteria.PrsToMatch)
			{
				var normalizedPrToMatch = ChangelogBundlingService.NormalizePrForComparison(pr, criteria.DefaultOwner, criteria.DefaultRepo);
				if (normalizedPr == normalizedPrToMatch)
				{
					_ = matchedPrs.Add(pr);
					return true;
				}
			}
		}

		return false;
	}

	private static bool MatchesIssueFilter(ChangelogEntryDto data, ChangelogFilterCriteria criteria, HashSet<string> matchedIssues)
	{
		if (data.Issues is not { Count: > 0 })
			return false;

		foreach (var dataIssue in data.Issues)
		{
			if (string.IsNullOrWhiteSpace(dataIssue))
				continue;
			var normalizedIssue = ChangelogBundlingService.NormalizeIssueForComparison(dataIssue, criteria.DefaultOwner, criteria.DefaultRepo);
			foreach (var issue in criteria.IssuesToMatch)
			{
				var normalizedIssueToMatch = ChangelogBundlingService.NormalizeIssueForComparison(issue, criteria.DefaultOwner, criteria.DefaultRepo);
				if (normalizedIssue == normalizedIssueToMatch)
				{
					_ = matchedIssues.Add(issue);
					return true;
				}
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
	public required HashSet<string> IssuesToMatch { get; init; }
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
/// A matched changelog file with parsed data and file metadata
/// </summary>
public record MatchedChangelogFile
{
	public required ChangelogEntry Data { get; init; }
	public required string FilePath { get; init; }
	public required string FileName { get; init; }
	public required string Checksum { get; init; }
}

/// <summary>
/// Result of matching changelog entries
/// </summary>
public record ChangelogMatchResult
{
	public required IReadOnlyList<MatchedChangelogFile> Entries { get; init; }
	public required HashSet<string> MatchedPrs { get; init; }
	public required HashSet<string> MatchedIssues { get; init; }
}
