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

		markdown.ToString().Should().StartWith($"{fence}http\n").And.Contain($"\n{fence}\n");
	}

	[Test]
	public void Prepare_MustacheWithBacktick_StaysInsideItsCodeSpan() =>
		ApiMarkdown.Prepare("Set {{`name`}} first.", "/api/doc/fixture").Should().Be("Set ``{{`name`}}`` first.");

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

		ApiPropertyMarkdown.WriteList(markdown, list, "/api/doc/fixture");

		markdown.ToString().Should().Contain("- ``a`b`` (enum)").And.Contain("Values: ``x`y``, `plain`");
	}
}
