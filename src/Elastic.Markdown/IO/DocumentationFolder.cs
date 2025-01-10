// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.IO;

public class DocumentationFolder
{
	public MarkdownFile? Index { get; set; }

	public List<MarkdownFile> FilesInOrder { get; }
	public List<DocumentationFolder> GroupsInOrder { get; }

	public required DocumentationFolder? Parent { get; init; }

	private HashSet<MarkdownFile> OwnFiles { get; }

	public int Level { get; }

	public DocumentationFolder(
		IReadOnlyCollection<ITocItem> toc,
		IDictionary<string, DocumentationFile> lookup,
		IDictionary<string, DocumentationFile[]> folderLookup,
		int level = 0,
		MarkdownFile? index = null
	)
	{
		Level = level;
		var foundIndex = ProcessTocItems(toc, lookup, folderLookup, level, out var groupsInOrder, out var filesInOrder);

		GroupsInOrder = groupsInOrder;
		FilesInOrder = filesInOrder;
		Index = index ?? foundIndex;

		if (Index is not null)
			FilesInOrder = FilesInOrder.Except([Index]).ToList();

		OwnFiles = [.. FilesInOrder];
	}

	private MarkdownFile? ProcessTocItems(
		IReadOnlyCollection<ITocItem> toc,
		IDictionary<string, DocumentationFile> lookup,
		IDictionary<string, DocumentationFile[]> folderLookup,
		int level,
		out List<DocumentationFolder> groupsInOrder,
		out List<MarkdownFile> filesInOrder
	)
	{
		groupsInOrder = [];
		filesInOrder = [];
		MarkdownFile? index = null;
		foreach (var tocItem in toc)
		{
			if (tocItem is FileReference file)
			{
				if (!lookup.TryGetValue(file.Path, out var d) || d is not MarkdownFile md)
					continue;

				md.Parent = this;

				if (file.Children.Count > 0 && d is MarkdownFile virtualIndex)
				{
					var group = new DocumentationFolder(file.Children, lookup, folderLookup, level + 1, virtualIndex)
					{
						Parent = this
					};
					groupsInOrder.Add(group);
					continue;
				}

				filesInOrder.Add(md);
				if (file.Path.EndsWith("index.md") && d is MarkdownFile i)
					index ??= i;
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

				var group = new DocumentationFolder(children, lookup, folderLookup, level + 1)
				{
					Parent = this
				};
				groupsInOrder.Add(group);
			}
		}

		return index ?? filesInOrder.FirstOrDefault();
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
