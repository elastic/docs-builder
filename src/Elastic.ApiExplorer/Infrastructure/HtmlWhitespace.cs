// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Drops the template indentation from a rendered page. The largest API pages repeat a nested row partial thousands of
/// times, and its tabs alone cost hundreds of kilobytes against Googlebot's 2 MB limit.
/// </summary>
/// <remarks>
/// A single pass over the page, not a regex: pages reach several megabytes and render in parallel, where a regex over
/// the whole page ran past its match timeout and failed the build.
/// </remarks>
public static class HtmlWhitespace
{
	private static readonly string[] RawTextElements = ["pre", "textarea", "script", "style"];

	/// <summary>
	/// Turns each whitespace run that holds a line break in text into one line break, which renders the same under
	/// <c>white-space: normal</c>. Raw-text and preformatted elements, comments and tags with their attribute values are
	/// kept as they are. Only HTML's own whitespace counts, so a non-breaking space is never dropped.
	/// </summary>
	public static string Collapse(string html)
	{
		var output = new StringBuilder(html.Length);
		var position = 0;
		while (position < html.Length)
		{
			if (html[position] == '<')
			{
				var end = EndOfMarkup(html, position);
				_ = output.Append(html, position, end - position);
				position = end;
				continue;
			}

			var next = html.IndexOf('<', position);
			if (next < 0)
				next = html.Length;
			AppendText(output, html, position, next);
			position = next;
		}

		return output.ToString();
	}

	/// <summary>Where the comment, raw-text element or tag starting at <paramref name="start"/> ends.</summary>
	private static int EndOfMarkup(string html, int start)
	{
		if (string.CompareOrdinal(html, start, "<!--", 0, 4) == 0)
			return EndAfter(html, html.IndexOf("-->", start + 4, StringComparison.Ordinal), 3);

		if (RawTextElement(html, start) is { } name)
		{
			var close = html.IndexOf("</" + name, start + name.Length + 1, StringComparison.OrdinalIgnoreCase);
			return close < 0 ? html.Length : EndOfTag(html, close);
		}

		// A '<' that opens no tag is text; encoded pages never hold one, but it must not swallow the page.
		var opensTag = start + 1 < html.Length && (char.IsAsciiLetter(html[start + 1]) || html[start + 1] is '/' or '!' or '?');
		return opensTag ? EndOfTag(html, start) : start + 1;
	}

	/// <summary>The end of the tag at <paramref name="start"/>, skipping a quoted attribute value that holds a '>'.</summary>
	private static int EndOfTag(string html, int start)
	{
		for (var i = start + 1; i < html.Length; i++)
		{
			var c = html[i];
			if (c == '>')
				return i + 1;
			if (c is '"' or '\'' && PreviousNonSpace(html, i, start) == '=')
				i = html.IndexOf(c, i + 1) is var closing and >= 0 ? closing : html.Length;
		}

		return html.Length;
	}

	private static char PreviousNonSpace(string html, int index, int floor)
	{
		for (var i = index - 1; i > floor; i--)
		{
			if (!IsHtmlWhitespace(html[i]))
				return html[i];
		}

		return '\0';
	}

	private static string? RawTextElement(string html, int start)
	{
		foreach (var name in RawTextElements)
		{
			var after = start + 1 + name.Length;
			if (
				after < html.Length
				&& string.Compare(html, start + 1, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0
				&& (IsHtmlWhitespace(html[after]) || html[after] is '>' or '/')
			)
				return name;
		}

		return null;
	}

	private static void AppendText(StringBuilder output, string html, int start, int end)
	{
		var i = start;
		while (i < end)
		{
			if (!IsHtmlWhitespace(html[i]))
			{
				_ = output.Append(html[i]);
				i++;
				continue;
			}

			var runStart = i;
			var hasLineBreak = false;
			while (i < end && IsHtmlWhitespace(html[i]))
			{
				hasLineBreak |= html[i] == '\n';
				i++;
			}

			_ = hasLineBreak ? output.Append('\n') : output.Append(html, runStart, i - runStart);
		}
	}

	private static int EndAfter(string html, int index, int length) => index < 0 ? html.Length : index + length;

	// ASCII whitespace as HTML defines it; char.IsWhiteSpace would also take a non-breaking space.
	private static bool IsHtmlWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
}
