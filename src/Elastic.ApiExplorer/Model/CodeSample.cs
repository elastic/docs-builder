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
	/// <summary>True when docs-builder built this sample from an example body rather than reading it from the spec.</summary>
	public bool Generated { get; init; }

	/// <summary>
	/// The example this sample belongs to when the spec encodes it in the language name, e.g. <c>cURL_tag_names</c>
	/// is the curl sample of a "Tag names" example. Null for samples of the default example.
	/// </summary>
	public string? Scenario { get; init; }

	/// <summary>
	/// Splits a spec <c>lang</c> into the language, in its canonical casing (<c>cURL</c> is <c>curl</c>), and the
	/// example suffix when one follows a known language name.
	/// </summary>
	public static (string Language, string? Scenario) SplitLanguage(string lang)
	{
		var separator = lang.IndexOf('_');
		if (separator > 0 && separator < lang.Length - 1 && Canonical(lang[..separator]) is { } prefix)
			return (prefix, lang[(separator + 1)..]);
		return (Canonical(lang) ?? lang, null);
	}

	private static string? Canonical(string language) =>
		LanguageRanks.Keys.FirstOrDefault(k => k.Equals(language, StringComparison.OrdinalIgnoreCase));

	/// <summary>The client library or tool that runs this sample, e.g. <c>elasticsearch-java</c>. Empty when unknown.</summary>
	public string ClientLabel => ClientLabels.GetValueOrDefault(Language, "");

	/// <summary>Position in the carousel: Console, then languages by how many developers use them. Unranked languages sort last.</summary>
	public int Rank => LanguageRanks.GetValueOrDefault(Language, int.MaxValue);

	// Console is the docs' native sample. The rest follow GitHub's Innovation Graph global
	// programming-language ranking (unique pushers, 2026 Q1); curl counts as Shell there.
	// https://innovationgraph.github.com/global-metrics/programming-languages
	private static readonly Dictionary<string, int> LanguageRanks = new(StringComparer.OrdinalIgnoreCase)
	{
		["Console"] = 0,
		["JavaScript"] = 1,
		["Python"] = 2,
		["curl"] = 3,
		["Java"] = 4,
		["C#"] = 5,
		["PHP"] = 6,
		["Ruby"] = 7,
		["Go"] = 8,
		["Rust"] = 9,
	};

	private static readonly Dictionary<string, string> ClientLabels = new(StringComparer.OrdinalIgnoreCase)
	{
		["Console"] = "Kibana Dev Tools",
		["curl"] = "Shell",
		["Python"] = "elasticsearch-py",
		["JavaScript"] = "@elastic/elasticsearch",
		["Ruby"] = "elasticsearch-ruby",
		["PHP"] = "elasticsearch-php",
		["Java"] = "elasticsearch-java",
		["C#"] = "Elastic.Clients.Elasticsearch",
	};

	private static readonly Dictionary<string, string> LanguageHighlightMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["Console"] = "language-console",
		["curl"] = "language-curl",
		["Python"] = "language-python",
		["JavaScript"] = "language-javascript",
		["Ruby"] = "language-ruby",
		["PHP"] = "language-php",
		["Java"] = "language-java",
		["C#"] = "language-csharp",
	};

	public static string GetHighlightClass(string language) =>
		LanguageHighlightMap.GetValueOrDefault(language, $"language-{language.ToLowerInvariant()}");

	/// <summary>
	/// Picks a highlight language for OpenAPI example bodies. Only real JSON objects/arrays
	/// use <c>language-json</c> (Figma Card/Code token colors); SSE and other payloads stay plaintext
	/// so hljs does not invent misleading token colors.
	/// </summary>
	public static string HighlightClassForExampleBody(string? source)
	{
		if (string.IsNullOrWhiteSpace(source))
			return "language-plaintext";

		var span = source.AsSpan().TrimStart();
		return span.Length > 0 && (span[0] == '{' || span[0] == '[') ? "language-json" : "language-plaintext";
	}

	/// <summary>Maps a hljs <c>language-*</c> class to the outer Myst-style wrapper, e.g. <c>language-json</c> to <c>highlight-json</c>.</summary>
	public static string GetHighlightGroupClass(string? highlightClass)
	{
		if (string.IsNullOrEmpty(highlightClass) || !highlightClass.StartsWith("language-", StringComparison.Ordinal))
			return "highlight-plaintext";

		var id = highlightClass["language-".Length..];
		return string.IsNullOrEmpty(id) ? "highlight-plaintext" : $"highlight-{id}";
	}
}
