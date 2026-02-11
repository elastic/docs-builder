// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Elastic.Markdown.Myst.Renderers;

public class WrappedTableRenderer : HtmlTableRenderer
{
	protected override void Write(HtmlRenderer renderer, Table table)
	{
		// Wrap the table in a div to allow for overflow scrolling
		_ = renderer.Write("<div class=\"table-wrapper\">");

		var columnWidths = table.GetData("column-widths") as double[];
		if (columnWidths is not null)
			WriteTableWithColgroup(renderer, table, columnWidths);
		else
			base.Write(renderer, table);

		_ = renderer.Write("</div>");
	}

	private static void WriteTableWithColgroup(HtmlRenderer renderer, Table table, double[] columnWidths)
	{
		if (!renderer.EnableHtmlForBlock)
		{
			WriteTablePlainText(renderer, table);
			return;
		}

		_ = renderer.EnsureLine();

		// Open table tag with attributes (includes the "fixed-widths" class)
		_ = renderer.Write("<table");
		_ = renderer.WriteAttributes(table);
		_ = renderer.WriteLine(">");

		// Render colgroup with column widths
		_ = renderer.Write("<colgroup>");
		foreach (var width in columnWidths)
			_ = renderer.Write($"<col style=\"width:{width.ToString("0.##", CultureInfo.InvariantCulture)}%\" />");
		_ = renderer.WriteLine("</colgroup>");

		// Render rows
		var hasAlreadyHeader = false;
		var isHeaderOpen = false;
		var hasBody = false;

		foreach (var rowObj in table)
		{
			var row = (TableRow)rowObj;

			if (row.IsHeader)
			{
				if (!hasAlreadyHeader)
				{
					_ = renderer.WriteLine("<thead>");
					isHeaderOpen = true;
				}

				hasAlreadyHeader = true;
			}
			else if (!hasBody)
			{
				if (isHeaderOpen)
				{
					_ = renderer.WriteLine("</thead>");
					isHeaderOpen = false;
				}

				_ = renderer.WriteLine("<tbody>");
				hasBody = true;
			}

			_ = renderer.Write("<tr");
			_ = renderer.WriteAttributes(row);
			_ = renderer.WriteLine(">");

			for (var i = 0; i < row.Count; i++)
			{
				var cell = (TableCell)row[i];
				var tag = row.IsHeader ? "th" : "td";

				_ = renderer.Write("<");
				_ = renderer.Write(tag);

				// Write alignment style from column definitions
				if (i < table.ColumnDefinitions.Count)
				{
					var alignment = table.ColumnDefinitions[i].Alignment;
					if (alignment.HasValue)
					{
						_ = renderer.Write(" style=\"text-align: ");
						_ = renderer.Write(alignment.Value switch
						{
							TableColumnAlign.Center => "center",
							TableColumnAlign.Right => "right",
							_ => "left"
						});
						_ = renderer.Write("\"");
					}
				}

				if (cell.ColumnSpan != 1)
					_ = renderer.Write($" colspan=\"{cell.ColumnSpan}\"");

				if (cell.RowSpan != 1)
					_ = renderer.Write($" rowspan=\"{cell.RowSpan}\"");

				_ = renderer.Write(">");
				renderer.Write(cell);
				_ = renderer.Write("</");
				_ = renderer.Write(tag);
				_ = renderer.WriteLine(">");
			}

			_ = renderer.WriteLine("</tr>");
		}

		if (hasBody)
			_ = renderer.WriteLine("</tbody>");
		else if (isHeaderOpen)
			_ = renderer.WriteLine("</thead>");

		_ = renderer.WriteLine("</table>");
	}

	private static void WriteTablePlainText(HtmlRenderer renderer, Table table)
	{
		var implicitParagraph = renderer.ImplicitParagraph;
		renderer.ImplicitParagraph = true;

		foreach (var rowObj in table)
		{
			var row = (TableRow)rowObj;
			for (var i = 0; i < row.Count; i++)
			{
				var cell = (TableCell)row[i];
				renderer.Write(cell);
				_ = renderer.Write(' ');
			}
		}

		renderer.ImplicitParagraph = implicitParagraph;
	}
}
