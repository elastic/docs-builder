// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Markdown.Diagnostics;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Directives.Table;

/// <summary>
/// A wrapper directive for pipe tables that allows controlling column widths using a 12-unit grid system.
/// Supports presets (<c>auto</c>, <c>description</c>) and custom dash-separated values (e.g., <c>4-8</c>, <c>4-4-4</c>).
/// </summary>
public class TableDirectiveBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "table";

	/// <summary>
	/// Column widths as percentages (e.g., 33.33, 66.67 for 4-8). Empty when using <c>auto</c> preset or no widths specified.
	/// </summary>
	public IReadOnlyList<double> ColumnWidths { get; private set; } = [];

	/// <summary>
	/// When set, hovering a cell highlights its whole row and column (cross-hair highlight).
	/// Intended for lookup-style tables with a row-heading first column and a header row.
	/// </summary>
	public bool Matrix { get; private set; }

	/// <summary>
	/// When set, wraps the table in a <c>filterable-table</c> host element so the client
	/// can progressively enhance it with a search box and per-column facet filters.
	/// The server-rendered table stays fully usable without JavaScript.
	/// </summary>
	public bool Filterable { get; private set; }

	/// <summary>
	/// Set by <c>:filterable: search</c>. The client adds the search box and count but no
	/// dropdown filters, for tables where no column groups the rows in a useful way.
	/// </summary>
	public bool FilterSearchOnly { get; private set; }

	/// <summary>
	/// Column header names from <c>:filter-columns:</c>. When set, only these columns get a
	/// dropdown filter, in place of the automatic choice. Empty means automatic.
	/// </summary>
	public IReadOnlyList<string> FilterColumns { get; private set; } = [];

	public override void FinalizeAndValidate(ParserContext context)
	{
		Matrix = PropBool("matrix");
		ParseFilterOptions();

		var widthsValue = Prop("widths")?.Trim();

		if (string.IsNullOrEmpty(widthsValue) || widthsValue.Equals("auto", StringComparison.OrdinalIgnoreCase))
		{
			ColumnWidths = [];
			return;
		}

		if (widthsValue.Equals("description", StringComparison.OrdinalIgnoreCase))
			widthsValue = "4-8";

		var gridUnits = ParseCustomWidths(widthsValue, this);
		if (gridUnits is null)
			return;

		var sum = gridUnits.Sum();
		if (sum != 12)
		{
			this.EmitError($"Column widths must sum to 12 (Bootstrap grid). Got sum of {sum}.");
			return;
		}

		ColumnWidths = gridUnits.Select(u => u / 12.0 * 100).ToArray();
	}

	/// <summary>
	/// Reads <c>:filterable:</c> and <c>:filter-columns:</c>. A bare <c>:filterable:</c>
	/// (or <c>true</c>) keeps the automatic dropdowns, <c>search</c> turns them off, and
	/// <c>:filter-columns:</c> names the columns that get one.
	/// </summary>
	private void ParseFilterOptions()
	{
		var filterable = Prop("filterable")?.Trim();
		if (filterable is not null)
		{
			if (filterable.Length == 0 || filterable.Equals("true", StringComparison.OrdinalIgnoreCase))
				Filterable = true;
			else if (filterable.Equals("search", StringComparison.OrdinalIgnoreCase))
				(Filterable, FilterSearchOnly) = (true, true);
			else if (!filterable.Equals("false", StringComparison.OrdinalIgnoreCase))
				this.EmitError(
					$"Invalid filterable value '{filterable}'. Use :filterable: on its own for automatic dropdown filters, or :filterable: search for a search box without dropdowns."
				);
		}

		var columns = Prop("filter-columns");
		if (columns is null)
			return;

		if (!Filterable)
		{
			this.EmitWarning(":filter-columns: has no effect without :filterable:. Add :filterable: to the table directive.");
			return;
		}

		if (FilterSearchOnly)
		{
			this.EmitError(
				":filter-columns: can't be combined with :filterable: search, which shows no dropdown filters. Remove one of the two options."
			);
			return;
		}

		var names = columns.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (names.Length == 0)
		{
			this.EmitError(":filter-columns: needs at least one column header name. To show no dropdown filters, use :filterable: search.");
			return;
		}

		FilterColumns = names;
	}

	/// <summary>
	/// Checks every <c>:filter-columns:</c> name against the table's header row, so a typo
	/// fails the build instead of silently dropping a filter. Unknown names are removed.
	/// </summary>
	private void ValidateFilterColumns(Markdig.Extensions.Tables.Table table)
	{
		if (FilterColumns.Count == 0)
			return;

		var headers = table.Count > 0 && table[0] is TableRow { IsHeader: true } headerRow
			? headerRow.OfType<TableCell>().Select(CellText).Where(h => h.Length > 0).ToList()
			: [];

		if (headers.Count == 0)
		{
			this.EmitError(":filter-columns: needs a table with a header row, because it matches columns by their header text.");
			FilterColumns = [];
			return;
		}

		var known = new List<string>(FilterColumns.Count);
		foreach (var name in FilterColumns)
		{
			if (headers.Any(h => h.Equals(name, StringComparison.OrdinalIgnoreCase)))
				known.Add(name);
			else
				this.EmitError($"Filter column '{name}' doesn't match any column header. Available headers: {string.Join(", ", headers)}.");
		}

		FilterColumns = known;
	}

	/// <summary>
	/// The visible text of a header cell, matching what the browser reads as its
	/// <c>textContent</c>: literal and code text, with emphasis and links unwrapped.
	/// </summary>
	private static string CellText(TableCell cell)
	{
		var sb = new System.Text.StringBuilder();
		foreach (var paragraph in cell.OfType<LeafBlock>())
		{
			if (paragraph.Inline is not null)
				AppendInlineText(paragraph.Inline, sb);
		}

		return string.Join(' ', sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
	}

	private static void AppendInlineText(ContainerInline container, System.Text.StringBuilder sb)
	{
		foreach (var inline in container)
		{
			switch (inline)
			{
				case LiteralInline literal:
					_ = sb.Append(literal.Content);
					break;
				case CodeInline code:
					_ = sb.Append(code.Content);
					break;
				case ContainerInline nested:
					AppendInlineText(nested, sb);
					break;
			}
		}
	}

	/// <summary>
	/// Validates the column count against the table after pipe-table parsing has completed.
	/// Must be called at render time, not during <see cref="FinalizeAndValidate"/>,
	/// because Markdig converts paragraph blocks to table blocks during inline parsing (after Close).
	/// </summary>
	public void ValidateTableColumnCount()
	{
		var tables = this.OfType<Markdig.Extensions.Tables.Table>().ToList();

		if (tables.Count == 0)
		{
			this.EmitError("Table directive must contain a pipe table. Add a table using Markdown pipe syntax.");
			ColumnWidths = [];
			return;
		}

		if (tables.Count > 1)
		{
			this.EmitError("Table directive must contain exactly one pipe table.");
			ColumnWidths = [];
			return;
		}

		var table = tables[0];
		var columnCount = GetTableColumnCount(table);
		if (columnCount == 0)
		{
			this.EmitError("Table directive contains an empty table.");
			ColumnWidths = [];
			return;
		}

		if (ColumnWidths.Count > 0 && ColumnWidths.Count != columnCount)
		{
			this.EmitError($"Column width count ({ColumnWidths.Count}) does not match table column count ({columnCount}).");
			ColumnWidths = [];
		}

		ValidateFilterColumns(table);
	}

	private static int GetTableColumnCount(Markdig.Extensions.Tables.Table table)
	{
		if (table.Count == 0)
			return 0;

		if (table[0] is TableRow headerRow)
			return headerRow.Count;

		return 0;
	}

	private static List<int>? ParseCustomWidths(string value, TableDirectiveBlock block)
	{
		var parts = value.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		var result = new List<int>(parts.Length);

		foreach (var part in parts)
		{
			if (!int.TryParse(part, out var unit) || unit < 1 || unit > 12)
			{
				block.EmitError(
					$"Invalid widths value '{value}'. Use preset (auto, description) or dash-separated integers 1-12 that sum to 12 (e.g., 4-8, 4-4-4)."
				);
				return null;
			}

			result.Add(unit);
		}

		return result.Count > 0 ? result : null;
	}
}
