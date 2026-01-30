// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for loading PR filter values from files or command line
/// </summary>
public class PrFilterLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads PR filter values from the provided input.
	/// Values can be file paths, URLs, short PR format (owner/repo#number), or PR numbers.
	/// </summary>
	public async Task<PrFilterResult> LoadPrsAsync(
		IDiagnosticsCollector collector,
		string[]? prs,
		string? owner,
		string? repo,
		Cancel ctx)
	{
		var prsToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (prs is not { Length: > 0 })
		{
			return new PrFilterResult
			{
				IsValid = true,
				PrsToMatch = prsToMatch
			};
		}

		// Process PR values
		var nonExistentFiles = new List<string>();

		if (prs.Length == 1)
		{
			var result = await ProcessSingleValueAsync(collector, prs[0], prsToMatch, ctx);
			if (!result)
			{
				return new PrFilterResult
				{
					IsValid = false,
					PrsToMatch = prsToMatch
				};
			}
		}
		else
		{
			await ProcessMultipleValuesAsync(prsToMatch, nonExistentFiles, prs, ctx);

			// After processing all values, handle non-existent files
			if (nonExistentFiles.Count > 0)
			{
				// If there are no valid PRs and we have non-existent files, return error
				if (prsToMatch.Count == 0)
				{
					collector.EmitError(nonExistentFiles[0], $"File does not exist: {nonExistentFiles[0]}");
					return new PrFilterResult
					{
						IsValid = false,
						PrsToMatch = prsToMatch
					};
				}

				// Emit warnings for non-existent files since we have valid PRs
				foreach (var file in nonExistentFiles)
					collector.EmitWarning(file, $"File does not exist, skipping: {file}");
			}
		}

		// Validate that numeric-only PRs have owner/repo provided
		if (!ValidateNumericPrs(collector, prsToMatch, owner, repo))
		{
			return new PrFilterResult
			{
				IsValid = false,
				PrsToMatch = prsToMatch
			};
		}

		return new PrFilterResult
		{
			IsValid = true,
			PrsToMatch = prsToMatch
		};
	}

	private async Task<bool> ProcessSingleValueAsync(
		IDiagnosticsCollector collector,
		string singleValue,
		HashSet<string> prsToMatch,
		Cancel ctx)
	{
		// Check if it's a URL - URLs should always be treated as PRs, not file paths
		var isUrl = IsUrl(singleValue);

		if (!isUrl && fileSystem.File.Exists(singleValue))
		{
			// File exists, read PRs from it
			await ReadPrsFromFileAsync(singleValue, prsToMatch, ctx);
			return true;
		}

		if (!isUrl)
		{
			// Check for short PR format (owner/repo#number) first
			if (IsShortPrFormat(singleValue))
			{
				_ = prsToMatch.Add(singleValue);
				return true;
			}

			// Check if it looks like a file path
			if (LooksLikeFilePath(singleValue))
			{
				// File path doesn't exist
				collector.EmitError(singleValue, $"File does not exist: {singleValue}");
				return false;
			}

			// Doesn't look like a file path, treat as PR identifier
			_ = prsToMatch.Add(singleValue);
			return true;
		}

		// URL, treat as PR identifier
		_ = prsToMatch.Add(singleValue);
		return true;
	}

	private async Task ProcessMultipleValuesAsync(
		HashSet<string> prsToMatch,
		List<string> nonExistentFiles,
		string[] values,
		Cancel ctx)
	{
		foreach (var value in values)
		{
			var isUrl = IsUrl(value);

			if (!isUrl && fileSystem.File.Exists(value))
			{
				// File exists, read PRs from it
				await ReadPrsFromFileAsync(value, prsToMatch, ctx);
			}
			else if (isUrl)
			{
				// URL, treat as PR identifier
				_ = prsToMatch.Add(value);
			}
			else if (IsShortPrFormat(value))
			{
				// Short PR format (owner/repo#number)
				_ = prsToMatch.Add(value);
			}
			else if (LooksLikeFilePath(value))
			{
				// Track non-existent files to check later
				nonExistentFiles.Add(value);
			}
			else
			{
				// Doesn't look like a file path, treat as PR identifier
				_ = prsToMatch.Add(value);
			}
		}
	}

	private static bool ValidateNumericPrs(
		IDiagnosticsCollector collector,
		HashSet<string> prsToMatch,
		string? owner,
		string? repo)
	{
		var hasNumericOnlyPr = false;

		foreach (var pr in prsToMatch)
		{
			// Check if it's a URL - URLs don't need owner/repo
			if (IsUrl(pr))
				continue;

			// Check if it's in owner/repo#number format - these don't need owner/repo
			if (IsShortPrFormat(pr))
				continue;

			// If it's just a number, it needs owner/repo
			if (int.TryParse(pr, out _))
			{
				hasNumericOnlyPr = true;
				break;
			}
		}

		if (hasNumericOnlyPr && (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)))
		{
			collector.EmitError(string.Empty, "When --prs contains PR numbers (not URLs or owner/repo#number format), both --owner and --repo must be provided");
			return false;
		}

		return true;
	}

	private async Task ReadPrsFromFileAsync(string filePath, HashSet<string> prsToMatch, Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
		var prs = content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(p => !string.IsNullOrWhiteSpace(p));

		foreach (var pr in prs)
			_ = prsToMatch.Add(pr);
	}

	private static bool IsUrl(string value) =>
		value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
		value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

	private static bool IsShortPrFormat(string value)
	{
		var hashIndex = value.LastIndexOf('#');
		if (hashIndex <= 0 || hashIndex >= value.Length - 1)
			return false;

		var repoPart = value[..hashIndex];
		var prPart = value[(hashIndex + 1)..];
		var repoParts = repoPart.Split('/');

		// Check if it matches owner/repo#number format
		return repoParts.Length == 2 && int.TryParse(prPart, out _);
	}

	private bool LooksLikeFilePath(string value) =>
		value.Contains(fileSystem.Path.DirectorySeparatorChar) ||
		value.Contains(fileSystem.Path.AltDirectorySeparatorChar) ||
		fileSystem.Path.HasExtension(value);
}

/// <summary>
/// Result of loading PR filter values
/// </summary>
public record PrFilterResult
{
	public required bool IsValid { get; init; }
	public required HashSet<string> PrsToMatch { get; init; }
}
