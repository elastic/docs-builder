// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Markdig.Extensions.Tables;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Table;

/// <summary>
/// A wrapper directive for pipe tables that allows controlling column widths using a 12-unit grid system.
/// Supports presets (<c>even</c>, <c>definition</c>) and custom dash-separated values (e.g., <c>4-8</c>, <c>4-4-4</c>).
/// </summary>
public class TableDirectiveBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "table";

	/// <summary>
	/// Column widths as percentages (e.g., 33.33, 66.67 for 4-8). Empty when using <c>even</c> preset or no widths specified.
	/// </summary>
	public IReadOnlyList<double> ColumnWidths { get; private set; } = [];

	public override void FinalizeAndValidate(ParserContext context)
	{
		var widthsValue = Prop("widths")?.Trim();

		if (string.IsNullOrEmpty(widthsValue) || widthsValue.Equals("even", StringComparison.OrdinalIgnoreCase))
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
	/// Validates the column count against the table after pipe-table parsing has completed.
	/// Must be called at render time, not during <see cref="FinalizeAndValidate"/>,
	/// because Markdig converts paragraph blocks to table blocks during inline parsing (after Close).
	/// </summary>
	public void ValidateTableColumnCount()
	{
		Markdig.Extensions.Tables.Table? table = null;
		foreach (var block in this)
		{
			if (block is Markdig.Extensions.Tables.Table t)
			{
				table = t;
				break;
			}
		}

		if (table is null)
		{
			this.EmitError("Table directive must contain a pipe table. Add a table using Markdown pipe syntax.");
			ColumnWidths = [];
			return;
		}

		var columnCount = GetTableColumnCount(table);
		if (columnCount == 0)
		{
			this.EmitError("Table directive contains an empty table.");
			ColumnWidths = [];
			return;
		}

		if (ColumnWidths.Count > 0 && ColumnWidths.Count != columnCount)
		{
			this.EmitError(
				$"Column width count ({ColumnWidths.Count}) does not match table column count ({columnCount}).");
			ColumnWidths = [];
		}
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
					$"Invalid widths value '{value}'. Use preset (even, definition) or dash-separated integers 1-12 that sum to 12 (e.g., 4-8, 4-4-4).");
				return null;
			}

			result.Add(unit);
		}

		return result.Count > 0 ? result : null;
	}
}
