// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.GitDiff;
using Elastic.Markdown.Myst.Directives.CsvInclude;
using Elastic.Markdown.Myst.Directives.Include;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.GitDiff;

public sealed class GitDiffMarkdownExporter : IMarkdownExporter
{
	private readonly ILoggerFactory _logFactory;
	private readonly ILogger _logger;
	private readonly Func<string[], string>? _gitCommand;
	private readonly ConcurrentDictionary<string, BuiltPageInfo> _builtPages = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _includeIndex = new(StringComparer.OrdinalIgnoreCase);
	private BuildContext? _buildContext;
	private string _docsetPrefix = string.Empty;

	public GitDiffMarkdownExporter(ILoggerFactory logFactory) : this(logFactory, null)
	{
	}

	internal GitDiffMarkdownExporter(ILoggerFactory logFactory, Func<string[], string>? gitCommand)
	{
		_logFactory = logFactory;
		_gitCommand = gitCommand;
		_logger = logFactory.CreateLogger<GitDiffMarkdownExporter>();
	}

	public ValueTask StartAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask StopAsync(Cancel ctx = default) => ValueTask.CompletedTask;

	public ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		if (_buildContext is null)
		{
			_buildContext = fileContext.BuildContext;
			_docsetPrefix = GitDiffPathNormalization.Normalize(Path.GetRelativePath(
				fileContext.BuildContext.DocumentationCheckoutDirectory.FullName,
				fileContext.BuildContext.DocumentationSourceDirectory.FullName
			));
		}

		var sourcePath = GitDiffPathNormalization.Normalize(fileContext.SourceFile.RelativePath);
		_builtPages[sourcePath] = new BuiltPageInfo(
			fileContext.NavigationItem.Url,
			fileContext.SourceFile.Title
		);

		foreach (var includePath in CollectIncludePaths(fileContext.Document))
		{
			var normalizedInclude = GitDiffPathNormalization.Normalize(includePath);
			_ = _includeIndex.GetOrAdd(normalizedInclude, static _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase))
				.TryAdd(sourcePath, 0);
		}

		return ValueTask.FromResult(true);
	}

	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx)
	{
		if (_buildContext is null)
		{
			_logger.LogWarning("Git diff exporter did not process any pages; skipping changed-pages.json");
			return true;
		}

		var changeResult = new GitChangedFileSource(
			_logFactory,
			_buildContext.DocumentationCheckoutDirectory,
			_docsetPrefix,
			_buildContext.Environment,
			_gitCommand
		).GetChanges();

		var includeIndex = _includeIndex.ToDictionary(
			static pair => pair.Key,
			static pair => (IReadOnlyCollection<string>)pair.Value.Keys,
			StringComparer.OrdinalIgnoreCase);

		var export = ChangedPagesMapper.Map(
			changeResult.Base,
			_docsetPrefix,
			_builtPages,
			includeIndex,
			changeResult.Changes
		);

		if (!outputFolder.Exists)
			outputFolder.Create();

		var outputPath = Path.Join(outputFolder.FullName, ChangedPagesExportFile.FileName);
		await _buildContext.WriteFileSystem.File.WriteAllTextAsync(outputPath, ChangedPagesExportFile.Serialize(export), ctx);
		_logger.LogInformation("Wrote {Count} changed pages to {OutputPath}", export.Pages.Count, outputPath);
		return true;
	}

	private static IEnumerable<string> CollectIncludePaths(MarkdownDocument document)
	{
		foreach (var includeBlock in document.Descendants<IncludeBlock>())
		{
			if (!includeBlock.Found || string.IsNullOrWhiteSpace(includeBlock.IncludePathRelativeToSource))
				continue;

			yield return includeBlock.IncludePathRelativeToSource;
		}

		foreach (var csvIncludeBlock in document.Descendants<CsvIncludeBlock>())
		{
			if (!csvIncludeBlock.Found || string.IsNullOrWhiteSpace(csvIncludeBlock.CsvFilePathRelativeToSource))
				continue;

			yield return csvIncludeBlock.CsvFilePathRelativeToSource;
		}
	}
}
