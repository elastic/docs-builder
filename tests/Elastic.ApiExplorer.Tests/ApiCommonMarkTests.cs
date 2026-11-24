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
		ApiSeoDescription.ToPlainText(ApiCommonMark.Text(text)).Should().Be(text);

	[Test]
	[Arguments("Search by #", "# Search by \\#")]
	[Arguments("C#", "# C#")]
	[Arguments("1. List roles", "# 1. List roles")]
	public void Heading_LiteralTitle_ReadsBackAsWritten(string title, string expected)
	{
		var markdown = new StringBuilder();
		ApiCommonMark.Heading(markdown, 1, ApiCommonMark.Text(title));

		markdown.ToString().TrimEnd().Should().Be(expected);
		ApiSeoDescription.ToPlainText(markdown.ToString()).Should().Be(title);
	}

	[Test]
	public void Text_LinkLikeName_StaysTextThroughTheLinkPass() =>
		ApiMarkdown
			.CanonicalizeLinks(ApiCommonMark.Text("See [status](status.md)"), new Uri("https://www.elastic.co"))
			.Should()
			.Be("See \\[status\\](status.md)");

	[Test]
	[Arguments("Returns <index> when available.", "Returns <index> when available.")]
	[Arguments("<p>Returns the index name.</p>", "<p>Returns the index name.</p>")]
	public void ToPlainText_Html_StaysAsWritten(string markdown, string expected) =>
		ApiSeoDescription.ToPlainText(markdown).Should().Be(expected);

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
