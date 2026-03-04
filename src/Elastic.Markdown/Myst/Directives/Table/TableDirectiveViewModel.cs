// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Elastic.Markdown.Helpers;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.Table;

/// <summary>
/// View model for the table directive. Handles rendering the table with optional column width constraints.
/// </summary>
public class TableDirectiveViewModel : DirectiveViewModel
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
		var styleAttr = hasTableLayout ? "" : " style=\"table-layout:fixed\"";
		var newOpening = html[tableIndex..bracketEnd] + styleAttr + ">";
		var result = html[..tableIndex] + newOpening + colgroup + html[(bracketEnd + 1)..];

		return new HtmlString(result.EnsureTrimmed());
	}
}
