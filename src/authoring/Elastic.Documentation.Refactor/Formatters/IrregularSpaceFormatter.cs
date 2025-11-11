// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Text;

namespace Elastic.Documentation.Refactor.Formatters;

/// <summary>
/// Formatter that handles irregular space characters appropriately:
/// - Removes invisible characters entirely
/// - Preserves semantically meaningful spaces
/// - Replaces problematic spaces with regular spaces
/// </summary>
public class IrregularSpaceFormatter : IFormatter
{
	public string Name => "irregular space";

	// Characters to remove entirely (invisible/problematic)
	private static readonly char[] CharactersToRemove =
	[
		'\u000B', // Line Tabulation (\v) - <VT>
		'\u000C', // Form Feed (\f) - <FF>
		'\u0085', // Next Line
		'\u1680', // Ogham Space Mark
		'\u180E', // Mongolian Vowel Separator - <MVS>
		'\ufeff', // Zero Width No-Break Space - <BOM>
		'\u200B', // Zero Width Space - <ZWSP>
		'\u2028', // Line Separator
		'\u2029'  // Paragraph Separator
	];

	// Characters to preserve (semantically meaningful)
	private static readonly char[] CharactersToPreserve =
	[
		'\u00A0', // No-Break Space - <NBSP>
		'\u2007', // Figure Space
		'\u202F', // Narrow No-Break Space
		'\u205F'  // Medium Mathematical Space
	];

	// Characters to replace with regular spaces (visible but problematic)
	private static readonly char[] CharactersToReplace =
	[
		'\u2000', // En Quad
		'\u2001', // Em Quad
		'\u2002', // En Space - <ENSP>
		'\u2003', // Em Space - <EMSP>
		'\u2004', // Tree-Per-Em
		'\u2005', // Four-Per-Em
		'\u2006', // Six-Per-Em
		'\u2008', // Punctuation Space - <PUNCSP>
		'\u2009', // Thin Space
		'\u200A', // Hair Space
		'\u3000'  // Ideographic Space
	];

	private static readonly SearchValues<char> CharactersToRemoveValues = SearchValues.Create(CharactersToRemove);
	private static readonly SearchValues<char> CharactersToPreserveValues = SearchValues.Create(CharactersToPreserve);
	private static readonly SearchValues<char> CharactersToReplaceValues = SearchValues.Create(CharactersToReplace);

	public FormatResult Format(string content)
	{
		// Quick check - if no irregular space characters, return original
		var span = content.AsSpan();
		if (span.IndexOfAny(CharactersToRemoveValues) == -1 &&
			span.IndexOfAny(CharactersToPreserveValues) == -1 &&
			span.IndexOfAny(CharactersToReplaceValues) == -1)
			return new FormatResult(content, 0);

		// Process each character with appropriate handling
		var sb = new StringBuilder(content.Length);
		var replacements = 0;

		foreach (var c in content)
		{
			if (CharactersToRemoveValues.Contains(c))
			{
				// Remove invisible/problematic characters entirely
				replacements++;
			}
			else if (CharactersToPreserveValues.Contains(c))
			{
				// Preserve semantically meaningful characters
				_ = sb.Append(c);
			}
			else if (CharactersToReplaceValues.Contains(c))
			{
				// Replace problematic visible characters with regular spaces
				_ = sb.Append(' ');
				replacements++;
			}
			else
			{
				// Keep regular characters as-is
				_ = sb.Append(c);
			}
		}

		return new FormatResult(sb.ToString(), replacements);
	}
}
