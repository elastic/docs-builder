// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Documentation.Text;

namespace Elastic.Changelog.Utilities;

/// <summary>
/// Utilities for normalizing UTF-8 encoding in changelog YAML files.
/// Ensures YAML output is UTF-8 without BOM for better tooling compatibility and review ergonomics.
/// This class now serves as a thin forwarder to the shared UTF-8 text normalization utilities.
/// </summary>
public static class ChangelogUtf8Normalization
{
	/// <summary>
	/// UTF-8 Byte Order Mark character (U+FEFF).
	/// </summary>
	public const char Utf8BomChar = Utf8TextNormalization.Utf8BomChar;

	/// <summary>
	/// UTF-8 Byte Order Mark as byte sequence (EF BB BF).
	/// </summary>
	public static readonly byte[] Utf8BomBytes = Utf8TextNormalization.Utf8BomBytes;

	/// <summary>
	/// Strips the leading UTF-8 BOM character from a string if present.
	/// YAML should be UTF-8 without BOM for tooling and review ergonomics.
	/// </summary>
	/// <param name="text">The text to normalize</param>
	/// <returns>Text with leading BOM character removed if it was present</returns>
	public static string StripLeadingUtf8BomChar(string text) =>
		Utf8TextNormalization.StripLeadingUtf8Bom(text)!;

	/// <summary>
	/// Checks if a byte span starts with the UTF-8 BOM sequence (EF BB BF).
	/// </summary>
	/// <param name="bytes">The byte span to check</param>
	/// <returns>True if the span starts with UTF-8 BOM bytes</returns>
	public static bool HasUtf8Bom(ReadOnlySpan<byte> bytes) =>
		Utf8TextNormalization.HasUtf8Bom(bytes);
}
