// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.CsvInclude;

public class CsvIncludeViewModel : DirectiveViewModel
{
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
		}).Take(csvBlock.PreviewOnly ? 100 : int.MaxValue);
	}

	public static CsvIncludeViewModel Create(CsvIncludeBlock csvBlock) =>
		new()
		{
			DirectiveBlock = csvBlock
		};
}
