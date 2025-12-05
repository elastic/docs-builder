// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Search;

public static class StringHighlightExtensions
{
	private const string MarkOpen = "<mark>";
	private const string MarkClose = "</mark>";

	/// <summary>
	/// Highlights search tokens in text by wrapping them with &lt;mark&gt; tags.
	/// Skips tokens that are already highlighted or are inside existing mark tags.
	/// </summary>
	/// <param name="text">The text to highlight tokens in</param>
	/// <param name="tokens">The search tokens to highlight</param>
	/// <param name="synonyms">Optional dictionary of synonyms to also highlight</param>
	/// <returns>Text with highlighted tokens</returns>
	public static string HighlightTokens(
		this string text,
		ReadOnlySpan<string> tokens,
		IReadOnlyDictionary<string, string[]>? synonyms = null)
	{
		if (tokens.Length == 0 || string.IsNullOrEmpty(text))
			return text;

		var result = text;

		foreach (var token in tokens)
		{
			if (string.IsNullOrEmpty(token))
				continue;

			// Highlight the token itself
			result = HighlightSingleToken(result, token);

			// Highlight synonyms for this token
			if (synonyms == null || !synonyms.TryGetValue(token, out var tokenSynonyms))
				continue;

			foreach (var synonym in tokenSynonyms)
			{
				if (!string.IsNullOrEmpty(synonym))
					result = HighlightSingleToken(result, synonym);
			}
		}

		return result;
	}

	private static string HighlightSingleToken(string text, string token)
	{
		// Check if this exact token is already fully highlighted somewhere
		// This prevents double-highlighting
		if (text.Contains($"{MarkOpen}{token}{MarkClose}", StringComparison.OrdinalIgnoreCase))
			return text;

		var sb = new StringBuilder(text.Length + 26); // Room for a couple of mark tags
		var textSpan = text.AsSpan();
		var tokenSpan = token.AsSpan();
		var pos = 0;

		while (pos < textSpan.Length)
		{
			var remaining = textSpan[pos..];
			var matchIndex = remaining.IndexOf(tokenSpan, StringComparison.OrdinalIgnoreCase);

			if (matchIndex < 0)
			{
				// No more matches, append rest and exit
				_ = sb.Append(remaining);
				break;
			}

			var absoluteIndex = pos + matchIndex;

			// Check if we're inside mark tag syntax or inside mark tag content
			if (IsInsideMarkTagSyntax(textSpan, absoluteIndex, tokenSpan.Length) || IsInsideMarkTagContent(textSpan, absoluteIndex))
			{
				// Append up to and including this match without highlighting
				_ = sb.Append(remaining[..(matchIndex + tokenSpan.Length)]);
				pos = absoluteIndex + token.Length;
				continue;
			}

			// Append text before match, then highlighted token (preserving original case)
			_ = sb.Append(remaining[..matchIndex])
				.Append(MarkOpen)
				.Append(remaining.Slice(matchIndex, tokenSpan.Length))
				.Append(MarkClose);

			pos = absoluteIndex + token.Length;
		}

		return sb.ToString();
	}

	private static bool IsInsideMarkTagSyntax(ReadOnlySpan<char> text, int position, int tokenLength)
	{
		// Check if the match position overlaps with <mark> or </mark> tag syntax
		// We want to protect the literal tag strings, not arbitrary HTML

		var matchEnd = position + tokenLength;

		// Look for <mark> that contains our position
		var searchStart = Math.Max(0, position - 5); // <mark> is 6 chars, so look back 5
		var searchEnd = Math.Min(text.Length, matchEnd + 6);
		var searchRegion = text[searchStart..searchEnd];

		var markOpenIdx = searchRegion.IndexOf(MarkOpen.AsSpan(), StringComparison.OrdinalIgnoreCase);
		if (markOpenIdx >= 0)
		{
			var absoluteMarkStart = searchStart + markOpenIdx;
			var absoluteMarkEnd = absoluteMarkStart + MarkOpen.Length;
			// Check if our match overlaps with this <mark> tag
			if (position < absoluteMarkEnd && matchEnd > absoluteMarkStart)
				return true;
		}

		// Look for </mark> that contains our position
		searchStart = Math.Max(0, position - 6); // </mark> is 7 chars
		searchEnd = Math.Min(text.Length, matchEnd + 7);
		searchRegion = text[searchStart..searchEnd];

		var markCloseIdx = searchRegion.IndexOf(MarkClose.AsSpan(), StringComparison.OrdinalIgnoreCase);
		if (markCloseIdx >= 0)
		{
			var absoluteMarkStart = searchStart + markCloseIdx;
			var absoluteMarkEnd = absoluteMarkStart + MarkClose.Length;
			// Check if our match overlaps with this </mark> tag
			if (position < absoluteMarkEnd && matchEnd > absoluteMarkStart)
				return true;
		}

		return false;
	}

	private static bool IsInsideMarkTagContent(ReadOnlySpan<char> text, int position)
	{
		// Look backwards from position to find the last <mark> or </mark>
		var beforePosition = text[..position];

		var lastOpen = beforePosition.LastIndexOf(MarkOpen.AsSpan(), StringComparison.OrdinalIgnoreCase);
		var lastClose = beforePosition.LastIndexOf(MarkClose.AsSpan(), StringComparison.OrdinalIgnoreCase);

		// If we found an opening tag after the last closing tag, we're inside a mark's content
		return lastOpen > lastClose;
	}
}
