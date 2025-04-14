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
	string Id { get; }
	INavigationItem? Parent { get; set; }
	int Depth { get; }
}

[DebuggerDisplay("Toc >{Depth} {Group.FolderName}")]
public record TocNavigationItem(int Depth, DocumentationGroup Group, Uri Source, INavigationItem? Parent)
	: GroupNavigationItem(Depth, Group, Parent)
{
	/// Only used for tests
	public IReadOnlyDictionary<Uri, TocNavigationItem> NavigationLookup =>
		Group.NavigationItems.OfType<TocNavigationItem>().ToDictionary(i => i.Source, i => i);
}

[DebuggerDisplay("Group >{Depth} {Group.FolderName}")]
public record GroupNavigationItem(int Depth, DocumentationGroup Group, INavigationItem? Parent) : INavigationItem
{
	public string Id { get; } = Group.Id;
	public INavigationItem? Parent { get; set; } = Parent;
}

[DebuggerDisplay("File >{Depth} {File.RelativePath}")]
public record FileNavigationItem(int Depth, MarkdownFile File, INavigationItem? Parent) : INavigationItem
{
	public string Id { get; } = File.Id;
	public INavigationItem? Parent { get; set; } = Parent;
}

public interface INavigationGroup : INavigationItem
{
	IReadOnlyCollection<INavigationItem> NavigationItems { get; }
	string? IndexFileName { get; }
}

public interface INavigationScope
{
	INavigationGroup NavigationRoot { get; }
}

public class TableOfContentsTreeCollector
{
	private Dictionary<Uri, TableOfContentsTree> NestedTableOfContentsTrees { get; } = [];

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
		: base(treeCollector, context, lookups, source, ref fileIndex)
	{
		TreeCollector = treeCollector;
		NavigationRoot = this;

		Source = source;
		TreeCollector.Collect(source, this);
		DocumentationSet = documentationSet;

		//edge case if tree only holds a single group ensure we collapse it down to the root (this)
		if (NavigationItems.Count == 1 && NavigationItems.First() is GroupNavigationItem { Group.NavigationItems.Count: 0 })
			NavigationItems = [];

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
	) : base(folderName, treeCollector, context, lookups, source, ref fileIndex, depth, toplevelTree, parent, index)
	{
		Source = source;
		TreeCollector = treeCollector;
		NavigationRoot = this;
		TreeCollector.Collect(source, this);
	}

}

[DebuggerDisplay("Group >{Depth} {FolderName} ({NavigationItems.Count} items)")]
public class DocumentationGroup : INavigationGroup
{
	private readonly TableOfContentsTreeCollector _treeCollector;

	public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

	public string NavigationRootId => NavigationRoot.Id;

	public INavigationGroup NavigationRoot { get; set; }

	public Uri NavigationSource { get; set; }

	public MarkdownFile? Index { get; set; }

	private IReadOnlyCollection<MarkdownFile> FilesInOrder { get; }

	private IReadOnlyCollection<DocumentationGroup> GroupsInOrder { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; }

	public string? IndexFileName => Index?.FileName;

	public int Depth { get; set; }

	public INavigationItem? Parent { get; set; }

	public string FolderName { get; }

	protected DocumentationGroup(
		TableOfContentsTreeCollector treeCollector,
		BuildContext context,
		NavigationLookups lookups,
		Uri navigationSource,
		ref int fileIndex
	)
		: this(".", treeCollector, context, lookups, navigationSource, ref fileIndex, depth: 0, null, null)
	{
		NavigationSource = navigationSource;
		_treeCollector = treeCollector;
	}

	protected DocumentationGroup(
		string folderName,
		TableOfContentsTreeCollector treeCollector,
		BuildContext context,
		NavigationLookups lookups,
		Uri navigationSource,
		ref int fileIndex,
		int depth,
		DocumentationGroup? toplevelTree,
		INavigationItem? parent,
		MarkdownFile? index = null
	)
	{
		Parent = parent;
		FolderName = folderName;
		NavigationSource = navigationSource;
		_treeCollector = treeCollector;
		Depth = depth;
		toplevelTree ??= this;
		NavigationRoot = toplevelTree;
		// ReSharper restore VirtualMemberCallInConstructor
		Index = ProcessTocItems(context, toplevelTree, index, lookups, depth, ref fileIndex, out var groups, out var files, out var navigationItems);

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


				md.Hidden = file.Hidden;
				var navigationIndex = Interlocked.Increment(ref fileIndex);
				md.NavigationIndex = navigationIndex;
				md.ScopeDirectory = file.TableOfContentsScope.ScopeDirectory;
				md.NavigationRoot = topLevelGroup;
				md.NavigationSource = NavigationSource;

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
						}, NavigationSource, ref fileIndex, depth + 1, topLevelGroup, this, virtualIndex);
					groups.Add(group);
					navigationItems.Add(new GroupNavigationItem(depth, group, this));
					indexFile ??= virtualIndex;
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
					navigationItems.Add(new FileNavigationItem(depth, md, this));
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

					group = toc;
					navigationItems.Add(new TocNavigationItem(depth, toc, tocReference.Source, this));
				}
				else
				{
					group = new DocumentationGroup(folder.Path, _treeCollector, context, lookups with
					{
						TableOfContents = children
					}, NavigationSource, ref fileIndex, depth + 1, topLevelGroup, this);
					navigationItems.Add(new GroupNavigationItem(depth, group, this));
				}

				groups.Add(group);
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

		if (indexFile is not null)
			indexFile.IsIndex = true;

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
}
