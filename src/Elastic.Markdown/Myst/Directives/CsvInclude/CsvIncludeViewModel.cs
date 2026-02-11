// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.CsvInclude;

public class CsvIncludeViewModel : DirectiveViewModel
{
	public required Func<string, HtmlString> RenderMarkdown { get; init; }

	public IEnumerable<string[]> GetCsvRows()
	{
		if (DirectiveBlock is not CsvIncludeBlock csvBlock || !csvBlock.Found || string.IsNullOrEmpty(csvBlock.CsvFilePath))
			return [];

		var csvData = CsvReader.ReadCsvFile(csvBlock.CsvFilePath, csvBlock.Separator, csvBlock.Build.ReadFileSystem);
		var rowCount = 0;
		var columnCountExceeded = false;

		return csvData.TakeWhile(row =>
		{
			if (rowCount >= csvBlock.MaxRows)
			{
				csvBlock.EmitWarning($"CSV file contains more than {csvBlock.MaxRows} rows. Only the first {csvBlock.MaxRows} rows will be displayed.");
				return false;
			}

			if (row.Length > csvBlock.MaxColumns)
			{
				if (!columnCountExceeded)
				{
					csvBlock.EmitWarning($"CSV file contains more than {csvBlock.MaxColumns} columns. Only the first {csvBlock.MaxColumns} columns will be displayed.");
					columnCountExceeded = true;
				}
			}

			rowCount++;
			return true;
		}).Select(row =>
		{
			if (row.Length > csvBlock.MaxColumns)
			{
				var trimmedRow = new string[csvBlock.MaxColumns];
				Array.Copy(row, trimmedRow, csvBlock.MaxColumns);
				return trimmedRow;
			}
			return row;
		});
	}

	public HtmlString RenderCell(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return HtmlString.Empty;

		return RenderMarkdown(value);
	}

	/// <summary>
	/// Returns normalized column width percentages, or null if no widths are specified.
	/// Validates that the width count matches the column count and emits an error if not.
	/// </summary>
	public double[]? GetColumnWidthPercentages(int columnCount)
	{
		if (DirectiveBlock is not CsvIncludeBlock csvBlock || csvBlock.Widths is null)
			return null;

		if (csvBlock.Widths.Length != columnCount)
		{
			csvBlock.EmitError(
				$"{{csv-include}} :widths: specifies {csvBlock.Widths.Length} values but the CSV has {columnCount} columns. " +
				$"The number of widths must match the number of columns."
			);
			return null;
		}

		return WidthsParser.NormalizeToPercentages(csvBlock.Widths);
	}

	public static CsvIncludeViewModel Create(CsvIncludeBlock csvBlock, Func<string, HtmlString> renderMarkdown) =>
		new()
		{
			DirectiveBlock = csvBlock,
			RenderMarkdown = renderMarkdown
		};
}
