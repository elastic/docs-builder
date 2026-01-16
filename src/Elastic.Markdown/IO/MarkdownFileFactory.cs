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

	public MarkdownFileFactory(BuildContext context, MarkdownParser markdownParser, IReadOnlyCollection<IDocsBuilderExtension> enabledExtensions)
	{
		_context = context;
		_markdownParser = markdownParser;
		EnabledExtensions = enabledExtensions;

		var files = ScanDocumentationFiles(context, context.DocumentationSourceDirectory);
		var additionalSources = enabledExtensions
			.SelectMany(extension => extension.ScanDocumentationFiles(DefaultFileHandling))
			.ToArray();

		Files = files.Concat(additionalSources)
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

	private (IFileInfo, DocumentationFile)[] ScanDocumentationFiles(BuildContext build, IDirectoryInfo sourceDirectory) =>
	[.. build.ReadFileSystem.Directory
		.EnumerateFiles(sourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
		.Select(f => build.ReadFileSystem.FileInfo.New(f))
		.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
		.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
		// skip hidden folders
		.Where(f => !Path.GetRelativePath(sourceDirectory.FullName, f.FullName).StartsWith('.'))
		.Select<IFileInfo, (IFileInfo,DocumentationFile)>(file => file.Extension switch
		{
			".jpg" => (file, new ImageFile(file, sourceDirectory, build.Git.RepositoryName, "image/jpeg")),
			".jpeg" => (file, new ImageFile(file, sourceDirectory, build.Git.RepositoryName, "image/jpeg")),
			".gif" => (file, new ImageFile(file, sourceDirectory, build.Git.RepositoryName, "image/gif")),
			".svg" => (file, new ImageFile(file, sourceDirectory, build.Git.RepositoryName, "image/svg+xml")),
			".png" => (file, new ImageFile(file, sourceDirectory, build.Git.RepositoryName)),
			".md" => (file, CreateMarkDownFile(file, build)),
			_ => (file, DefaultFileHandling(file, sourceDirectory))
		})];

	private DocumentationFile CreateMarkDownFile(IFileInfo file, BuildContext context)
	{
		var sourceDirectory = context.DocumentationSourceDirectory;
		var relativePath = Path.GetRelativePath(sourceDirectory.FullName, file.FullName);
		if (context.Configuration.IsExcluded(relativePath))
			return new ExcludedFile(file, sourceDirectory, context.Git.RepositoryName);

		if (relativePath.Contains("_snippets"))
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
