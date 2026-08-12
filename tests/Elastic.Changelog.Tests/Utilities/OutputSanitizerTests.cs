// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Utilities;

namespace Elastic.Changelog.Tests.Utilities;

public class OutputSanitizerTests
{
	[Fact]
	public void NullInput_ReturnsEmpty() =>
		OutputSanitizer.SanitizeForOutput(null, 100).Should().Be(string.Empty);

	[Fact]
	public void EmptyInput_ReturnsEmpty() =>
		OutputSanitizer.SanitizeForOutput(string.Empty, 100).Should().Be(string.Empty);

	[Fact]
	public void ZeroMaxLength_ReturnsEmpty() =>
		OutputSanitizer.SanitizeForOutput("anything", 0).Should().Be(string.Empty);

	[Fact]
	public void NegativeMaxLength_ReturnsEmpty() =>
		OutputSanitizer.SanitizeForOutput("anything", -1).Should().Be(string.Empty);

	[Fact]
	public void PlainAscii_PassesThrough() =>
		OutputSanitizer.SanitizeForOutput("Add new search API", 100)
			.Should().Be("Add new search API");

	[Fact]
	public void PreservesNewlinesAndTabs()
	{
		var input = "line1\nline2\twith tab\nline3";
		OutputSanitizer.SanitizeForOutput(input, 100).Should().Be(input);
	}

	[Fact]
	public void StripsNullBytes() =>
		OutputSanitizer.SanitizeForOutput("hello\0world", 100).Should().Be("helloworld");

	[Fact]
	public void StripsCarriageReturn() =>
		OutputSanitizer.SanitizeForOutput("line1\r\nline2", 100).Should().Be("line1\nline2");

	[Theory]
	[InlineData('\u0001')]
	[InlineData('\u0007')]
	[InlineData('\u001b')]
	[InlineData('\u001f')]
	[InlineData('\u007f')]
	public void StripsC0AndDelControlCharacters(char control)
	{
		var input = $"safe{control}value";
		OutputSanitizer.SanitizeForOutput(input, 100).Should().Be("safevalue");
	}

	[Fact]
	public void TruncatesAtMaxLength()
	{
		var input = new string('a', 250);
		OutputSanitizer.SanitizeForOutput(input, 200).Should().HaveLength(200);
	}

	[Fact]
	public void TruncatesBeforeStrippedCharsAreCounted()
	{
		// Stripped characters do not count toward the cap; the result
		// should contain exactly maxLength surviving characters.
		var input = "\0\0abc\0def\0ghi";
		OutputSanitizer.SanitizeForOutput(input, 5).Should().Be("abcde");
	}

	[Fact]
	public void TruncationIsCharacterBasedNotByteBased()
	{
		// Emoji are surrogate pairs (2 chars each in C#); the cap counts chars,
		// not graphemes — that's the same behavior as String.Substring.
		var input = "🚀🚀🚀🚀🚀";
		var result = OutputSanitizer.SanitizeForOutput(input, 4);
		result.Should().HaveLength(4);
	}

	[Fact]
	public void GitHubOutputDelimiterMimic_HasControlCharsStripped()
	{
		// A hostile PR title that tries to inject a fake GITHUB_OUTPUT line.
		// The C0 stripping leaves the visible text intact; the random-delimiter
		// framing in Actions.Core handles the rest.
		var input = "evil\u0000fake-output<<EOF\nattacker=true\nEOF";
		var result = OutputSanitizer.SanitizeForOutput(input, 200);
		result.Should().Be("evilfake-output<<EOF\nattacker=true\nEOF");
		result.Should().NotContain("\u0000");
	}

	[Fact]
	public void RealisticPrTitle_FitsWithinTitleCap()
	{
		var input = "[7.17] Backport: improve search aggregation performance for large indices";
		OutputSanitizer.SanitizeForOutput(input, OutputSanitizer.TitleMaxLength)
			.Should().Be(input);
	}

	[Fact]
	public void HugeBody_TruncatedToDescriptionCap()
	{
		var input = new string('x', OutputSanitizer.DescriptionMaxLength * 2);
		OutputSanitizer.SanitizeForOutput(input, OutputSanitizer.DescriptionMaxLength)
			.Should().HaveLength(OutputSanitizer.DescriptionMaxLength);
	}
}
