// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;

namespace Elastic.ApiExplorer.Landing;

internal static partial class ApiCatalogTeaser
{
	public static string? From(string? description)
	{
		if (string.IsNullOrWhiteSpace(description))
			return null;

		var normalized = description.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
		var blank = normalized.IndexOf("\n\n", StringComparison.Ordinal);
		var paragraph = blank < 0 ? normalized : normalized[..blank].Trim();
		var withoutLinks = MarkdownLink().Replace(paragraph, "$1");
		var stripped = Whitespace()
			.Replace(withoutLinks.Replace("**", "", StringComparison.Ordinal).Replace("`", "", StringComparison.Ordinal), " ")
			.Trim();
		return string.IsNullOrWhiteSpace(stripped) ? null : stripped;
	}

	[GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
	private static partial Regex MarkdownLink();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
