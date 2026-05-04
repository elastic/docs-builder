// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Changelog.Utilities;

/// <summary>
/// Utilities for normalizing UTF-8 encoding in changelog YAML files.
/// Ensures YAML output is UTF-8 without BOM for better tooling compatibility and review ergonomics.
/// </summary>
public static class ChangelogUtf8Normalization
{
	/// <summary>
	/// UTF-8 Byte Order Mark character (U+FEFF).
	/// </summary>
	public const char Utf8BomChar = '\uFEFF';

	/// <summary>
	/// UTF-8 Byte Order Mark as byte sequence (EF BB BF).
	/// </summary>
	public static readonly byte[] Utf8BomBytes = [0xEF, 0xBB, 0xBF];

	/// <summary>
	/// Strips the leading UTF-8 BOM character from a string if present.
	/// YAML should be UTF-8 without BOM for tooling and review ergonomics.
	/// </summary>
	/// <param name="text">The text to normalize</param>
	/// <returns>Text with leading BOM character removed if it was present</returns>
	public static string StripLeadingUtf8BomChar(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		return text.StartsWith(Utf8BomChar) ? text.Substring(1) : text;
	}

	/// <summary>
	/// Checks if a byte span starts with the UTF-8 BOM sequence (EF BB BF).
	/// </summary>
	/// <param name="bytes">The byte span to check</param>
	/// <returns>True if the span starts with UTF-8 BOM bytes</returns>
	public static bool HasUtf8Bom(ReadOnlySpan<byte> bytes) => bytes.Length >= 3 &&
			   bytes[0] == 0xEF &&
			   bytes[1] == 0xBB &&
			   bytes[2] == 0xBF;
}
