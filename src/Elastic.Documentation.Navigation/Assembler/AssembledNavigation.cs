// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;

namespace Elastic.Documentation.Navigation.Assembler;


public record SiteModel(string NavigationTitle) : INavigationModel;

public class SiteNavigation : IRootNavigationItem<SiteModel, SiteTableOfContentsNavigation>
{
	public SiteNavigation(SiteNavigationFile siteNavigationFile, IDocumentationSetContext context)
	{
		// Initialize root properties
		NavigationRoot = this;
		Parent = null;
		Depth = 0;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create("site");
		Index = new SiteModel("Site Navigation");
		IsUsingNavigationDropdown = false;

		// Convert SiteTableOfContentsRef items to navigation items
		var items = new List<SiteTableOfContentsNavigation>();
		var index = 0;
		foreach (var tocRef in siteNavigationFile.TableOfContents)
		{
			var navItem = CreateSiteTableOfContentsNavigation(
				tocRef,
				index++,
				context,
				parent: null,
				root: NavigationRoot,
				urlRoot: NavigationRoot,
				depth: Depth
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
	}

	/// <inheritdoc />
	public string Url { get; set; } = "/";

	/// <inheritdoc />
	public string NavigationTitle => Index.NavigationTitle;

	/// <inheritdoc />
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; }

	/// <inheritdoc />
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; }

	/// <inheritdoc />
	public bool Hidden { get; }

	/// <inheritdoc />
	public int NavigationIndex { get; set; }

	/// <inheritdoc />
	public bool IsCrossLink { get; }

	/// <inheritdoc />
	public int Depth { get; }

	/// <inheritdoc />
	public string Id { get; }

	/// <inheritdoc />
	public SiteModel Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<SiteTableOfContentsNavigation> NavigationItems { get; }

	private SiteTableOfContentsNavigation? CreateSiteTableOfContentsNavigation(
		SiteTableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context,
		INodeNavigationItem<INavigationModel, INavigationItem>? parent,
		IRootNavigationItem<INavigationModel, INavigationItem> root,
		IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
		int depth
	)
	{
		// Determine the TOC path from the URI
		// For URIs like docs-content://elasticsearch/reference, we need both host and path
		var tocPath = string.IsNullOrEmpty(tocRef.Source.Host)
			? tocRef.Source.AbsolutePath.TrimStart('/')
			: $"{tocRef.Source.Host}{tocRef.Source.AbsolutePath}";

		// Resolve the TOC directory
		var tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, tocPath)
		);

		// Create the TOC navigation that will be the parent for children
		var tocNavigation = new SiteTableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			tocPath,
			parent,
			urlRoot,
			[]
		)
		{
			PathPrefix = tocRef.PathPrefix
		};

		// Convert children recursively
		var children = new List<SiteTableOfContentsNavigation>();
		var childIndex = 0;

		foreach (var child in tocRef.Children)
		{
			var childNav = CreateSiteTableOfContentsNavigation(
				child,
				childIndex++,
				context,
				tocNavigation,
				root,
				tocNavigation, // Each SiteTableOfContentsRef becomes a new URL root
				depth + 1
			);

			if (childNav != null)
				children.Add(childNav);
		}

		// Create final navigation with actual children
		var finalNavigation = new SiteTableOfContentsNavigation(
			tocDirectory,
			depth + 1,
			tocPath,
			parent,
			urlRoot,
			children
		)
		{
			NavigationIndex = index,
			PathPrefix = tocRef.PathPrefix
		};

		// Update children's Parent to point to the final navigation
		foreach (var child in children)
			child.Parent = finalNavigation;

		return finalNavigation;
	}
}

/// <inheritdoc />
public class SiteTableOfContentsNavigation(
	IDirectoryInfo tableOfContentsDirectory,
	int depth,
	string parentPath,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
	IReadOnlyCollection<INavigationItem> navigationItems
)
	: TableOfContentsNavigation(tableOfContentsDirectory, depth, parentPath, parent, urlRoot, navigationItems)
{
	public string PathPrefix { get; init; } = string.Empty;

	public override string Url
	{
		get
		{
			// If PathPrefix is specified, use it
			if (!string.IsNullOrEmpty(PathPrefix))
				return PathPrefix;

			// Otherwise, use the base implementation
			return base.Url;
		}
	}
}
