// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Text.Json;
using Elastic.Documentation;

namespace Elastic.Markdown.Myst.InlineParsers.Substitution;

/// <summary>
/// Shared utility for parsing and applying substitution mutations
/// </summary>
public static class SubstitutionMutationHelper
{
	/// <summary>
	/// Parses a substitution key with mutations and returns the key and mutation components
	/// </summary>
	/// <param name="rawKey">The raw substitution key (e.g., "version.stack | M.M")</param>
	/// <returns>A tuple containing the cleaned key and array of mutation strings</returns>
	public static (string Key, string[] Mutations) ParseKeyWithMutations(string rawKey)
	{
		// Improved handling of pipe-separated components with better whitespace handling
		var components = rawKey.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var key = components[0].Trim();
		var mutations = components.Length > 1 ? components[1..] : [];

		return (key, mutations);
	}

	/// <summary>
	/// Applies mutations to a value using the existing SubstitutionMutation system
	/// </summary>
	/// <param name="value">The original value to transform</param>
	/// <param name="mutations">Collection of SubstitutionMutation enums to apply</param>
	/// <returns>The transformed value</returns>
	public static string ApplyMutations(string value, IReadOnlyCollection<SubstitutionMutation> mutations)
	{
		var result = value;
		foreach (var mutation in mutations)
		{
			result = ApplySingleMutation(result, mutation);
		}
		return result;
	}

	/// <summary>
	/// Applies mutations to a value using the existing SubstitutionMutation system
	/// </summary>
	/// <param name="value">The original value to transform</param>
	/// <param name="mutations">Array of mutation strings to apply</param>
	/// <returns>The transformed value</returns>
	public static string ApplyMutations(string value, string[] mutations)
	{
		var result = value;
		foreach (var mutationStr in mutations)
		{
			var trimmedMutation = mutationStr.Trim();
			if (SubstitutionMutationExtensions.TryParse(trimmedMutation, out var mutation, true, true))
			{
				result = ApplySingleMutation(result, mutation);
			}
		}
		return result;
	}

	/// <summary>
	/// Applies a single mutation to a value
	/// </summary>
	/// <param name="value">The value to transform</param>
	/// <param name="mutation">The mutation to apply</param>
	/// <returns>The transformed value</returns>
	private static string ApplySingleMutation(string value, SubstitutionMutation mutation)
	{
		var (success, result) = mutation switch
		{
			SubstitutionMutation.MajorComponent => TryGetVersion(value, v => $"{v.Major}"),
			SubstitutionMutation.MajorX => TryGetVersion(value, v => $"{v.Major}.x"),
			SubstitutionMutation.MajorMinor => TryGetVersion(value, v => $"{v.Major}.{v.Minor}"),
			SubstitutionMutation.IncreaseMajor => TryGetVersion(value, v => $"{v.Major + 1}.0.0"),
			SubstitutionMutation.IncreaseMinor => TryGetVersion(value, v => $"{v.Major}.{v.Minor + 1}.0"),
			SubstitutionMutation.LowerCase => (true, value.ToLowerInvariant()),
			SubstitutionMutation.UpperCase => (true, value.ToUpperInvariant()),
			SubstitutionMutation.Capitalize => (true, Capitalize(value)),
			SubstitutionMutation.KebabCase => (true, ToKebabCase(value)),
			SubstitutionMutation.CamelCase => (true, ToCamelCase(value)),
			SubstitutionMutation.PascalCase => (true, ToPascalCase(value)),
			SubstitutionMutation.SnakeCase => (true, ToSnakeCase(value)),
			SubstitutionMutation.TitleCase => (true, TitleCase(value)),
			SubstitutionMutation.Trim => (true, Trim(value)),
			_ => (false, value)
		};
		return success ? result : value;
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

	private static string TitleCase(string str) => str.Split(' ').Select(word => Capitalize(word)).Aggregate((a, b) => $"{a} {b}");

	private static string Trim(string str) => str.TrimEnd('!', '.', ',', ';', ':', '?', ' ', '\t', '\n', '\r');
}
