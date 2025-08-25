// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Myst;
using Elastic.Markdown.Myst.InlineParsers.Substitution;

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
		return span.ReplaceSubstitutions([context.Substitutions, context.ContextSubstitutions], context.Build.Collector, out var replacement)
			? replacement : input;
	}

	public static bool ReplaceSubstitutions(
		this ReadOnlySpan<char> span,
		IReadOnlyDictionary<string, string>? properties,
		IDiagnosticsCollector? collector,
		[NotNullWhen(true)] out string? replacement
	)
	{
		replacement = null;
		return properties is not null && properties.Count != 0 &&
			span.IndexOf("}}") >= 0 && span.ReplaceSubstitutions([properties], collector, out replacement);
	}

	private static bool ReplaceSubstitutions(
		this ReadOnlySpan<char> span,
		IReadOnlyDictionary<string, string>[] properties,
		IDiagnosticsCollector? collector,
		[NotNullWhen(true)] out string? replacement
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
			var fullKey = spanMatch.Trim(['{', '}']);

			// Enhanced mutation support: parse key and mutations using shared utility
			var (cleanKey, mutations) = SubstitutionMutationHelper.ParseKeyWithMutations(fullKey.ToString());

			foreach (var lookup in lookups)
			{
				if (lookup.TryGetValue(cleanKey, out var value))
				{
					// Apply mutations if present using a shared utility
					if (mutations.Length > 0)
						value = SubstitutionMutationHelper.ApplyMutations(value, mutations);

					collector?.CollectUsedSubstitutionKey(cleanKey);

					replacement ??= span.ToString();
					replacement = replacement.Replace(spanMatch.ToString(), value);
					replaced = true;
				}
			}
		}

		return replaced;
	}
}
