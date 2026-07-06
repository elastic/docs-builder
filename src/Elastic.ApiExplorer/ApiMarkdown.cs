// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Documentation;
using Microsoft.AspNetCore.Html;

namespace Elastic.ApiExplorer;

/// <summary>
/// Renders OpenAPI description markdown to HTML, escaping mustache-style patterns that would
/// otherwise be interpreted as docs-builder substitutions.
/// </summary>
public static partial class ApiMarkdown
{
	public static HtmlString Render(IMarkdownStringRenderer renderer, string? markdown)
	{
		if (string.IsNullOrEmpty(markdown))
			return HtmlString.Empty;

		// Escape mustache-style patterns by wrapping in backticks (inline code won't process substitutions)
		var escaped = MustachePattern().Replace(markdown, match => $"`{match.Value}`");
		return new HtmlString(renderer.Render(escaped, null));
	}

	// Regex to match mustache-style patterns like {{var}} or {{{var}}} that conflict with docs-builder substitutions
	[GeneratedRegex(@"\{\{\{?[^}]+\}?\}\}")]
	private static partial Regex MustachePattern();
}
