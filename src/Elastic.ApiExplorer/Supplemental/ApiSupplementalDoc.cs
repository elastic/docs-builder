// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
	public static ApiSupplementalDoc? Parse(string? raw)
	{
		if (raw is null)
			return null;

		var (frontMatter, rawContent) = ExtractFrontMatter(raw);
		var trimmed = rawContent.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
		{
			return string.IsNullOrWhiteSpace(frontMatter)
				? null
				: Empty(frontMatter, description: null);
		}

		if (!trimmed.Contains("\n## ") && !trimmed.StartsWith("## ", StringComparison.Ordinal))
			return Empty(frontMatter, trimmed);

		var sections = SplitSections(trimmed);
		string? description = null;
		var parameterOverrides = EmptyOverrides();
		var requestBodyOverrides = EmptyOverrides();
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

		return new ApiSupplementalDoc(
			frontMatter,
			description,
			parameterOverrides,
			requestBodyOverrides,
			postSections);
	}

	private static ApiSupplementalDoc Empty(string? frontMatter, string? description) =>
		new(frontMatter, description, EmptyOverrides(), EmptyOverrides(), []);

	private static Dictionary<string, string> EmptyOverrides() =>
		new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
