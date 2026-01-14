// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Services.Changelog;

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
	/// </summary>
	public static BundleInput? Parse(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return null;

		// Split by pipe to get parts (comma is auto-split by ConsoleAppFramework)
		var parts = input.Split('|', StringSplitOptions.TrimEntries);

		if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
			return null;

		var bundleInput = new BundleInput
		{
			BundleFile = parts[0]
		};

		// Directory is optional (second part)
		if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
		{
			bundleInput.Directory = parts[1];
		}

		// Repo is optional (third part)
		if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]))
		{
			bundleInput.Repo = parts[2];
		}

		// Link visibility is optional (fourth part) - "hide-links" or "keep-links"
		if (parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]))
		{
			bundleInput.HideLinks = parts[3].Equals("hide-links", StringComparison.OrdinalIgnoreCase);
		}

		return bundleInput;
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
}

