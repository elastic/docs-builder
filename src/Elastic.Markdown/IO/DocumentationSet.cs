// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Myst;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.IO;

public class DocumentationSet
{
	public BuildContext Context { get; }
	public string Name { get; }
	public IFileInfo OutputStateFile { get; }
	public IFileInfo LinkReferenceFile { get; }

	public IDirectoryInfo SourcePath { get; }
	public IDirectoryInfo OutputPath { get; }

	public string RelativeSourcePath { get; }

	public DateTimeOffset LastWrite { get; }

	public ConfigurationFile Configuration { get; }

	public MarkdownParser MarkdownParser { get; }

	public ICrossLinkResolver LinkResolver { get; }

	public DocumentationSet(BuildContext context, ILoggerFactory logger, ICrossLinkResolver? linkResolver = null)
	{
		Context = context;
		SourcePath = context.SourcePath;
		OutputPath = context.OutputPath;
		RelativeSourcePath = Path.GetRelativePath(Paths.Root.FullName, SourcePath.FullName);
		LinkResolver = linkResolver ?? new CrossLinkResolver(context.Configuration, logger);
		Configuration = context.Configuration;

		MarkdownParser = new MarkdownParser(SourcePath, context, GetMarkdownFile, context.Configuration, LinkResolver);

		Name = SourcePath.FullName;
		OutputStateFile = OutputPath.FileSystem.FileInfo.New(Path.Combine(OutputPath.FullName, ".doc.state"));
		LinkReferenceFile = OutputPath.FileSystem.FileInfo.New(Path.Combine(OutputPath.FullName, "links.json"));

		Files = [.. context.ReadFileSystem.Directory
			.EnumerateFiles(SourcePath.FullName, "*.*", SearchOption.AllDirectories)
			.Select(f => context.ReadFileSystem.FileInfo.New(f))
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			// skip hidden folders
			.Where(f => !Path.GetRelativePath(SourcePath.FullName, f.FullName).StartsWith('.'))
			.Select<IFileInfo, DocumentationFile>(file => file.Extension switch
			{
				".jpg" => new ImageFile(file, SourcePath, "image/jpeg"),
				".jpeg" => new ImageFile(file, SourcePath, "image/jpeg"),
				".gif" => new ImageFile(file, SourcePath, "image/gif"),
				".svg" => new ImageFile(file, SourcePath, "image/svg+xml"),
				".png" => new ImageFile(file, SourcePath),
				".md" => CreateMarkDownFile(file, context),
				_ => new StaticFile(file, SourcePath)
			})];

		LastWrite = Files.Max(f => f.SourceFile.LastWriteTimeUtc);

		FlatMappedFiles = Files.ToDictionary(file => file.RelativePath, file => file).ToFrozenDictionary();

		var folderFiles = Files
			.GroupBy(file => file.RelativeFolder)
			.ToDictionary(g => g.Key, g => g.ToArray());

		var fileIndex = 0;
		Tree = new DocumentationGroup(Context, Configuration.TableOfContents, FlatMappedFiles, folderFiles, ref fileIndex)
		{
			Parent = null
		};

		var markdownFiles = Files.OfType<MarkdownFile>().ToArray();

		var excludedChildren = markdownFiles.Where(f => f.NavigationIndex == -1).ToArray();
		foreach (var excludedChild in excludedChildren)
			Context.EmitError(Context.ConfigurationPath, $"{excludedChild.RelativePath} is unreachable in the TOC because one of its parents matches exclusion glob");

		MarkdownFiles = markdownFiles.Where(f => f.NavigationIndex > -1).ToDictionary(i => i.NavigationIndex, i => i).ToFrozenDictionary();

	}

	public FrozenDictionary<int, MarkdownFile> MarkdownFiles { get; }

	public MarkdownFile? GetMarkdownFile(IFileInfo sourceFile)
	{
		var relativePath = Path.GetRelativePath(SourcePath.FullName, sourceFile.FullName);
		if (FlatMappedFiles.TryGetValue(relativePath, out var file) && file is MarkdownFile markdownFile)
			return markdownFile;
		return null;
	}

	public MarkdownFile? GetPrevious(MarkdownFile current)
	{
		var index = current.NavigationIndex;
		do
		{
			var previous = MarkdownFiles.GetValueOrDefault(index - 1);
			if (previous is null)
				return null;
			if (!previous.Hidden)
				return previous;
			index--;
		} while (index > 0);

		return null;
	}

	public MarkdownFile? GetNext(MarkdownFile current)
	{
		var index = current.NavigationIndex;
		do
		{
			var previous = MarkdownFiles.GetValueOrDefault(index + 1);
			if (previous is null)
				return null;
			if (!previous.Hidden)
				return previous;
			index++;
		} while (index <= MarkdownFiles.Count - 1);

		return null;
	}


	public async Task ResolveDirectoryTree(Cancel ctx) =>
		await Tree.Resolve(ctx);

	private DocumentationFile CreateMarkDownFile(IFileInfo file, BuildContext context)
	{
		var relativePath = Path.GetRelativePath(SourcePath.FullName, file.FullName);
		if (Configuration.Exclude.Any(g => g.IsMatch(relativePath)))
			return new ExcludedFile(file, SourcePath);

		if (Configuration.Files.Contains(relativePath))
			return new MarkdownFile(file, SourcePath, MarkdownParser, context);

		if (Configuration.Globs.Any(g => g.IsMatch(relativePath)))
			return new MarkdownFile(file, SourcePath, MarkdownParser, context);

		// we ignore files in folders that start with an underscore
		if (relativePath.Contains("_snippets"))
			return new SnippetFile(file, SourcePath);

		// we ignore files in folders that start with an underscore
		if (relativePath.IndexOf("/_", StringComparison.Ordinal) > 0 || relativePath.StartsWith('_'))
			return new ExcludedFile(file, SourcePath);

		context.EmitError(Configuration.SourceFile, $"Not linked in toc: {relativePath}");
		return new ExcludedFile(file, SourcePath);
	}

	public DocumentationGroup Tree { get; }

	public IReadOnlyCollection<DocumentationFile> Files { get; }

	public FrozenDictionary<string, DocumentationFile> FlatMappedFiles { get; }

	public void ClearOutputDirectory()
	{
		if (OutputPath.Exists)
			OutputPath.Delete(true);
		OutputPath.Create();
	}
}
