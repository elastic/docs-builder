// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.Helpers;

internal static partial class InterpolationRegex
{
	[GeneratedRegex(@"\{\{[^\r\n}]+?\}\}", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchSubstitutions();
}

public static class Interpolation
{
	public static string ReplaceSubstitutions(
		this string input,
		ParserContext context
	)
	{
		var span = input.AsSpan();
		if (span.ReplaceSubstitutions([context.Substitutions, context.ContextSubstitutions], out var replacement))
			return replacement;
		return input;
	}


	public static bool ReplaceSubstitutions(
		this ReadOnlySpan<char> span,
		ParserContext context,
		[NotNullWhen(true)] out string? replacement
	) =>
		span.ReplaceSubstitutions([context.Substitutions, context.ContextSubstitutions], out replacement);

	public static bool ReplaceSubstitutions(
		this ReadOnlySpan<char> span,
		IReadOnlyDictionary<string, string>? properties,
		[NotNullWhen(true)] out string? replacement
	)
	{
		replacement = null;
		if (properties is null || properties.Count == 0)
			return false;

		if (span.IndexOf("}}") < 0)
			return false;

		return span.ReplaceSubstitutions([properties], out replacement);
	}

	public static bool ReplaceSubstitutions(
		this ReadOnlySpan<char> span,
		IReadOnlyDictionary<string, string>[] properties,
		[NotNullWhen(true)]out string? replacement
	)
	{
		replacement = null;
		if (span.IndexOf("}}") < 0)
			return false;

		if (properties.Length == 0 || properties.Sum(p => p.Count) == 0)
			return false;

		var lookups = properties
			.Select(p => p as Dictionary<string, string> ?? new Dictionary<string, string>(p, StringComparer.OrdinalIgnoreCase))
			.Select(d => d.GetAlternateLookup<ReadOnlySpan<char>>())
			.ToArray();

		var matchSubs = InterpolationRegex.MatchSubstitutions().EnumerateMatches(span);

		var replaced = false;
		foreach (var match in matchSubs)
		{
			if (match.Length == 0)
				continue;

			var spanMatch = span.Slice(match.Index, match.Length);
			var key = spanMatch.Trim(['{', '}']);
			foreach (var lookup in lookups)
			{
				if (!lookup.TryGetValue(key, out var value))
					continue;

				replacement ??= span.ToString();
				replacement = replacement.Replace(spanMatch.ToString(), value);
				replaced = true;
			}
		}

		return replaced;
	}
}
