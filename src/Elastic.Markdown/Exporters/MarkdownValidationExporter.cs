// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;

namespace Elastic.Markdown.Exporters;

public class MarkdownValidationExporter : IMarkdownExporter
{
	private readonly List<MarkdownFile> _files = [];

	/// <inheritdoc />
	public ValueTask StartAsync(Cancel ctx = default) => default;

	/// <inheritdoc />
	public ValueTask StopAsync(Cancel ctx = default)
	{
		var titleMap = new Dictionary<string, List<MarkdownFile>>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in _files)
		{
			if (string.IsNullOrWhiteSpace(file.Title))
			{
				Console.WriteLine($"Error: File {file.FilePath} has no title");
				continue;
			}
			if (!titleMap.TryGetValue(file.Title, out var list))
				titleMap[file.Title] = [];
			titleMap[file.Title].Add(file);
		}
		foreach (var kv in titleMap)
		{
			var files = kv.Value;
			if (files.Count > 1)
			{
				var title = kv.Key;
				var filePaths = string.Join(", ", files.Select(f => f.FilePath));
				Console.WriteLine($"Error: Title '{title}' is used in multiple files: {filePaths}");
			}
		}
		return default;
	}

	/// <inheritdoc />
	public ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var markdownFile = fileContext.SourceFile;
		_files.Add(markdownFile);
		Console.WriteLine($"+++ MarkdownValidationExporter: document.Title: {markdownFile.Title}");
		return ValueTask.FromResult(true);
	}

	/// <inheritdoc />
	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx) => ValueTask.FromResult(true);
}
