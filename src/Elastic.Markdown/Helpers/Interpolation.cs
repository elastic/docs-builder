// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Elastic.Documentation;
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
					value = ApplyMutationsUsingExistingSystem(value, components[1..]);
				}

				replacement ??= span.ToString();
				replacement = replacement.Replace(spanMatch.ToString(), value);
				replaced = true;
			}
		}

		return replaced;
	}

	private static string ApplyMutationsUsingExistingSystem(string value, string[] mutations)
	{
		var result = value;
		foreach (var mutationStr in mutations)
		{
			var trimmedMutation = mutationStr.Trim();
			if (SubstitutionMutationExtensions.TryParse(trimmedMutation, out var mutation, true, true))
			{
				// Use the same logic as SubstitutionRenderer.Write
				var (success, update) = mutation switch
				{
					SubstitutionMutation.MajorComponent => TryGetVersion(result, v => $"{v.Major}"),
					SubstitutionMutation.MajorX => TryGetVersion(result, v => $"{v.Major}.x"),
					SubstitutionMutation.MajorMinor => TryGetVersion(result, v => $"{v.Major}.{v.Minor}"),
					SubstitutionMutation.IncreaseMajor => TryGetVersion(result, v => $"{v.Major + 1}.0.0"),
					SubstitutionMutation.IncreaseMinor => TryGetVersion(result, v => $"{v.Major}.{v.Minor + 1}.0"),
					SubstitutionMutation.LowerCase => (true, result.ToLowerInvariant()),
					SubstitutionMutation.UpperCase => (true, result.ToUpperInvariant()),
					SubstitutionMutation.Capitalize => (true, Capitalize(result)),
					SubstitutionMutation.KebabCase => (true, ToKebabCase(result)),
					SubstitutionMutation.CamelCase => (true, ToCamelCase(result)),
					SubstitutionMutation.PascalCase => (true, ToPascalCase(result)),
					SubstitutionMutation.SnakeCase => (true, ToSnakeCase(result)),
					SubstitutionMutation.TitleCase => (true, TitleCase(result)),
					SubstitutionMutation.Trim => (true, Trim(result)),
					_ => (false, result)
				};
				if (success)
				{
					result = update;
				}
			}
		}
		return result;
	}

	private static (bool Success, string Result) TryGetVersion(string version, Func<SemVersion, string> transform)
	{
		if (!SemVersion.TryParse(version, out var v) && !SemVersion.TryParse(version + ".0", out v))
			return (false, version);
		return (true, transform(v));
	}

	// These methods match the exact implementation in SubstitutionRenderer
	private static string Capitalize(string input) =>
		input switch
		{
			null => string.Empty,
			"" => string.Empty,
			_ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
		};

	private static string ToKebabCase(string str) => JsonNamingPolicy.KebabCaseLower.ConvertName(str).Replace(" ", string.Empty);

	private static string ToCamelCase(string str) => JsonNamingPolicy.CamelCase.ConvertName(str).Replace(" ", string.Empty);

	private static string ToPascalCase(string str) => TitleCase(str).Replace(" ", string.Empty);

	private static string ToSnakeCase(string str) => JsonNamingPolicy.SnakeCaseLower.ConvertName(str).Replace(" ", string.Empty);

	private static string TitleCase(string str) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str);

	private static string Trim(string str) =>
		str.AsSpan().Trim(['!', ' ', '\t', '\r', '\n', '.', ',', ')', '(', ':', ';', '<', '>', '[', ']']).ToString();
}
