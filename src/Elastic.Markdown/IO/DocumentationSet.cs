// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst;

namespace Elastic.Markdown.IO;

public class DocumentationSet
{
	public string Name { get; }
	public IDirectoryInfo SourcePath { get; }
	public IDirectoryInfo OutputPath { get; }
	public DateTimeOffset LastWrite { get; }
	public IFileInfo OutputStateFile { get; }

	public ConfigurationFile Configuration { get; }

	private MarkdownParser MarkdownParser { get; }

	public DocumentationSet(BuildContext context) : this(null, null, context) { }

	public DocumentationSet(IDirectoryInfo? sourcePath, IDirectoryInfo? outputPath, BuildContext context)
	{
		SourcePath = sourcePath ?? context.ReadFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, "docs/source"));
		OutputPath = outputPath ?? context.WriteFileSystem.DirectoryInfo.New(Path.Combine(Paths.Root.FullName, ".artifacts/docs/html"));
		Name = SourcePath.FullName;
		MarkdownParser = new MarkdownParser(SourcePath, context);
		OutputStateFile = OutputPath.FileSystem.FileInfo.New(Path.Combine(OutputPath.FullName, ".doc.state"));

		var configurationFile = context.ReadFileSystem.FileInfo.New(Path.Combine(SourcePath.FullName, "docset.yml"));
		Configuration = new ConfigurationFile(configurationFile, SourcePath, context);

		Files = context.ReadFileSystem.Directory
			.EnumerateFiles(SourcePath.FullName, "*.*", SearchOption.AllDirectories)
			.Select(f => context.ReadFileSystem.FileInfo.New(f))
			.Select<IFileInfo, DocumentationFile>(file => file.Extension switch
			{
				".svg" => new ImageFile(file, SourcePath, "image/svg+xml"),
				".png" => new ImageFile(file, SourcePath),
				".md" => CreateMarkDownFile(file, context),
				_ => new StaticFile(file, SourcePath)
			})

			.ToList();

		LastWrite = Files.Max(f => f.SourceFile.LastWriteTimeUtc);

		FlatMappedFiles = Files.ToDictionary(file => file.RelativePath, file => file);
		var folderFiles = Files
			.GroupBy(file => file.RelativeFolder)
			.ToDictionary(g=>g.Key, g=>g.ToArray());

		Tree = new DocumentationFolder(Configuration.TableOfContents, FlatMappedFiles, folderFiles);
	}

	private DocumentationFile CreateMarkDownFile(IFileInfo file, BuildContext context)
	{
		if (Configuration.Exclude.Any(g => g.IsMatch(file.Name)))
			return new ExcludedFile(file, SourcePath);

		var relativePath = Path.GetRelativePath(SourcePath.FullName, file.FullName);
		if (Configuration.Files.Contains(relativePath))
			return new MarkdownFile(file, SourcePath, MarkdownParser, context);

		if (Configuration.Globs.Any(g => g.IsMatch(relativePath)))
			return new MarkdownFile(file, SourcePath, MarkdownParser, context);

		if (relativePath.IndexOf("/_", StringComparison.Ordinal) > 0 || relativePath.StartsWith("_"))
			return new ExcludedFile(file, SourcePath);

		context.EmitError(Configuration.SourceFile, $"Not linked in toc: {relativePath}");
		return new ExcludedFile(file, SourcePath);
	}

	public DocumentationFolder Tree { get; }

	public List<DocumentationFile> Files { get; }

	public Dictionary<string, DocumentationFile> FlatMappedFiles { get; }

	public void ClearOutputDirectory()
	{
		if (OutputPath.Exists)
			OutputPath.Delete(true);
		OutputPath.Create();
	}
}
