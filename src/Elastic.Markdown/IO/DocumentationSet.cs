// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO.Configuration;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Myst;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.IO;

public class DocumentationSet
{
	public BuildContext Build { get; }
	public string Name { get; }
	public IFileInfo OutputStateFile { get; }
	public IFileInfo LinkReferenceFile { get; }

	public IDirectoryInfo SourceDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	public string RelativeSourcePath { get; }

	public DateTimeOffset LastWrite { get; }

	public ConfigurationFile Configuration { get; }

	private MarkdownParser MarkdownParser { get; }

	public ICrossLinkResolver LinkResolver { get; }

	public DocumentationSet(BuildContext build, ILoggerFactory logger, ICrossLinkResolver? linkResolver = null)
	{
		Build = build;
		SourceDirectory = build.DocumentationSourceDirectory;
		OutputDirectory = build.DocumentationOutputDirectory;
		RelativeSourcePath = Path.GetRelativePath(Paths.Root.FullName, SourceDirectory.FullName);
		LinkResolver =
			linkResolver ?? new CrossLinkResolver(new ConfigurationCrossLinkFetcher(build.Configuration, logger));
		Configuration = build.Configuration;

		var resolver = new ParserResolvers
		{
			CrossLinkResolver = LinkResolver,
			DocumentationFileLookup = DocumentationFileLookup
		};
		MarkdownParser = new MarkdownParser(build, resolver);

		Name = SourceDirectory.FullName;
		OutputStateFile = OutputDirectory.FileSystem.FileInfo.New(Path.Combine(OutputDirectory.FullName, ".doc.state"));
		LinkReferenceFile = OutputDirectory.FileSystem.FileInfo.New(Path.Combine(OutputDirectory.FullName, "links.json"));

		Files = [.. build.ReadFileSystem.Directory
			.EnumerateFiles(SourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
			.Select(f => build.ReadFileSystem.FileInfo.New(f))
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			// skip hidden folders
			.Where(f => !Path.GetRelativePath(SourceDirectory.FullName, f.FullName).StartsWith('.'))
			.Select<IFileInfo, DocumentationFile>(file => file.Extension switch
			{
				".jpg" => new ImageFile(file, SourceDirectory, "image/jpeg"),
				".jpeg" => new ImageFile(file, SourceDirectory, "image/jpeg"),
				".gif" => new ImageFile(file, SourceDirectory, "image/gif"),
				".svg" => new ImageFile(file, SourceDirectory, "image/svg+xml"),
				".png" => new ImageFile(file, SourceDirectory),
				".md" => CreateMarkDownFile(file, build),
				_ => new StaticFile(file, SourceDirectory)
			})];

		LastWrite = Files.Max(f => f.SourceFile.LastWriteTimeUtc);

		FlatMappedFiles = Files.ToDictionary(file => file.RelativePath, file => file).ToFrozenDictionary();

		var folderFiles = Files
			.GroupBy(file => file.RelativeFolder)
			.ToDictionary(g => g.Key, g => g.ToArray());

		var fileIndex = 0;
		Tree = new DocumentationGroup(Build, Configuration.TableOfContents, FlatMappedFiles, folderFiles, ref fileIndex)
		{
			Parent = null
		};

		var markdownFiles = Files.OfType<MarkdownFile>().ToArray();

		var excludedChildren = markdownFiles.Where(f => f.NavigationIndex == -1).ToArray();
		foreach (var excludedChild in excludedChildren)
			Build.EmitError(Build.ConfigurationPath, $"{excludedChild.RelativePath} is unreachable in the TOC because one of its parents matches exclusion glob");

		MarkdownFiles = markdownFiles.Where(f => f.NavigationIndex > -1).ToDictionary(i => i.NavigationIndex, i => i).ToFrozenDictionary();
		ValidateRedirectsExists();
	}

	private void ValidateRedirectsExists()
	{
		if (Configuration.Redirects is null || Configuration.Redirects.Count == 0)
			return;
		foreach (var redirect in Configuration.Redirects)
		{
			if (redirect.Value.To is not null)
				ValidateExists(redirect.Key, redirect.Value.To, redirect.Value.Anchors);
			else if (redirect.Value.Many is not null)
			{
				foreach (var r in redirect.Value.Many)
				{
					if (r.To is not null)
						ValidateExists(redirect.Key, r.To, r.Anchors);
				}
			}
		}

		void ValidateExists(string from, string to, IReadOnlyDictionary<string, string?>? valueAnchors)
		{
			if (!FlatMappedFiles.TryGetValue(to, out var file))
			{
				Build.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which does not exist");
				return;

			}

			if (file is not MarkdownFile markdownFile)
			{
				Build.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which is not a markdown file");
				return;
			}

			if (valueAnchors is null or { Count: 0 })
				return;

			markdownFile.AnchorRemapping =
				markdownFile.AnchorRemapping?
					.Concat(valueAnchors)
					.DistinctBy(kv => kv.Key)
					.ToDictionary(kv => kv.Key, kv => kv.Value) ?? valueAnchors;
		}
	}

	public FrozenDictionary<int, MarkdownFile> MarkdownFiles { get; }

	public MarkdownFile? DocumentationFileLookup(IFileInfo sourceFile)
	{
		var relativePath = Path.GetRelativePath(SourceDirectory.FullName, sourceFile.FullName);
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
		var relativePath = Path.GetRelativePath(SourceDirectory.FullName, file.FullName);
		if (Configuration.Exclude.Any(g => g.IsMatch(relativePath)))
			return new ExcludedFile(file, SourceDirectory);

		// we ignore files in folders that start with an underscore
		if (relativePath.Contains("_snippets"))
			return new SnippetFile(file, SourceDirectory);

		if (Configuration.Files.Contains(relativePath))
			return new MarkdownFile(file, SourceDirectory, MarkdownParser, context, this);

		if (Configuration.Globs.Any(g => g.IsMatch(relativePath)))
			return new MarkdownFile(file, SourceDirectory, MarkdownParser, context, this);

		// we ignore files in folders that start with an underscore
		if (relativePath.IndexOf("/_", StringComparison.Ordinal) > 0 || relativePath.StartsWith('_'))
			return new ExcludedFile(file, SourceDirectory);

		context.EmitError(Configuration.SourceFile, $"Not linked in toc: {relativePath}");
		return new ExcludedFile(file, SourceDirectory);
	}

	public DocumentationGroup Tree { get; }

	public IReadOnlyCollection<DocumentationFile> Files { get; }

	public FrozenDictionary<string, DocumentationFile> FlatMappedFiles { get; }

	public void ClearOutputDirectory()
	{
		if (OutputDirectory.Exists)
			OutputDirectory.Delete(true);
		OutputDirectory.Create();
	}
}
