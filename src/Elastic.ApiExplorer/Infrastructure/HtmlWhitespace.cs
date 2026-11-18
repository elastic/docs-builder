// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>
/// Drops the template indentation from a rendered page. The largest API pages repeat a nested row partial thousands of
/// times, and its tabs alone cost hundreds of kilobytes against Googlebot's 2 MB limit.
/// </summary>
public static partial class HtmlWhitespace
{
	/// <summary>
	/// Turns each whitespace run that holds a line break in text into one line break, which renders the same under
	/// <c>white-space: normal</c>. Raw-text and preformatted elements, comments and attribute values are kept as they are.
	/// </summary>
	public static string Collapse(string html) =>
		Tokens().Replace(html, static m => m.Groups["text"].Success ? LineBreakRun().Replace(m.Value, "\n") : m.Value);

	// One token at a time: a whole raw element, a comment, a tag with quoted attributes, or a text run.
	[GeneratedRegex("""<(?<raw>pre|textarea|script|style)(?=[\s>])[\s\S]*?</\k<raw>\s*>|<!--[\s\S]*?-->|<[^>"']*(?:(?:"[^"]*"|'[^']*')[^>"']*)*>|(?<text>[^<]+)""", RegexOptions.IgnoreCase
		| RegexOptions.ExplicitCapture, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex Tokens();

	[GeneratedRegex(@"\s*\n\s*", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex LineBreakRun();
}
