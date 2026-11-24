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
			return EndOfElement(html, start, name);

		// A '<' that opens no tag is text; encoded pages never hold one, but it must not swallow the page.
		var opensTag = start + 1 < html.Length && (char.IsAsciiLetter(html[start + 1]) || html[start + 1] is '/' or '!' or '?');
		return opensTag ? EndOfTag(html, start) : start + 1;
	}

	/// <summary>
	/// The end of the element whose start tag begins at <paramref name="start"/>: after its own closing tag, which is the
	/// name followed by whitespace, '/' or '>', so <c>&lt;/script-example&gt;</c> does not close a script. A
	/// <c>pre</c> holds markup, so its tags are skipped whole, and a closing tag inside an attribute value does not count;
	/// <c>script</c>, <c>style</c> and <c>textarea</c> hold raw text.
	/// </summary>
	private static int EndOfElement(string html, int start, string name)
	{
		var holdsMarkup = name == "pre";
		// A pre inside the pre is counted, not recursed into, so no nesting depth can exhaust the stack.
		var depth = 1;
		var position = EndOfTag(html, start);
		while (position < html.Length)
		{
			var next = html.IndexOf('<', position);
			if (next < 0)
				return html.Length;
			if (IsClosingTag(html, next, name))
			{
				position = EndOfTag(html, next);
				if (--depth == 0)
					return position;
				continue;
			}

			if (holdsMarkup && RawTextElement(html, next) == "pre")
			{
				depth++;
				position = EndOfTag(html, next);
				continue;
			}

			position = holdsMarkup ? EndOfMarkupInPre(html, next) : next + 1;
		}

		return html.Length;
	}

	/// <summary>
	/// Where the markup at <paramref name="index"/> inside a <c>pre</c> ends: a nested raw-text element through its own
	/// closing tag, a comment through its <c>--&gt;</c> (so text such as <c>="</c> in it is not read as an attribute), a tag
	/// whole, and anything else one character on.
	/// </summary>
	private static int EndOfMarkupInPre(string html, int index)
	{
		// A textarea, script or style inside the pre holds raw text, where "<!--" is not a comment. Raw text holds no
		// elements, so this goes one level deep at most.
		if (RawTextElement(html, index) is { } nested and not "pre")
			return EndOfElement(html, index, nested);
		if (string.CompareOrdinal(html, index, "<!--", 0, 4) == 0)
			return EndAfter(html, html.IndexOf("-->", index + 4, StringComparison.Ordinal), 3);
		var opensTag = index + 1 < html.Length && (char.IsAsciiLetter(html[index + 1]) || html[index + 1] is '/' or '!');
		return opensTag ? EndOfTag(html, index) : index + 1;
	}

	private static bool IsClosingTag(string html, int index, string name)
	{
		var after = index + 2 + name.Length;
		return after < html.Length
			&& html[index + 1] == '/'
			&& string.Compare(html, index + 2, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0
			&& (IsHtmlWhitespace(html[after]) || html[after] is '>' or '/');
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
