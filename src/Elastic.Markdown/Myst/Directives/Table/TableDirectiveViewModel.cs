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

	public required bool Matrix { get; init; }

	/// <summary>
	/// When set, the rendered table is wrapped in a <c>&lt;filterable-table&gt;</c> custom
	/// element for client-side search + facet filtering (progressive enhancement).
	/// </summary>
	public required bool Filterable { get; init; }

	/// <summary>
	/// Renders the table content. When <see cref="ColumnWidths"/> is specified, injects a colgroup and table-layout:fixed.
	/// When <see cref="Matrix"/> is set, adds the table-matrix class to the wrapper.
	/// </summary>
	public HtmlString RenderTableWithColumns()
	{
		var html = RenderBlock().Value ?? string.Empty;
		if (Matrix)
			html = InjectMatrixClass(html);
		if (ColumnWidths.Count > 0)
			html = InjectColumnWidths(html);
		if (Filterable)
			html = WrapFilterable(html);
		return new HtmlString(html.EnsureTrimmed());
	}

	/// <summary>
	/// Injects a colgroup (and table-layout:fixed) when explicit column widths are
	/// configured. Returns the html unchanged when no table tag is found.
	/// </summary>
	private string InjectColumnWidths(string html)
	{
		var tableIndex = html.IndexOf("<table", StringComparison.OrdinalIgnoreCase);
		if (tableIndex < 0)
			return html;

		var bracketEnd = html.IndexOf('>', tableIndex);
		if (bracketEnd < 0)
			return html;

		var colgroup = "<colgroup>"
			+ string.Join("", ColumnWidths.Select(w => string.Format(CultureInfo.InvariantCulture, "<col style=\"width:{0:F2}%\">", w)))
			+ "</colgroup>";

		var openingTag = html[tableIndex..bracketEnd];
		var hasTableLayout = openingTag.Contains("table-layout", StringComparison.OrdinalIgnoreCase);
		var newOpening = hasTableLayout ? openingTag + ">" : AppendTableLayoutFixed(openingTag);
		return html[..tableIndex] + newOpening + colgroup + html[(bracketEnd + 1)..];
	}

	/// <summary>
	/// Wraps the rendered table in a &lt;filterable-table&gt; custom element so the client
	/// can progressively enhance it with search + facet filters. Without JavaScript the
	/// table renders exactly as before.
	/// </summary>
	private static string WrapFilterable(string html) => "<filterable-table>" + html + "</filterable-table>";

	/// <summary>
	/// Adds the table-matrix class to the wrapper div's opening tag emitted by WrappedTableRenderer,
	/// locating that specific tag rather than replacing the class attribute wherever it appears.
	/// </summary>
	private static string InjectMatrixClass(string html)
	{
		const string wrapperOpenTag = "<div class=\"table-wrapper\">";
		var wrapperIndex = html.IndexOf(wrapperOpenTag, StringComparison.Ordinal);
		return wrapperIndex < 0
			? html
			: html[..wrapperIndex] + "<div class=\"table-wrapper table-matrix\">" + html[(wrapperIndex + wrapperOpenTag.Length)..];
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
			return openingTag[..styleMatch.Groups[1].Index]
				+ newStyle
				+ openingTag[(styleMatch.Groups[1].Index + styleMatch.Groups[1].Length)..]
				+ ">";
		}

		return openingTag + " style=\"table-layout:fixed\">";
	}
}
