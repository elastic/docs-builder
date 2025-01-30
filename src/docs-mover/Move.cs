// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Markdown.IO;
using Elastic.Markdown.Slices;
using Microsoft.Extensions.Logging;

namespace Documentation.Mover;

public record ChangeSet(IFileInfo From, IFileInfo To);
public record Change(IFileInfo Source, string OriginalContent, string NewContent);
public record LinkModification(string OldLink, string NewLink, string SourceFile, int LineNumber, int ColumnNumber);

public class Move(IFileSystem readFileSystem, IFileSystem writeFileSystem, DocumentationSet documentationSet, ILoggerFactory loggerFactory)
{
	private const string ChangeFormatString = "Change \e[31m{0}\e[0m to \e[32m{1}\e[0m at \e[34m{2}:{3}:{4}\e[0m";

	private readonly ILogger _logger = loggerFactory.CreateLogger<Move>();
	private readonly Dictionary<ChangeSet, List<Change>> _changes = [];
	private readonly Dictionary<ChangeSet, List<LinkModification>> _linkModifications = [];

	public IReadOnlyDictionary<ChangeSet, List<LinkModification>> LinkModifications => _linkModifications.AsReadOnly();
	public IReadOnlyCollection<ChangeSet> Changes => _changes.Keys;

	public async Task<int> Execute(string source, string target, bool isDryRun, Cancel ctx = default)
	{
		if (isDryRun)
			_logger.LogInformation("Running in dry-run mode");

		if (!ValidateInputs(source, target, out var fromFiles, out var toFiles))
			return 1;

		foreach (var (fromFile, toFile) in fromFiles.Zip(toFiles))
		{
			var changeSet = new ChangeSet(fromFile, toFile);
			_logger.LogInformation($"Requested to move from '{fromFile}' to '{toFile}");
			await SetupChanges(changeSet, ctx);
		}

		return await MoveAndRewriteLinks(isDryRun, ctx);
	}

	private async Task SetupChanges(ChangeSet changeSet, Cancel ctx)
	{
		var sourcePath = changeSet.From.FullName;
		var targetPath = changeSet.To.FullName;

		var sourceContent = await readFileSystem.File.ReadAllTextAsync(sourcePath, ctx);

		var markdownLinkRegex = new Regex(@"\[([^\]]*)\]\(((?:\.{0,2}\/)?[^:)]+\.md(?:#[^)]*)?)\)", RegexOptions.Compiled);

		var change = Regex.Replace(sourceContent, markdownLinkRegex.ToString(), match =>
		{
			var originalPath = match.Value.Substring(match.Value.IndexOf('(') + 1, match.Value.LastIndexOf(')') - match.Value.IndexOf('(') - 1);

			var newPath = originalPath;
			var isAbsoluteStylePath = originalPath.StartsWith('/');
			if (!isAbsoluteStylePath)
			{
				var targetDirectory = Path.GetDirectoryName(targetPath)!;
				var sourceDirectory = Path.GetDirectoryName(sourcePath)!;
				var fullPath = Path.GetFullPath(Path.Combine(sourceDirectory, originalPath));
				var relativePath = Path.GetRelativePath(targetDirectory, fullPath);

				if (originalPath.StartsWith("./") && !relativePath.StartsWith("./"))
					newPath = "./" + relativePath;
				else
					newPath = relativePath;
			}
			var newLink = $"[{match.Groups[1].Value}]({newPath})";
			var lineNumber = sourceContent.Substring(0, match.Index).Count(c => c == '\n') + 1;
			var columnNumber = match.Index - sourceContent.LastIndexOf('\n', match.Index);
			if (!_linkModifications.ContainsKey(changeSet))
				_linkModifications[changeSet] = [];

			_linkModifications[changeSet].Add(new LinkModification(
				match.Value,
				newLink,
				sourcePath,
				lineNumber,
				columnNumber
			));
			return newLink;
		});

		_changes[changeSet] = [ new Change(changeSet.From, sourceContent, change) ];

		foreach (var (_, markdownFile) in documentationSet.MarkdownFiles)
		{
			await ProcessMarkdownFile(
				changeSet,
				markdownFile,
				ctx
			);
		}

	}

	private async Task<int> MoveAndRewriteLinks(bool isDryRun, Cancel ctx)
	{
		foreach (var (changeSet, linkModifications) in _linkModifications)
		{
			foreach (var (oldLink, newLink, sourceFile, lineNumber, columnNumber) in linkModifications)
			{
				_logger.LogInformation(string.Format(
					ChangeFormatString,
					oldLink,
					newLink,
					sourceFile == changeSet.From.FullName && !isDryRun ? changeSet.To.FullName : sourceFile,
					lineNumber,
					columnNumber
				));
			}
		}

		if (isDryRun)
			return 0;

		try
		{
			foreach (var (changeSet, changes) in _changes)
			{
				foreach (var (filePath, _, newContent) in changes)
				{
					if (!filePath.Directory!.Exists)
						writeFileSystem.Directory.CreateDirectory(filePath.Directory.FullName);
					await writeFileSystem.File.WriteAllTextAsync(filePath.FullName, newContent, ctx);

				}

				var targetDirectory = Path.GetDirectoryName(changeSet.To.FullName);
				readFileSystem.Directory.CreateDirectory(targetDirectory!);
				readFileSystem.File.Move(changeSet.From.FullName, changeSet.To.FullName);
			}
		}
		catch (Exception)
		{
			if (_changes.Count > 1)
			{
				_logger.LogError("An error occurred while moving files. Can only revert a single file move at this time");
				throw;
			}

			foreach (var (changeSet, changes) in _changes)
			{
				foreach (var (filePath, originalContent, _) in changes)
					await writeFileSystem.File.WriteAllTextAsync(filePath.FullName, originalContent, ctx);
				if (!changeSet.To.Exists)
					writeFileSystem.File.Move(changeSet.To.FullName, changeSet.From.FullName);
				else
					writeFileSystem.File.Copy(changeSet.To.FullName, changeSet.From.FullName, overwrite: true);
				_logger.LogError("An error occurred while moving files. Reverting changes");
			}
			throw;
		}

		return 0;
	}

	private bool ValidateInputs(string source, string target, out IFileInfo[] fromFiles, out IFileInfo[] toFiles)
	{
		fromFiles = [];
		toFiles = [];

		var fromFile = readFileSystem.FileInfo.New(source);
		var fromDirectory = readFileSystem.DirectoryInfo.New(source);
		var toFile = readFileSystem.FileInfo.New(target);
		var toDirectory = readFileSystem.DirectoryInfo.New(target);

		//from does not exist at all
		if (!fromFile.Exists && !fromDirectory.Exists)
		{
			_logger.LogError(!string.IsNullOrEmpty(fromFile.Extension)
				? $"Source file '{fromFile}' does not exist"
				: $"Source directory '{fromDirectory}' does not exist");
			return false;
		}
		//moving file
		if (fromFile.Exists)
		{
			if (!fromFile.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogError("Source path must be a markdown file. Directory paths are not supported yet");
				return false;
			}

			//if toFile has no extension assume move to folder
			if (toFile.Extension == string.Empty)
				toFile = readFileSystem.FileInfo.New(Path.Combine(toDirectory.FullName, fromFile.Name));

			if (!toFile.Extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogError($"Target path '{toFile.FullName}' must be a markdown file.");
				return false;
			}
			if (toFile.Exists)
			{
				_logger.LogError($"Target file {target} already exists");
				return false;
			}
			fromFiles = [fromFile];
			toFiles = [toFile];
		}
		//moving folder
		else if (fromDirectory.Exists)
		{
			if (toDirectory.Exists)
			{
				_logger.LogError($"Target directory '{toDirectory.FullName}' already exists.");
				return false;
			}

			if (toDirectory.FullName.StartsWith(fromDirectory.FullName))
			{
				_logger.LogError($"Can not move source directory '{toDirectory.FullName}' to a  {toFile.FullName}");
				return false;
			}

			fromFiles = fromDirectory.GetFiles("*.md", SearchOption.AllDirectories);
			toFiles = fromFiles.Select(f =>
			{
				var relative = Path.GetRelativePath(fromDirectory.FullName, f.FullName);
				return readFileSystem.FileInfo.New(Path.Combine(toDirectory.FullName, relative));
			}).ToArray();
		}

		return true;
	}

	private async Task ProcessMarkdownFile(ChangeSet changeSet, MarkdownFile value, Cancel ctx)
	{
		var source = changeSet.From.FullName;
		var target = changeSet.To.FullName;

		var content = await readFileSystem.File.ReadAllTextAsync(value.FilePath, ctx);
		var currentDir = Path.GetDirectoryName(value.FilePath)!;
		var pathInfo = GetPathInfo(currentDir, source, target);
		var linkPattern = BuildLinkPattern(pathInfo);

		if (Regex.IsMatch(content, linkPattern))
		{
			var newContent = ReplaceLinks(changeSet, content, linkPattern, pathInfo.absoluteStyleTarget, target, value);
			_changes[changeSet].Add(new Change(value.SourceFile, content, newContent));
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
		ChangeSet changeSet,
		string content,
		string linkPattern,
		string absoluteStyleTarget,
		string target,
		MarkdownFile value) =>
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
					newLink = originalPath.StartsWith("./") && !relativeTarget.StartsWith("./")
						? $"[{match.Groups[1].Value}](./{relativeTarget}{anchor})"
						: $"[{match.Groups[1].Value}]({relativeTarget}{anchor})";
				}

				var lineNumber = content.Substring(0, match.Index).Count(c => c == '\n') + 1;
				var columnNumber = match.Index - content.LastIndexOf('\n', match.Index);
				if (!_linkModifications.ContainsKey(changeSet))
					_linkModifications[changeSet] = [];
				_linkModifications[changeSet].Add(new LinkModification(
					match.Value,
					newLink,
					value.SourceFile.FullName,
					lineNumber,
					columnNumber
				));
				return newLink;
			});
}
