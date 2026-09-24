// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst;

namespace Elastic.ApiExplorer.Tests;

/// <summary>
/// Verifies that HTML in OAS description fields renders as actual HTML, not as escaped literal text.
/// See https://github.com/elastic/docs-eng-team/issues/871
/// </summary>
public class ApiDescriptionHtmlRenderingTests
{
	[Test]
	public void ApiDescriptionPipeline_RendersHtmlTags_NotLiteralText()
	{
		var markdown = "Some text.<br>\n\nNext paragraph.";
		var html = Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline);

		html.Should().Contain("<br>");
		html.Should().NotContain("&lt;br&gt;");
	}

	[Test]
	public void ApiDescriptionPipeline_RendersAnchorLinks_NotEscapedText()
	{
		var markdown = """See <a href="https://example.com" class="reference">the docs</a> for more.""";
		var html = Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline);

		html.Should().Contain("<a href=\"https://example.com\"");
		html.Should().NotContain("&lt;a href");
	}

	[Test]
	public void StandardPipeline_EscapesHtmlBlocks()
	{
		// Verify the standard pipeline still disables HTML blocks — don't regress that behaviour.
		var markdown = "<div class=\"custom\">\n\nSome text.\n\n</div>";
		var html = Markdig.Markdown.ToHtml(markdown, MarkdownParser.Pipeline);

		html.Should().NotContain("<div");
	}
}
