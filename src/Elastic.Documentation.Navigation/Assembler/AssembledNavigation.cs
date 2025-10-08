// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;

namespace Elastic.Documentation.Navigation.Assembler;


public class SiteNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	public SiteNavigation(
		SiteNavigationFile siteNavigationFile,
		IDocumentationContext context,
		IReadOnlyCollection<IDocumentationSetNavigation> documentationSetNavigations
	)
	{
		// Initialize root properties
		NavigationRoot = this;
		Parent = null;
		Depth = 0;
		Hidden = false;
		IsCrossLink = false;
		Id = ShortId.Create("site");
		IsUsingNavigationDropdown = false;
		_nodes = [];
		foreach (var setNavigation in documentationSetNavigations)
		{
			foreach (var (identifier, node) in setNavigation.TableOfContentNodes)
			{
				if (_nodes.ContainsKey(identifier))
				{
					//TODO configurationFileProvider navigation path
					context.EmitError(context.ConfigurationPath, $"Duplicate navigation identifier: {identifier} in navigation.yml");
					continue;
				}
				_nodes.Add(identifier, node);
			}
		}

		// Build NavigationItems from SiteTableOfContentsRef items
		var items = new List<INavigationItem>();
		var index = 0;
		foreach (var tocRef in siteNavigationFile.TableOfContents)
		{
			var navItem = CreateSiteTableOfContentsNavigation(
				tocRef,
				index++,
				context
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
		Index = NavigationItems.OfType<ILeafNavigationItem<IDocumentationFile>>().First();
	}

	private readonly Dictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> _nodes;
	public IReadOnlyDictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> Nodes => _nodes;


	//TODO Obsolete?
	public IReadOnlyCollection<INodeNavigationItem<INavigationModel, INavigationItem>> TopLevelItems =>
		NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList();

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
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	private INavigationItem? CreateSiteTableOfContentsNavigation(
		SiteTableOfContentsRef tocRef,
		int index,
		IDocumentationContext context
	)
	{
		// Validate that path_prefix is set
		if (string.IsNullOrWhiteSpace(tocRef.PathPrefix))
		{
			context.EmitError(context.ConfigurationPath, $"path_prefix is required for TOC reference: {tocRef.Source}");
			return null;
		}

		// Look up the node in the collected nodes
		if (!_nodes.TryGetValue(tocRef.Source, out var node))
		{
			context.EmitError(context.ConfigurationPath, $"Could not find navigation node for identifier: {tocRef.Source} (from source: {tocRef.Source})");
			return null;
		}
		if (node is not INavigationPathPrefixProvider prefixProvider)
		{
			context.EmitError(context.ConfigurationPath, $"Navigation contains an node navigation that does not implement: {nameof(IPathPrefixProvider)} (from source: {tocRef.Source})");
			return null;
		}

		// Set the navigation index
		node.NavigationIndex = index;
		prefixProvider.PathPrefixProvider = new PathPrefixProvider(tocRef.PathPrefix);

		// Recursively create child navigation items if children are specified
		var children = new List<INavigationItem>();
		if (tocRef.Children.Count > 0)
		{
			var childIndex = 0;
			foreach (var child in tocRef.Children)
			{
				var childItem = CreateSiteTableOfContentsNavigation(
					child,
					childIndex++,
					context
				);
				if (childItem != null)
					children.Add(childItem);
			}
		}
		else
		{
			// If no children specified, use the node's original children
			children = node.NavigationItems.ToList();
		}

		// Always return a wrapper to ensure path_prefix is the URL (not path_prefix + node's URL)
		return new SiteTableOfContentsNavigation(node, prefixProvider.PathPrefixProvider, children);
	}
}

/// <summary>
/// Wrapper for a navigation node that applies a path prefix to URLs and optionally
/// overrides the children to show only the children specified in the site navigation configuration.
/// </summary>
/// <remarks>
/// Wrapper for a navigation node that applies a path prefix to URLs and optionally
/// overrides the children to show only the children specified in the site navigation configuration.
/// </remarks>
internal sealed class SiteTableOfContentsNavigation(
	INodeNavigationItem<INavigationModel, INavigationItem> wrappedNode,
	IPathPrefixProvider pathPrefixProvider,
	IReadOnlyCollection<INavigationItem> children
	) : INodeNavigationItem<INavigationModel, INavigationItem>, INavigationPathPrefixProvider
{
	// For site navigation TOC references, the path_prefix IS the URL
	// We don't append the wrapped node's URL
	public string Url
	{
		get
		{
			var url = PathPrefixProvider.PathPrefix.TrimEnd('/');
			return string.IsNullOrEmpty(url) ? "/" : url;
		}
	}

	public string NavigationTitle => wrappedNode.NavigationTitle;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => wrappedNode.NavigationRoot;

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent
	{
		get => wrappedNode.Parent;
		set => wrappedNode.Parent = value;
	}

	public bool Hidden => wrappedNode.Hidden;

	public int NavigationIndex
	{
		get => wrappedNode.NavigationIndex;
		set => wrappedNode.NavigationIndex = value;
	}

	public bool IsCrossLink => wrappedNode.IsCrossLink;
	public int Depth => wrappedNode.Depth;
	public string Id => wrappedNode.Id;
	public ILeafNavigationItem<INavigationModel> Index => wrappedNode.Index;

	// Override to return the specified children from site navigation
	// Wrap children to apply path prefix recursively - but don't wrap children that are
	// already SiteTableOfContentsNavigation (they have their own path prefix)
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; } = children;

	/// <inheritdoc />
	public IPathPrefixProvider PathPrefixProvider { get; set; } = pathPrefixProvider;
}

