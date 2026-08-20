// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.Myst;
using Generator.Equals;

namespace Elastic.Markdown.IO;

[Equatable]
[DebuggerDisplay("{RelativePath,nq}")]
public partial record FilePath
{
	public FilePath(IFileInfo fileInfo, IDirectoryInfo sourceDirectory)
	{
		FileInfo = fileInfo;
		RelativePath = Path.GetRelativePath(sourceDirectory.FullName, fileInfo.FullName);
	}

	public FilePath(string relativePath, IDirectoryInfo sourceDirectory)
	{
		FileInfo = sourceDirectory.FileSystem.NewFileInfo(sourceDirectory.FullName, relativePath);
		RelativePath = relativePath;
	}

	[StringEquality(StringComparison.OrdinalIgnoreCase)]
	public string RelativePath { get; }

	[IgnoreEquality]
	public IFileInfo FileInfo { get; }
}

public class MarkdownFileFactory : IDocumentationFileFactory<MarkdownFile>
{
	private readonly BuildContext _context;
	private readonly MarkdownParser _markdownParser;

	public MarkdownFileFactory(
		BuildContext context,
		MarkdownParser markdownParser,
		IReadOnlyCollection<IDocsBuilderExtension> enabledExtensions
	)
	{
		_context = context;
		_markdownParser = markdownParser;
		EnabledExtensions = enabledExtensions;

		var files = ScanDocumentationFiles(context, context.DocumentationSourceDirectory);
		var additionalSources = enabledExtensions.SelectMany(extension => extension.ScanDocumentationFiles(DefaultFileHandling)).ToArray();

		Files =
			files.Concat(additionalSources)
				.Where(t => t.Item2 is not ExcludedFile)
				.ToDictionary(kv => new FilePath(kv.Item1, context.DocumentationSourceDirectory), kv => kv.Item2)
				.ToFrozenDictionary();
	}

	public FrozenDictionary<FilePath, DocumentationFile> Files { get; }

	private IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; }

	/// <inheritdoc />
	public MarkdownFile? TryCreateDocumentationFile(IFileInfo path, IFileSystem readFileSystem)
	{
		var filePath = new FilePath(path, _context.DocumentationSourceDirectory);
		if (Files.TryGetValue(filePath, out var file))
		{
			if (file is MarkdownFile markdown)
				return markdown;
		}

		return null;
	}

	private (IFileInfo, DocumentationFile)[] ScanDocumentationFiles(BuildContext build, IDirectoryInfo sourceDirectory)
	{
		// Cache directory-attribute lookups so that a directory shared by many files is only
		// stat'd once rather than once per file it contains (the old code allocated a fresh
		// IDirectoryInfo wrapper and triggered a stat for every file).
		var dirAttrCache = new Dictionary<string, FileAttributes>(StringComparer.Ordinal);

		return [
			.. build.ReadFileSystem
				.Directory
				.EnumerateFiles(sourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
				// Compute relative path once from the raw string before IFileInfo allocation.
				// This also lets us do the hidden-folder dot-prefix check with zero metadata syscalls.
				.Select(path => (path, relative: Path.GetRelativePath(sourceDirectory.FullName, path)))
				// Skip dot-prefixed paths (Unix hidden dirs) — pure string, no stat
				.Where(t => !t.relative.StartsWith('.'))
				// Now create the IFileInfo (triggers stat on first property access)
				.Select(t => (file: build.ReadFileSystem.FileInfo.New(t.path), t.relative))
				.Where(t =>
				{
					// Single Attributes read covers hidden, system, and symlink (ReparsePoint) checks;
					// the original code read Attributes twice for the file and twice more via Directory.
					var fileAttr = t.file.Attributes;
					if (fileAttr.HasFlag(FileAttributes.Hidden) || fileAttr.HasFlag(FileAttributes.System))
						return false;
					// Skip symlinks
					if (t.file.LinkTarget != null)
						return false;
					// Check parent directory attributes with per-directory caching
					var dirPath = Path.GetDirectoryName(t.file.FullName)!;
					if (!dirAttrCache.TryGetValue(dirPath, out var dirAttr))
					{
						dirAttr = build.ReadFileSystem.DirectoryInfo.New(dirPath).Attributes;
						dirAttrCache[dirPath] = dirAttr;
					}
					return !dirAttr.HasFlag(FileAttributes.Hidden) && !dirAttr.HasFlag(FileAttributes.System);
				})
				.Select<(IFileInfo file, string relative), (IFileInfo, DocumentationFile)>(
					t =>
						t.file.Extension switch
						{
							".jpg" => (t.file, CreateImageFile(t.file, sourceDirectory, build, t.relative, "image/jpeg")),
							".jpeg" => (t.file, CreateImageFile(t.file, sourceDirectory, build, t.relative, "image/jpeg")),
							".gif" => (t.file, CreateImageFile(t.file, sourceDirectory, build, t.relative, "image/gif")),
							".svg" => (t.file, CreateImageFile(t.file, sourceDirectory, build, t.relative, "image/svg+xml")),
							".png" => (t.file, CreateImageFile(t.file, sourceDirectory, build, t.relative)),
							".md" => CreateMarkdownTuple(t.file, build),
							_ => (t.file, DefaultFileHandling(t.file, sourceDirectory))
						}
				)
		];
	}

	private DocumentationFile CreateImageFile(
		IFileInfo file,
		IDirectoryInfo sourceDirectory,
		BuildContext context,
		string relativePath,
		string mimeType = "image/png"
	)
	{
		if (context.Configuration.IsExcluded(relativePath))
			return new ExcludedFile(file, sourceDirectory, context.Git.RepositoryName);

		return new ImageFile(file, sourceDirectory, context.Git.RepositoryName, mimeType);
	}

	private (IFileInfo, DocumentationFile) CreateMarkdownTuple(IFileInfo file, BuildContext context)
	{
		var doc = CreateMarkDownFile(file, context);
		// Extensions may create files with a canonical SourceFile different from the discovery path
		// (e.g. CLI cmd-upload.md → upload.md). Register under SourceFile so navigation lookups work.
		return (doc.SourceFile, doc);
	}

	private DocumentationFile CreateMarkDownFile(IFileInfo file, BuildContext context)
	{
		var sourceDirectory = context.DocumentationSourceDirectory;
		var relativePath = Path.GetRelativePath(sourceDirectory.FullName, file.FullName);
		if (context.Configuration.IsExcluded(relativePath))
			return new ExcludedFile(file, sourceDirectory, context.Git.RepositoryName);

		if (
			relativePath.Contains($"{Path.DirectorySeparatorChar}_snippets{Path.DirectorySeparatorChar}") ||
			relativePath.StartsWith($"_snippets{Path.DirectorySeparatorChar}")
		)
			return new SnippetFile(file, sourceDirectory, context.Git.RepositoryName);

		// we ignore files in folders that start with an underscore
		var folder = Path.GetDirectoryName(relativePath);
		if (folder is not null && (folder.Contains($"{Path.DirectorySeparatorChar}_", StringComparison.Ordinal) || folder.StartsWith('_')))
			return new ExcludedFile(file, sourceDirectory, context.Git.RepositoryName);

		// Todo re-enable not included check else where
		// var config = context.ConfigurationYaml;
		//if (config.Files.Contains(relativePath))
		return ExtensionOrDefaultMarkdown();

		//context.Collector.EmitError(config.SourceFile, $"Not linked in toc: {relativePath}");
		//return new ExcludedFile(file, sourceDirectory, context.Git.RepositoryName);

		MarkdownFile ExtensionOrDefaultMarkdown()
		{
			foreach (var extension in EnabledExtensions)
			{
				var documentationFile = extension.CreateMarkdownFile(file, sourceDirectory, _markdownParser);
				if (documentationFile is not null)
					return documentationFile;
			}
			return new MarkdownFile(file, sourceDirectory, _markdownParser, context);
		}
	}

	private DocumentationFile DefaultFileHandling(IFileInfo file, IDirectoryInfo sourceDirectory)
	{
		foreach (var extension in EnabledExtensions)
		{
			var documentationFile = extension.CreateDocumentationFile(file, _markdownParser);
			if (documentationFile is not null)
				return documentationFile;
		}
		return new ExcludedFile(file, sourceDirectory, _context.Git.RepositoryName);
	}
}
