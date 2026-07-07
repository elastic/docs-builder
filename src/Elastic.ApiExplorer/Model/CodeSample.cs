// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.ApiExplorer.Operations;
namespace Elastic.ApiExplorer.Model;

/// <summary>
/// A single code sample extracted from the x-codeSamples OpenAPI extension.
/// </summary>
public record CodeSample(string Language, string Source, string HighlightClass)
{
	private static readonly Dictionary<string, string> LanguageHighlightMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["Console"] = "language-console",
		["curl"] = "language-bash",
		["Python"] = "language-python",
		["JavaScript"] = "language-javascript",
		["Ruby"] = "language-ruby",
		["PHP"] = "language-php",
		["Java"] = "language-java",
	};

	public static string GetHighlightClass(string language) =>
		LanguageHighlightMap.GetValueOrDefault(language, $"language-{language.ToLowerInvariant()}");

	/// <summary>Maps a hljs <c>language-*</c> class to the outer Myst-style wrapper, e.g. <c>language-json</c> to <c>highlight-json</c>.</summary>
	public static string GetHighlightGroupClass(string? highlightClass)
	{
		if (string.IsNullOrEmpty(highlightClass) || !highlightClass.StartsWith("language-", StringComparison.Ordinal))
			return "highlight-plaintext";

		var id = highlightClass["language-".Length..];
		return string.IsNullOrEmpty(id) ? "highlight-plaintext" : $"highlight-{id}";
	}
}
