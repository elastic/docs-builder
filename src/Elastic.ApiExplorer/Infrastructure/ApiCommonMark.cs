// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Small CommonMark helpers shared by API page writers.</summary>
internal static class ApiCommonMark
{
	public static void Heading(StringBuilder markdown, int level, string text)
	{
		_ = markdown.Append('#', level);
		_ = markdown.Append(' ');
		_ = markdown.AppendLine(text);
		_ = markdown.AppendLine();
	}

	public static void Paragraph(StringBuilder markdown, string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		_ = markdown.AppendLine(text.TrimEnd());
		_ = markdown.AppendLine();
	}

	public static void Prepared(StringBuilder markdown, string? source, string apiBaseUrl)
	{
		var prepared = ApiMarkdown.Prepare(source, apiBaseUrl);
		if (string.IsNullOrWhiteSpace(prepared))
			return;

		_ = markdown.AppendLine(prepared.TrimEnd());
		_ = markdown.AppendLine();
	}

	/// <summary>A fenced code block. The fence is longer than any backtick run in the source, so a sample that holds a fence stays whole.</summary>
	public static void Fence(StringBuilder markdown, string language, string? source)
	{
		if (string.IsNullOrEmpty(source))
			return;

		var fence = new string('`', Math.Max(3, LongestBacktickRun(source) + 1));
		_ = markdown.Append(fence);
		_ = markdown.AppendLine(language);
		_ = markdown.AppendLine(source.TrimEnd());
		_ = markdown.AppendLine(fence);
		_ = markdown.AppendLine();
	}

	/// <summary>Text as an inline code span. The delimiter is longer than any backtick run inside it, so the text survives as written.</summary>
	public static string CodeSpan(string? text)
	{
		text ??= "";
		var delimiter = new string('`', LongestBacktickRun(text) + 1);
		// A backtick at either end would merge with the delimiter, so CommonMark strips one space of padding on each side.
		var padding = text.StartsWith('`') || text.EndsWith('`') ? " " : "";
		return $"{delimiter}{padding}{text}{padding}{delimiter}";
	}

	private static int LongestBacktickRun(string text)
	{
		var longest = 0;
		var run = 0;
		foreach (var character in text)
		{
			run = character == '`' ? run + 1 : 0;
			longest = Math.Max(longest, run);
		}

		return longest;
	}

	public static string Link(string text, string? url)
	{
		if (string.IsNullOrEmpty(url))
			return text;

		return $"[{text}]({url})";
	}
}
