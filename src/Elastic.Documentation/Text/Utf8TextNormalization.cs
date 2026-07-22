// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Text;

/// <summary>
/// UTF-8 text normalization utilities for handling Byte Order Marks (BOMs) and related text encoding concerns.
/// </summary>
public static class Utf8TextNormalization
{
	/// <summary>
	/// UTF-8 Byte Order Mark character (U+FEFF Zero Width No-Break Space).
	/// </summary>
	public const char Utf8BomChar = '\uFEFF';

	/// <summary>
	/// UTF-8 Byte Order Mark byte sequence (EF BB BF).
	/// </summary>
	public static readonly byte[] Utf8BomBytes = [0xEF, 0xBB, 0xBF];

	/// <summary>
	/// Strips all consecutive leading UTF-8 BOM characters (U+FEFF) from the beginning of a string.
	/// <para>
	/// This method removes the UTF-8 Byte Order Mark / Zero Width No-Break Space character only.
	/// It does NOT strip other zero-width characters like U+200B (Zero Width Space) or U+2060 (Word Joiner)
	/// as they can appear in legitimate content and are not part of the UTF-8 BOM sequence.
	/// </para>
	/// </summary>
	/// <param name="text">The input string, which may be null or empty.</param>
	/// <returns>The string with leading BOM characters removed, or the original string if null/empty or no BOM present.</returns>
	public static string? StripLeadingUtf8Bom(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		// Strip all consecutive leading U+FEFF characters
		var span = text.AsSpan();
		while (span.Length > 0 && span[0] == Utf8BomChar)
		{
			span = span[1..];
		}

		return span.Length == text.Length ? text : span.ToString();
	}

	/// <summary>
	/// Checks if the given byte span starts with the UTF-8 Byte Order Mark sequence (EF BB BF).
	/// </summary>
	/// <param name="bytes">The byte span to check.</param>
	/// <returns>True if the span starts with the UTF-8 BOM sequence, false otherwise.</returns>
	public static bool HasUtf8Bom(ReadOnlySpan<byte> bytes) =>
		bytes.Length >= 3 &&
		bytes[0] == 0xEF &&
		bytes[1] == 0xBB &&
		bytes[2] == 0xBF;
}
