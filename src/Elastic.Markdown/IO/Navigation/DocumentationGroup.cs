// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO.Configuration;

namespace Elastic.Markdown.IO.Navigation;

public interface INavigationItem
{
	int Order { get; }
	int Depth { get; }
	string Id { get; }
}

[DebuggerDisplay("Group >{Depth} #{Order} {Group.FolderName}")]
public record TocNavigationItem(int Order, int Depth, DocumentationGroup Group, Uri Source)
	: GroupNavigationItem(Order, Depth, Group)
{
	public IReadOnlyDictionary<Uri, TocNavigationItem> NavigationLookup { get; } =
		Group.NavigationItems.OfType<TocNavigationItem>().ToDictionary(i => i.Source, i => i);
}

[DebuggerDisplay("Group >{Depth} #{Order} {Group.FolderName}")]
public record GroupNavigationItem(int Order, int Depth, DocumentationGroup Group) : INavigationItem
{
	public string Id { get; } = Group.Id;
}

[DebuggerDisplay("File >{Depth} #{Order} {File.RelativePath}")]
public record FileNavigationItem(int Order, int Depth, MarkdownFile File) : INavigationItem
{
	public string Id { get; } = File.Id;
}

public interface INavigation
{
	string Id { get; }
	IReadOnlyCollection<INavigationItem> NavigationItems { get; }
	int Depth { get; }
	string? IndexFileName { get; }
}

public interface INavigationScope
{
	INavigation NavigationRoot { get; }
}

public class TableOfContentsTreeCollector
{
	private Dictionary<Uri, TableOfContentsTree> NestedTableOfContentsTrees { get; } = [];

	public IReadOnlyDictionary<Uri, TableOfContentsTree> Trees => NestedTableOfContentsTrees;

	public void Collect(Uri source, TableOfContentsTree tree) =>
		NestedTableOfContentsTrees[source] = tree;

	public void Collect(TocReference tocReference, TableOfContentsTree tree) =>
		NestedTableOfContentsTrees[tocReference.Source] = tree;

	public bool TryGetTableOfContentsTree(Uri source, [NotNullWhen(true)] out TableOfContentsTree? tree) =>
		NestedTableOfContentsTrees.TryGetValue(source, out tree);
}

[DebuggerDisplay("Toc >{Depth} {FolderName} ({NavigationItems.Count} items)")]
public class TableOfContentsTree : DocumentationGroup
{
	public Uri Source { get; }

	public TableOfContentsTreeCollector TreeCollector { get; }

	public DocumentationSet? DocumentationSet { get; }

	//TODO remove documentation set argument once navigation.yml fully bootstraps.
	//See GlobalNavigation.BuildNavigation which has fallback logic that needs to be removed
	public TableOfContentsTree(
		DocumentationSet documentationSet,
		Uri source,
		BuildContext context,
		NavigationLookups lookups,
		TableOfContentsTreeCollector treeCollector,
		ref int fileIndex)
		: base(treeCollector, context, lookups, ref fileIndex)
	{
		TreeCollector = treeCollector;
		NavigationRoot = this;

		Source = source;
		TreeCollector.Collect(source, this);
		DocumentationSet = documentationSet;
	}

	internal TableOfContentsTree(
		Uri source,
		string folderName,
		TableOfContentsTreeCollector treeCollector,
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex,
		int depth,
		DocumentationGroup? toplevelTree,
		DocumentationGroup? parent,
		MarkdownFile? index = null
	) : base(folderName, treeCollector, context, lookups, ref fileIndex, depth, toplevelTree, parent, index)
	{
		Source = source;
		TreeCollector = treeCollector;
		NavigationRoot = this;
		TreeCollector.Collect(source, this);
	}

	protected override DocumentationGroup DefaultNavigation => this;
}

[DebuggerDisplay("Group >{Depth} {FolderName} ({NavigationItems.Count} items)")]
public class DocumentationGroup : INavigation
{
	private readonly TableOfContentsTreeCollector _treeCollector;

	public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

	public string NavigationRootId => NavigationRoot.Id;

	public INavigation NavigationRoot { get; set; }

	public MarkdownFile? Index { get; set; }

	private IReadOnlyCollection<MarkdownFile> FilesInOrder { get; }

	private IReadOnlyCollection<DocumentationGroup> GroupsInOrder { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; }

	public string? IndexFileName => Index?.FileName;

	public int Depth { get; set; }

	public DocumentationGroup? Parent { get; }

	public string FolderName { get; }

	protected virtual DocumentationGroup DefaultNavigation => this;

	public DocumentationGroup(
		TableOfContentsTreeCollector treeCollector,
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex
	)
		: this(".", treeCollector, context, lookups, ref fileIndex, depth: 0, null, null) =>
		_treeCollector = treeCollector;

	internal DocumentationGroup(
		string folderName,
		TableOfContentsTreeCollector treeCollector,
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex,
		int depth,
		DocumentationGroup? toplevelTree,
		DocumentationGroup? parent,
		MarkdownFile? index = null
	)
	{
		FolderName = folderName;
		_treeCollector = treeCollector;
		Depth = depth;
		Parent = parent;
		// Virtual call on purpose, implementations use no state
		// ReSharper disable VirtualMemberCallInConstructor
		toplevelTree ??= DefaultNavigation;
		if (parent?.Depth == 0)
			toplevelTree = DefaultNavigation;
		NavigationRoot = toplevelTree;
		// ReSharper restore VirtualMemberCallInConstructor
		Index = ProcessTocItems(context, toplevelTree, index, lookups, depth, ref fileIndex, out var groups, out var files, out var navigationItems);
		if (Index is not null)
			Index.GroupId = Id;

		GroupsInOrder = groups;
		FilesInOrder = files;
		NavigationItems = navigationItems;

		if (Index is not null)
			FilesInOrder = [.. FilesInOrder.Except([Index])];
	}

	private MarkdownFile? ProcessTocItems(
		BuildContext context,
		DocumentationGroup topLevelGroup,
		MarkdownFile? configuredIndex,
		NavigationLookups lookups,
		int depth,
		ref int fileIndex,
		out List<DocumentationGroup> groups,
		out List<MarkdownFile> files,
		out List<INavigationItem> navigationItems)
	{
		groups = [];
		navigationItems = [];
		files = [];
		var indexFile = configuredIndex;
		foreach (var (tocItem, index) in lookups.TableOfContents.Select((t, i) => (t, i)))
		{
			if (tocItem is FileReference file)
			{
				if (!lookups.FlatMappedFiles.TryGetValue(file.Path, out var d))
				{
					context.EmitError(context.ConfigurationPath,
						$"The following file could not be located: {file.Path} it may be excluded from the build in docset.yml");
					continue;
				}

				if (d is ExcludedFile excluded && excluded.RelativePath.EndsWith(".md"))
				{
					context.EmitError(context.ConfigurationPath, $"{excluded.RelativePath} matches exclusion glob from docset.yml yet appears in TOC");
					continue;
				}

				if (d is not MarkdownFile md)
					continue;


				md.Parent = this;
				md.Hidden = file.Hidden;
				var navigationIndex = Interlocked.Increment(ref fileIndex);
				md.NavigationIndex = navigationIndex;
				md.ScopeDirectory = file.TableOfContentsScope.ScopeDirectory;
				md.NavigationRoot = topLevelGroup;

				foreach (var extension in context.Configuration.EnabledExtensions)
					extension.Visit(d, tocItem);

				if (file.Children.Count > 0 && d is MarkdownFile virtualIndex)
				{
					if (file.Hidden)
						context.EmitError(context.ConfigurationPath, $"The following file is hidden but has children: {file.Path}");
					var group = new DocumentationGroup(virtualIndex.RelativePath,
						_treeCollector, context, lookups with
						{
							TableOfContents = file.Children
						}, ref fileIndex, depth + 1, topLevelGroup, this, virtualIndex);
					groups.Add(group);
					navigationItems.Add(new GroupNavigationItem(index, depth, group));
					continue;
				}

				files.Add(md);
				if (file.Path.EndsWith("index.md") && d is MarkdownFile i)
					indexFile ??= i;

				// add the page to navigation items unless it's the index file
				// the index file can either be the discovered `index.md` or the parent group's
				// explicit index page. E.g. when grouping related files together.
				// if the page is referenced as hidden in the TOC do not include it in the navigation
				if (indexFile != md && !md.Hidden)
					navigationItems.Add(new FileNavigationItem(index, depth, md));
			}
			else if (tocItem is FolderReference folder)
			{
				var children = folder.Children;
				if (children.Count == 0 && lookups.FilesGroupedByFolder.TryGetValue(folder.Path, out var documentationFiles))
				{
					children =
					[
						.. documentationFiles
							.Select(d => new FileReference(folder.TableOfContentsScope, d.RelativePath, true, false, []))
					];
				}
				DocumentationGroup group;
				if (folder is TocReference tocReference)
				{
					var toc = new TableOfContentsTree(tocReference.Source, folder.Path, _treeCollector, context, lookups with
					{
						TableOfContents = children
					}, ref fileIndex, depth + 1, topLevelGroup, this);

					//if (lookups.IndexedTableOfContents.TryGetValue(tocReference.Source, out var indexedToc))
					//	toc.NavigationItems = toc.NavigationItems.Concat(indexedToc.Children).ToArray();
					group = toc;
				}
				else
				{
					group = new DocumentationGroup(folder.Path, _treeCollector, context, lookups with
					{
						TableOfContents = children
					}, ref fileIndex, depth + 1, topLevelGroup, this);
				}

				groups.Add(group);
				navigationItems.Add(new GroupNavigationItem(index, depth, group));
			}
			else
			{
				foreach (var extension in lookups.EnabledExtensions)
				{
					if (extension.InjectsIntoNavigation(tocItem))
						extension.CreateNavigationItem(this, tocItem, lookups, groups, navigationItems, depth, ref fileIndex, index);
				}
			}
		}

		return indexFile ?? files.FirstOrDefault();
	}

	private bool _resolved;

	public async Task Resolve(Cancel ctx = default)
	{
		if (_resolved)
			return;

		await Parallel.ForEachAsync(FilesInOrder, ctx, async (file, token) => await file.MinimalParseAsync(token));
		await Parallel.ForEachAsync(GroupsInOrder, ctx, async (group, token) => await group.Resolve(token));

		await (Index?.MinimalParseAsync(ctx) ?? Task.CompletedTask);

		_resolved = true;
	}

	private DocumentationGroup(IReadOnlyCollection<INavigationItem> tocChildren,
		DocumentationGroup cloneFrom,
		TableOfContentsTreeCollector treeCollector, INavigation navigationRoot)
	{
		_treeCollector = treeCollector;
		NavigationItems = cloneFrom.NavigationItems.Concat(tocChildren).ToArray();
		FolderName = cloneFrom.FolderName;
		foreach (var item in NavigationItems)
		{
			if (item is GroupNavigationItem g)
				g.Group.NavigationRoot = navigationRoot;
			else if (item is FileNavigationItem f)
				f.File.NavigationRoot = navigationRoot;
			else
				throw new Exception($"Unknown navigation item type {item.GetType()}");
		}

		FilesInOrder = NavigationItems
			.OfType<FileNavigationItem>()
			.OrderBy(i => i.Order)
			.Select(i => i.File)
			.ToArray();
		GroupsInOrder = NavigationItems
			.OfType<GroupNavigationItem>()
			.OrderBy(i => i.Order)
			.Select(i => i.Group)
			.ToArray();
		Index = cloneFrom.Index;
		Depth = cloneFrom.Depth;
		Id = cloneFrom.Id;
		NavigationRoot = navigationRoot;
	}

	public DocumentationGroup CloneWithExtraChildren(IReadOnlyCollection<INavigationItem> tocChildren, INavigation navigationRoot) =>
		new(tocChildren, this, _treeCollector, navigationRoot);
}
