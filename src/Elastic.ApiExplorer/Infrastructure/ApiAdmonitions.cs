// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Rewrites the callout conventions found in OpenAPI descriptions into docs-builder admonition directives:
/// bump.sh style blockquotes (<c>&gt; info</c> / <c>&gt; warn</c> followed by quoted lines) and paragraphs
/// that start with a label such as <c>NOTE:</c>, <c>**Warning:**</c> or <c>IMPORTANT:</c>.
/// </summary>
public static partial class ApiAdmonitions
{
	public static string Convert(string markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return markdown;

		var lines = markdown.ReplaceLineEndings("\n").Split('\n');
		var output = new List<string>(lines.Length + 8);
		var converted = false;
		CodeFences.Fence? openFence = null;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (CodeFences.InCode(line, ref openFence))
			{
				Append(output, line);
				continue;
			}

			var quote = QuoteTypePattern().Match(line);
			if (quote.Success)
			{
				var body = new List<string>();
				while (i + 1 < lines.Length && QuoteLinePattern().Match(lines[i + 1]) is { Success: true } next)
				{
					body.Add(next.Groups["text"].Value);
					i++;
				}
				WriteBlock(output, KindOf(quote.Groups["kind"].Value), body);
				converted = true;
				continue;
			}

			// A label counts only at the start of a paragraph, and only capitalised: "note:" mid-prose is not a callout.
			var label = i == 0 || string.IsNullOrWhiteSpace(lines[i - 1]) ? LabelPattern().Match(line) : Match.Empty;
			if (label.Success && char.IsUpper(label.Groups["kind"].Value[0]))
			{
				var body = new List<string> { label.Groups["text"].Value };
				while (i + 1 < lines.Length && !string.IsNullOrWhiteSpace(lines[i + 1]) && !CodeFences.IsFenceLine(lines[i + 1]))
				{
					body.Add(lines[i + 1]);
					i++;
				}
				WriteBlock(output, KindOf(label.Groups["kind"].Value), body);
				converted = true;
				continue;
			}

			Append(output, line);
		}

		return converted ? string.Join('\n', output).TrimEnd('\n') : markdown;
	}

	// Directives interrupt the surrounding paragraphs poorly, so always isolate them with blank lines.
	private static void Append(List<string> output, string line)
	{
		if (line.Length > 0 && output is [.., ":::"])
			output.Add(string.Empty);
		output.Add(line);
	}

	private static void WriteBlock(List<string> output, string kind, List<string> body)
	{
		if (output is [.., not ""])
			output.Add(string.Empty);
		output.Add($":::{{{kind}}}");
		output.AddRange(body);
		output.Add(":::");
	}

	private static string KindOf(string label) => label.ToLowerInvariant() switch
	{
		"warn" or "warning" or "caution" or "deprecated" => "warning",
		"important" => "important",
		"tip" => "tip",
		_ => "note"
	};

	[GeneratedRegex(@"^\s{0,3}>\s*(?<kind>info|warn|warning|note|tip|important|caution)\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex QuoteTypePattern();

	[GeneratedRegex(@"^\s{0,3}>\s?(?<text>.*)$")]
	private static partial Regex QuoteLinePattern();

	// "NOTE: text", "Warning: text", "**Note:** text", "**IMPORTANT**: text"; the colon sits inside or after the bold.
	[GeneratedRegex(@"^(?<bold>\*\*)?(?<kind>note|important|warning|tip|deprecated)(?(bold)(?::\*\*|\*\*:)|:)\s*(?<text>.*)$", RegexOptions.IgnoreCase)]
	private static partial Regex LabelPattern();
}
