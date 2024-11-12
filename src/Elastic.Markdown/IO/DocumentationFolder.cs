// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Markdig.Helpers;

namespace Elastic.Markdown.IO;

public class DocumentationFolder
{
	public MarkdownFile? Index { get; }
	private MarkdownFile[] Files { get; }
	private DocumentationFolder[] Nested { get; }

	public OrderedList<MarkdownFile> FilesInOrder { get; private set; }
	public OrderedList<DocumentationFolder> GroupsInOrder { get; private set; }
	public int Level { get; }
	public string? FolderName { get; }

	public DocumentationFolder(Dictionary<string, MarkdownFile[]> markdownFiles, int level, string folderName)
	{
		Level = level;
		FolderName = folderName;

		var files = markdownFiles
			.Where(k => k.Key.EndsWith(".md")).SelectMany(g => g.Value)
			.Where(file => file.ParentFolders.Count == level)
			.ToArray();


		Files = files
			.Where(file => file.FileName != "index.md")
			.ToArray();

		FilesInOrder = new OrderedList<MarkdownFile>(Files);

		Index = files.FirstOrDefault(f => f.FileName == "index.md");

		var newLevel = level + 1;
		var groups = new List<DocumentationFolder>();
		foreach (var kv in markdownFiles.Where(kv=> !kv.Key.EndsWith(".md")))
		{
			var folder = kv.Key;
			var folderFiles = kv.Value
				.Where(file => file.ParentFolders.Count > level)
				.Where(file => file.ParentFolders[level] == folder).ToArray();
			var mapped = folderFiles
				.GroupBy(file =>
				{
					var path = file.ParentFolders.Count > newLevel ? file.ParentFolders[newLevel] : file.FileName;
					return path;
				})
				.ToDictionary(k => k.Key, v => v.ToArray());
			var documentationGroup  = new DocumentationFolder(mapped, newLevel, folder);
			groups.Add(documentationGroup);

		}
		Nested = groups.ToArray();
		GroupsInOrder = new OrderedList<DocumentationFolder>(Nested);
	}

	public bool HoldsCurrent(MarkdownFile current) =>
		Index == current || Files.Contains(current) || Nested.Any(n => n.HoldsCurrent(current));

	private bool _resolved;
	public async Task Resolve(Cancel ctx = default)
	{
		if (_resolved) return;

		await Parallel.ForEachAsync(Files, ctx, async (file, token) => await file.ParseAsync(token));
		await Parallel.ForEachAsync(Nested, ctx, async (group, token) => await group.Resolve(token));

		await (Index?.ParseAsync(ctx) ?? Task.CompletedTask);

		var fileList = new OrderedList<MarkdownFile>();
		var groupList = new OrderedList<DocumentationFolder>();


		FilesInOrder = fileList;
		GroupsInOrder = groupList;
		_resolved = true;
	}
}
