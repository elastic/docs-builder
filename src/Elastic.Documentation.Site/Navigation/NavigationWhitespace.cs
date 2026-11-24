// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Drops the template indentation from the rendered sidebar. Every page carries the whole tree, so on the largest
/// API products the tabs alone cost about 30 KB per page.
/// </summary>
public static class NavigationWhitespace
{
	/// <summary>
	/// Turns each whitespace run that holds a line break into one line break, which renders the same under
	/// <c>white-space: normal</c>. The sidebar holds no preformatted text and no attribute value with a line break. One
	/// pass, so a long run of spaces costs its length once; only HTML's own whitespace counts, so a non-breaking space stays.
	/// </summary>
	public static string Collapse(string html)
	{
		var output = new StringBuilder(html.Length);
		var i = 0;
		while (i < html.Length)
		{
			if (!IsHtmlWhitespace(html[i]))
			{
				_ = output.Append(html[i++]);
				continue;
			}

			var runStart = i;
			var hasLineBreak = false;
			for (; i < html.Length && IsHtmlWhitespace(html[i]); i++)
				hasLineBreak |= html[i] == '\n';
			_ = hasLineBreak ? output.Append('\n') : output.Append(html, runStart, i - runStart);
		}

		return output.ToString();
	}

	private static bool IsHtmlWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';
}
