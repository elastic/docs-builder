// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Extensions;
using Elastic.Markdown.Extensions.DetectionRules;
using Elastic.Markdown.IO.Configuration;

namespace Elastic.Markdown.IO.Navigation;

public interface INavigationItem
{
	int Order { get; }
	int Depth { get; }
	string Id { get; }
}

public record GroupNavigation(int Order, int Depth, DocumentationGroup Group) : INavigationItem
{
	public string Id { get; } = Group.Id;
}

public record FileNavigation(int Order, int Depth, MarkdownFile File) : INavigationItem
{
	public string Id { get; } = File.Id;
}

public class DocumentationGroup
{
	public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

	public MarkdownFile? Index { get; set; }

	private IReadOnlyCollection<MarkdownFile> FilesInOrder { get; }

	private IReadOnlyCollection<DocumentationGroup> GroupsInOrder { get; }

	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	public required DocumentationGroup? Parent { get; init; }

	public int Depth { get; }

	public DocumentationGroup(
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex)
		: this(context, lookups, ref fileIndex, depth: 0)
	{
	}

	internal DocumentationGroup(
		BuildContext context,
		NavigationLookups lookups,
		ref int fileIndex,
		int depth,
		MarkdownFile? index = null)
	{
		Depth = depth;
		Index = ProcessTocItems(context, index, lookups, depth, ref fileIndex, out var groups, out var files, out var navigationItems);
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

				if (d is DetectionRuleFile df && tocItem is RuleReference r)
					df.Rule = r.Rule;

				md.Parent = this;
				md.Hidden = file.Hidden;
				var navigationIndex = Interlocked.Increment(ref fileIndex);
				md.NavigationIndex = navigationIndex;

				if (file.Children.Count > 0 && d is MarkdownFile virtualIndex)
				{
					if (file.Hidden)
						context.EmitError(context.ConfigurationPath, $"The following file is hidden but has children: {file.Path}");

					var group = new DocumentationGroup(
						context, lookups with { TableOfContents = file.Children }, ref fileIndex, depth + 1, virtualIndex)
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
				// if the page is referenced as hidden in the TOC do not include it in the navigation
				if (indexFile != md && !md.Hidden)
					navigationItems.Add(new FileNavigation(index, depth, md));
			}
			else if (tocItem is FolderReference folder)
			{
				var children = folder.Children;
				if (children.Count == 0 && lookups.FilesGroupedByFolder.TryGetValue(folder.Path, out var documentationFiles))
				{
					children =
					[
						.. documentationFiles
							.Select(d => new FileReference(d.RelativePath, true, false, []))
					];
				}

				var group = new DocumentationGroup(context, lookups with { TableOfContents = children }, ref fileIndex, depth + 1)
				{
					Parent = this
				};
				groups.Add(group);
				navigationItems.Add(new GroupNavigation(index, depth, group));
			}
			else
			{
				foreach (var extension in lookups.EnabledExtensions)
				{
					if (extension.Processes(tocItem))
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
}
