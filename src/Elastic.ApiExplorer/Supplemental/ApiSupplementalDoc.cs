// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Supplemental;

internal sealed record ApiSupplementalSection(string Heading, string Body);

internal sealed partial record ApiSupplementalDoc(
	string? FrontMatter,
	string? Description,
	Dictionary<string, string> ParameterOverrides,
	Dictionary<string, string> RequestBodyOverrides,
	IReadOnlyList<ApiSupplementalSection> PostSections
)
{
	public string? DescriptionOr(string? spec) => string.IsNullOrWhiteSpace(Description) ? spec : Description;

	public string? ParameterOr(string name, string? spec) => ParameterOverrides.TryGetValue(name, out var value) ? value : spec;

	public string? RequestBodyOr(string name, string? spec) => RequestBodyOverrides.TryGetValue(name, out var value) ? value : spec;

	public static IReadOnlyDictionary<string, ApiSupplementalDoc> Load(IReadOnlyDictionary<string, IFileInfo> files)
	{
		if (files.Count == 0)
			return FrozenDictionary<string, ApiSupplementalDoc>.Empty;

		var parsed = new Dictionary<string, ApiSupplementalDoc>(files.Count, StringComparer.Ordinal);
		foreach (var (key, file) in files)
		{
			var doc = Parse(file.FileSystem.File.ReadAllText(file.FullName));
			if (doc is not null)
				parsed[key] = doc;
		}

		return parsed;
	}

	internal static ApiSupplementalDoc Overlay(ApiSupplementalDoc? baseline, ApiSupplementalDoc overlay)
	{
		if (baseline is null)
			return overlay;

		return new(
			overlay.FrontMatter ?? baseline.FrontMatter,
			overlay.Description ?? baseline.Description,
			MergeMaps(baseline.ParameterOverrides, overlay.ParameterOverrides),
			MergeMaps(baseline.RequestBodyOverrides, overlay.RequestBodyOverrides),
			OverlayPostSections(baseline.PostSections, overlay.PostSections)
		);
	}

	internal static IReadOnlyDictionary<string, ApiSupplementalDoc> OverlayVersionFiles(
		IReadOnlyDictionary<string, ApiSupplementalDoc> baseline,
		IReadOnlyList<ApiSupplementalVersionedFile> versioned,
		int major,
		Func<string, ApiSupplementalKind, string?> resolveKey
	)
	{
		Dictionary<string, ApiSupplementalDoc>? copy = null;
		foreach (var versionedFile in versioned)
		{
			if (versionedFile.Name.VersionMajor != major)
				continue;

			var key = resolveKey(versionedFile.Name.Stem, versionedFile.Name.Kind);
			if (key is null)
				continue;

			var parsed = Parse(versionedFile.File.FileSystem.File.ReadAllText(versionedFile.File.FullName));
			if (parsed is null)
				continue;

			copy ??= new Dictionary<string, ApiSupplementalDoc>(baseline, StringComparer.Ordinal);
			copy[key] = Overlay(copy.TryGetValue(key, out var existing) ? existing : null, parsed);
		}

		return copy ?? baseline;
	}

	public static ApiSupplementalDoc? Parse(string? raw)
	{
		if (raw is null)
			return null;

		raw = raw.ReplaceLineEndings("\n");
		var (frontMatter, rawContent) = ExtractFrontMatter(raw);
		var trimmed = rawContent.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return string.IsNullOrWhiteSpace(frontMatter) ? null : Empty(frontMatter, description: null);
		}

		if (!trimmed.Contains("\n## ") && !trimmed.StartsWith("## ", StringComparison.Ordinal))
			return Empty(frontMatter, trimmed);

		var sections = SplitSections(trimmed);
		string? description = null;
		var parameterOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var requestBodyOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var postSections = new List<ApiSupplementalSection>();

		foreach (var (heading, body) in sections)
		{
			if (heading is null)
			{
				var preamble = body.Trim();
				if (!string.IsNullOrEmpty(preamble))
					description = description is null ? preamble : description + "\n\n" + preamble;
			}
			else if (heading.Equals("Description", StringComparison.OrdinalIgnoreCase))
			{
				var content = body.Trim();
				if (!string.IsNullOrEmpty(content))
					description = content;
			}
			else if (IsParametersHeading(heading))
				ParseFieldOverrides(body, parameterOverrides);
			else if (heading.Equals("Request body", StringComparison.OrdinalIgnoreCase))
				ParseFieldOverrides(body, requestBodyOverrides);
			else
				postSections.Add(new ApiSupplementalSection(heading, body.Trim()));
		}

		return new ApiSupplementalDoc(frontMatter, description, parameterOverrides, requestBodyOverrides, postSections);
	}

	private static ApiSupplementalDoc Empty(string? frontMatter, string? description) => new(frontMatter, description, [], [], []);

	private static Dictionary<string, string> MergeMaps(Dictionary<string, string> baseline, Dictionary<string, string> overlay)
	{
		if (overlay.Count == 0)
			return baseline;

		var merged = new Dictionary<string, string>(baseline, StringComparer.OrdinalIgnoreCase);
		foreach (var (key, value) in overlay)
			merged[key] = value;
		return merged;
	}

	private static IReadOnlyList<ApiSupplementalSection> OverlayPostSections(
		IReadOnlyList<ApiSupplementalSection> baseline,
		IReadOnlyList<ApiSupplementalSection> overlay
	)
	{
		if (overlay.Count == 0)
			return baseline;
		if (baseline.Count == 0)
			return overlay;

		var remaining = new Dictionary<string, ApiSupplementalSection>(StringComparer.OrdinalIgnoreCase);
		foreach (var section in overlay)
			remaining[section.Heading] = section;

		var result = new List<ApiSupplementalSection>(baseline.Count + overlay.Count);
		foreach (var section in baseline)
			result.Add(remaining.Remove(section.Heading, out var replacement) ? replacement : section);

		foreach (var section in overlay)
		{
			if (remaining.Remove(section.Heading, out var extra))
				result.Add(extra);
		}

		return result;
	}

	private static bool IsParametersHeading(string heading) =>
		heading.Equals("Parameters", StringComparison.OrdinalIgnoreCase)
			|| heading.Equals("Query parameters", StringComparison.OrdinalIgnoreCase)
			|| heading.Equals("Path parameters", StringComparison.OrdinalIgnoreCase);

	private static (string? FrontMatter, string Content) ExtractFrontMatter(string raw)
	{
		var match = FrontMatterRegex().Match(raw);
		if (!match.Success)
			return (null, raw);

		return (match.Value.Trim(), raw[match.Length..]);
	}

	private static List<(string? heading, string body)> SplitSections(string text)
	{
		var result = new List<(string?, string)>();
		var lines = text.Split('\n');
		string? currentHeading = null;
		var bodyLines = new List<string>();

		foreach (var line in lines)
		{
			if (line.StartsWith("## ", StringComparison.Ordinal))
			{
				if (bodyLines.Count > 0 || currentHeading is not null)
					result.Add((currentHeading, string.Join("\n", bodyLines)));
				currentHeading = line[3..].Trim();
				bodyLines = [];
			}
			else
				bodyLines.Add(line);
		}

		result.Add((currentHeading, string.Join("\n", bodyLines)));
		return result;
	}

	private static void ParseFieldOverrides(string body, Dictionary<string, string> overrides)
	{
		var lines = body.Split('\n');
		string? currentKey = null;
		var descLines = new List<string>();

		foreach (var rawLine in lines)
		{
			var termMatch = TermLineRegex().Match(rawLine);
			if (termMatch.Success)
			{
				if (currentKey is not null)
					overrides[currentKey] = string.Join("\n", descLines).Trim();
				currentKey = NormalizeKey(termMatch.Groups[1].Value);
				descLines = [];
			}
			else if (currentKey is not null)
				descLines.Add(rawLine);
		}

		if (currentKey is not null)
			overrides[currentKey] = string.Join("\n", descLines).Trim();
	}

	private static string NormalizeKey(string raw)
	{
		var s = raw.Trim().Trim('`').Trim();
		var spaceIdx = s.IndexOf(' ');
		if (spaceIdx > 0)
			s = s[..spaceIdx];
		return s.Trim();
	}

	[GeneratedRegex(@"^:\s+(`[^`]+`|[A-Za-z0-9_.-]+)")]
	private static partial Regex TermLineRegex();

	[GeneratedRegex(@"\A---\r?\n[\s\S]*?\r?\n---[ \t]*(?:\r?\n|$)")]
	private static partial Regex FrontMatterRegex();
}
