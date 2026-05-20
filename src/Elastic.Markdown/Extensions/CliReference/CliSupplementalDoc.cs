// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Markdown.Extensions.CliReference;

internal sealed partial record CliSupplementalDoc(
	string? Description,
	Dictionary<string, string> OptionOverrides,
	Dictionary<string, string> ArgumentOverrides,
	string? PostContent
)
{
	public static CliSupplementalDoc? Parse(string? raw)
	{
		if (raw is null)
			return null;

		var trimmed = raw.Trim();
		if (string.IsNullOrWhiteSpace(trimmed))
			return null;

		// Backward compat: no ## headings → entire content is description
		if (!trimmed.Contains("\n## ") && !trimmed.StartsWith("## ", StringComparison.Ordinal))
			return new CliSupplementalDoc(trimmed, [], [], null);

		var sections = SplitSections(trimmed);
		string? description = null;
		var optionOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var argumentOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		var postParts = new List<string>();

		foreach (var (heading, body) in sections)
		{
			if (heading is null)
			{
				var preamble = body.Trim();
				if (!string.IsNullOrEmpty(preamble))
					description = (description is null) ? preamble : description + "\n\n" + preamble;
			}
			else if (heading.Equals("Description", StringComparison.OrdinalIgnoreCase))
			{
				var content = body.Trim();
				if (!string.IsNullOrEmpty(content))
					description = content;
			}
			else if (heading.Equals("Options", StringComparison.OrdinalIgnoreCase))
				ParseParameterOverrides(body, optionOverrides);
			else if (heading.Equals("Arguments", StringComparison.OrdinalIgnoreCase))
				ParseParameterOverrides(body, argumentOverrides);
			else
				postParts.Add($"## {heading}\n\n{body.Trim()}");
		}

		var postContent = postParts.Count > 0 ? string.Join("\n\n", postParts) : null;
		return new CliSupplementalDoc(description, optionOverrides, argumentOverrides, postContent);
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

	// Parses entries in the form: `: `--flag`` or `: --flag` or `: <name>` followed by description lines.
	private static void ParseParameterOverrides(string body, Dictionary<string, string> overrides)
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
		if (s.StartsWith("--", StringComparison.Ordinal))
			s = s[2..];
		else if (s.StartsWith('-') && s.Length > 1)
			s = s[1..];
		var spaceIdx = s.IndexOf(' ');
		if (spaceIdx > 0)
			s = s[..spaceIdx];
		s = s.Trim('<', '>');
		return s.Trim();
	}

	// Matches: `: `--flag`` or `: --flag` or `: <name>`
	[GeneratedRegex(@"^:\s+(`[^`]+`|--[\w-]+|<[\w-]+>)")]
	private static partial Regex TermLineRegex();
}
