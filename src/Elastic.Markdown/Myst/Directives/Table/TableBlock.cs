// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;
using Markdig.Extensions.Tables;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Table;

/// <summary>
/// A table directive that wraps a standard pipe table, allowing authors to specify
/// column widths and an optional caption.
/// </summary>
/// <example>
/// :::{table} Optional caption
/// :widths: 30 70
///
/// | Name  | Description |
/// | ----- | ----------- |
/// | Alpha | A short one |
/// :::
/// </example>
public class TableBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "table";

	/// <summary>
	/// Optional caption for the table, taken from the directive argument.
	/// </summary>
	public string? Caption { get; private set; }

	/// <summary>
	/// The parsed relative column widths from the :widths: option.
	/// </summary>
	public int[]? Widths { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Caption = Arguments?.ReplaceSubstitutions(context);
		ParseWidths();
	}

	/// <summary>
	/// Applies the parsed column widths to the inner Markdig Table block by setting
	/// inline style attributes on header cells and adding a CSS class to the table.
	/// Must be called at render time, after the inner table has been fully parsed.
	/// </summary>
	public bool ApplyWidthsToInnerTable()
	{
		if (Widths is null)
			return false;

		// Find the inner Markdig Table block among descendants
		var innerTable = this.Descendants<Markdig.Extensions.Tables.Table>().FirstOrDefault();

		if (innerTable is null)
		{
			this.EmitWarning("{table} directive does not contain a pipe table.");
			return false;
		}

		// Use actual cell count from the first row, not ColumnDefinitions.Count
		// (Markdig's pipe table parser can create extra ColumnDefinitions for leading/trailing pipes)
		var firstRow = innerTable.OfType<TableRow>().FirstOrDefault();
		var columnCount = firstRow?.Count ?? innerTable.ColumnDefinitions.Count;

		if (Widths.Length != columnCount)
		{
			this.EmitError(
				$"{{table}} :widths: specifies {Widths.Length} values but the table has {columnCount} columns. " +
				$"The number of widths must match the number of columns."
			);
			return false;
		}

		// Add fixed-widths class to the table element
		innerTable.GetAttributes().AddClass("fixed-widths");

		// Normalize widths to percentages and store on the table for the renderer
		var total = (float)Widths.Sum();
		var percentages = Widths.Select(w => System.Math.Round(w / total * 100f, 2)).ToArray();
		innerTable.SetData("column-widths", percentages);

		return true;
	}

	private void ParseWidths()
	{
		var widthsProp = Prop("widths");
		if (string.IsNullOrWhiteSpace(widthsProp))
			return;

		// "auto" means let the browser decide — no explicit widths
		if (widthsProp.Trim().Equals("auto", StringComparison.OrdinalIgnoreCase))
			return;

		var parts = widthsProp.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var widths = new List<int>(parts.Length);

		foreach (var part in parts)
		{
			if (!int.TryParse(part, out var width) || width <= 0)
			{
				this.EmitError($"Invalid column width '{part}' in {{table}} :widths: option. Values must be positive integers.");
				return;
			}

			widths.Add(width);
		}

		Widths = [.. widths];
	}
}
