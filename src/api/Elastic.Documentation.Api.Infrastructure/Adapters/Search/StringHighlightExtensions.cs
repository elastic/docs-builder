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
	/// <param name="wholeWordOnly">When true, only highlights complete words (requires word boundaries at both start and end)</param>
	/// <returns>Text with highlighted tokens</returns>
	public static string HighlightTokens(
		this string text,
		ReadOnlySpan<string> tokens,
		IReadOnlyDictionary<string, string[]>? synonyms = null,
		bool wholeWordOnly = false)
	{
		if (tokens.Length == 0 || string.IsNullOrEmpty(text))
			return text;

		var result = text;

		foreach (var token in tokens)
		{
			if (string.IsNullOrEmpty(token))
				continue;

			// Highlight the token itself
			result = HighlightSingleToken(result, token, wholeWordOnly);

			if (synonyms == null)
				continue;

			// Highlight synonyms for this token (direct lookup)
			if (synonyms.TryGetValue(token, out var tokenSynonyms))
			{
				foreach (var synonym in tokenSynonyms)
				{
					var synonymToHighlight = ExtractSynonymTarget(synonym);
					if (!string.IsNullOrEmpty(synonymToHighlight))
						result = HighlightSingleToken(result, synonymToHighlight, wholeWordOnly);
				}
			}

			// Also check for hard replacements where this token is the source
			// Format: "source => target" means when searching for "source", also highlight "target"
			foreach (var kvp in synonyms)
			{
				foreach (var synonym in kvp.Value)
				{
					if (string.IsNullOrEmpty(synonym) || !synonym.Contains("=>"))
						continue;

					var (source, target) = ParseHardReplacement(synonym);
					if (!string.IsNullOrEmpty(source) &&
						!string.IsNullOrEmpty(target) &&
						source.Equals(token, StringComparison.OrdinalIgnoreCase))
					{
						result = HighlightSingleToken(result, target, wholeWordOnly);
					}
				}
			}
		}

		return result;
	}

	/// <summary>
	/// Extracts the target from a synonym entry, handling hard replacement format.
	/// For "source => target" returns "target", otherwise returns the original synonym.
	/// </summary>
	private static string? ExtractSynonymTarget(string? synonym)
	{
		if (string.IsNullOrEmpty(synonym))
			return null;

		if (!synonym.Contains("=>"))
			return synonym;

		var (_, target) = ParseHardReplacement(synonym);
		return target;
	}

	/// <summary>
	/// Parses a hard replacement synonym format: "source => target"
	/// </summary>
	private static (string? Source, string? Target) ParseHardReplacement(string synonym)
	{
		var arrowIndex = synonym.IndexOf("=>", StringComparison.Ordinal);
		if (arrowIndex < 0)
			return (null, null);

		var source = synonym[..arrowIndex].Trim();
		var target = synonym[(arrowIndex + 2)..].Trim();

		return (source, target);
	}

	private static string HighlightSingleToken(string text, string token, bool wholeWordOnly = false)
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

			// Only highlight if the match is at the start of a word (word boundary)
			if (!IsAtWordBoundary(textSpan, absoluteIndex))
			{
				// Not at word boundary, skip this match
				_ = sb.Append(remaining[..(matchIndex + tokenSpan.Length)]);
				pos = absoluteIndex + token.Length;
				continue;
			}

			// When wholeWordOnly is true, also check for word boundary at the end of the match
			if (wholeWordOnly && !IsAtWordBoundaryEnd(textSpan, absoluteIndex + tokenSpan.Length))
			{
				// Not a complete word, skip this match
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

	/// <summary>
	/// Checks if the given position is at the start of a word (word boundary).
	/// A word boundary is: start of string, after whitespace, after punctuation,
	/// or after a closing mark tag.
	/// </summary>
	private static bool IsAtWordBoundary(ReadOnlySpan<char> text, int position)
	{
		// Start of string is a word boundary
		if (position == 0)
			return true;

		// Check if we're right after a closing </mark> tag
		if (position >= MarkClose.Length)
		{
			var potentialMarkClose = text[(position - MarkClose.Length)..position];
			if (potentialMarkClose.Equals(MarkClose.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;
		}

		var prevChar = text[position - 1];

		// After whitespace is a word boundary
		if (char.IsWhiteSpace(prevChar))
			return true;

		// After punctuation (but not letters/digits) is a word boundary
		if (char.IsPunctuation(prevChar) || char.IsSymbol(prevChar))
			return true;

		// After a digit when current is not a digit, or vice versa - not a word boundary for alphanumeric continuity
		// After a letter when current is also a letter - not a word boundary
		return false;
	}

	/// <summary>
	/// Checks if the given position is at the end of a word (word boundary after the match).
	/// A word boundary is: end of string, before whitespace, before punctuation,
	/// or before an opening mark tag.
	/// </summary>
	private static bool IsAtWordBoundaryEnd(ReadOnlySpan<char> text, int position)
	{
		// End of string is a word boundary
		if (position >= text.Length)
			return true;

		// Check if we're right before an opening <mark> tag
		if (position + MarkOpen.Length <= text.Length)
		{
			var potentialMarkOpen = text[position..(position + MarkOpen.Length)];
			if (potentialMarkOpen.Equals(MarkOpen.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;
		}

		var nextChar = text[position];

		// Before whitespace is a word boundary
		if (char.IsWhiteSpace(nextChar))
			return true;

		// Before punctuation (but not letters/digits) is a word boundary
		if (char.IsPunctuation(nextChar) || char.IsSymbol(nextChar))
			return true;

		// Before a letter/digit means we're still inside a word - not a word boundary
		return false;
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
