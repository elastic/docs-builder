// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Markdown.Helpers;

internal static partial class InterpolationRegex
{
	[GeneratedRegex(@"\{\{[^\r\n}]+?\}\}", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchSubstitutions();
}

public static class Interpolation
{
	public static bool ReplaceSubstitutions(this ReadOnlySpan<char> span, Dictionary<string, string>? properties, out string? replacement)
	{
		replacement = null;
		var substitutions = properties ?? new();
		if (substitutions.Count == 0)
			return false;

		var matchSubs = InterpolationRegex.MatchSubstitutions().EnumerateMatches(span);
		var lookup = substitutions.GetAlternateLookup<ReadOnlySpan<char>>();

		var replaced = false;
		foreach (var match in matchSubs)
		{
			if (match.Length == 0)
				continue;

			var spanMatch = span.Slice(match.Index, match.Length);
			var key = spanMatch.Trim(['{', '}']);

			if (!lookup.TryGetValue(key, out var value))
				continue;

			replacement ??= span.ToString();
			replacement = replacement.Replace(spanMatch.ToString(), value);
			replaced = true;

		}

		return replaced;
	}
}
