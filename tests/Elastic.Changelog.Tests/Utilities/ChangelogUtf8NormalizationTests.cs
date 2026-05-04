// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Changelog.Utilities;

namespace Elastic.Changelog.Tests.Utilities;

public class ChangelogUtf8NormalizationTests
{
	[Fact]
	public void StripLeadingUtf8BomChar_EmptyString_ReturnsEmpty()
	{
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(string.Empty);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void StripLeadingUtf8BomChar_NullString_ReturnsNull()
	{
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(null!);

		result.Should().BeNull();
	}

	[Fact]
	public void StripLeadingUtf8BomChar_StringWithoutBom_ReturnsUnchanged()
	{
		const string input = "type: feature\ntitle: Test";

		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);

		result.Should().Be(input);
	}

	[Fact]
	public void StripLeadingUtf8BomChar_StringWithLeadingBom_RemovesBom()
	{
		const string content = "type: feature\ntitle: Test";
		var input = ChangelogUtf8Normalization.Utf8BomChar + content;

		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);

		result.Should().Be(content);
	}

	[Fact]
	public void StripLeadingUtf8BomChar_StringOnlyBom_ReturnsEmpty()
	{
		var input = ChangelogUtf8Normalization.Utf8BomChar.ToString();

		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);

		result.Should().Be(string.Empty);
	}

	[Fact]
	public void StripLeadingUtf8BomChar_StringWithBomInMiddle_DoesNotChange()
	{
		var input = $"type: feature{ChangelogUtf8Normalization.Utf8BomChar}title: Test";

		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);

		result.Should().Be(input);
	}

	[Fact]
	public void HasUtf8Bom_EmptySpan_ReturnsFalse()
	{
		var bytes = ReadOnlySpan<byte>.Empty;

		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasUtf8Bom_TooShortSpan_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB]);

		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasUtf8Bom_ValidBomBytes_ReturnsTrue()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBF, 0x74, 0x79]);

		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);

		result.Should().BeTrue();
	}

	[Fact]
	public void HasUtf8Bom_ExactBomBytes_ReturnsTrue()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBF]);

		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);

		result.Should().BeTrue();
	}

	[Fact]
	public void HasUtf8Bom_InvalidBomBytes_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBE, 0x74, 0x79]);

		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}

	[Fact]
	public void HasUtf8Bom_NormalYamlBytes_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0x74, 0x79, 0x70, 0x65]);

		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);

		result.Should().BeFalse();
	}
}
