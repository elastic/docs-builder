// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.ApiExplorer.Infrastructure;
using Elastic.Markdown.Myst;
using Nullean.Snapshots.Html;

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

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>Some text.<br>
			</p>
			<p>Next paragraph.</p>
			"""
		);
	}

	[Test]
	public void ApiDescriptionPipeline_RendersAnchorLinks_NotEscapedText()
	{
		var markdown = """See <a href="https://example.com" class="reference">the docs</a> for more.""";
		var html = Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>See
				<a href="https://example.com" class="reference">the docs</a>
				for more.</p>
			"""
		);
	}

	[Test]
	public void ApiDescriptionPipeline_RendersEveryLineOfKibanaOperationDescription()
	{
		var markdown =
			"""
			**Spaces method and path for this operation:**

			<div><span class="operation-verb get">get</span>&nbsp;<span class="operation-path">/s/{space_id}/api/spaces/space</span></div>

			Refer to [Spaces](https://www.elastic.co/docs/deploy-manage/manage-spaces) for more information.

			Get a space by its identifier.
			""";
		var html = ApiHtmlSanitizer.Sanitize(Markdig.Markdown.ToHtml(markdown, MarkdownParser.ApiDescriptionPipeline));

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>
				<strong>Spaces method and path for this operation:</strong>
			</p>
			<div>
				<span class="operation-verb get">get</span><span class="operation-path">/s/{space_id}/api/spaces/space</span>
			</div>
			<p>Refer to
				<a href="https://www.elastic.co/docs/deploy-manage/manage-spaces">Spaces</a>
				for more information.</p>
			<p>Get a space by its identifier.</p>
			"""
		);
	}

	[Test]
	public void StandardPipeline_EscapesHtmlBlocks()
	{
		// Verify the standard pipeline still disables HTML blocks — don't regress that behaviour.
		var markdown = "<div class=\"custom\">\n\nSome text.\n\n</div>";
		var html = Markdig.Markdown.ToHtml(markdown, MarkdownParser.Pipeline);

		html.Should().MatchInlineHtmlSnapshot(
			"""
			<p>&lt;div class="custom"&gt;</p>
			<p>Some text.</p>
			<p>&lt;/div&gt;</p>
			"""
		);
	}
}
