// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.LegacyDocs.Migration.Asciidoc;

namespace Elastic.LegacyDocs.Migration.Tests;

public class LexerTests
{
	[Fact]
	public void OpenBlock_DashDash_IsNotVerbatim()
	{
		var tokens = AsciidocLexer.Tokenize("--\nSome content\n--");
		var delimiters = tokens.Where(t => t.Type == TokenType.BlockDelimiter).ToList();

		delimiters.Should().HaveCount(2);
		// Both are regular (non-verbatim) block delimiters
		var text = tokens.Where(t => t.Type == TokenType.Text).ToList();
		text.Should().HaveCount(1);
		text[0].Raw.Should().Be("Some content");
	}

	[Fact]
	public void VerbatimBlock_FourDashes_IsVerbatim()
	{
		var tokens = AsciidocLexer.Tokenize("----\n<1> callout marker\n----");
		var text = tokens.Where(t => t.Type == TokenType.Text).ToList();

		// Content inside ---- block is raw text (verbatim)
		text.Should().HaveCount(1);
		text[0].Raw.Should().Be("<1> callout marker");
	}

	[Fact]
	public void ClosingDelimiter_RequiresExactLength()
	{
		// A longer closing delimiter should NOT close the block
		var content = "----\nfoo\n--------\nbar\n----";
		var tokens = AsciidocLexer.Tokenize(content);
		var textTokens = tokens.Where(t => t.Type == TokenType.Text).ToList();

		// "foo", "--------" (not a valid close), and "bar" should all be Text inside the block
		textTokens.Should().HaveCount(3);
	}
}
