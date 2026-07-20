// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Diagnostics;

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Loads changelog file paths for the <c>--files</c> / path-list filter.
/// Values may be changelog YAML paths or a newline-delimited path-list file.
/// </summary>
public class FileFilterLoader(IFileSystem fileSystem)
{
	/// <summary>
	/// Resolves <paramref name="files"/> into changelog YAML paths that exist on disk.
	/// </summary>
	/// <param name="baseDirectory">
	/// Optional directory used to resolve basename-only or relative paths that are not found from the current directory.
	/// </param>
	public async Task<FileFilterResult> LoadFilesAsync(
		IDiagnosticsCollector collector,
		string[]? files,
		string? baseDirectory,
		Cancel ctx)
	{
		var resolved = new List<string>();

		if (files is not { Length: > 0 })
			return new FileFilterResult { IsValid = true, FilePaths = resolved };

		foreach (var value in files)
		{
			if (string.IsNullOrWhiteSpace(value))
				continue;

			if (IsPathListFile(value))
			{
				if (!await ReadPathListFileAsync(collector, value, baseDirectory, resolved, ctx))
					return new FileFilterResult { IsValid = false, FilePaths = resolved };
				continue;
			}

			var path = ResolveChangelogPath(value, baseDirectory);
			if (path == null)
			{
				EmitMissingFileError(collector, value, "--files");
				return new FileFilterResult { IsValid = false, FilePaths = resolved };
			}

			if (!IsYamlExtension(path))
			{
				collector.EmitError(path, $"--files values must be changelog YAML paths (.yaml/.yml) or a newline-delimited path list file. Found: {value}");
				return new FileFilterResult { IsValid = false, FilePaths = resolved };
			}

			resolved.Add(path);
		}

		if (resolved.Count == 0)
		{
			collector.EmitError(string.Empty, "No changelog file paths were resolved from --files");
			return new FileFilterResult { IsValid = false, FilePaths = resolved };
		}

		return new FileFilterResult { IsValid = true, FilePaths = resolved };
	}

	/// <summary>
	/// Reads a newline-delimited path list and appends resolved changelog paths to <paramref name="resolved"/>.
	/// </summary>
	public async Task<bool> ReadPathListFileAsync(
		IDiagnosticsCollector collector,
		string listFilePath,
		string? baseDirectory,
		List<string> resolved,
		Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(listFilePath, ctx);
		var lines = content
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(l => !string.IsNullOrWhiteSpace(l))
			.ToArray();

		if (lines.Length == 0)
		{
			collector.EmitError(listFilePath, "Path list file is empty");
			return false;
		}

		foreach (var line in lines)
		{
			if (LooksLikeHttpUrl(line))
			{
				collector.EmitError(
					listFilePath,
					$"Path list file must contain changelog YAML paths (.yaml/.yml), not URLs. Found: {line}"
				);
				return false;
			}

			if (!IsYamlExtension(line))
			{
				collector.EmitError(
					listFilePath,
					$"Path list file must contain changelog YAML paths (.yaml/.yml). Found: {line}"
				);
				return false;
			}

			var path = ResolveChangelogPath(line, baseDirectory);
			if (path == null)
			{
				EmitMissingFileError(collector, line, "--files");
				return false;
			}

			resolved.Add(path);
		}

		return true;
	}

	private bool IsPathListFile(string value)
	{
		if (!fileSystem.File.Exists(value))
			return false;

		// An existing .yaml/.yml file is a changelog entry, not a path list.
		if (IsYamlExtension(value))
			return false;

		return true;
	}

	private string? ResolveChangelogPath(string value, string? baseDirectory)
	{
		if (fileSystem.File.Exists(value))
			return value;

		if (!string.IsNullOrWhiteSpace(baseDirectory))
		{
			var joined = fileSystem.Path.Join(baseDirectory, value);
			if (fileSystem.File.Exists(joined))
				return joined;
		}

		return null;
	}

	private void EmitMissingFileError(IDiagnosticsCollector collector, string file, string optionName)
	{
		var currentDir = fileSystem.Directory.GetCurrentDirectory();
		collector.EmitError(
			file,
			$"File does not exist. Current directory: {currentDir}. " +
			$"Tip: Repeat {optionName} for each file, or use comma-separated values (e.g., {optionName} \"file1.yaml,file2.yaml\"). " +
			"Paths support tilde (~) expansion and can be relative or absolute."
		);
	}

	internal static bool IsYamlExtension(string path)
	{
		var ext = Path.GetExtension(path);
		return ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase)
			|| ext.Equals(".yml", StringComparison.OrdinalIgnoreCase);
	}

	private static bool LooksLikeHttpUrl(string value) =>
		value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
		|| value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}

/// <summary>Result of loading file-path filter values.</summary>
public record FileFilterResult
{
	public required bool IsValid { get; init; }
	public required IReadOnlyList<string> FilePaths { get; init; }
}
