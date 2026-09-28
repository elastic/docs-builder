// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Myst.CodeBlocks;

namespace Elastic.Authoring.Tests.Blocks.CodeBlocks;

public class WarnsOnInvalidLanguage : MarkdownTest
{
	protected override string Markdown => """
		```not-a-valid-language
		```
		""";

	[Test, DisplayName("validate HTML: generates link and alt attr")]
	public async Task ValidateHtml() => await Docs.HasWarning("Unknown language: not-a-valid-language");

	[Test, DisplayName("parses to EnhancedCodeBlock")]
	public async Task ParsesCodeBlock()
	{
		var codeBlocks = await Docs.Converts("index.md").Parses<EnhancedCodeBlock>();
		codeBlocks.Should().HaveCount(1);
	}
}
