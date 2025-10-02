// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated;

namespace Elastic.Documentation.Navigation.Assembler;


public record SiteModel(string NavigationTitle) : INavigationModel;

public class SiteNavigation : IRootNavigationItem<SiteModel, INavigationItem>
{
	public SiteNavigation(
		SiteNavigationFile siteNavigationFile,
		IDocumentationSetContext context,
		IReadOnlyCollection<DocumentationSetNavigation> documentationSetNavigations
	)
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
	}

	private readonly Dictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> _nodes;
	public IReadOnlyDictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> Nodes => _nodes;

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
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	private INavigationItem? CreateSiteTableOfContentsNavigation(
		SiteTableOfContentsRef tocRef,
		int index,
		IDocumentationSetContext context
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

		// Set the navigation index
		node.NavigationIndex = index;

		// Handle children if defined in tocRef
		if (tocRef.Children.Count > 0)
		{
			// Recursively create child navigation items
			var children = new List<INavigationItem>();
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

			// Return a wrapper that contains only the specified children and applies path prefix
			return new SiteTableOfContentsNavigation(node, children, tocRef.PathPrefix);
		}

		// Return a wrapper that applies the path prefix
		return new SiteTableOfContentsNavigation(node, node.NavigationItems, tocRef.PathPrefix);
	}
}

/// <summary>
/// Wrapper for a navigation node that applies a path prefix to URLs and optionally
/// overrides the children to show only the children specified in the site navigation configuration.
/// </summary>
internal sealed class SiteTableOfContentsNavigation(
	INodeNavigationItem<INavigationModel, INavigationItem> wrappedNode,
	IReadOnlyCollection<INavigationItem> children,
	string pathPrefix)
	: INodeNavigationItem<INavigationModel, INavigationItem>
{
	// For site navigation TOC references, the path_prefix IS the URL
	// We don't append the wrapped node's URL
	public string Url { get; } = pathPrefix.TrimEnd('/');

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
	public INavigationModel Index => wrappedNode.Index;

	// Override to return the specified children from site navigation
	// Wrap children to apply path prefix recursively - but don't wrap children that are
	// already SiteTableOfContentsNavigation (they have their own path prefix)
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; } = WrapChildren(children, pathPrefix);

	private static IReadOnlyCollection<INavigationItem> WrapChildren(
		IReadOnlyCollection<INavigationItem> children,
		string parentPathPrefix)
	{
		var wrappedChildren = new List<INavigationItem>();
		foreach (var child in children)
		{
			// Don't wrap SiteTableOfContentsNavigation - it already has its own path prefix
			if (child is SiteTableOfContentsNavigation)
			{
				wrappedChildren.Add(child);
			}
			// Wrap other node items to apply path prefix to their URLs
			else if (child is INodeNavigationItem<INavigationModel, INavigationItem> nodeChild)
			{
				wrappedChildren.Add(new SiteNavigationItemWrapper(nodeChild, parentPathPrefix));
			}
			// Wrap non-node items as well
			else
			{
				wrappedChildren.Add(new SiteNavigationItemWrapper(child, parentPathPrefix));
			}
		}
		return wrappedChildren;
	}
}

/// <summary>
/// Wrapper for nested navigation items to apply path prefix to their URLs
/// </summary>
internal sealed class SiteNavigationItemWrapper(
	INavigationItem wrappedItem,
	string pathPrefix) : INavigationItem
{
	private readonly string _pathPrefix = pathPrefix.TrimEnd('/');

	public string Url
	{
		get
		{
			// For root nodes with URL "/", use just the path prefix
			if (wrappedItem.Url == "/")
				return _pathPrefix;

			// For other nodes, concatenate path prefix with the item's URL
			return _pathPrefix + wrappedItem.Url;
		}
	}

	public string NavigationTitle => wrappedItem.NavigationTitle;
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot => wrappedItem.NavigationRoot;

	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent
	{
		get => wrappedItem.Parent;
		set => wrappedItem.Parent = value;
	}

	public bool Hidden => wrappedItem.Hidden;

	public int NavigationIndex
	{
		get => wrappedItem.NavigationIndex;
		set => wrappedItem.NavigationIndex = value;
	}

	public bool IsCrossLink => wrappedItem.IsCrossLink;
}
