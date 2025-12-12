// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.Documentation.Services.Changelog;

namespace Documentation.Builder.Arguments;

/// <summary>
/// Parser for bundle input format: "bundle-file-path, changelog-directory, repo"
/// Only bundle-file-path is required.
/// Can be specified multiple times.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class BundleInputParserAttribute : Attribute, IArgumentParser<List<BundleInput>>
{
	public static bool TryParse(ReadOnlySpan<char> s, out List<BundleInput> result)
	{
		result = [];

		// Split by comma to get parts
		var parts = s.ToString().Split(',', StringSplitOptions.TrimEntries);

		if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
		{
			return false;
		}

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

		result.Add(bundleInput);
		return true;
	}
}

