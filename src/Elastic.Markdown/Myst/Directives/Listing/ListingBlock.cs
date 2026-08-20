// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Markdown.Myst.Directives.Listing;

/// <summary>A single page-card entry in a listing group.</summary>
public record ListingEntry(string Title, string Url, string? Description, string? GroupKey, string? GroupTitle);

/// <summary>
/// Internal directive emitted by <c>ListingDocsBuilderExtension</c> into generated listing index pages.
/// Renders a grouped page-card index with a client-side filter and group chips.
/// Not intended as a public authoring syntax.
/// </summary>
public class ListingBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "listing";

	public ParserContext Context { get; } = context;

	public IReadOnlyList<ListingEntry> Entries { get; private set; } = [];

	public override void FinalizeAndValidate(ParserContext context)
	{
		var entries = new List<ListingEntry>();
		var sourcePath = context.MarkdownParentPath ?? context.MarkdownSourcePath;
		var document = context.TryFindDocument(sourcePath);

		if (document is not IDocumentationFile docFile)
		{
			Entries = entries;
			return;
		}

		if (!context.NavigationTraversable.NavigationDocumentationFileLookup.TryGetValue(docFile, out var lookupResult))
		{
			Entries = entries;
			return;
		}

		// We want the listing root node (FolderNavigation) that owns this index page.
		// If lookup returned the leaf (index page), get its parent.
		var listingNode = lookupResult is INodeNavigationItem<INavigationModel, INavigationItem> node && node.Index.Model == docFile
			? node
			: lookupResult.Parent;

		if (listingNode is null)
		{
			Entries = entries;
			return;
		}

		CollectEntries(listingNode, entries, groupKey: null, groupTitle: null, isRoot: true);
		Entries = entries;
	}

	private static void CollectEntries(
		INodeNavigationItem<INavigationModel, INavigationItem> node,
		List<ListingEntry> entries,
		string? groupKey,
		string? groupTitle,
		bool isRoot
	)
	{
		foreach (var item in node.NavigationItems)
		{
			switch (item)
			{
				// Group folder node — recurse with its key and title
				case INodeNavigationItem<INavigationModel, INavigationItem> groupNode when isRoot:
					var gTitle = groupNode.NavigationTitle;
					CollectEntries(groupNode, entries, groupNode.Url, gTitle, isRoot: false);
					break;
				// Content page leaf — add as a card
				case ILeafNavigationItem<IDocumentationFile> leaf:
					entries.Add(new ListingEntry(leaf.NavigationTitle, leaf.Url, leaf.Model.Description, groupKey, groupTitle));
					break;
				// Folder inside a group — flatten
				case INodeNavigationItem<INavigationModel, INavigationItem> subNode:
					CollectEntries(subNode, entries, groupKey, groupTitle, isRoot: false);
					break;
			}
		}
	}
}
