// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.CsvFile;

public class CsvFileViewModel : DirectiveViewModel
{
	public required List<string[]> CsvRows { get; init; }

	public static CsvFileViewModel Create(CsvFileBlock csvBlock)
	{
		var rows = new List<string[]>();

		if (csvBlock.Found && !string.IsNullOrEmpty(csvBlock.CsvFilePath))
		{
			var csvData = CsvReader.ReadCsvFile(csvBlock.CsvFilePath, csvBlock.Separator, csvBlock.Build.ReadFileSystem);
			var rowCount = 0;
			var columnCountExceeded = false;

			foreach (var row in csvData)
			{
				if (rowCount >= csvBlock.MaxRows)
				{
					csvBlock.EmitWarning($"CSV file contains more than {csvBlock.MaxRows} rows. Only the first {csvBlock.MaxRows} rows will be displayed. Use :max-rows to increase the limit.");
					break;
				}

				if (row.Length > csvBlock.MaxColumns)
				{
					if (!columnCountExceeded)
					{
						csvBlock.EmitWarning($"CSV file contains more than {csvBlock.MaxColumns} columns. Only the first {csvBlock.MaxColumns} columns will be displayed. Use :max-columns to increase the limit.");
						columnCountExceeded = true;
					}

					var trimmedRow = new string[csvBlock.MaxColumns];
					Array.Copy(row, trimmedRow, csvBlock.MaxColumns);
					rows.Add(trimmedRow);
				}
				else
				{
					rows.Add(row);
				}

				rowCount++;
			}

			if (csvBlock.PreviewOnly && rowCount > 100)
			{
				csvBlock.EmitWarning("Preview mode is enabled. Showing first 100 rows of CSV data.");
				rows = rows.Take(100).ToList();
			}
		}

		return new CsvFileViewModel
		{
			DirectiveBlock = csvBlock,
			CsvRows = rows
		};
	}
}
