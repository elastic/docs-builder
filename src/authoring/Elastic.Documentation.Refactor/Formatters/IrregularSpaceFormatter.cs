// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Text;

namespace Elastic.Documentation.Refactor.Formatters;

/// <summary>
/// Formatter that replaces irregular space characters with regular spaces
/// </summary>
public class IrregularSpaceFormatter : IFormatter
{
	public string Name => "irregular space";

	// Collection of irregular space characters that may impair Markdown rendering
	private static readonly char[] IrregularSpaceChars =
	[
		'\u000B', // Line Tabulation (\v) - <VT>
		'\u000C', // Form Feed (\f) - <FF>
		'\u00A0', // No-Break Space - <NBSP>
		'\u0085', // Next Line
		'\u1680', // Ogham Space Mark
		'\u180E', // Mongolian Vowel Separator - <MVS>
		'\ufeff', // Zero Width No-Break Space - <BOM>
		'\u2000', // En Quad
		'\u2001', // Em Quad
		'\u2002', // En Space - <ENSP>
		'\u2003', // Em Space - <EMSP>
		'\u2004', // Tree-Per-Em
		'\u2005', // Four-Per-Em
		'\u2006', // Six-Per-Em
		'\u2007', // Figure Space
		'\u2008', // Punctuation Space - <PUNCSP>
		'\u2009', // Thin Space
		'\u200A', // Hair Space
		'\u200B', // Zero Width Space - <ZWSP>
		'\u2028', // Line Separator
		'\u2029', // Paragraph Separator
		'\u202F', // Narrow No-Break Space
		'\u205F', // Medium Mathematical Space
		'\u3000'  // Ideographic Space
	];

	private static readonly SearchValues<char> IrregularSpaceSearchValues = SearchValues.Create(IrregularSpaceChars);

	public (string content, int changes) Format(string content)
	{
		// Quick check - if no irregular space, return original
		if (content.AsSpan().IndexOfAny(IrregularSpaceSearchValues) == -1)
			return (content, 0);

		// Replace irregular space with regular spaces
		var sb = new StringBuilder(content.Length);
		var replacements = 0;

		foreach (var c in content)
		{
			if (IrregularSpaceSearchValues.Contains(c))
			{
				_ = sb.Append(' ');
				replacements++;
			}
			else
			{
				_ = sb.Append(c);
			}
		}

		return (sb.ToString(), replacements);
	}
}
