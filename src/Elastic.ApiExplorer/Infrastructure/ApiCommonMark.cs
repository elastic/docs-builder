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

	public static void Fence(StringBuilder markdown, string language, string? source)
	{
		if (string.IsNullOrEmpty(source))
			return;

		_ = markdown.Append("```");
		_ = markdown.AppendLine(language);
		_ = markdown.AppendLine(source.TrimEnd());
		_ = markdown.AppendLine("```");
		_ = markdown.AppendLine();
	}

	public static string Link(string text, string? url)
	{
		if (string.IsNullOrEmpty(url))
			return text;

		return $"[{text}]({url})";
	}
}
