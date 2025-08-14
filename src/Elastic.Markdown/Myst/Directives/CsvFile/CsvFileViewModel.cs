// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


namespace Elastic.Markdown.Myst.Directives.CsvFile;

public class CsvFileViewModel : DirectiveViewModel
{
	public required List<string[]> CsvRows { get; init; }

	public static CsvFileViewModel Create(CsvFileBlock csvBlock)
	{
		var rows = csvBlock.Found && !string.IsNullOrEmpty(csvBlock.CsvFilePath)
			? CsvReader.ReadCsvFile(csvBlock.CsvFilePath, csvBlock.Separator, csvBlock.Build.ReadFileSystem)
			: [];

		return new CsvFileViewModel
		{
			DirectiveBlock = csvBlock,
			CsvRows = rows
		};
	}
}
