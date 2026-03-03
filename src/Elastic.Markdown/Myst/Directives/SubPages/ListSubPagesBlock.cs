// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Markdown.Myst.Directives.SubPages;

/// <summary>Represents a single sub-page entry for the list-sub-pages directive.</summary>
public record SubPageEntry(string Title, string Url, string? Description);

/// <summary>
/// A directive that lists sibling pages (sub-pages) of the current section from the TOC.
/// When used in index.md, lists all siblings. For folder siblings, shows the folder's index page.
/// </summary>
/// <example>
/// :::{list-sub-pages}
/// :::
/// </example>
public class ListSubPagesBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "list-sub-pages";

	public ParserContext Context { get; } = context;

	public IReadOnlyList<SubPageEntry> SubPages { get; private set; } = [];

	public override void FinalizeAndValidate(ParserContext context)
	{
		var subPages = new List<SubPageEntry>();
		var sourcePath = context.MarkdownParentPath ?? context.MarkdownSourcePath;
		var document = context.TryFindDocument(sourcePath);
		if (document is IDocumentationFile docFile &&
			context.NavigationTraversable.NavigationDocumentationFileLookup.TryGetValue(docFile, out var lookupResult))
		{
			// When current page is the index of a node, lookup returns the node (not the leaf). Use that node's NavigationItems as siblings.
			var parent = lookupResult is INodeNavigationItem<INavigationModel, INavigationItem> indexNode && indexNode.Index.Model == docFile
				? indexNode
				: lookupResult.Parent;

			var currentUrl = lookupResult.Url;
			if (parent is not null)
			{
				foreach (var item in parent.NavigationItems)
				{
					if (item.Hidden)
						continue;
					if (item.Url == currentUrl)
						continue;

					var description = item switch
					{
						ILeafNavigationItem<IDocumentationFile> leaf => leaf.Model.Description,
						INodeNavigationItem<INavigationModel, INavigationItem> node when node.Index.Model is IDocumentationFile doc => doc.Description,
						_ => null
					};

					subPages.Add(new SubPageEntry(item.NavigationTitle, item.Url, description));
				}
			}
		}

		SubPages = subPages;
	}
}
