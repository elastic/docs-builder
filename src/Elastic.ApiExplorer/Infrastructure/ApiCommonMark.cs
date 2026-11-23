// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Small CommonMark helpers shared by API page writers.</summary>
internal static partial class ApiCommonMark
{
	/// <summary>
	/// Drops the <c>[anchor]</c> that ends a heading, as the <c>.md</c> output of docs pages does: it sets the HTML id and
	/// means nothing in plain Markdown. Lines inside fenced code blocks stay as written.
	/// </summary>
	public static string StripHeadingAnchors(string markdown)
	{
		var lines = markdown.Split('\n');
		string? fence = null;
		for (var i = 0; i < lines.Length; i++)
		{
			var trimmed = lines[i].TrimStart();
			var run = FenceRun(trimmed);
			if (fence is not null)
			{
				// As in CommonMark, only a run of the same character, at least as long, with nothing after it closes the block.
				if (run is not null && run[0] == fence[0] && run.Length >= fence.Length && string.IsNullOrWhiteSpace(trimmed[run.Length..]))
					fence = null;
			}
			else if (run is not null)
				fence = run;
			else
				lines[i] = HeadingAnchor().Replace(lines[i], "$1");
		}

		return string.Join('\n', lines);
	}

	// The backticks or tildes that open or close a fenced code block, or null when the line is not a fence.
	private static string? FenceRun(string line)
	{
		if (line.Length == 0 || line[0] is not ('`' or '~'))
			return null;

		var length = line.TakeWhile(c => c == line[0]).Count();
		return length >= 3 ? line[..length] : null;
	}

	[GeneratedRegex(@"^(#{1,6}\s.*?)\s*\[[^\[\]]+\]\s*$", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex HeadingAnchor();

	public static void Heading(StringBuilder markdown, int level, string text)
	{
		_ = markdown.Append('#', level);
		_ = markdown.Append(' ');
		_ = markdown.AppendLine(text);
		_ = markdown.AppendLine();
	}

	public static void Paragraph(StringBuilder markdown, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		_ = markdown.AppendLine(text.TrimEnd());
		_ = markdown.AppendLine();
	}

	public static void Prepared(StringBuilder markdown, string? source, string apiBaseUrl)
	{
		var prepared = StripHeadingAnchors(ApiMarkdown.Prepare(source, apiBaseUrl));
		if (string.IsNullOrWhiteSpace(prepared))
			return;

		_ = markdown.AppendLine(prepared.TrimEnd());
		_ = markdown.AppendLine();
	}

	/// <summary>A fenced code block. The fence is longer than any backtick run in the source, so a sample that holds a fence stays whole.</summary>
	public static void Fence(StringBuilder markdown, string language, string? source)
	{
		if (string.IsNullOrEmpty(source))
			return;

		var fence = new string('`', Math.Max(3, LongestBacktickRun(source) + 1));
		_ = markdown.Append(fence);
		_ = markdown.AppendLine(language);
		_ = markdown.AppendLine(source.TrimEnd());
		_ = markdown.AppendLine(fence);
		_ = markdown.AppendLine();
	}

	/// <summary>Text as an inline code span. The delimiter is longer than any backtick run inside it, so the text survives as written.</summary>
	public static string CodeSpan(string? text)
	{
		text ??= "";
		var delimiter = new string('`', LongestBacktickRun(text) + 1);
		// A backtick at either end would merge with the delimiter, so CommonMark strips one space of padding on each side.
		var padding = text.StartsWith('`') || text.EndsWith('`') ? " " : "";
		return $"{delimiter}{padding}{text}{padding}{delimiter}";
	}

	private static int LongestBacktickRun(string text)
	{
		var longest = 0;
		var run = 0;
		foreach (var character in text)
		{
			run = character == '`' ? run + 1 : 0;
			longest = Math.Max(longest, run);
		}

		return longest;
	}

	public static string Link(string text, string? url)
	{
		if (string.IsNullOrEmpty(url))
			return text;

		return $"[{text}]({url})";
	}
}
