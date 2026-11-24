// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.ApiExplorer.Components.PropertyTree;
using Elastic.ApiExplorer.Infrastructure;
using static Elastic.ApiExplorer.Tests.TestSpecs;

namespace Elastic.ApiExplorer.Tests;

/// <summary>Text from a spec reaches the markdown copy as code, so a backtick inside it must not end the code early.</summary>
public class ApiCommonMarkTests
{
	[Test]
	[Arguments("HTTP <code> responses")]
	[Arguments("Snapshot & restore")]
	[Arguments("R&D;")]
	[Arguments("&amp; and &#123;")]
	[Arguments("max_docs and _types.query_dsl")]
	[Arguments("__init__ and *glob* and `tick`")]
	[Arguments("[link](url) and ![img](x)")]
	[Arguments("back\\slash > quote")]
	public void Text_ReadBackAsMarkdown_IsTheTextAsWritten(string text) =>
		ApiMarkdown.ToPlainText(ApiCommonMark.Text(text)).Should().Be(text);

	[Test]
	[Arguments("Search by #", "# Search by \\#")]
	[Arguments("C#", "# C#")]
	[Arguments("1. List roles", "# 1. List roles")]
	public void Heading_LiteralTitle_ReadsBackAsWritten(string title, string expected)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, ApiCommonMark.Text(title));

		markdown.ToString().TrimEnd().Should().Be(expected);
		ApiMarkdown.ToPlainText(markdown.ToString()).Should().Be(title);
	}

	[Test]
	public void Heading_AuthoredClosingSequence_IsLeftForMarkdownToRead()
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 2, "Support ##");

		ApiMarkdown.ToPlainText(markdown.ToString()).Should().Be("Support");
	}

	[Test]
	public void CanonicalizeLinks_LabelEndingInAnEscapedBackslash_IsStillALink() =>
		ApiMarkdown
			.CanonicalizeLinks("[path\\\\](status.md)", new Uri("https://www.elastic.co"))
			.Should()
			.Be("[path\\\\](https://www.elastic.co/status.md)");

	[Test]
	public void Text_LinkLikeName_StaysTextThroughTheLinkPass() =>
		ApiMarkdown
			.CanonicalizeLinks(ApiCommonMark.Text("See [status](status.md)"), new Uri("https://www.elastic.co"))
			.Should()
			.Be("See \\[status\\](status.md)");

	[Test]
	[Arguments("<p>Returns the index name.</p>", "Returns the index name.")]
	[Arguments("See <a href=\"https://example.com\">the docs</a>.", "See the docs.")]
	[Arguments("Use `<index>` or \\<alias\\>.", "Use <index> or <alias>.")]
	[Arguments("Uses `max_docs` and **bold** [links](x) &amp; more.", "Uses max_docs and bold links & more.")]
	// The page renders a description's raw HTML and its sanitizer removes an unknown tag, so a bare placeholder is not shown, and an
	// image shows as a picture, not as its alt text. Plain text follows the page; a placeholder meant to show goes in a code span.
	[Arguments("Returns <index> when available.", "Returns when available.")]
	[Arguments("See ![shard layout](diagram.png).", "See .")]
	public void ToPlainText_Markdown_IsTheTextThePageShows(string markdown, string expected) =>
		ApiMarkdown.ToPlainText(markdown).Should().Be(expected);

	[Test]
	[Arguments("Snapshot and restore", "Snapshot and restore")]
	[Arguments("max_docs", "max_docs")]
	[Arguments("HTTP <code>", "HTTP \\<code\\>")]
	public void Text_PlainName_EscapesOnlyWhatCouldBeMarkup(string text, string expected) => ApiCommonMark.Text(text).Should().Be(expected);

	[Test]
	[Arguments("plain", "`plain`")]
	[Arguments("a`b", "``a`b``")]
	[Arguments("`x", "`` `x ``")]
	[Arguments("x`", "`` x` ``")]
	[Arguments("a``b", "```a``b```")]
	[Arguments("", "``")]
	[Arguments(null, "``")]
	public void CodeSpan_TextWithBackticks_UsesALongerDelimiter(string? text, string expected) =>
		ApiCommonMark.CodeSpan(text).Should().Be(expected);

	[Test]
	[Arguments("GET /_search", "```")]
	[Arguments("a ``` b", "````")]
	[Arguments("line\n```\nline", "````")]
	[Arguments("a ```` b", "`````")]
	public void Fence_SourceWithBackticks_OpensWithALongerFence(string source, string fence)
	{
		var markdown = new StringBuilder();

		ApiCommonMark.Fence(markdown, "http", source);

		// AppendLine writes \r\n on Windows.
		markdown.ToString().ReplaceLineEndings("\n").Should().StartWith($"{fence}http\n").And.Contain($"\n{fence}\n");
	}

	[Test]
	public void Prepare_MustacheWithBacktick_StaysInsideItsCodeSpan() =>
		ApiMarkdown.Prepare("Set {{`name`}} first.", "/api/fixture").Should().Be("Set ``{{`name`}}`` first.");

	[Test]
	public async Task WriteList_NameAndValuesWithBackticks_KeepEachInItsOwnCodeSpan()
	{
		var document = await LoadSpecAsync(
			"""
			{
			  "openapi": "3.0.3",
			  "info": { "title": "t", "version": "1" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Holder": {
			        "type": "object",
			        "properties": {
			          "a`b": { "type": "string", "enum": ["x`y", "plain"] }
			        }
			      }
			    }
			  }
			}
			"""
		);
		var list = BuilderFor(document).BuildPropertyList(document.Components!.Schemas!["Holder"], new PropertyTreeScope { Prefix = "" })!;
		var markdown = new StringBuilder();

		ApiPropertyMarkdown.WriteList(markdown, list, "/api/fixture");

		markdown.ToString().Should().Contain("- ``a`b`` (enum)").And.Contain("Values: ``x`y``, `plain`");
	}
}
