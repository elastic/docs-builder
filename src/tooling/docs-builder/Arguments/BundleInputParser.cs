// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Changelog.Rendering;

namespace Documentation.Builder.Arguments;

/// <summary>
/// Utility class for parsing bundle input format: "bundle-file-path|changelog-file-path|repo|link-visibility"
/// Uses pipe (|) as delimiter since ConsoleAppFramework auto-splits string[] by comma.
/// Only bundle-file-path is required.
/// </summary>
public static class BundleInputParser
{
	/// <summary>
	/// Parses a single input string into a BundleInput object.
	/// Format: "bundle-file-path|changelog-file-path|repo|link-visibility" (only bundle-file-path is required)
	/// Uses pipe (|) as delimiter since ConsoleAppFramework auto-splits string[] by comma.
	/// link-visibility can be "hide-links" or "keep-links" (default is keep-links if omitted).
	/// Paths support tilde (~) expansion and relative paths.
	/// </summary>
	public static BundleInput? Parse(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return null;

		// Split by pipe to get parts (comma is auto-split by ConsoleAppFramework)
		var parts = input.Split('|', StringSplitOptions.TrimEntries);

		if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
			return null;

		return new BundleInput
		{
			BundleFile = NormalizePath(parts[0]),
			Directory = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? NormalizePath(parts[1]) : null,
			Repo = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null,
			HideLinks = parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) && parts[3].Equals("hide-links", StringComparison.OrdinalIgnoreCase)
		};
	}

	/// <summary>
	/// Parses multiple input strings into a list of BundleInput objects.
	/// Each input is in format: "bundle-file-path|changelog-file-path|repo|link-visibility" (only bundle-file-path is required)
	/// Uses pipe (|) as delimiter since ConsoleAppFramework auto-splits string[] by comma.
	/// Multiple bundles can be specified by comma-separating them in a single --input option.
	/// link-visibility can be "hide-links" or "keep-links" (default is keep-links if omitted).
	/// </summary>
	public static List<BundleInput> ParseAll(string[]? inputs)
	{
		var result = new List<BundleInput>();

		if (inputs == null || inputs.Length == 0)
			return result;

		foreach (var input in inputs)
		{
			var bundleInput = Parse(input);
			if (bundleInput != null)
				result.Add(bundleInput);
		}

		return result;
	}

	/// <summary>
	/// Normalizes a file path by expanding tilde (~) to the user's home directory
	/// and converting relative paths to absolute paths.
	/// </summary>
	private static string NormalizePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return path;

		var trimmedPath = path.Trim();

		// Expand tilde to user's home directory
		if (trimmedPath.StartsWith("~/", StringComparison.Ordinal) || trimmedPath.StartsWith("~\\", StringComparison.Ordinal))
		{
			var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			var relativeFromHome = trimmedPath[2..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			// Ensure that the path segment after "~/" is treated as relative so that
			// Path.Combine does not discard the home directory if an absolute path is supplied.
			if (Path.IsPathRooted(relativeFromHome))
			{
				var root = Path.GetPathRoot(relativeFromHome);
				if (!string.IsNullOrEmpty(root) && relativeFromHome.Length > root.Length)
					relativeFromHome = relativeFromHome.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			}
			// Final safeguard: ensure the segment passed to Path.Combine is never rooted.
			if (Path.IsPathRooted(relativeFromHome))
			{
				var root = Path.GetPathRoot(relativeFromHome);
				if (!string.IsNullOrEmpty(root) && relativeFromHome.Length > root.Length)
					relativeFromHome = relativeFromHome.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				else
					relativeFromHome = relativeFromHome.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			}
			trimmedPath = Path.Combine(homeDirectory, relativeFromHome);
		}
		else if (trimmedPath == "~")
		{
			trimmedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}

		// Convert to absolute path (handles relative paths like ./file or ../file)
		return Path.GetFullPath(trimmedPath);
	}
}

