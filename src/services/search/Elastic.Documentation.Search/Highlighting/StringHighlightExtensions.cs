// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Documentation.Search.Highlighting;

public static class StringHighlightExtensions
{
	private const string MarkOpen = "<mark>";
	private const string MarkClose = "</mark>";

	/// <summary>
	/// Wraps each occurrence of any <paramref name="tokens"/> in <paramref name="text"/> with
	/// <c>&lt;mark&gt;</c>...<c>&lt;/mark&gt;</c>. Optionally also highlights bi-directional
	/// synonyms (including <c>source =&gt; target</c> hard replacements) for each token.
	/// Already-highlighted regions are preserved.
	/// </summary>
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

			result = HighlightSingleToken(result, token, wholeWordOnly);

			if (synonyms == null)
				continue;

			if (synonyms.TryGetValue(token, out var tokenSynonyms))
			{
				foreach (var synonym in tokenSynonyms)
				{
					var synonymToHighlight = ExtractSynonymTarget(synonym);
					if (!string.IsNullOrEmpty(synonymToHighlight))
						result = HighlightSingleToken(result, synonymToHighlight, wholeWordOnly);
				}
			}

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

	private static string? ExtractSynonymTarget(string? synonym)
	{
		if (string.IsNullOrEmpty(synonym))
			return null;

		if (!synonym.Contains("=>"))
			return synonym;

		var (_, target) = ParseHardReplacement(synonym);
		return target;
	}

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
		if (text.Contains($"{MarkOpen}{token}{MarkClose}", StringComparison.OrdinalIgnoreCase))
			return text;

		var sb = new StringBuilder(text.Length + 26);
		var textSpan = text.AsSpan();
		var tokenSpan = token.AsSpan();
		var pos = 0;

		while (pos < textSpan.Length)
		{
			var remaining = textSpan[pos..];
			var matchIndex = remaining.IndexOf(tokenSpan, StringComparison.OrdinalIgnoreCase);

			if (matchIndex < 0)
			{
				_ = sb.Append(remaining);
				break;
			}

			var absoluteIndex = pos + matchIndex;

			if (IsInsideMarkTagSyntax(textSpan, absoluteIndex, tokenSpan.Length) || IsInsideMarkTagContent(textSpan, absoluteIndex))
			{
				_ = sb.Append(remaining[..(matchIndex + tokenSpan.Length)]);
				pos = absoluteIndex + token.Length;
				continue;
			}

			if (!IsAtWordBoundary(textSpan, absoluteIndex))
			{
				_ = sb.Append(remaining[..(matchIndex + tokenSpan.Length)]);
				pos = absoluteIndex + token.Length;
				continue;
			}

			if (wholeWordOnly && !IsAtWordBoundaryEnd(textSpan, absoluteIndex + tokenSpan.Length))
			{
				_ = sb.Append(remaining[..(matchIndex + tokenSpan.Length)]);
				pos = absoluteIndex + token.Length;
				continue;
			}

			_ = sb.Append(remaining[..matchIndex])
				.Append(MarkOpen)
				.Append(remaining.Slice(matchIndex, tokenSpan.Length))
				.Append(MarkClose);

			pos = absoluteIndex + token.Length;
		}

		return sb.ToString();
	}

	private static bool IsAtWordBoundary(ReadOnlySpan<char> text, int position)
	{
		if (position == 0)
			return true;

		if (position >= MarkClose.Length)
		{
			var potentialMarkClose = text[(position - MarkClose.Length)..position];
			if (potentialMarkClose.Equals(MarkClose.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;
		}

		var prevChar = text[position - 1];

		if (char.IsWhiteSpace(prevChar))
			return true;

		if (char.IsPunctuation(prevChar) || char.IsSymbol(prevChar))
			return true;

		return false;
	}

	private static bool IsAtWordBoundaryEnd(ReadOnlySpan<char> text, int position)
	{
		if (position >= text.Length)
			return true;

		if (position + MarkOpen.Length <= text.Length)
		{
			var potentialMarkOpen = text[position..(position + MarkOpen.Length)];
			if (potentialMarkOpen.Equals(MarkOpen.AsSpan(), StringComparison.OrdinalIgnoreCase))
				return true;
		}

		var nextChar = text[position];

		if (char.IsWhiteSpace(nextChar))
			return true;

		if (char.IsPunctuation(nextChar) || char.IsSymbol(nextChar))
			return true;

		return false;
	}

	private static bool IsInsideMarkTagSyntax(ReadOnlySpan<char> text, int position, int tokenLength)
	{
		var matchEnd = position + tokenLength;

		var searchStart = Math.Max(0, position - 5);
		var searchEnd = Math.Min(text.Length, matchEnd + 6);
		var searchRegion = text[searchStart..searchEnd];

		var markOpenIdx = searchRegion.IndexOf(MarkOpen.AsSpan(), StringComparison.OrdinalIgnoreCase);
		if (markOpenIdx >= 0)
		{
			var absoluteMarkStart = searchStart + markOpenIdx;
			var absoluteMarkEnd = absoluteMarkStart + MarkOpen.Length;
			if (position < absoluteMarkEnd && matchEnd > absoluteMarkStart)
				return true;
		}

		searchStart = Math.Max(0, position - 6);
		searchEnd = Math.Min(text.Length, matchEnd + 7);
		searchRegion = text[searchStart..searchEnd];

		var markCloseIdx = searchRegion.IndexOf(MarkClose.AsSpan(), StringComparison.OrdinalIgnoreCase);
		if (markCloseIdx >= 0)
		{
			var absoluteMarkStart = searchStart + markCloseIdx;
			var absoluteMarkEnd = absoluteMarkStart + MarkClose.Length;
			if (position < absoluteMarkEnd && matchEnd > absoluteMarkStart)
				return true;
		}

		return false;
	}

	private static bool IsInsideMarkTagContent(ReadOnlySpan<char> text, int position)
	{
		var beforePosition = text[..position];

		var lastOpen = beforePosition.LastIndexOf(MarkOpen.AsSpan(), StringComparison.OrdinalIgnoreCase);
		var lastClose = beforePosition.LastIndexOf(MarkClose.AsSpan(), StringComparison.OrdinalIgnoreCase);

		return lastOpen > lastClose;
	}
}
