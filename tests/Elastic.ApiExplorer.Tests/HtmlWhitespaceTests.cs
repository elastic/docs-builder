// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;

namespace Elastic.ApiExplorer.Tests;

public class HtmlWhitespaceTests
{
	[Test]
	public void Collapse_IndentationBetweenTags_BecomesOneLineBreak() =>
		HtmlWhitespace
			.Collapse("<dl>\n\t\t<dt>\n\t\t\t<code>a</code> <span>or</span>\n\t\t</dt>\n</dl>\n")
			.Should()
			.Be("<dl>\n<dt>\n<code>a</code> <span>or</span>\n</dt>\n</dl>\n");

	[Test]
	public void Collapse_PreformattedAndRawText_StayAsTheyAre()
	{
		const string kept =
			"<pre><code>{\n    \"a\": 1\n}</code></pre><textarea>x\n\n  y</textarea><script>\n\tlet a = `\n\t`\n</script><style>\n\ta {}\n</style><!--\n\tnote\n-->";
		HtmlWhitespace.Collapse(kept).Should().Be(kept);
	}

	[Test]
	public void Collapse_AttributeValues_StayAsTheyAre()
	{
		const string kept = "<span data-tippy-content=\"line one\n\n    line two > three\" title='a\n  b'>x</span>";
		HtmlWhitespace.Collapse(kept).Should().Be(kept);
	}

	[Test]
	public void Collapse_NonBreakingSpace_IsKeptBesideALineBreak() =>
		HtmlWhitespace.Collapse("<span>a</span>\u00A0\n\t<span>b</span>").Should().Be("<span>a</span>\u00A0\n<span>b</span>");

	[Test]
	public void Collapse_StrayLessThanInText_IsText() => HtmlWhitespace.Collapse("<p>a < b\n\t\tc</p>").Should().Be("<p>a < b\nc</p>");

	[Test]
	public void Collapse_ApostropheInUnquotedValue_DoesNotOpenAQuote() =>
		HtmlWhitespace.Collapse("<a title=it's>x</a>\n\t<b>y</b>").Should().Be("<a title=it's>x</a>\n<b>y</b>");

	[Test]
	public void Collapse_LookalikeClosingTag_DoesNotEndTheScript()
	{
		const string kept = "<script>\n\tlet t = '</script-example>';\n\tlet u = 1;\n</script>";
		HtmlWhitespace.Collapse(kept + "\n\t<p>x</p>").Should().Be(kept + "\n<p>x</p>");
	}

	[Test]
	public void Collapse_ClosingTagInsideAnAttribute_DoesNotEndThePre()
	{
		const string kept = "<pre><span title=\"a </pre> b\">{\n    \"a\": 1\n}</span></pre>";
		HtmlWhitespace.Collapse(kept + "\n\t<p>x</p>").Should().Be(kept + "\n<p>x</p>");
	}

	[Test]
	public void Collapse_CommentInsideAPre_DoesNotHideItsClosingTag() =>
		// Read as a tag, the comment's quote would pair with the title's and hide </pre>, leaving the rest uncollapsed.
		HtmlWhitespace
			.Collapse("<pre><!-- a=\" --> x</pre>\n\t<p title=\"t\">y</p>\n\t<p>z</p>")
			.Should()
			.Be("<pre><!-- a=\" --> x</pre>\n<p title=\"t\">y</p>\n<p>z</p>");

	[Test]
	public void Collapse_CommentOpenerInATextareaInsideAPre_IsText() =>
		// In a textarea "<!--" is text; read as a comment with no "-->", it would leave the rest of the page uncollapsed.
		HtmlWhitespace
			.Collapse("<pre><textarea><!-- a</textarea> x</pre>\n\t<p>y</p>")
			.Should()
			.Be("<pre><textarea><!-- a</textarea> x</pre>\n<p>y</p>");

	[Test]
	public void Collapse_NestedPre_EndsAtTheOutermostClosingTag() =>
		HtmlWhitespace.Collapse("<pre><pre>a\n  b</pre>\n  c</pre>\n\t<p>y</p>").Should().Be("<pre><pre>a\n  b</pre>\n  c</pre>\n<p>y</p>");

	[Test]
	public void Collapse_DeeplyNestedPre_DoesNotExhaustTheStack()
	{
		const int depth = 200_000;
		var nested = string.Concat(Enumerable.Repeat("<pre>", depth)) + "x" + string.Concat(Enumerable.Repeat("</pre>", depth));

		HtmlWhitespace.Collapse(nested + "\n\t<p>y</p>").Should().Be(nested + "\n<p>y</p>");
	}

	[Test]
	public void Collapse_LargePage_DropsEveryRowIndentation()
	{
		// The largest API pages reach about 7 MB; this is the row partial repeated at that scale.
		var row =
			"<div class=\"property-item depth-6\">\n\t\t<dt id=\"a\"><a aria-label=\"Link to a.&lt;string&gt;.b\"></a>\n\t\t\t<code>b</code></dt>\n\t</div>\n";
		var page = string.Concat(Enumerable.Repeat(row, 70_000));

		var collapsed = HtmlWhitespace.Collapse(page);

		collapsed.Should().Be(string.Concat(Enumerable.Repeat(row.Replace("\t", ""), 70_000)));
	}
}
