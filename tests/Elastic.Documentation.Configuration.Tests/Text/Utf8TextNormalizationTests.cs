// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Text;

namespace Elastic.Documentation.Configuration.Tests.Text;

public class Utf8TextNormalizationTests
{
	[Fact]
	public void StripLeadingUtf8Bom_EmptyString_ReturnsEmpty()
	{
		var result = Utf8TextNormalization.StripLeadingUtf8Bom(string.Empty);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void StripLeadingUtf8Bom_NullString_ReturnsNull()
	{
		var result = Utf8TextNormalization.StripLeadingUtf8Bom(null);

		result.Should().BeNull();
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringWithoutBom_ReturnsUnchanged()
	{
		const string input = "type: feature\ntitle: Test changelog entry";

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().BeSameAs(input); // Should return the same instance for efficiency
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringWithSingleLeadingBom_RemovesBom()
	{
		const string content = "type: feature\ntitle: Test changelog entry";
		var input = Utf8TextNormalization.Utf8BomChar + content;

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().Be(content);
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringWithTwoConsecutiveLeadingBoms_RemovesBothBoms()
	{
		const string content = "type: feature\ntitle: Test changelog entry";
		var input = Utf8TextNormalization.Utf8BomChar.ToString() +
					Utf8TextNormalization.Utf8BomChar + content;

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().Be(content);
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringWithThreeConsecutiveLeadingBoms_RemovesAllBoms()
	{
		const string content = "type: feature\ntitle: Test changelog entry";
		var input = Utf8TextNormalization.Utf8BomChar.ToString() +
					Utf8TextNormalization.Utf8BomChar +
					Utf8TextNormalization.Utf8BomChar + content;

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().Be(content);
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringOnlyBoms_ReturnsEmpty()
	{
		var input = Utf8TextNormalization.Utf8BomChar.ToString() +
					Utf8TextNormalization.Utf8BomChar;

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringWithBomInMiddle_DoesNotStripMiddleBom()
	{
		var input = $"type: feature{Utf8TextNormalization.Utf8BomChar}title: Test";

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().Be(input);
	}

	[Fact]
	public void StripLeadingUtf8Bom_StringWithBomAtEnd_DoesNotStripEndBom()
	{
		var input = $"type: feature\ntitle: Test{Utf8TextNormalization.Utf8BomChar}";

		var result = Utf8TextNormalization.StripLeadingUtf8Bom(input);

		result.Should().Be(input);
	}

	[Fact]
	public void HasUtf8Bom_EmptySpan_ReturnsFalse()
	{
		var bytes = ReadOnlySpan<byte>.Empty;

		var result = Utf8TextNormalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasUtf8Bom_TooShortSpan_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB]);

		var result = Utf8TextNormalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasUtf8Bom_ValidBomBytes_ReturnsTrue()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBF, 0x74, 0x79]);

		var result = Utf8TextNormalization.HasUtf8Bom(bytes);

		result.Should().BeTrue();
	}

	[Fact]
	public void HasUtf8Bom_ExactBomBytes_ReturnsTrue()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBF]);

		var result = Utf8TextNormalization.HasUtf8Bom(bytes);

		result.Should().BeTrue();
	}

	[Fact]
	public void HasUtf8Bom_InvalidBomBytes_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBE, 0x74, 0x79]);

		var result = Utf8TextNormalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasUtf8Bom_NormalTextBytes_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0x74, 0x79, 0x70, 0x65]);

		var result = Utf8TextNormalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void Utf8BomChar_MatchesExpectedValue()
	{
		Utf8TextNormalization.Utf8BomChar.Should().Be('\uFEFF');
	}

	[Fact]
	public void Utf8BomBytes_MatchesExpectedSequence()
	{
		Utf8TextNormalization.Utf8BomBytes.Should().Equal([0xEF, 0xBB, 0xBF]);
	}
}
