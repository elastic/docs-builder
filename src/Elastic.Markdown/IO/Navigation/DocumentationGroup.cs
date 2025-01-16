// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.IO.Configuration;

namespace Elastic.Markdown.IO.Navigation;

public interface INavigationItem
{
	int Order { get; }
	int Depth { get; }
}

public record GroupNavigation(int Order, int Depth, DocumentationGroup Group) : INavigationItem;
public record FileNavigation(int Order, int Depth, MarkdownFile File) : INavigationItem;


public class DocumentationGroup
{
	public MarkdownFile? Index { get; set; }

	private IReadOnlyCollection<MarkdownFile> FilesInOrder { get; }

	private IReadOnlyCollection<DocumentationGroup> GroupsInOrder { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	public required DocumentationGroup? Parent { get; init; }

	private HashSet<MarkdownFile> OwnFiles { get; }

	public int Depth { get; }

	public DocumentationGroup(
		IReadOnlyCollection<ITocItem> toc,
		IDictionary<string, DocumentationFile> lookup,
		IDictionary<string, DocumentationFile[]> folderLookup,
		ref int fileIndex,
		int depth = 0,
		MarkdownFile? index = null
	)
	{
		Depth = depth;
		Index = ProcessTocItems(index, toc, lookup, folderLookup, depth, ref fileIndex, out var groups, out var files, out var navigationItems);

		GroupsInOrder = groups;
		FilesInOrder = files;
		NavigationItems = navigationItems;

		if (Index is not null)
			FilesInOrder = FilesInOrder.Except([Index]).ToList();

		OwnFiles = [.. FilesInOrder];
	}

	private MarkdownFile? ProcessTocItems(
		MarkdownFile? configuredIndex,
		IReadOnlyCollection<ITocItem> toc,
		IDictionary<string, DocumentationFile> lookup,
		IDictionary<string, DocumentationFile[]> folderLookup,
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
		foreach (var (tocItem, index) in toc.Select((t, i) => (t, i)))
		{
			if (tocItem is FileReference file)
			{
				if (!lookup.TryGetValue(file.Path, out var d) || d is not MarkdownFile md)
					continue;

				md.Parent = this;
				md.NavigationIndex = ++fileIndex;

				if (file.Children.Count > 0 && d is MarkdownFile virtualIndex)
				{
					var group = new DocumentationGroup(file.Children, lookup, folderLookup, ref fileIndex, depth + 1, virtualIndex)
					{
						Parent = this
					};
					groups.Add(group);
					navigationItems.Add(new GroupNavigation(index, depth, group));
					continue;
				}

				files.Add(md);
				if (file.Path.EndsWith("index.md") && d is MarkdownFile i)
					indexFile ??= i;

				// add the page to navigation items unless it's the index file
				// the index file can either be the discovered `index.md` or the parent group's
				// explicit index page. E.g. when grouping related files together.
				if (indexFile != md)
					navigationItems.Add(new FileNavigation(index, depth, md));
			}
			else if (tocItem is FolderReference folder)
			{
				var children = folder.Children;
				if (children.Count == 0
					&& folderLookup.TryGetValue(folder.Path, out var documentationFiles))
				{
					children = documentationFiles
						.Select(d => new FileReference(d.RelativePath, true, []))
						.ToArray();
				}

				var group = new DocumentationGroup(children, lookup, folderLookup, ref fileIndex, depth + 1)
				{
					Parent = this
				};
				groups.Add(group);
				navigationItems.Add(new GroupNavigation(index, depth, group));
			}
		}

		return indexFile ?? files.FirstOrDefault();
	}

	public bool HoldsCurrent(MarkdownFile current) =>
		Index == current || OwnFiles.Contains(current) || GroupsInOrder.Any(n => n.HoldsCurrent(current));

	private bool _resolved;

	public async Task Resolve(Cancel ctx = default)
	{
		if (_resolved)
			return;

		await Parallel.ForEachAsync(FilesInOrder, ctx, async (file, token) => await file.MinimalParse(token));
		await Parallel.ForEachAsync(GroupsInOrder, ctx, async (group, token) => await group.Resolve(token));

		await (Index?.MinimalParse(ctx) ?? Task.CompletedTask);

		_resolved = true;
	}
}
