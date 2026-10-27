// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Plain-text meta description excerpt. Caps at 150 characters like <c>DescriptionGenerator</c>.</summary>
internal static partial class ApiSeoDescription
{
	internal const int MaxLength = 150;

	internal static string? Excerpt(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return null;

		var paragraph = FirstParagraph(markdown) ?? markdown;
		var plain = ToPlainText(paragraph);
		return string.IsNullOrWhiteSpace(plain) ? null : Truncate(plain);
	}

	internal static string? FirstParagraph(string markdown)
	{
		using var reader = new StringReader(markdown);
		var buffer = new StringBuilder();
		while (reader.ReadLine() is { } line)
		{
			if (line.StartsWith('#'))
			{
				if (buffer.Length > 0)
					break;
				continue;
			}

			if (string.IsNullOrWhiteSpace(line))
			{
				if (buffer.Length > 0)
					break;
				continue;
			}

			if (buffer.Length > 0)
				_ = buffer.Append(' ');
			_ = buffer.Append(line.Trim());
		}

		return buffer.Length == 0 ? null : buffer.ToString();
	}

	internal static string ToPlainText(string markdown)
	{
		var text = ImagePattern().Replace(markdown, "$1");
		text = LinkPattern().Replace(text, "$1");
		text = BoldStarPattern().Replace(text, "$1");
		text = BoldUnderscorePattern().Replace(text, "$1");
		text = CodePattern().Replace(text, "$1");
		text = ItalicStarPattern().Replace(text, "$1");
		text = ItalicUnderscorePattern().Replace(text, "$1");
		text = HeadingPattern().Replace(text, string.Empty);
		return WhitespacePattern().Replace(text, " ").Trim();
	}

	internal static string Truncate(string text)
	{
		if (text.Length <= MaxLength)
			return text;

		var endIndex = text.IndexOf(' ', MaxLength - 1);
		if (endIndex == -1)
			endIndex = MaxLength;
		return string.Concat(text.AsSpan(0, endIndex + 1).Trim().TrimEnd('.'), "...");
	}

	[GeneratedRegex(@"!\[([^\]]*)\]\([^)]+\)")]
	private static partial Regex ImagePattern();

	[GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
	private static partial Regex LinkPattern();

	[GeneratedRegex(@"\*\*([^*]+)\*\*")]
	private static partial Regex BoldStarPattern();

	[GeneratedRegex(@"__([^_]+)__")]
	private static partial Regex BoldUnderscorePattern();

	[GeneratedRegex(@"\*([^*]+)\*")]
	private static partial Regex ItalicStarPattern();

	[GeneratedRegex(@"_([^_]+)_")]
	private static partial Regex ItalicUnderscorePattern();

	[GeneratedRegex(@"`([^`]+)`")]
	private static partial Regex CodePattern();

	[GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Multiline)]
	private static partial Regex HeadingPattern();

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespacePattern();
}
