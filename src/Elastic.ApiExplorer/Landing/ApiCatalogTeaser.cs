// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Landing;

internal static partial class ApiCatalogTeaser
{
	public static string? From(string? description)
	{
		if (string.IsNullOrWhiteSpace(description))
			return null;

		var paragraph = FirstParagraph(description);
		if (paragraph is null)
			return null;

		var withoutLinks = MarkdownLink().Replace(paragraph, "$1");
		var stripped = Whitespace()
			.Replace(withoutLinks.Replace("**", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal), " ")
			.Trim();
		return string.IsNullOrWhiteSpace(stripped) ? null : stripped;
	}

	private static string? FirstParagraph(string description)
	{
		var buffer = new StringBuilder();
		foreach (var raw in description.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
		{
			var line = raw.Trim();
			if (line.StartsWith('#'))
			{
				if (buffer.Length > 0)
					break;
				continue;
			}

			if (line.Length == 0)
			{
				if (buffer.Length > 0)
					break;
				continue;
			}

			if (buffer.Length > 0)
				_ = buffer.Append(' ');
			_ = buffer.Append(line);
		}

		return buffer.Length == 0 ? null : buffer.ToString();
	}

	[GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
	private static partial Regex MarkdownLink();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
