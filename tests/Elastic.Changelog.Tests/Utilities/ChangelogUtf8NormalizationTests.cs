using System;
using Elastic.Changelog.Utilities;
using TUnit.Assertions;
using TUnit.Core;

namespace Elastic.Changelog.Tests.Utilities;

public class ChangelogUtf8NormalizationTests
{
	[Test]
	public async Task StripLeadingUtf8BomChar_EmptyString_ReturnsEmpty()
	{
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(string.Empty);
		
		await Assert.That(result).IsEqualTo(string.Empty);
	}

	[Test]
	public async Task StripLeadingUtf8BomChar_NullString_ReturnsNull()
	{
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(null!);
		
		await Assert.That(result).IsNull();
	}

	[Test]
	public async Task StripLeadingUtf8BomChar_StringWithoutBom_ReturnsUnchanged()
	{
		const string input = "type: feature\ntitle: Test";
		
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);
		
		await Assert.That(result).IsEqualTo(input);
	}

	[Test]
	public async Task StripLeadingUtf8BomChar_StringWithLeadingBom_RemovesBom()
	{
		const string content = "type: feature\ntitle: Test";
		var input = ChangelogUtf8Normalization.Utf8BomChar + content;
		
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);
		
		await Assert.That(result).IsEqualTo(content);
	}

	[Test]
	public async Task StripLeadingUtf8BomChar_StringOnlyBom_ReturnsEmpty()
	{
		var input = ChangelogUtf8Normalization.Utf8BomChar.ToString();
		
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);
		
		await Assert.That(result).IsEqualTo(string.Empty);
	}

	[Test]
	public async Task StripLeadingUtf8BomChar_StringWithBomInMiddle_DoesNotChange()
	{
		var input = $"type: feature{ChangelogUtf8Normalization.Utf8BomChar}title: Test";
		
		var result = ChangelogUtf8Normalization.StripLeadingUtf8BomChar(input);
		
		await Assert.That(result).IsEqualTo(input);
	}

	[Test]
	public async Task HasUtf8Bom_EmptySpan_ReturnsFalse()
	{
		var bytes = ReadOnlySpan<byte>.Empty;
		
		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);
		
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task HasUtf8Bom_TooShortSpan_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB]);
		
		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);
		
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task HasUtf8Bom_ValidBomBytes_ReturnsTrue()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBF, 0x74, 0x79]);
		
		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);
		
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task HasUtf8Bom_ExactBomBytes_ReturnsTrue()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBF]);
		
		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);
		
		await Assert.That(result).IsTrue();
	}

	[Test]
	public async Task HasUtf8Bom_InvalidBomBytes_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0xEF, 0xBB, 0xBE, 0x74, 0x79]); // Wrong third byte
		
		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);
		
		await Assert.That(result).IsFalse();
	}

	[Test]
	public async Task HasUtf8Bom_NormalYamlBytes_ReturnsFalse()
	{
		var bytes = new ReadOnlySpan<byte>([0x74, 0x79, 0x70, 0x65]); // "type" in UTF-8
		
		var result = ChangelogUtf8Normalization.HasUtf8Bom(bytes);
		
		await Assert.That(result).IsFalse();
	}
}