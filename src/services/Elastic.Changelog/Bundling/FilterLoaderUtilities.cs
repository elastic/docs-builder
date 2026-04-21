// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Shared utilities for loading PR and issue filter values from files or command line.
/// </summary>
internal static class FilterLoaderUtilities
{
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

	private static bool LooksLikeFilePath(IFileSystem fileSystem, string value) =>
		value.Contains(fileSystem.Path.DirectorySeparatorChar) ||
		value.Contains(fileSystem.Path.AltDirectorySeparatorChar) ||
		fileSystem.Path.HasExtension(value);

	private static async Task<bool> ReadUrlsFromFileAsync(
		IFileSystem fileSystem, IDiagnosticsCollector collector, string filePath,
		HashSet<string> valuesToMatch, string exampleUrlSegment, Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(filePath, ctx);
		var lines = content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(p => !string.IsNullOrWhiteSpace(p));

		foreach (var line in lines)
		{
			if (!IsUrl(line))
			{
				collector.EmitError(
					filePath,
					$"File must contain fully-qualified GitHub URLs (e.g. https://github.com/owner/repo/{exampleUrlSegment}). " +
					$"Numbers and short forms are not allowed. Found: {line}"
				);
				return false;
			}
			_ = valuesToMatch.Add(line);
		}
		return true;
	}

	private static async Task<bool> ProcessSingleValueAsync(
		IFileSystem fileSystem, IDiagnosticsCollector collector, string singleValue,
		HashSet<string> valuesToMatch, string exampleUrlSegment, Cancel ctx)
	{
		var isUrl = IsUrl(singleValue);

		if (!isUrl && fileSystem.File.Exists(singleValue))
			return await ReadUrlsFromFileAsync(fileSystem, collector, singleValue, valuesToMatch, exampleUrlSegment, ctx);

		if (!isUrl)
		{
			if (IsShortFormat(singleValue))
			{
				_ = valuesToMatch.Add(singleValue);
				return true;
			}

			if (LooksLikeFilePath(fileSystem, singleValue))
			{
				collector.EmitError(singleValue, $"File does not exist: {singleValue}");
				return false;
			}

			_ = valuesToMatch.Add(singleValue);
			return true;
		}

		_ = valuesToMatch.Add(singleValue);
		return true;
	}

	private static async Task<bool> ProcessMultipleValuesAsync(
		IFileSystem fileSystem, IDiagnosticsCollector collector, HashSet<string> valuesToMatch,
		List<string> nonExistentFiles, string[] values, string exampleUrlSegment, Cancel ctx)
	{
		foreach (var value in values)
		{
			var isUrl = IsUrl(value);

			if (!isUrl && fileSystem.File.Exists(value))
			{
				if (!await ReadUrlsFromFileAsync(fileSystem, collector, value, valuesToMatch, exampleUrlSegment, ctx))
					return false;
			}
			else if (isUrl)
			{
				_ = valuesToMatch.Add(value);
			}
			else if (IsShortFormat(value))
			{
				_ = valuesToMatch.Add(value);
			}
			else if (LooksLikeFilePath(fileSystem, value))
			{
				nonExistentFiles.Add(value);
			}
			else
			{
				_ = valuesToMatch.Add(value);
			}
		}
		return true;
	}

	private static bool ValidateNumericValues(
		IDiagnosticsCollector collector, HashSet<string> valuesToMatch,
		string? owner, string? repo, string numericValidationMessage)
	{
		var hasNumericOnly = valuesToMatch
			.Where(v => !IsUrl(v) && !IsShortFormat(v))
			.Any(v => int.TryParse(v, out _));

		if (hasNumericOnly && (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo)))
		{
			collector.EmitError(string.Empty, numericValidationMessage);
			return false;
		}

		return true;
	}

	public static async Task<(bool IsValid, HashSet<string> Matches)> LoadValuesAsync(
		IFileSystem fileSystem, IDiagnosticsCollector collector, string[]? values,
		string? owner, string? repo, string exampleUrlSegment, string numericValidationMessage, Cancel ctx)
	{
		var valuesToMatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (values is not { Length: > 0 })
			return (true, valuesToMatch);

		var nonExistentFiles = new List<string>();

		if (values.Length == 1)
		{
			if (!await ProcessSingleValueAsync(fileSystem, collector, values[0], valuesToMatch, exampleUrlSegment, ctx))
				return (false, valuesToMatch);
		}
		else
		{
			if (!await ProcessMultipleValuesAsync(fileSystem, collector, valuesToMatch, nonExistentFiles, values, exampleUrlSegment, ctx))
				return (false, valuesToMatch);

			if (nonExistentFiles.Count > 0)
			{
				if (valuesToMatch.Count == 0)
				{
					collector.EmitError(nonExistentFiles[0], $"File does not exist: {nonExistentFiles[0]}");
					return (false, valuesToMatch);
				}

				foreach (var file in nonExistentFiles)
					collector.EmitWarning(file, $"File does not exist, skipping: {file}");
			}
		}

		if (!ValidateNumericValues(collector, valuesToMatch, owner, repo, numericValidationMessage))
			return (false, valuesToMatch);

		return (true, valuesToMatch);
	}
}
