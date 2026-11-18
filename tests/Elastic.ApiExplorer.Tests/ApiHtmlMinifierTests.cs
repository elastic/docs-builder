// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Nullean.Snapshots;

namespace Elastic.ApiExplorer.Tests;

public class ApiHtmlMinifierTests
{
	private static string Minify(string html) => ApiHtmlMinifier.Minify(html, "/api/test", NullLogger.Instance);

	[Test]
	public void Minify_FoldedRows_KeepHiddenUntilFound() =>
		Minify("<div class=\"nested-properties\" id=\"a--children\" hidden=\"until-found\">\n\t<p>x</p>\n</div>\n<div hidden>y</div>")
			.Should()
			.MatchInlineSnapshot(
				"<div class=\"nested-properties\" id=\"a--children\" hidden=\"until-found\"><p>x</p></div><div hidden>y</div>"
			);

	[Test]
	public void Minify_RowMarkup_DropsIndentationAndKeepsInlineSpaces() =>
		Minify(
			"<dl class=\"property-list\">\n\t<div class=\"property-item depth-0    \">\n\t\t<dt id=\"q\">\n\t\t\t<code>a</code> <span class=\"union-inline-separator\">or</span> <code>b</code>\n\t\t</dt>\n\t\t<dd>\n\t\t\t<p>Text</p>\n\t\t</dd>\n\t</div>\n</dl>"
		)
			.Should()
			.MatchInlineSnapshot(
				"<dl class=\"property-list\"><div class=\"property-item depth-0\"><dt id=\"q\"><code>a</code> <span class=\"union-inline-separator\">or</span> <code>b</code></dt><dd><p>Text</p></dd></div></dl>"
			);

	[Test]
	public void Minify_Preformatted_KeepsItsContent() =>
		Minify("<pre><code class=\"language-json\">{\n  \"a\": 1\n}</code></pre>")
			.Should()
			.MatchInlineSnapshot(
				"""
				<pre><code class="language-json">{
				  "a": 1
				}</code></pre>
				"""
			);
}
