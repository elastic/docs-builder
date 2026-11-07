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

	/// <summary>
	/// The operation's own text for a meta description. Drops the leading "all methods and paths" block (a bold
	/// sentence ending in a colon, the method and path lines, an optional "Refer to ..." line), then falls back to the summary.
	/// </summary>
	internal static string? OperationText(string? description, string? summary)
	{
		var own = SkipPathPreamble(description);
		if (!string.IsNullOrWhiteSpace(own))
			return own;

		return string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
	}

	internal static string? SkipPathPreamble(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return null;

		var paragraphs = markdown.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var index = 0;
		var sawListing = false;
		while (index < paragraphs.Length)
		{
			var paragraph = paragraphs[index];
			var isIntro = index == 0 && IsPathIntro(paragraph);
			var isListing = IsMethodPathListing(paragraph);
			var isReference = sawListing && IsReferenceLine(paragraph);
			if (!isIntro && !isListing && !isReference)
				break;

			sawListing |= isListing;
			index++;
		}

		return index == 0 ? markdown : string.Join("\n\n", paragraphs.Skip(index));
	}

	private static bool IsPathIntro(string paragraph) =>
		paragraph.StartsWith("**", StringComparison.Ordinal)
			&& paragraph.EndsWith(":**", StringComparison.Ordinal)
			&& PathWordPattern().IsMatch(paragraph);

	private static bool IsReferenceLine(string paragraph) =>
		paragraph.StartsWith("Refer to ", StringComparison.OrdinalIgnoreCase)
			&& paragraph.EndsWith("for more information.", StringComparison.OrdinalIgnoreCase);

	private static bool IsMethodPathListing(string paragraph)
	{
		if (!paragraph.Contains('/'))
			return false;

		var tokens = ApiMarkdown.StripHtml(paragraph).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length == 0 || tokens.Length % 2 != 0)
			return false;

		for (var i = 0; i < tokens.Length; i += 2)
		{
			if (!HttpMethodPattern().IsMatch(tokens[i]) || !tokens[i + 1].StartsWith('/'))
				return false;
		}

		return true;
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

	[GeneratedRegex(@"\bpaths?\b", RegexOptions.IgnoreCase)]
	private static partial Regex PathWordPattern();

	[GeneratedRegex(@"^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE)$", RegexOptions.IgnoreCase)]
	private static partial Regex HttpMethodPattern();

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
