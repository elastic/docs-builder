// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;

namespace Elastic.Documentation.OpenApiIndex;

/// <summary>
/// Builds a <see cref="RootVersionIndex"/> from the object keys in the <c>elastic-docs-openapi-specs</c>
/// bucket, keeping the highest minor published for each major. Keys are expected in the shape written by
/// <c>elastic/docs-actions/openapi/upload</c>: <c>{org}/{repo}/{version}/{fileName}</c>, where
/// <c>version</c> is either <c>main</c> or a validated <c>{major}.{minor}</c> release version.
/// </summary>
public static class VersionIndexBuilder
{
	/// <summary>
	/// Returns the index, plus any keys that did not match the expected shape. Such a key cannot have come
	/// from the version-validating uploader, so it is reported and skipped rather than failing the build.
	/// </summary>
	public static (RootVersionIndex Index, IReadOnlyList<string> InvalidKeys) Build(IEnumerable<string> keys)
	{
		Dictionary<(string Repo, string File, string Major), (string Version, int Minor)> highest = [];
		List<string> invalidKeys = [];

		foreach (var key in keys)
		{
			if (!TryParseKey(key, out var repo, out var version, out var file) || !TryParseVersion(version, out var major, out var minor))
			{
				invalidKeys.Add(key);
				continue;
			}

			if (!highest.TryGetValue((repo, file, major), out var current) || minor > current.Minor)
				highest[(repo, file, major)] = (version, minor);
		}

		// An explicit ordinal comparer at every level, so the serialized key order is locale-independent.
		// The collection expression the analyzer suggests below cannot carry that comparer.
#pragma warning disable IDE0028
		RootVersionIndex index = new(StringComparer.Ordinal);
		foreach (var ((repo, file, major), (version, _)) in highest)
		{
			if (!index.TryGetValue(repo, out var byFile))
				index[repo] = byFile = new(StringComparer.Ordinal);
			if (!byFile.TryGetValue(file, out var byMajor))
				byFile[file] = byMajor = new(StringComparer.Ordinal);
			byMajor[major] = new VersionIndexEntry { Version = version };
		}
#pragma warning restore IDE0028

		return (index, invalidKeys);
	}

	/// <summary>False for anything not shaped as exactly four non-empty segments.</summary>
	private static bool TryParseKey(string key, out string repo, out string version, out string file)
	{
		repo = version = file = "";
		var parts = key.Split('/');
		if (parts.Length != 4 || Array.Exists(parts, string.IsNullOrEmpty))
			return false;

		repo = $"{parts[0]}/{parts[1]}";
		version = parts[2];
		file = parts[3];
		return true;
	}

	/// <summary>Resolves the index key ("main", or the major number) and a sortable minor for a version segment.</summary>
	private static bool TryParseVersion(string version, out string major, out int minor)
	{
		if (version == "main")
		{
			major = "main";
			minor = 0;
			return true;
		}

		major = "";
		minor = 0;
		var dot = version.IndexOf('.');
		if (dot <= 0 || dot == version.Length - 1)
			return false;

		// NumberStyles.None, so a segment carrying a sign or surrounding whitespace cannot reach the index
		// under a key that no longer matches the text it was parsed from.
		if (
			!int.TryParse(version[..dot], NumberStyles.None, CultureInfo.InvariantCulture, out _) ||
			!int.TryParse(version[(dot + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out minor)
		)
			return false;

		major = version[..dot];
		return true;
	}
}
