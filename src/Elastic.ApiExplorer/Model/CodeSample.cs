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

	private static string? Canonical(string language) => ByName.TryGetValue(language, out var known) ? known.Name : null;

	public bool IsConsole => Language.Equals("Console", StringComparison.OrdinalIgnoreCase);

	public bool IsCurl => Language.Equals("curl", StringComparison.OrdinalIgnoreCase);

	/// <summary>The client library or tool that runs this sample, e.g. <c>elasticsearch-java</c>. Empty when unknown.</summary>
	public string ClientLabel => ByName.TryGetValue(Language, out var known) ? known.Client ?? "" : "";

	/// <summary>Position in the carousel: Console, then languages by how many developers use them. Unranked languages sort last.</summary>
	public int Rank => ByName.TryGetValue(Language, out var known) ? known.Rank : int.MaxValue;

	public static string GetHighlightClass(string language) =>
		ByName.TryGetValue(language, out var known) && known.Highlight is { } highlight
			? highlight
			: $"language-{language.ToLowerInvariant()}";

	private sealed record KnownLanguage(string Name, int Rank, string? Client, string? Highlight);

	// One row per language we know, in carousel order: Console is the docs' native sample, the rest follow
	// GitHub's Innovation Graph global ranking (unique pushers, 2026 Q1); curl counts as Shell there.
	// https://innovationgraph.github.com/global-metrics/programming-languages
	private static readonly Dictionary<string, KnownLanguage> ByName = new (string Name, string? Client, string? Highlight)[]
	{
		("Console", "Kibana Dev Tools", "language-console"),
		("JavaScript", "@elastic/elasticsearch", "language-javascript"),
		("Python", "elasticsearch-py", "language-python"),
		("curl", "Shell", "language-curl"),
		("Java", "elasticsearch-java", "language-java"),
		("C#", "Elastic.Clients.Elasticsearch", "language-csharp"),
		("PHP", "elasticsearch-php", "language-php"),
		("Ruby", "elasticsearch-ruby", "language-ruby"),
		("Go", null, null),
		("Rust", null, null),
	}.Select(static (l, rank) => new KnownLanguage(l.Name, rank, l.Client, l.Highlight)).ToDictionary(
		static l => l.Name,
		StringComparer.OrdinalIgnoreCase
	);

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
