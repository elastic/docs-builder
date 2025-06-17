// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.TableOfContents;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Site.Navigation;

namespace Elastic.Markdown.IO.Navigation;

[DebuggerDisplay("Current: {Model.RelativePath}")]
public record FileNavigationItem(MarkdownFile Model, DocumentationGroup Group, bool Hidden = false) : ILeafNavigationItem<MarkdownFile>
{
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = Group;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = Group.NavigationRoot;
	public string Url => Model.Url;
	public string NavigationTitle => Model.NavigationTitle;
	public int NavigationIndex { get; set; }
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
public class TableOfContentsTree : DocumentationGroup, IRootNavigationItem<MarkdownFile, INavigationItem>
{
	public Uri Source { get; }

	public TableOfContentsTreeCollector TreeCollector { get; }

	public TableOfContentsTree(
		Uri source,
		BuildContext context,
		NavigationLookups lookups,
		TableOfContentsTreeCollector treeCollector,
		ref int fileIndex)
		: base(".", treeCollector, context, lookups, source, ref fileIndex, 0, null, null)
	{
		TreeCollector = treeCollector;
		NavigationRoot = this;

		Source = source;
		TreeCollector.Collect(source, this);

		//edge case if a tree only holds a single group, ensure we collapse it down to the root (this)
		if (NavigationItems.Count == 1 && NavigationItems.First() is DocumentationGroup { NavigationItems.Count: 0 })
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
		IRootNavigationItem<MarkdownFile, INavigationItem> toplevelTree,
		DocumentationGroup? parent
	) : base(folderName, treeCollector, context, lookups, source, ref fileIndex, depth, toplevelTree, parent)
	{
		Source = source;
		TreeCollector = treeCollector;
		NavigationRoot = this;
		TreeCollector.Collect(source, this);
	}

	protected override IRootNavigationItem<MarkdownFile, INavigationItem> DefaultNavigation => this;

	// We rely on IsPrimaryNavEnabled to determine if we should show the dropdown
	/// <inheritdoc />
	public bool IsUsingNavigationDropdown => false;
}

[DebuggerDisplay("Group >{Depth} {FolderName} ({NavigationItems.Count} items)")]
public class DocumentationGroup : INodeNavigationItem<MarkdownFile, INavigationItem>
{
	private readonly TableOfContentsTreeCollector _treeCollector;

	public string Id { get; }

	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; protected init; }

	public Uri NavigationSource { get; set; }

	public MarkdownFile Index { get; }

	public string Url => Index.Url;

	public string NavigationTitle => Index.NavigationTitle;

	public bool Hidden { get; set; }

	public int NavigationIndex { get; set; }

	private IReadOnlyCollection<MarkdownFile> FilesInOrder { get; }

	private IReadOnlyCollection<DocumentationGroup> GroupsInOrder { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; set; }

	public int Depth { get; }

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	public string FolderName { get; }

	private readonly IRootNavigationItem<MarkdownFile, INavigationItem>? _root;

	protected virtual IRootNavigationItem<MarkdownFile, INavigationItem> DefaultNavigation =>
		_root ?? throw new InvalidOperationException("root navigation's model is not of type MarkdownFile");

	protected DocumentationGroup(string folderName,
		TableOfContentsTreeCollector treeCollector,
		BuildContext context,
		NavigationLookups lookups,
		Uri navigationSource,
		ref int fileIndex,
		int depth,
		IRootNavigationItem<MarkdownFile, INavigationItem>? toplevelTree,
		DocumentationGroup? parent,
		MarkdownFile? virtualIndexFile = null
	)
	{
		Parent = parent;
		FolderName = folderName;
		NavigationSource = navigationSource;
		_treeCollector = treeCollector;
		Depth = depth;
		// Virtual calls don't use state, so while ugly not an issue
		// We'll need to address this more structurally
		// ReSharper disable VirtualMemberCallInConstructor
		_root = toplevelTree;
		toplevelTree ??= DefaultNavigation;
		if (parent?.Depth == 0)
			toplevelTree = DefaultNavigation;
		// ReSharper enable VirtualMemberCallInConstructor
		NavigationRoot = toplevelTree;
		Index = ProcessTocItems(context, toplevelTree, lookups, depth, virtualIndexFile, ref fileIndex, out var groups, out var files, out var navigationItems);

		GroupsInOrder = groups;
		FilesInOrder = files;
		NavigationItems = navigationItems;
		Id = ShortId.Create(NavigationSource.ToString(), FolderName);

		FilesInOrder = [.. FilesInOrder.Except([Index])];
	}

	private MarkdownFile ProcessTocItems(BuildContext context,
		IRootNavigationItem<MarkdownFile, INavigationItem> rootNavigationItem,
		NavigationLookups lookups,
		int depth,
		MarkdownFile? virtualIndexFile,
		ref int fileIndex,
		out List<DocumentationGroup> groups,
		out List<MarkdownFile> files,
		out List<INavigationItem> navigationItems
	)
	{
		groups = [];
		navigationItems = [];
		files = [];
		var indexFile = virtualIndexFile;

		var list = navigationItems;

		void AddToNavigationItems(INavigationItem item, ref int fileIndex)
		{
			item.NavigationIndex = Interlocked.Increment(ref fileIndex);
			list.Add(item);
		}

		foreach (var tocItem in lookups.TableOfContents)
		{
			if (tocItem is FileReference file)
			{
				if (!lookups.FlatMappedFiles.TryGetValue(file.RelativePath, out var d))
				{
					context.EmitError(context.ConfigurationPath,
						$"The following file could not be located: {file.RelativePath} it may be excluded from the build in docset.yml");
					continue;
				}

				if (d is ExcludedFile excluded && excluded.RelativePath.EndsWith(".md"))
				{
					context.EmitError(context.ConfigurationPath, $"{excluded.RelativePath} matches exclusion glob from docset.yml yet appears in TOC");
					continue;
				}

				if (d is not MarkdownFile md)
				{
					if (d is not SnippetFile)
						context.EmitError(context.ConfigurationPath, $"{d.RelativePath} is not a Markdown file.");
					continue;
				}

				md.PartOfNavigation = true;

				// TODO these have to be refactor to be pure navigational properties
				md.ScopeDirectory = file.TableOfContentsScope.ScopeDirectory;
				md.NavigationRoot = rootNavigationItem;
				md.NavigationSource = NavigationSource;

				foreach (var extension in lookups.EnabledExtensions)
					extension.Visit(d, tocItem);

				if (file.Children.Count > 0)
				{
					if (file.Hidden)
						context.EmitError(context.ConfigurationPath, $"The following file is hidden but has children: {file.RelativePath}");
					var group = new DocumentationGroup(md.RelativePath,
						_treeCollector, context, lookups with
						{
							TableOfContents = file.Children,
						}, NavigationSource, ref fileIndex, depth + 1, rootNavigationItem, this, md);
					groups.Add(group);
					AddToNavigationItems(group, ref fileIndex);
					indexFile ??= md;
					continue;
				}

				files.Add(md);
				if (file.RelativePath.EndsWith("index.md") && d is MarkdownFile i)
					indexFile ??= i;

				// Add the page to navigation items unless it's the index file
				// the index file can either be the discovered `index.md` or the parent group's
				// explicit index page. E.g., when grouping related files together.
				// If the page is referenced as hidden in the TOC do not include it in the navigation
				if (indexFile != md)
					AddToNavigationItems(new FileNavigationItem(md, this, file.Hidden), ref fileIndex);
			}
			else if (tocItem is FolderReference folder)
			{
				var children = folder.Children;
				if (children.Count == 0 && lookups.FilesGroupedByFolder.TryGetValue(folder.RelativePath, out var documentationFiles))
				{
					children =
					[
						.. documentationFiles
							.Select(d => new FileReference(folder.TableOfContentsScope, d.RelativePath, false, []))
					];
				}

				DocumentationGroup group;
				if (folder is TocReference tocReference)
				{
					var toc = new TableOfContentsTree(tocReference.Source, folder.RelativePath, _treeCollector, context, lookups with
					{
						TableOfContents = children
					}, ref fileIndex, depth + 1, rootNavigationItem, this);

					group = toc;
					AddToNavigationItems(toc, ref fileIndex);
				}
				else
				{
					group = new DocumentationGroup(folder.RelativePath, _treeCollector, context, lookups with
					{
						TableOfContents = children
					}, NavigationSource, ref fileIndex, depth + 1, rootNavigationItem, this);
					AddToNavigationItems(group, ref fileIndex);
				}

				groups.Add(group);
			}
		}

		var index = indexFile ?? files.FirstOrDefault() ?? groups.FirstOrDefault()?.Index;
		return index ?? throw new InvalidOperationException($"No index file found. {depth}, {fileIndex}");
	}

	private bool _resolved;

	public async Task Resolve(Cancel ctx = default)
	{
		if (_resolved)
			return;

		await Parallel.ForEachAsync(FilesInOrder, ctx, async (file, token) => await file.MinimalParseAsync(token));
		await Parallel.ForEachAsync(GroupsInOrder, ctx, async (group, token) => await group.Resolve(token));

		_ = await Index.MinimalParseAsync(ctx);

		_resolved = true;
	}
}
