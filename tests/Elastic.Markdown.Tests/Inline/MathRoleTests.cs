// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Testing;
using Nullean.Snapshots.Html;

namespace Elastic.Markdown.Tests.Inline;

public class MathRoleTests() : InlineTest("""
	Einstein's famous equation {math}`E = mc^2` relates energy and mass.
	""")
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Einstein's famous equation
			<span class="math">E = mc^2</span>
			relates energy and mass.</p>
		""",
			DocsHtml.Body()
		);
}

public class MathRoleEscapesContentTests() : InlineTest("""
	Compare {math}`a < b & b > c` for ordering.
	""")
{
	[Test]
	public void Render() =>
		Html.Should().MatchInlineHtmlSnapshot(
			"""
		<p>Compare
			<span class="math">a &lt; b &amp; b &gt; c</span>
			for ordering.</p>
		""",
			DocsHtml.Body()
		);
}
