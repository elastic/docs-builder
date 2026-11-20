// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.RegularExpressions;

namespace Elastic.Documentation.Assembler.ApiCrawl;

/// <summary>The canonical URL and robots value a page declares in its <c>&lt;head&gt;</c>.</summary>
public sealed partial record ApiPageHead(string? Canonical, string? Robots)
{
	public static ApiPageHead Parse(string html) =>
		new(Attribute(CanonicalLink().Match(html), HrefAttribute()), Attribute(RobotsMeta().Match(html), ContentAttribute()));

	private static string? Attribute(Match tag, Regex attribute)
	{
		if (!tag.Success)
			return null;
		var value = attribute.Match(tag.Value);
		return value.Success ? WebUtility.HtmlDecode(value.Groups["v"].Value) : null;
	}

	[GeneratedRegex("""<link\b[^>]*\brel\s*=\s*["']canonical["'][^>]*>""", RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex CanonicalLink();

	[GeneratedRegex("""<meta\b[^>]*\bname\s*=\s*["']robots["'][^>]*>""", RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex RobotsMeta();

	[GeneratedRegex("""\bhref\s*=\s*["'](?<v>[^"']*)["']""", RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex HrefAttribute();

	[GeneratedRegex("""\bcontent\s*=\s*["'](?<v>[^"']*)["']""", RegexOptions.IgnoreCase, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex ContentAttribute();
}
