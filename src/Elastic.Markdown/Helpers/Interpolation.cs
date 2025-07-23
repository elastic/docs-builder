// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Elastic.Documentation.Diagnostics;
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

			// Handle mutation operators (same logic as SubstitutionParser)
			var components = fullKey.ToString().Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			var key = components.Length > 1 ? components[0].Trim() : fullKey.ToString();
			foreach (var lookup in lookups)
			{
				if (!lookup.TryGetValue(key, out var value))
					continue;

				collector?.CollectUsedSubstitutionKey(key);

				// Apply mutations if present
				if (components.Length > 1)
				{
					value = ApplyMutations(value, components[1..]);
				}

				replacement ??= span.ToString();
				replacement = replacement.Replace(spanMatch.ToString(), value);
				replaced = true;
			}
		}

		return replaced;
	}

	private static string ApplyMutations(string value, string[] mutations)
	{
		var result = value;
		foreach (var mutation in mutations)
		{
			var mutationStr = mutation.Trim();
			result = mutationStr switch
			{
				"M" => TryGetVersionMajor(result),
				"M.M" => TryGetVersionMajorMinor(result),
				"M.x" => TryGetVersionMajorX(result),
				"M+1" => TryGetVersionIncreaseMajor(result),
				"M.M+1" => TryGetVersionIncreaseMinor(result),
				"lc" => result.ToLowerInvariant(),
				"uc" => result.ToUpperInvariant(),
				"tc" => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLowerInvariant()),
				"c" => char.ToUpperInvariant(result[0]) + result[1..].ToLowerInvariant(),
				"trim" => result.Trim(),
				_ => result // Unknown mutation, return unchanged
			};
		}
		return result;
	}

	private static string TryGetVersionMajor(string version)
	{
		if (Version.TryParse(version, out var v))
			return v.Major.ToString();
		return version;
	}

	private static string TryGetVersionMajorMinor(string version)
	{
		if (Version.TryParse(version, out var v))
			return $"{v.Major}.{v.Minor}";
		return version;
	}

	private static string TryGetVersionMajorX(string version)
	{
		if (Version.TryParse(version, out var v))
			return $"{v.Major}.x";
		return version;
	}

	private static string TryGetVersionIncreaseMajor(string version)
	{
		if (Version.TryParse(version, out var v))
			return $"{v.Major + 1}.0.0";
		return version;
	}

	private static string TryGetVersionIncreaseMinor(string version)
	{
		if (Version.TryParse(version, out var v))
			return $"{v.Major}.{v.Minor + 1}.0";
		return version;
	}
}
