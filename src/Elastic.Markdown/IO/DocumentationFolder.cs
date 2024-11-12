// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Helpers;

namespace Elastic.Markdown.IO;

public class DocumentationFolder
{
	public MarkdownFile? Index { get; }

	public List<MarkdownFile> FilesInOrder { get; } = new();
	public List<DocumentationFolder> GroupsInOrder { get; } = new();

	private HashSet<MarkdownFile> OwnFiles { get; }

	public int Level { get; }

	public DocumentationFolder(IReadOnlyCollection<ITocItem> toc,
		IDictionary<string, DocumentationFile> lookup,
		IDictionary<string, DocumentationFile[]> folderLookup,
		int level = 0)
	{
		Level = level;

		foreach (var tocItem in toc)
		{
			if (tocItem is TocFile file)
			{
				if (lookup.TryGetValue(file.Path, out var d) && d is MarkdownFile md)
					FilesInOrder.Add(md);
				if (file.Path.EndsWith("index.md") && d is MarkdownFile index)
					Index = index;
			}
			else if (tocItem is TocFolder folder)
			{
				var children = folder.Children;
				if (children.Count == 0
				    && folderLookup.TryGetValue(folder.Path, out var documentationFiles))
				{
					children = documentationFiles
						.Select(d => new TocFile(d.RelativePath, true, []))
						.ToArray();
				}

				var group = new DocumentationFolder(children, lookup, folderLookup, level + 1);
				GroupsInOrder.Add(group);
			}
		}

		Index ??= FilesInOrder.FirstOrDefault();
		if (Index != null)
			FilesInOrder = FilesInOrder.Except(new[] { Index }).ToList();
		OwnFiles = [..FilesInOrder];
	}

	public bool HoldsCurrent(MarkdownFile current) =>
		Index == current || OwnFiles.Contains(current) || GroupsInOrder.Any(n => n.HoldsCurrent(current));

	private bool _resolved;

	public async Task Resolve(Cancel ctx = default)
	{
		if (_resolved) return;

		await Parallel.ForEachAsync(FilesInOrder, ctx, async (file, token) => await file.ParseAsync(token));
		await Parallel.ForEachAsync(GroupsInOrder, ctx, async (group, token) => await group.Resolve(token));

		await (Index?.ParseAsync(ctx) ?? Task.CompletedTask);

		_resolved = true;
	}
}
