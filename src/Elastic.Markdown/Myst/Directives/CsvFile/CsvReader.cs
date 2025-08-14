// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using nietras.SeparatedValues;

namespace Elastic.Markdown.Myst.Directives.CsvFile;

public static class CsvReader
{
	public static List<string[]> ReadCsvFile(string filePath, string separator, IFileSystem? fileSystem = null)
	{
		var rows = new List<string[]>();

		try
		{
			var fs = fileSystem ?? new FileSystem();

			// Try to use Sep library first
			if (TryReadWithSep(filePath, separator, fs, out var sepRows))
			{
				return sepRows;
			}

			// Fallback to custom parser if Sep fails
			var content = fs.File.ReadAllText(filePath);
			var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (string.IsNullOrWhiteSpace(line.Trim()))
					continue;

				var fields = ParseCsvLine(line, separator);
				rows.Add(fields);
			}
		}
		catch
		{
			// If parsing fails, return empty rows
		}

		return rows;
	}

	private static bool TryReadWithSep(string filePath, string separator, IFileSystem fileSystem, out List<string[]> rows)
	{
		rows = [];

		try
		{
			var separatorChar = separator == "," ? ',' : separator[0];
			var spec = Sep.New(separatorChar);

			// Sep works with actual file paths, not virtual file systems
			// For testing with MockFileSystem, we'll read content first
			if (fileSystem.GetType().Name == "MockFileSystem")
			{
				var content = fileSystem.File.ReadAllText(filePath);
				using var reader = spec.Reader(o => o with { HasHeader = false, Unescape = true }).FromText(content);

				foreach (var row in reader)
				{
					var rowData = new string[row.ColCount];
					for (var i = 0; i < row.ColCount; i++)
					{
						rowData[i] = row[i].ToString();
					}
					rows.Add(rowData);
				}
			}
			else
			{
				using var reader = spec.Reader(o => o with { HasHeader = false, Unescape = true }).FromFile(filePath);

				foreach (var row in reader)
				{
					var rowData = new string[row.ColCount];
					for (var i = 0; i < row.ColCount; i++)
					{
						rowData[i] = row[i].ToString();
					}
					rows.Add(rowData);
				}
			}

			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string[] ParseCsvLine(string line, string separator)
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
			else if (c.ToString() == separator && !inQuotes)
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
