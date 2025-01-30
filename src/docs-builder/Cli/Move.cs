// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Documentation.Builder.Cli;

internal class Move(IFileSystem fileSystem, DocumentationSet documentationSet, ILoggerFactory loggerFactory)
{
	private readonly ILogger _logger = loggerFactory.CreateLogger<Move>();
	private readonly List<(string filePath, string originalContent,string newContent)> _changes = [];
	private const string ChangeFormatString = "Change \e[31m{0}\e[0m to \e[32m{1}\e[0m at \e[34m{2}:{3}:{4}\e[0m";

	public async Task<int> Execute(string? source, string? target, bool isDryRun, Cancel ctx = default)
	{
		if (isDryRun)
			_logger.LogInformation("Running in dry-run mode");

		if (!ValidateInputs(source, target))
		{
			return 1;
		}

		var sourcePath = Path.GetFullPath(source!);
		var targetPath = Path.GetFullPath(target!);

		var (_, sourceMarkdownFile) = documentationSet.MarkdownFiles.Single(i => i.Value.FilePath == sourcePath);

		var soureContent = await fileSystem.File.ReadAllTextAsync(sourceMarkdownFile.FilePath, ctx);

		var markdownLinkRegex = new Regex(@"\[([^\]]*)\]\(((?:\.{0,2}\/)?[^:)]+\.md(?:#[^)]*)?)\)", RegexOptions.Compiled);
		var matches = markdownLinkRegex.Matches(soureContent);

		var change = Regex.Replace(soureContent, markdownLinkRegex.ToString(), match =>
		{
			var originalPath = match.Value.Substring(match.Value.IndexOf('(') + 1, match.Value.LastIndexOf(')') - match.Value.IndexOf('(') - 1);
			// var anchor = originalPath.Contains('#') ? originalPath[originalPath.IndexOf('#')..] : "";

			string newPath;

			var isAbsoluteStylePath = originalPath.StartsWith('/');
			if (isAbsoluteStylePath)
			{
				newPath = originalPath;
			} else {
				var targetDirectory = Path.GetDirectoryName(targetPath)!;
				var sourceDirectory = Path.GetDirectoryName(sourceMarkdownFile.FilePath)!;
				var fullPath = Path.GetFullPath(Path.Combine(sourceDirectory, originalPath));
				var relativePath = Path.GetRelativePath(targetDirectory, fullPath);
				newPath = relativePath;
			}
			var newLink = $"[{match.Groups[1].Value}]({newPath})";
			var lineNumber = soureContent.Substring(0, match.Index).Count(c => c == '\n') + 1;
			var columnNumber = match.Index - soureContent.LastIndexOf('\n', match.Index);
			_logger.LogInformation(string.Format(ChangeFormatString, match.Value, newLink,
				sourceMarkdownFile.SourceFile.FullName, lineNumber, columnNumber));

			return newLink;
		});

		_changes.Add((sourceMarkdownFile.FilePath, soureContent, change));

		foreach (var (_, markdownFile) in documentationSet.MarkdownFiles)
		{
			await ProcessMarkdownFile(
				sourcePath,
				targetPath,
				isDryRun,
				markdownFile,
				ctx
			);
		}

		if (isDryRun)
			return 0;

		var targetDirectory = Path.GetDirectoryName(targetPath);
		fileSystem.Directory.CreateDirectory(targetDirectory!); // CreateDirectory automatically creates all necessary parent directories
		fileSystem.File.Move(sourcePath, targetPath);
		// Write changes to disk
        try {
            foreach (var (filePath, _, newContent) in _changes)
                await fileSystem.File.WriteAllTextAsync(filePath, newContent, ctx);
        } catch (Exception) {
            foreach (var (filePath, originalContent, _) in _changes)
                await fileSystem.File.WriteAllTextAsync(filePath, originalContent, ctx);
            fileSystem.File.Move(targetPath, sourcePath);
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

		if (!fileSystem.File.Exists(source))
		{
			_logger.LogError($"Source file {source} does not exist");
			return false;
		}

		if (fileSystem.File.Exists(target))
		{
			_logger.LogError($"Target file {target} already exists");
			return false;
		}

		return true;
	}

	private async Task ProcessMarkdownFile(
		string source,
		string target,
		bool isDryRun,
		MarkdownFile value,
		Cancel ctx)
	{
		var content = await fileSystem.File.ReadAllTextAsync(value.FilePath, ctx);
		var currentDir = Path.GetDirectoryName(value.FilePath)!;
		var pathInfo = GetPathInfo(currentDir, source, target);
		var linkPattern = BuildLinkPattern(pathInfo);

		if (System.Text.RegularExpressions.Regex.IsMatch(content, linkPattern))
		{
			var newContent = ReplaceLinks(content, linkPattern, pathInfo.absoluteStyleTarget, target,value);
			_changes.Add((value.FilePath, content, newContent));
		}
	}

	private (string relativeSource, string relativeSourceWithDotSlash, string absolutStyleSource, string absoluteStyleTarget) GetPathInfo(
		string currentDir,
		string sourcePath,
		string targetPath
	)
	{
		var relativeSource = Path.GetRelativePath(currentDir, sourcePath).Replace('\\', '/');
		var relativeSourceWithDotSlash = Path.Combine(".", relativeSource).Replace('\\', '/');
		var relativeToDocsFolder = Path.GetRelativePath(documentationSet.SourcePath.FullName, sourcePath).Replace('\\', '/');
		var absolutStyleSource = $"/{relativeToDocsFolder}".Replace('\\', '/');
		var relativeToDocsFolderTarget = Path.GetRelativePath(documentationSet.SourcePath.FullName, targetPath);
		var absoluteStyleTarget = $"/{relativeToDocsFolderTarget}".Replace('\\', '/');
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
	)
	{
		return System.Text.RegularExpressions.Regex.Replace(
			content,
			linkPattern,
			match =>
			{
				var originalPath = match.Value.Substring(match.Value.IndexOf('(') + 1, match.Value.LastIndexOf(')') - match.Value.IndexOf('(') - 1);
				var anchor = originalPath.Contains('#')
					? originalPath[originalPath.IndexOf('#')..]
					: "";

				string newLink;
				if (originalPath.StartsWith("/"))
				{
					// Absolute style link
					newLink = $"[{match.Groups[1].Value}]({absoluteStyleTarget}{anchor})";
				}
				else
				{
					// Relative link
					var relativeTarget = Path.GetRelativePath(Path.GetDirectoryName(value.FilePath)!, target).Replace('\\', '/');
					newLink = $"[{match.Groups[1].Value}]({relativeTarget}{anchor})";
				}

				var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
				var columnNumber = match.Index - content.LastIndexOf('\n', match.Index);
				_logger.LogInformation(string.Format(ChangeFormatString, match.Value, newLink, value.SourceFile.FullName,
					lineNumber, columnNumber));
				return newLink;
			});
	}
}
