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

	private static readonly char[] PathSeparators = [' ', '\t', '\r', '\n', '\u00a0'];

	private static readonly string[] HttpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS", "TRACE"];

	internal static string? Excerpt(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return null;

		var paragraph = FirstParagraph(markdown) ?? markdown;
		var plain = ToPlainText(paragraph);
		return string.IsNullOrWhiteSpace(plain) ? null : Truncate(plain);
	}

	/// <summary>Skips the shared path preamble, then falls back to the operation summary.</summary>
	internal static string? Meaningful(string? description, string? summary)
	{
		var real = WithoutPathPreamble(description);
		if (!string.IsNullOrWhiteSpace(real))
			return real;

		return string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
	}

	private static string? WithoutPathPreamble(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
			return null;

		var paragraphs = SplitParagraphs(markdown);
		var start = 0;
		while (start < paragraphs.Count && IsPathPreamble(paragraphs[start]))
			start++;

		if (start == 0)
			return markdown;

		return start >= paragraphs.Count ? null : string.Join("\n\n", paragraphs.Skip(start));
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

	private static List<string> SplitParagraphs(string markdown)
	{
		var paragraphs = new List<string>();
		var buffer = new StringBuilder();
		using var reader = new StringReader(markdown);
		while (reader.ReadLine() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				FlushParagraph(paragraphs, buffer);
				continue;
			}

			if (buffer.Length > 0)
				_ = buffer.Append('\n');
			_ = buffer.Append(line);
		}

		FlushParagraph(paragraphs, buffer);
		return paragraphs;
	}

	private static void FlushParagraph(List<string> paragraphs, StringBuilder buffer)
	{
		if (buffer.Length == 0)
			return;

		paragraphs.Add(buffer.ToString());
		_ = buffer.Clear();
	}

	private static bool IsPathPreamble(string paragraph)
	{
		var plain = ToPlainText(ApiMarkdown.StripHtml(paragraph)).Trim().TrimEnd(':').Trim();
		if (plain.Length == 0)
			return true;

		if (plain.StartsWith("All methods and paths for this operation", StringComparison.OrdinalIgnoreCase))
			return true;

		if (plain.StartsWith("Spaces method and path for this operation", StringComparison.OrdinalIgnoreCase))
			return true;

		if (plain.StartsWith("Refer to Spaces for more information", StringComparison.OrdinalIgnoreCase))
			return true;

		return IsMethodPathList(plain);
	}

	private static bool IsMethodPathList(string plain)
	{
		var tokens = plain.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length == 0 || tokens.Length % 2 != 0)
			return false;

		for (var i = 0; i < tokens.Length; i += 2)
		{
			if (!IsHttpMethod(tokens[i]) || !tokens[i + 1].StartsWith('/'))
				return false;
		}

		return true;
	}

	private static bool IsHttpMethod(string token)
	{
		foreach (var method in HttpMethods)
		{
			if (token.Equals(method, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
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
