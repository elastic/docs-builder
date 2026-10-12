// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
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
		if (string.IsNullOrEmpty(markdown) || !MayContainAdmonition().IsMatch(markdown))
			return markdown;

		var lines = markdown.Replace("\r\n", "\n").Split('\n');
		var result = new StringBuilder(markdown.Length + 64);
		var openFence = (Fence?)null;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];

			if (IsFenceLine(line, ref openFence) || openFence is not null)
			{
				AppendLine(result, line);
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
				WriteBlock(result, KindOf(quote.Groups["kind"].Value), body);
				continue;
			}

			var label = LabelPattern().Match(line);
			if (label.Success && (i == 0 || string.IsNullOrWhiteSpace(lines[i - 1])))
			{
				var body = new List<string> { label.Groups["text"].Value };
				while (i + 1 < lines.Length && !string.IsNullOrWhiteSpace(lines[i + 1]) && !FencePattern().IsMatch(lines[i + 1]))
				{
					body.Add(lines[i + 1]);
					i++;
				}
				WriteBlock(result, KindOf(label.Groups["kind"].Value), body);
				continue;
			}

			AppendLine(result, line);
		}

		return result.ToString().TrimEnd('\n');
	}

	private static bool IsFenceLine(string line, ref Fence? openFence)
	{
		var match = FencePattern().Match(line);
		if (!match.Success)
			return false;

		var fence = new Fence(match.Groups["fence"].Value, match.Groups["quote"].Success);
		if (openFence is null)
			openFence = fence;
		else if (fence.Closes(openFence.Value, match.Groups["rest"].Value))
			openFence = null;
		return true;
	}

	// Directives interrupt the surrounding paragraphs poorly, so always isolate them with blank lines.
	private static void AppendLine(StringBuilder sb, string line)
	{
		if (line.Length > 0 && EndsWith(sb, ":::\n"))
			_ = sb.Append('\n');
		_ = sb.Append(line).Append('\n');
	}

	private static void WriteBlock(StringBuilder sb, string kind, List<string> body)
	{
		if (sb.Length > 0 && !EndsWith(sb, "\n\n"))
			_ = sb.Append('\n');
		_ = sb.Append(":::{").Append(kind).Append("}\n");
		foreach (var line in body)
			_ = sb.Append(line).Append('\n');
		_ = sb.Append(":::\n");
	}

	private static bool EndsWith(StringBuilder sb, string suffix)
	{
		if (sb.Length < suffix.Length)
			return false;
		for (var i = 0; i < suffix.Length; i++)
		{
			if (sb[sb.Length - suffix.Length + i] != suffix[i])
				return false;
		}
		return true;
	}

	private static string KindOf(string label) => label.ToLowerInvariant() switch
	{
		"warn" or "warning" or "caution" or "deprecated" => "warning",
		"important" => "important",
		"tip" => "tip",
		_ => "note"
	};

	// Cheap pre-check so descriptions without callouts skip the line-by-line pass.
	[GeneratedRegex(@"^\s{0,3}(>\s*(info|warn)|\*{0,2}(note|important|warning|tip|deprecated)\b)", RegexOptions.IgnoreCase
		| RegexOptions.Multiline)]
	private static partial Regex MayContainAdmonition();

	// A fence may sit inside a blockquote, so a `> info` line within that quoted code is still literal.
	[GeneratedRegex(@"^\s{0,3}(?<quote>>\s?)?\s{0,3}(?<fence>`{3,}|~{3,})(?<rest>.*)$")]
	private static partial Regex FencePattern();

	[GeneratedRegex(@"^\s{0,3}>\s*(?<kind>info|warn|warning|note|tip|important|caution)\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex QuoteTypePattern();

	[GeneratedRegex(@"^\s{0,3}>\s?(?<text>.*)$")]
	private static partial Regex QuoteLinePattern();

	// "NOTE: text", "Warning: text", "**Note:** text", "**IMPORTANT**: text" at the start of a paragraph.
	[GeneratedRegex(@"^(?:\*\*(?<kind>NOTE|Note|IMPORTANT|Important|WARNING|Warning|TIP|Tip|DEPRECATED)(?::\*\*|\*\*:)|(?<kind>NOTE|Note|IMPORTANT|Important|WARNING|Warning|TIP|Tip|DEPRECATED|Deprecated):)\s*(?<text>.*)$")]
	private static partial Regex LabelPattern();

	private readonly record struct Fence(string Delimiter, bool Quoted)
	{
		// CommonMark: the closing fence uses the same character, is at least as long and has nothing after it.
		// It must also sit in the same quote context, so a quoted fence never closes an unquoted block.
		public bool Closes(Fence open, string rest) =>
			Quoted == open.Quoted
				&& Delimiter[0] == open.Delimiter[0]
				&& Delimiter.Length >= open.Delimiter.Length
				&& string.IsNullOrWhiteSpace(rest);
	}
}
