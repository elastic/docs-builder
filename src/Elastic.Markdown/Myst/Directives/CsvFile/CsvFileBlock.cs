// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.CsvFile;

public class CsvFileBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "csv-file";

	public string? CsvFilePath { get; private set; }
	public string? CsvFilePathRelativeToSource { get; private set; }
	public bool Found { get; private set; }
	public string? Caption { get; private set; }
	public string Separator { get; private set; } = ",";
	public List<string[]> CsvData { get; private set; } = [];

	public override void FinalizeAndValidate(ParserContext context)
	{
		Caption = Prop("caption");

		var separator = Prop("separator", "delimiter");
		if (!string.IsNullOrEmpty(separator))
			Separator = separator;

		ExtractCsvPath(context);
		if (Found)
			ParseCsvFile();
	}

	private void ExtractCsvPath(ParserContext context)
	{
		var csvPath = Arguments;
		if (string.IsNullOrWhiteSpace(csvPath))
		{
			this.EmitError("csv-file requires an argument specifying the path to the CSV file.");
			return;
		}

		var csvFrom = context.MarkdownSourcePath.Directory!.FullName;
		if (csvPath.StartsWith('/'))
			csvFrom = Build.DocumentationSourceDirectory.FullName;

		CsvFilePath = Path.Combine(csvFrom, csvPath.TrimStart('/'));
		CsvFilePathRelativeToSource = Path.GetRelativePath(Build.DocumentationSourceDirectory.FullName, CsvFilePath);

		if (Build.ReadFileSystem.File.Exists(CsvFilePath))
			Found = true;
		else
			this.EmitError($"CSV file `{CsvFilePath}` does not exist.");
	}

	private void ParseCsvFile()
	{
		try
		{
			var file = Build.ReadFileSystem.FileInfo.New(CsvFilePath!);
			var content = file.FileSystem.File.ReadAllText(file.FullName);

			// Split into lines and parse each line
			var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line.Trim()))
					continue;

				var fields = ParseCsvLine(line);
				CsvData.Add(fields);
			}
		}
		catch (Exception e)
		{
			this.EmitError($"Failed to parse CSV file: {e.Message}");
		}
	}

	private string[] ParseCsvLine(string line)
	{
		var fields = new List<string>();
		var currentField = "";
		var inQuotes = false;
		var i = 0;

		while (i < line.Length)
		{
			var c = line[i];

			if (c == '"')
			{
				if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
				{
					// Escaped quote
					currentField += '"';
					i += 2;
				}
				else
				{
					// Toggle quote state
					inQuotes = !inQuotes;
					i++;
				}
			}
			else if (c.ToString() == Separator && !inQuotes)
			{
				// End of field
				fields.Add(currentField.Trim());
				currentField = "";
				i++;
			}
			else
			{
				currentField += c;
				i++;
			}
		}

		// Add the last field
		fields.Add(currentField.Trim());

		return fields.ToArray();
	}
}
