// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Documentation.Mover;

public class Move(IFileSystem readFileSystem, IFileSystem writeFileSystem, DocumentationSet documentationSet, ILoggerFactory loggerFactory)
{
	private readonly ILogger _logger = loggerFactory.CreateLogger<Move>();
	private readonly List<(string filePath, string originalContent, string newContent)> _changes = [];
	private readonly List<LinkModification> _linkModifications = [];
	private const string ChangeFormatString = "Change \e[31m{0}\e[0m to \e[32m{1}\e[0m at \e[34m{2}:{3}:{4}\e[0m";

	public record LinkModification(string OldLink, string NewLink, string SourceFile, int LineNumber, int ColumnNumber);


	public ReadOnlyCollection<LinkModification> LinkModifications => _linkModifications.AsReadOnly();

	public async Task<int> Execute(string? source, string? target, bool isDryRun, Cancel ctx = default)
	{
		_linkModifications.Clear();
		if (isDryRun)
			_logger.LogInformation("Running in dry-run mode");

		if (!ValidateInputs(source, target))
		{
			return 1;
		}


		var sourcePath = Path.GetFullPath(source!);
		var targetPath = Path.GetFullPath(target!);

		var (_, sourceMarkdownFile) = documentationSet.MarkdownFiles.Single(i => i.Value.FilePath == sourcePath);

		var sourceContent = await readFileSystem.File.ReadAllTextAsync(sourceMarkdownFile.FilePath, ctx);

		var markdownLinkRegex = new Regex(@"\[([^\]]*)\]\(((?:\.{0,2}\/)?[^:)]+\.md(?:#[^)]*)?)\)", RegexOptions.Compiled);

		var change = Regex.Replace(sourceContent, markdownLinkRegex.ToString(), match =>
		{
			var originalPath = match.Value.Substring(match.Value.IndexOf('(') + 1, match.Value.LastIndexOf(')') - match.Value.IndexOf('(') - 1);

			var newPath = originalPath;
			var isAbsoluteStylePath = originalPath.StartsWith('/');
			if (!isAbsoluteStylePath)
			{
				var targetDirectory = Path.GetDirectoryName(targetPath)!;
				var sourceDirectory = Path.GetDirectoryName(sourceMarkdownFile.FilePath)!;
				var fullPath = Path.GetFullPath(Path.Combine(sourceDirectory, originalPath));
				var relativePath = Path.GetRelativePath(targetDirectory, fullPath);
				newPath = relativePath;
			}
			var newLink = $"[{match.Groups[1].Value}]({newPath})";
			var lineNumber = sourceContent.Substring(0, match.Index).Count(c => c == '\n') + 1;
			var columnNumber = match.Index - sourceContent.LastIndexOf('\n', match.Index);
			_linkModifications.Add(new LinkModification(
				match.Value,
				newLink,
				sourceMarkdownFile.SourceFile.FullName,
				lineNumber,
				columnNumber
			));
			return newLink;
		});

		_changes.Add((sourceMarkdownFile.FilePath, sourceContent, change));

		foreach (var (_, markdownFile) in documentationSet.MarkdownFiles)
		{
			await ProcessMarkdownFile(
				sourcePath,
				targetPath,
				markdownFile,
				ctx
			);
		}

		foreach (var (oldLink, newLink, sourceFile, lineNumber, columnNumber) in LinkModifications)
		{
			_logger.LogInformation(string.Format(
				ChangeFormatString,
				oldLink,
				newLink,
				sourceFile,
				lineNumber,
				columnNumber
			));
		}

		if (isDryRun)
			return 0;

		var targetDirectory = Path.GetDirectoryName(targetPath);
		readFileSystem.Directory.CreateDirectory(targetDirectory!);
		readFileSystem.File.Move(sourcePath, targetPath);
		try
		{
			foreach (var (filePath, _, newContent) in _changes)
				await writeFileSystem.File.WriteAllTextAsync(filePath, newContent, ctx);
		}
		catch (Exception)
		{
			foreach (var (filePath, originalContent, _) in _changes)
				await writeFileSystem.File.WriteAllTextAsync(filePath, originalContent, ctx);
			writeFileSystem.File.Move(targetPath, sourcePath);
			throw;
		}
		return 0;
	}

	private bool ValidateInputs(string? source, string? target)
	{

		if (string.IsNullOrEmpty(source))
		{
			_logger.LogError("Source path is required");
			return false;
		}

		if (string.IsNullOrEmpty(target))
		{
			_logger.LogError("Target path is required");
			return false;
		}

		if (!Path.GetExtension(source).Equals(".md", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogError("Source path must be a markdown file. Directory paths are not supported yet");
			return false;
		}

		if (!Path.GetExtension(target).Equals(".md", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogError("Target path must be a markdown file. Directory paths are not supported yet");
			return false;
		}

		if (!readFileSystem.File.Exists(source))
		{
			_logger.LogError($"Source file {source} does not exist");
			return false;
		}

		if (readFileSystem.File.Exists(target))
		{
			_logger.LogError($"Target file {target} already exists");
			return false;
		}

		return true;
	}

	private async Task ProcessMarkdownFile(
		string source,
		string target,
		MarkdownFile value,
		Cancel ctx)
	{
		var content = await readFileSystem.File.ReadAllTextAsync(value.FilePath, ctx);
		var currentDir = Path.GetDirectoryName(value.FilePath)!;
		var pathInfo = GetPathInfo(currentDir, source, target);
		var linkPattern = BuildLinkPattern(pathInfo);

		if (Regex.IsMatch(content, linkPattern))
		{
			var newContent = ReplaceLinks(content, linkPattern, pathInfo.absoluteStyleTarget, target, value);
			_changes.Add((value.FilePath, content, newContent));
		}
	}

	private (string relativeSource, string relativeSourceWithDotSlash, string absolutStyleSource, string absoluteStyleTarget) GetPathInfo(
		string currentDir,
		string sourcePath,
		string targetPath
	)
	{
		var relativeSource = Path.GetRelativePath(currentDir, sourcePath);
		var relativeSourceWithDotSlash = Path.Combine(".", relativeSource);
		var relativeToDocsFolder = Path.GetRelativePath(documentationSet.SourcePath.FullName, sourcePath);
		var absolutStyleSource = $"/{relativeToDocsFolder}";
		var relativeToDocsFolderTarget = Path.GetRelativePath(documentationSet.SourcePath.FullName, targetPath);
		var absoluteStyleTarget = $"/{relativeToDocsFolderTarget}";
		return (
			relativeSource,
			relativeSourceWithDotSlash,
			absolutStyleSource,
			absoluteStyleTarget
		);
	}

	private static string BuildLinkPattern(
		(string relativeSource, string relativeSourceWithDotSlash, string absolutStyleSource, string _) pathInfo) =>
		$@"\[([^\]]*)\]\((?:{pathInfo.relativeSource}|{pathInfo.relativeSourceWithDotSlash}|{pathInfo.absolutStyleSource})(?:#[^\)]*?)?\)";

	private string ReplaceLinks(
		string content,
		string linkPattern,
		string absoluteStyleTarget,
		string target,
		MarkdownFile value
	) =>
		Regex.Replace(
			content,
			linkPattern,
			match =>
			{
				var originalPath = match.Value.Substring(match.Value.IndexOf('(') + 1, match.Value.LastIndexOf(')') - match.Value.IndexOf('(') - 1);
				var anchor = originalPath.Contains('#')
					? originalPath[originalPath.IndexOf('#')..]
					: "";

				string newLink;
				if (originalPath.StartsWith('/'))
				{
					newLink = $"[{match.Groups[1].Value}]({absoluteStyleTarget}{anchor})";
				}
				else
				{
					var relativeTarget = Path.GetRelativePath(Path.GetDirectoryName(value.FilePath)!, target);
					newLink = $"[{match.Groups[1].Value}]({relativeTarget}{anchor})";
				}

				var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
				var columnNumber = match.Index - content.LastIndexOf('\n', match.Index);
				_linkModifications.Add(new LinkModification(
					match.Value,
					newLink,
					value.SourceFile.FullName,
					lineNumber,
					columnNumber
				));
				return newLink;
			});
}
