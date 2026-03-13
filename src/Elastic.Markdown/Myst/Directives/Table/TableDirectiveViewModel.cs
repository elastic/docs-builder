// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;
using Elastic.Markdown.Helpers;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.Table;

/// <summary>
/// View model for the table directive. Handles rendering the table with optional column width constraints.
/// </summary>
public partial class TableDirectiveViewModel : DirectiveViewModel
{
	public required IReadOnlyList<double> ColumnWidths { get; init; }

	/// <summary>
	/// Renders the table content. When <see cref="ColumnWidths"/> is specified, injects a colgroup and table-layout:fixed.
	/// </summary>
	public HtmlString RenderTableWithColumns()
	{
		var html = RenderBlock().Value ?? string.Empty;
		if (ColumnWidths.Count == 0)
			return new HtmlString(html.EnsureTrimmed());

		var tableIndex = html.IndexOf("<table", StringComparison.OrdinalIgnoreCase);
		if (tableIndex < 0)
			return new HtmlString(html.EnsureTrimmed());

		var bracketEnd = html.IndexOf('>', tableIndex);
		if (bracketEnd < 0)
			return new HtmlString(html.EnsureTrimmed());

		var colgroup = "<colgroup>" +
			string.Join("", ColumnWidths.Select(w => string.Format(CultureInfo.InvariantCulture, "<col style=\"width:{0:F2}%\">", w))) +
			"</colgroup>";

		var openingTag = html[tableIndex..bracketEnd];
		var hasTableLayout = openingTag.Contains("table-layout", StringComparison.OrdinalIgnoreCase);
		var newOpening = hasTableLayout
			? openingTag + ">"
			: AppendTableLayoutFixed(openingTag);
		var result = html[..tableIndex] + newOpening + colgroup + html[(bracketEnd + 1)..];

		return new HtmlString(result.EnsureTrimmed());
	}

	[GeneratedRegex(@"\sstyle\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase)]
	private static partial Regex StyleAttributeRegex();

	/// <summary>
	/// Appends table-layout:fixed to the table opening tag. Merges into existing style attribute if present.
	/// </summary>
	private static string AppendTableLayoutFixed(string openingTag)
	{
		var styleMatch = StyleAttributeRegex().Match(openingTag);
		if (styleMatch.Success)
		{
			var existingStyle = styleMatch.Groups[1].Value.TrimEnd();
			var separator = string.IsNullOrEmpty(existingStyle) ? "" : "; ";
			var newStyle = existingStyle + separator + "table-layout:fixed";
			return openingTag[..styleMatch.Groups[1].Index] + newStyle + openingTag[(styleMatch.Groups[1].Index + styleMatch.Groups[1].Length)..] + ">";
		}

		return openingTag + " style=\"table-layout:fixed\">";
	}
}
