// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Drops the template indentation from the rendered sidebar. Every page carries the whole tree, so on the largest
/// API products the tabs alone cost about 30 KB per page.
/// </summary>
public static partial class NavigationWhitespace
{
	/// <summary>
	/// Turns each whitespace run that holds a line break into one line break, which renders the same under
	/// <c>white-space: normal</c>. The sidebar holds no preformatted text and no attribute value with a line break.
	/// </summary>
	public static string Collapse(string html) => LineBreakRun().Replace(html, "\n");

	[GeneratedRegex(@"\s*\n\s*", RegexOptions.None, RegexTimeouts.MatchTimeoutMilliseconds)]
	private static partial Regex LineBreakRun();
}
