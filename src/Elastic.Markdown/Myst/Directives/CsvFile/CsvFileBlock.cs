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
	public int MaxRows { get; private set; } = 10000;
	public long MaxFileSizeBytes { get; private set; } = 10 * 1024 * 1024; // 10MB
	public int MaxColumns { get; private set; } = 100;
	public bool PreviewOnly { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Caption = Prop("caption");

		var separator = Prop("separator", "delimiter");
		if (!string.IsNullOrEmpty(separator))
			Separator = separator;

		if (int.TryParse(Prop("max-rows"), out var maxRows) && maxRows > 0)
			MaxRows = maxRows;

		if (ParseFileSize(Prop("max-size"), out var maxSize) && maxSize > 0)
			MaxFileSizeBytes = maxSize;

		if (int.TryParse(Prop("max-columns"), out var maxColumns) && maxColumns > 0)
			MaxColumns = maxColumns;

		PreviewOnly = bool.TryParse(Prop("preview-only"), out var preview) && preview;

		ExtractCsvPath(context);
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
		{
			ValidateFileSize();
			Found = true;
		}
		else
			this.EmitError($"CSV file `{CsvFilePath}` does not exist.");
	}

	private void ValidateFileSize()
	{
		if (CsvFilePath == null)
			return;

		try
		{
			var fileInfo = Build.ReadFileSystem.FileInfo.New(CsvFilePath);
			if (fileInfo.Length > MaxFileSizeBytes)
			{
				var sizeMB = fileInfo.Length / (1024.0 * 1024.0);
				var maxSizeMB = MaxFileSizeBytes / (1024.0 * 1024.0);
				this.EmitError($"CSV file `{CsvFilePath}` is {sizeMB:F1}MB, which exceeds the maximum allowed size of {maxSizeMB:F1}MB. Use :max-size to increase the limit.");
				Found = false;
			}
		}
		catch (Exception ex)
		{
			this.EmitError($"Could not validate CSV file size: {ex.Message}");
			Found = false;
		}
	}

	private static bool ParseFileSize(string? sizeString, out long bytes)
	{
		bytes = 0;
		if (string.IsNullOrEmpty(sizeString))
			return false;

		var multiplier = 1L;
		var value = sizeString.ToUpperInvariant();

		if (value.EndsWith("KB"))
		{
			multiplier = 1024;
			value = value[..^2];
		}
		else if (value.EndsWith("MB"))
		{
			multiplier = 1024 * 1024;
			value = value[..^2];
		}
		else if (value.EndsWith("GB"))
		{
			multiplier = 1024 * 1024 * 1024;
			value = value[..^2];
		}

		if (double.TryParse(value, out var numericValue))
		{
			bytes = (long)(numericValue * multiplier);
			return true;
		}

		return false;
	}

}
