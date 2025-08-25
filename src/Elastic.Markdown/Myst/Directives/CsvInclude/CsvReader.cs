// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using nietras.SeparatedValues;

namespace Elastic.Markdown.Myst.Directives.CsvInclude;

public static class CsvReader
{
	public static IEnumerable<string[]> ReadCsvFile(string filePath, string separator, IFileSystem? fileSystem = null)
	{
		var fs = fileSystem ?? new FileSystem();
		return ReadWithSep(filePath, separator, fs);
	}

	private static IEnumerable<string[]> ReadWithSep(string filePath, string separator, IFileSystem fileSystem)
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
					rowData[i] = row[i].ToString();
				yield return rowData;
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
				yield return rowData;
			}
		}
	}

}
