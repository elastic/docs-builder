// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Site.Navigation;
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
	FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	INavigationItem? GetPrevious(MarkdownFile current)
	{
		if (!MarkdownNavigationLookup.TryGetValue(current.CrossLink, out var currentNavigation))
			return null;
		var index = currentNavigation.NavigationIndex;
		do
		{
			var previous = NavigationIndexedByOrder.GetValueOrDefault(index - 1);
			if (previous is not null && !previous.Hidden)
				return previous;
			index--;
		} while (index > 0);

		return null;
	}

	INavigationItem? GetNext(MarkdownFile current)
	{
		if (!MarkdownNavigationLookup.TryGetValue(current.CrossLink, out var currentNavigation))
			return null;
		var index = currentNavigation.NavigationIndex;
		do
		{
			var next = NavigationIndexedByOrder.GetValueOrDefault(index + 1);
			if (next is not null && !next.Hidden && next.Url != currentNavigation.Url)
				return next;
			index++;
		} while (index <= NavigationIndexedByOrder.Count - 1);

		return null;
	}

	INavigationItem GetCurrent(MarkdownFile file) =>
		MarkdownNavigationLookup.GetValueOrDefault(file.CrossLink) ?? throw new InvalidOperationException($"Could not find {file.CrossLink} in navigation");

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
	INavigationItem[] GetParentsOfMarkdownFile(MarkdownFile file) =>
		MarkdownNavigationLookup.TryGetValue(file.CrossLink, out var navigationItem) ? GetParents(navigationItem) : [];
}

public record NavigationLookups : INavigationLookups
{
	public required FrozenDictionary<string, DocumentationFile> FlatMappedFiles { get; init; }
	public required IReadOnlyCollection<ITocItem> TableOfContents { get; init; }
	public required IReadOnlyCollection<IDocsBuilderExtension> EnabledExtensions { get; init; }
	public required FrozenDictionary<string, DocumentationFile[]> FilesGroupedByFolder { get; init; }
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

	public ConcurrentDictionary<string, NavigationRenderResult> NavigationRenderResults { get; } = [];

	public DocumentationSet(
		BuildContext context,
		ILoggerFactory logFactory,
		ICrossLinkResolver? linkResolver = null,
		TableOfContentsTreeCollector? treeCollector = null
	)
	{
		Context = context;
		Source = ContentSourceMoniker.Create(context.Git.RepositoryName, null);
		SourceDirectory = context.DocumentationSourceDirectory;
		OutputDirectory = context.OutputDirectory;
		LinkResolver =
			linkResolver ?? new CrossLinkResolver(new ConfigurationCrossLinkFetcher(logFactory, context.Configuration, Aws3LinkIndexReader.CreateAnonymous()));
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
			FilesGroupedByFolder = FilesGroupedByFolder
		};

		Tree = new TableOfContentsTree(Source, Context, lookups, treeCollector, ref fileIndex);

		var navigationIndex = 0;
		UpdateNavigationIndex(Tree.NavigationItems, ref navigationIndex);
		var markdownFiles = Files.OfType<MarkdownFile>().ToArray();

		var excludedChildren = markdownFiles.Where(f => !f.PartOfNavigation).ToArray();
		foreach (var excludedChild in excludedChildren)
			Context.EmitError(Context.ConfigurationPath, $"{excludedChild.RelativePath} is unreachable in the TOC because one of its parents matches exclusion glob");

		MarkdownFiles = markdownFiles.Where(f => f.PartOfNavigation).ToFrozenSet();
		NavigationIndexedByOrder = CreateNavigationLookup(Tree)
			.ToDictionary(n => n.NavigationIndex, n => n)
			.ToFrozenDictionary();

		MarkdownNavigationLookup = Tree.NavigationItems
			.SelectMany(Pairs)
			.Concat(Pairs(Tree))
			.DistinctBy(kv => kv.Item1)
			.ToDictionary(kv => kv.Item1, kv => kv.Item2)
			.ToFrozenDictionary();

		ValidateRedirectsExists();
	}

	private void UpdateNavigationIndex(IReadOnlyCollection<INavigationItem> navigationItems, ref int navigationIndex)
	{
		foreach (var item in navigationItems)
		{
			switch (item)
			{
				case FileNavigationItem fileNavigationItem:
					var fileIndex = Interlocked.Increment(ref navigationIndex);
					fileNavigationItem.NavigationIndex = fileIndex;
					break;
				case DocumentationGroup documentationGroup:
					var groupIndex = Interlocked.Increment(ref navigationIndex);
					documentationGroup.NavigationIndex = groupIndex;
					UpdateNavigationIndex(documentationGroup.NavigationItems, ref navigationIndex);
					break;
				default:
					Context.EmitError(Context.ConfigurationPath, $"Unhandled navigation item type: {item.GetType()}");
					break;
			}
		}
	}

	public FrozenDictionary<int, INavigationItem> NavigationIndexedByOrder { get; }

	private static IReadOnlyCollection<INavigationItem> CreateNavigationLookup(INavigationItem item)
	{
		if (item is ILeafNavigationItem<INavigationModel> leaf)
			return [leaf];

		if (item is INodeNavigationItem<INavigationModel, INavigationItem> node)
		{
			var items = node.NavigationItems.SelectMany(CreateNavigationLookup);
			return items.Concat([node]).ToArray();
		}

		return [];
	}

	public static (string, INavigationItem)[] Pairs(INavigationItem item)
	{
		if (item is FileNavigationItem f)
			return [(f.Model.CrossLink, item)];
		if (item is DocumentationGroup g)
		{
			var index = new List<(string, INavigationItem)>
			{
				(g.Index.CrossLink, g)
			};

			return index.Concat(g.NavigationItems.SelectMany(Pairs).ToArray())
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
			if (to.Contains("://"))
			{
				if (!Uri.TryCreate(to, UriKind.Absolute, out _))
					Context.EmitError(Configuration.SourceFile, $"Redirect {from} points to {to} which is not a valid URI");

				return;
			}

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

	public FrozenSet<MarkdownFile> MarkdownFiles { get; }

	public string FirstInterestingUrl =>
		NavigationIndexedByOrder.Values.OfType<DocumentationGroup>().First().Url;

	public DocumentationFile? DocumentationFileLookup(IFileInfo sourceFile)
	{
		var relativePath = Path.GetRelativePath(SourceDirectory.FullName, sourceFile.FullName);
		return FlatMappedFiles.GetValueOrDefault(relativePath);
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
		var markdownInNavigation = NavigationIndexedByOrder.Values
			.OfType<FileNavigationItem>()
			.Select(m => (Markdown: m.Model, Navigation: (INavigationItem)m))
			.Concat(NavigationIndexedByOrder.Values
				.OfType<DocumentationGroup>()
				.Select(g => (Markdown: g.Index, Navigation: (INavigationItem)g))
			)
			.ToList();

		var links = markdownInNavigation
			.Select(tuple =>
			{
				var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
					? tuple.Markdown.LinkReferenceRelativePath.Replace('\\', '/')
					: tuple.Markdown.LinkReferenceRelativePath;
				return (Path: path, tuple.Markdown, tuple.Navigation);
			})
			.DistinctBy(tuple => tuple.Path)
			.ToDictionary(
				tuple => tuple.Path,
				tuple =>
				{
					var anchors = tuple.Markdown.Anchors.Count == 0 ? null : tuple.Markdown.Anchors.ToArray();
					return new LinkMetadata
					{
						Anchors = anchors,
						Hidden = tuple.Navigation.Hidden
					};
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
