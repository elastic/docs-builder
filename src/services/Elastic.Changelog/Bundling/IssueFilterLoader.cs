// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Service for loading issue filter values from files or command line.
/// </summary>
public class IssueFilterLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Loads issue filter values from the provided input.
	/// Values can be file paths, URLs, short format (owner/repo#number), or issue numbers.
	/// </summary>
	public async Task<IssueFilterResult> LoadIssuesAsync(
		IDiagnosticsCollector collector,
		string[]? issues,
		string? owner,
		string? repo,
		Cancel ctx)
	{
		var issuesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (issues is not { Length: > 0 })
		{
			return new IssueFilterResult
			{
				IsValid = true,
				IssuesToMatch = issuesToMatch
			};
		}

		var nonExistentFiles = new List<string>();

		if (issues.Length == 1)
		{
			var result = await ProcessSingleValueAsync(collector, issues[0], issuesToMatch, ctx);
			if (!result)
			{
				return new IssueFilterResult
				{
					IsValid = false,
					IssuesToMatch = issuesToMatch
				};
			}
		}
		else
		{
			await ProcessMultipleValuesAsync(issuesToMatch, nonExistentFiles, issues, ctx);

			if (nonExistentFiles.Count > 0)
			{
				if (issuesToMatch.Count == 0)
				{
					collector.EmitError(nonExistentFiles[0], $"File does not exist: {nonExistentFiles[0]}");
					return new IssueFilterResult
					{
						IsValid = false,
						IssuesToMatch = issuesToMatch
					};
				}

				foreach (var file in nonExistentFiles)
					collector.EmitWarning(file, $"File does not exist, skipping: {file}");
			}
		}

		if (!ValidateNumericIssues(collector, issuesToMatch, owner, repo))
		{
			return new IssueFilterResult
			{
				IsValid = false,
				IssuesToMatch = issuesToMatch
			};
		}

		return new IssueFilterResult
		{
			IsValid = true,
			IssuesToMatch = issuesToMatch
		};
	}

	private async Task<bool> ProcessSingleValueAsync(
		IDiagnosticsCollector collector,
		string singleValue,
		HashSet<string> issuesToMatch,
		Cancel ctx)
	{
		var isUrl = IsUrl(singleValue);

		if (!isUrl && fileSystem.File.Exists(singleValue))
		{
			await ReadIssuesFromFileAsync(singleValue, issuesToMatch, ctx);
			return true;
		}

		if (!isUrl)
		{
			if (IsShortFormat(singleValue))
			{
				_ = issuesToMatch.Add(singleValue);
				return true;
			}

			if (LooksLikeFilePath(singleValue))
			{
				collector.EmitError(singleValue, $"File does not exist: {singleValue}");
				return false;
			}

			_ = issuesToMatch.Add(singleValue);
			return true;
		}

		_ = issuesToMatch.Add(singleValue);
		return true;
	}

	private async Task ProcessMultipleValuesAsync(
		HashSet<string> issuesToMatch,
		List<string> nonExistentFiles,
		string[] values,
		Cancel ctx)
	{
		foreach (var value in values)
		{
			var isUrl = IsUrl(value);

			if (!isUrl && fileSystem.File.Exists(value))
			{
				await ReadIssuesFromFileAsync(value, issuesToMatch, ctx);
			}
			else if (isUrl)
			{
				_ = issuesToMatch.Add(value);
			}
			else if (IsShortFormat(value))
			{
				_ = issuesToMatch.Add(value);
			}
			else if (LooksLikeFilePath(value))
			{
				nonExistentFiles.Add(value);
			}
			else
			{
				_ = issuesToMatch.Add(value);
			}
		}
	}

	private static bool ValidateNumericIssues(
		IDiagnosticsCollector collector,
		HashSet<string> issuesToMatch,
		string? owner,
		string? repo)
	{
		var hasNumericOnly = false;

		foreach (var issue in issuesToMatch)
		{
			if (IsUrl(issue) || IsShortFormat(issue))
				continue;

			if (int.TryParse(issue, out _))
			{
				hasNumericOnly = true;
				break;
			}
		}

		if (hasNumericOnly && (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)))
		{
			collector.EmitError(string.Empty,
				"When --issues contains issue numbers (not URLs or owner/repo#number format), both --owner and --repo must be provided");
			return false;
		}

		return true;
	}

	private async Task ReadIssuesFromFileAsync(string filePath, HashSet<string> issuesToMatch, Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
		var lines = content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(p => !string.IsNullOrWhiteSpace(p));

		foreach (var line in lines)
			_ = issuesToMatch.Add(line);
	}

	private static bool IsUrl(string value) =>
		value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
		value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

	private static bool IsShortFormat(string value)
	{
		var hashIndex = value.LastIndexOf('#');
		if (hashIndex <= 0 || hashIndex >= value.Length - 1)
			return false;

		var repoPart = value[..hashIndex];
		var numPart = value[(hashIndex + 1)..];
		var repoParts = repoPart.Split('/');

		return repoParts.Length == 2 && int.TryParse(numPart, out _);
	}

	private bool LooksLikeFilePath(string value) =>
		value.Contains(fileSystem.Path.DirectorySeparatorChar) ||
		value.Contains(fileSystem.Path.AltDirectorySeparatorChar) ||
		fileSystem.Path.HasExtension(value);
}

/// <summary>
/// Result of loading issue filter values
/// </summary>
public record IssueFilterResult
{
	public required bool IsValid { get; init; }
	public required HashSet<string> IssuesToMatch { get; init; }
}
