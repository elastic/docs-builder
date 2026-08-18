// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Tests.Helpers;

public class StripMarkdown_PlainTextInput_ReturnsInputUnchanged
{
	[Theory]
	[InlineData("Hello World")]
	[InlineData("en/security/8.17/install-endpoint.md")]
	[InlineData("Getting started with Elasticsearch")]
	[InlineData("")]
	public void DoesNotAllocate(string input) =>
		// For plain-text strings the fast path must return the exact same reference,
		// avoiding the StringWriter allocation entirely.
		input.StripMarkdown().Should().BeSameAs(input);
}

public class StripMarkdown_EscapedAsterisks_StripsEscapes
{
	[Fact]
	public void UnescapesBackslashEscapedSpans() =>
		@"\*literal\*".StripMarkdown().Should().Be("*literal*");
}

public class StripMarkdown_MarkdownInput_StripsFormatting
{
	[Theory]
	[InlineData("`inline code`", "inline code")]
	[InlineData("**bold text**", "bold text")]
	[InlineData("_italic text_", "italic text")]
	[InlineData("[link text](https://example.com)", "link text")]
	public void RemovesMarkdownSyntax(string input, string expected) =>
		input.StripMarkdown().Should().Be(expected);
}
