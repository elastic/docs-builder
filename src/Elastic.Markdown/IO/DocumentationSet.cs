// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO.Navigation;
using Elastic.Markdown.Links.CrossLinks;
using Elastic.Markdown.Myst;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.IO;

public interface INavigationLookups
{
	FrozenDictionary<string, DocumentationFile> FlatMappedFiles { get; }
	IReadOnlyCollection<ITocItem> TableOfContents { get; }
	IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; }
	FrozenDictionary<string, DocumentationFile[]> FilesGroupedByFolder { get; }
}

public interface IPositionalNavigation
{
	FrozenDictionary<string, INavigationItem> MarkdownNavigationLookup { get; }

	MarkdownFile? GetPrevious(MarkdownFile current);
	MarkdownFile? GetNext(MarkdownFile current);

	INavigationItem[] GetParents(INavigationItem current)
	{
		var parents = new List<INavigationItem>();
		var parent = current.Parent;
		do
		{
			if (parent is null)
				continue;
			parents.Add(parent);
			parent = parent.Parent;
		} while (parent != null);

		return [.. parents];
	}
	MarkdownFile[] GetParentMarkdownFiles(INavigationItem current)
	{
		var parents = new List<MarkdownFile>();
		var navigationParents = GetParents(current);
		foreach (var parent in navigationParents)
		{
			if (parent is FileNavigationItem f)
				parents.Add(f.File);
			if (parent is GroupNavigationItem { Group.Index: not null } g)
				parents.Add(g.Group.Index);
			if (parent is DocumentationGroup { Index: not null } dg)
				parents.Add(dg.Index);
		}
		return [.. parents];
	}
	MarkdownFile[] GetParentMarkdownFiles(MarkdownFile file)
	{
		if (MarkdownNavigationLookup.TryGetValue(file.CrossLink, out var navigationItem))
			return GetParentMarkdownFiles(navigationItem);
		return [];
	}
}

public record NavigationLookups : INavigationLookups
{
	public required FrozenDictionary<string, DocumentationFile> FlatMappedFiles { get; init; }
	public required IReadOnlyCollection<ITocItem> TableOfContents { get; init; }
	public required IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; init; }
	public required FrozenDictionary<string, DocumentationFile[]> FilesGroupedByFolder { get; init; }
	//public required FrozenDictionary<Uri, TableOfContentsReference> IndexedTableOfContents { get; init; }
}

public class DocumentationSet : INavigationLookups, IPositionalNavigation
{
	public BuildContext Context { get; }
	public string Name { get; }
	public IFileInfo OutputStateFile { get; }
	public IFileInfo LinkReferenceFile { get; }

	public IDirectoryInfo SourceDirectory { get; }
	public IDirectoryInfo OutputDirectory { get; }

	public DateTimeOffset LastWrite { get; }

	public ConfigurationFile Configuration { get; }

	public MarkdownParser MarkdownParser { get; }

	public ICrossLinkResolver LinkResolver { get; }

	public TableOfContentsTree Tree { get; }

	public Uri Source { get; }

	public IReadOnlyCollection<DocumentationFile> Files { get; }

	public FrozenDictionary<string, DocumentationFile[]> FilesGroupedByFolder { get; }

	public FrozenDictionary<string, DocumentationFile> FlatMappedFiles { get; }

	IReadOnlyCollection<ITocItem> INavigationLookups.TableOfContents => Configuration.TableOfContents;

	public FrozenDictionary<string, INavigationItem> MarkdownNavigationLookup { get; }

	public IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; }

	public DocumentationSet(
		BuildContext context,
		ILoggerFactory logger,
		ICrossLinkResolver? linkResolver = null,
		TableOfContentsTreeCollector? treeCollector = null
	)
	{
		Context = context;
		Source = ContentSourceMoniker.Create(context.Git.RepositoryName, null);
		SourceDirectory = context.DocumentationSourceDirectory;
		OutputDirectory = context.DocumentationOutputDirectory;
		LinkResolver =
			linkResolver ?? new CrossLinkResolver(new ConfigurationCrossLinkFetcher(context.Configuration, Aws3LinkIndexReader.CreateAnonymous(), logger));
		Configuration = context.Configuration;
		EnabledExtensions = InstantiateExtensions();
		treeCollector ??= new TableOfContentsTreeCollector();

		var resolver = new ParserResolvers
		{
			CrossLinkResolver = LinkResolver,
			DocumentationFileLookup = DocumentationFileLookup
		};
		MarkdownParser = new MarkdownParser(context, resolver);

		Name = Context.Git != GitCheckoutInformation.Unavailable
			? Context.Git.RepositoryName
			: Context.DocumentationCheckoutDirectory?.Name ?? $"unknown-{Context.DocumentationSourceDirectory.Name}";
		OutputStateFile = OutputDirectory.FileSystem.FileInfo.New(Path.Combine(OutputDirectory.FullName, ".doc.state"));
		LinkReferenceFile = OutputDirectory.FileSystem.FileInfo.New(Path.Combine(OutputDirectory.FullName, "links.json"));

		var files = ScanDocumentationFiles(context, SourceDirectory);
		var additionalSources = EnabledExtensions
			.SelectMany(extension => extension.ScanDocumentationFiles(DefaultFileHandling))
			.ToArray();

		Files = files.Concat(additionalSources).Where(f => f is not ExcludedFile).ToArray();

		LastWrite = Files.Max(f => f.SourceFile.LastWriteTimeUtc);

		FlatMappedFiles = Files.ToDictionary(file => file.RelativePath, file => file).ToFrozenDictionary();

		FilesGroupedByFolder = Files
			.GroupBy(file => file.RelativeFolder)
			.ToDictionary(g => g.Key, g => g.ToArray())
			.ToFrozenDictionary();

		var fileIndex = 0;
		var lookups = new NavigationLookups
		{
			FlatMappedFiles = FlatMappedFiles,
			TableOfContents = Configuration.TableOfContents,
			EnabledExtensions = EnabledExtensions,
			FilesGroupedByFolder = FilesGroupedByFolder,
			//IndexedTableOfContents = indexedTableOfContents ?? new Dictionary<Uri, TableOfContentsReference>().ToFrozenDictionary()
		};

		Tree = new TableOfContentsTree(this, Source, Context, lookups, treeCollector, ref fileIndex);

		var markdownFiles = Files.OfType<MarkdownFile>().ToArray();

		var excludedChildren = markdownFiles.Where(f => f.NavigationIndex == -1).ToArray();
		foreach (var excludedChild in excludedChildren)
			Context.EmitError(Context.ConfigurationPath, $"{excludedChild.RelativePath} is unreachable in the TOC because one of its parents matches exclusion glob");

		MarkdownFiles = markdownFiles.Where(f => f.NavigationIndex > -1).ToDictionary(i => i.NavigationIndex, i => i).ToFrozenDictionary();

		MarkdownNavigationLookup = Tree.NavigationItems
			.SelectMany(Pairs)
			.ToDictionary(kv => kv.Item1, kv => kv.Item2)
			.ToFrozenDictionary();

		ValidateRedirectsExists();
	}

	public static (string, INavigationItem)[] Pairs(INavigationItem item)
	{
		if (item is FileNavigationItem f)
			return [(f.File.CrossLink, item)];
		if (item is GroupNavigationItem g)
		{
			var index = new List<(string, INavigationItem)>();
			if (g.Group.Index is not null)
				index.Add((g.Group.Index.CrossLink, g));

			return index.Concat(g.Group.NavigationItems.SelectMany(Pairs).ToArray())
				.DistinctBy(kv => kv.Item1)
				.ToArray();
		}

		return [];
	}

	private DocumentationFile[] ScanDocumentationFiles(BuildContext build, IDirectoryInfo sourceDirectory) =>
		[.. build.ReadFileSystem.Directory
			.EnumerateFiles(sourceDirectory.FullName, "*.*", SearchOption.AllDirectories)
			.Select(f => build.ReadFileSystem.FileInfo.New(f))
			.Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System))
			.Where(f => !f.Directory!.Attributes.HasFlag(FileAttributes.Hidden) && !f.Directory!.Attributes.HasFlag(FileAttributes.System))
			// skip hidden folders
			.Where(f => !Path.GetRelativePath(sourceDirectory.FullName, f.FullName).StartsWith('.'))
			.Select<IFileInfo, DocumentationFile>(file => file.Extension switch
			{
				".jpg" => new ImageFile(file, SourceDirectory, build.Git.RepositoryName, "image/jpeg"),
				".jpeg" => new ImageFile(file, SourceDirectory, build.Git.RepositoryName, "image/jpeg"),
				".gif" => new ImageFile(file, SourceDirectory, build.Git.RepositoryName, "image/gif"),
				".svg" => new ImageFile(file, SourceDirectory, build.Git.RepositoryName, "image/svg+xml"),
				".png" => new ImageFile(file, SourceDirectory, build.Git.RepositoryName),
				".md" => CreateMarkDownFile(file, build),
				_ => DefaultFileHandling(file, sourceDirectory)
		})];

	private DocumentationFile DefaultFileHandling(IFileInfo file, IDirectoryInfo sourceDirectory)
	{
		foreach (var extension in EnabledExtensions)
		{
			var documentationFile = extension.CreateDocumentationFile(file, this);
			if (documentationFile is not null)
				return documentationFile;
		}
		return new ExcludedFile(file, sourceDirectory, Context.Git.RepositoryName);
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
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				to = to.Replace('/', Path.DirectorySeparatorChar);

			if (!FlatMappedFiles.TryGetValue(to, out var file))
			{
				Context.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which does not exist");
				return;

			}

			if (file is not MarkdownFile markdownFile)
			{
				Context.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which is not a markdown file");
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

	public DocumentationFile? DocumentationFileLookup(IFileInfo sourceFile)
	{
		var relativePath = Path.GetRelativePath(SourceDirectory.FullName, sourceFile.FullName);
		return FlatMappedFiles.GetValueOrDefault(relativePath);
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
			return new ExcludedFile(file, SourceDirectory, context.Git.RepositoryName);

		if (relativePath.Contains("_snippets"))
			return new SnippetFile(file, SourceDirectory, context.Git.RepositoryName);

		// we ignore files in folders that start with an underscore
		var folder = Path.GetDirectoryName(relativePath);
		if (folder is not null && (folder.Contains($"{Path.DirectorySeparatorChar}_", StringComparison.Ordinal) || folder.StartsWith('_')))
			return new ExcludedFile(file, SourceDirectory, context.Git.RepositoryName);

		if (Configuration.Files.Contains(relativePath))
			return ExtensionOrDefaultMarkdown();

		if (Configuration.Globs.Any(g => g.IsMatch(relativePath)))
			return ExtensionOrDefaultMarkdown();

		context.EmitError(Configuration.SourceFile, $"Not linked in toc: {relativePath}");
		return new ExcludedFile(file, SourceDirectory, context.Git.RepositoryName);

		MarkdownFile ExtensionOrDefaultMarkdown()
		{
			foreach (var extension in EnabledExtensions)
			{
				var documentationFile = extension.CreateMarkdownFile(file, SourceDirectory, this);
				if (documentationFile is not null)
					return documentationFile;
			}
			return new MarkdownFile(file, SourceDirectory, MarkdownParser, context, this);
		}
	}

	public RepositoryLinks CreateLinkReference()
	{
		var redirects = Configuration.Redirects;
		var crossLinks = Context.Collector.CrossLinks.ToHashSet().ToArray();
		var links = MarkdownFiles.Values
			.Select(m => (m.LinkReferenceRelativePath, File: m))
			.ToDictionary(k => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? k.LinkReferenceRelativePath.Replace('\\', '/')
			: k.LinkReferenceRelativePath, v =>
			{
				var anchors = v.File.Anchors.Count == 0 ? null : v.File.Anchors.ToArray();
				return new LinkMetadata { Anchors = anchors, Hidden = v.File.Hidden };
			});

		return new RepositoryLinks
		{
			Redirects = redirects,
			UrlPathPrefix = Context.UrlPathPrefix,
			Origin = Context.Git,
			Links = links,
			CrossLinks = crossLinks
		};
	}

	public void ClearOutputDirectory()
	{
		if (OutputDirectory.Exists)
			OutputDirectory.Delete(true);
		OutputDirectory.Create();
	}

	private IReadOnlyCollection<IDocsBuilderExtension> InstantiateExtensions()
	{
		var list = new List<IDocsBuilderExtension>();
		foreach (var extension in Configuration.Extensions.Enabled)
		{
			switch (extension.ToLowerInvariant())
			{
				case "detection-rules":
					list.Add(new DetectionRulesDocsBuilderExtension(Context));
					continue;
			}
		}

		return list.AsReadOnly();
	}
}
