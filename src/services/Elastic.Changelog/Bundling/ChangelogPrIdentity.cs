// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Changelog.Bundling;

/// <summary>
/// Shared identity for matching a changelog entry to a pull request: leading numeric filename
/// segments or normalized YAML <c>prs:</c> references.
/// </summary>
internal static class ChangelogPrIdentity
{
	/// <summary>
	/// Parses PR numbers from the leading dash-separated numeric segments of an entry file name
	/// (for example <c>123.yaml</c>). Leftover hyphenated names may still yield leading digits.
	/// Current multi-PR authoring writes one <c>{n}.yaml</c> per PR; after upload the scrubber
	/// adds <c>{extra}.yaml</c> markers with <c>link: {canonical}</c> rather than combined
	/// filenames. File names survive scrubbing, so this match works when <c>prs:</c> is absent
	/// from the YAML the matcher reads (including local files where an author removed <c>prs:</c>).
	/// </summary>
	public static IReadOnlyList<int> ParseLeadingPrNumbers(string fileName)
	{
		var stem = fileName;
		var extensionIndex = stem.LastIndexOf('.');
		if (extensionIndex > 0)
			stem = stem[..extensionIndex];

		var numbers = new List<int>();
		foreach (var segment in stem.Split('-'))
		{
			if (segment.Length > 0 && segment.All(char.IsAsciiDigit) && int.TryParse(segment, out var number))
				numbers.Add(number);
			else
				break;
		}

		return numbers;
	}

	/// <summary>
	/// Extracts the PR number from a value already normalized by
	/// <see cref="ChangelogBundlingService.NormalizePrForComparison"/> (<c>owner/repo#n</c>).
	/// </summary>
	public static bool TryParseNumberFromNormalized(string normalized, out int number)
	{
		number = 0;
		var hash = normalized.LastIndexOf('#');
		if (hash < 0 || hash == normalized.Length - 1)
			return false;
		return int.TryParse(normalized[(hash + 1)..], out number);
	}
}
