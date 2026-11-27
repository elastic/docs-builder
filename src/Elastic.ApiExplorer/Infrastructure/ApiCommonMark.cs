// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Text;

namespace Elastic.ApiExplorer.Infrastructure;

/// <summary>Small CommonMark helpers shared by API page writers.</summary>
internal static class ApiCommonMark
{
	/// <summary>
	/// Literal text, such as a name from the spec, escaped so CommonMark reads it back as written: <c>HTTP &lt;code&gt; responses</c>
	/// stays text, not a tag. Characters that cannot start markup where they stand are left alone, so the export stays readable.
	/// </summary>
	public static string Text(string? text)
	{
		if (string.IsNullOrEmpty(text))
			return "";

		var escaped = new StringBuilder(text.Length + 8);
		for (var i = 0; i < text.Length; i++)
		{
			if (Escapes(text, i))
				_ = escaped.Append('\\');
			_ = escaped.Append(text[i]);
		}
		// A trailing run of `#` after a space would close a heading and vanish, so `Search by #` escapes it.
		var run = text.Length - text.TrimEnd('#').Length;
		if (run > 0 && (run == text.Length || text[^(run + 1)] == ' '))
			_ = escaped.Insert(escaped.Length - run, '\\');
		return escaped.ToString();
	}

	private static bool Escapes(string text, int i) => text[i] switch
	{
		'\\' or '`' or '*' or '[' or ']' or '<' or '>' => true,
		// An underscore inside a word never opens emphasis, so `max_docs` keeps its plain form.
		'_' => !IsWordCharacter(text, i - 1) || !IsWordCharacter(text, i + 1),
		// Only `&name;` or `&#123;` would read as an entity.
		'&' => IsEntity(text, i),
		_ => false
	};

	private static bool IsWordCharacter(string text, int i) => i >= 0 && i < text.Length && char.IsLetterOrDigit(text[i]);

	private static bool IsEntity(string text, int ampersand)
	{
		var end = text.IndexOf(';', ampersand);
		return end > ampersand + 1 && text.AsSpan(ampersand + 1, end - ampersand - 1).IndexOfAnyExcept(EntityCharacters) < 0;
	}

	private static readonly SearchValues<char> EntityCharacters = SearchValues.Create(
		"#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
	);

	/// <summary>A heading whose text is literal, such as a name from the spec.</summary>
	public static void TextHeading(StringBuilder markdown, int level, string? text) => Heading(markdown, level, Text(text));

	/// <summary>A link whose label is literal, such as a name from the spec.</summary>
	public static string TextLink(string? text, string? url) => Link(Text(text), url);

	/// <summary>A heading whose text is Markdown; <see cref="TextHeading"/> writes a literal one.</summary>
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
