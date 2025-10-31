// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Extensions;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.Assembler;

[DebuggerDisplay("{Url}")]
public class SiteNavigation : IRootNavigationItem<IDocumentationFile, INavigationItem>
{
	private readonly string? _sitePrefix;

	public SiteNavigation(
		SiteNavigationFile siteNavigationFile,
		IDocumentationContext context,
		IReadOnlyCollection<IDocumentationSetNavigation> documentationSetNavigations,
		string? sitePrefix
	)
	{
		// Normalize sitePrefix to ensure it has a leading slash and no trailing slash
		_sitePrefix = NormalizeSitePrefix(sitePrefix);
		// Initialize root properties
		NavigationRoot = this;
		Parent = null;
		Hidden = false;
		Id = ShortId.Create("site");
		IsUsingNavigationDropdown = false;
		Phantoms = siteNavigationFile.Phantoms;
		DeclaredPhantoms = [.. siteNavigationFile.Phantoms.Select(p => new Uri(p.Source))];
		DeclaredTableOfContents = SiteNavigationFile.GetAllDeclaredSources(siteNavigationFile);

		_nodes = [];
		foreach (var setNavigation in documentationSetNavigations)
		{
			foreach (var (identifier, node) in setNavigation.TableOfContentNodes)
			{
				if (!_nodes.TryAdd(identifier, node))
				{
					//TODO configurationFileProvider navigation path
					context.EmitError(context.ConfigurationPath, $"Duplicate navigation identifier: {identifier} in navigation.yml");
				}
			}
		}
		UnseenNodes = [.. _nodes.Keys];
		// Build NavigationItems from SiteTableOfContentsRef items
		var items = new List<INavigationItem>();
		var index = 0;
		foreach (var tocRef in siteNavigationFile.TableOfContents)
		{
			var navItem = CreateSiteTableOfContentsNavigation(
				tocRef,
				index++,
				context,
				this,
				null
			);

			if (navItem != null)
				items.Add(navItem);
		}

		var indexNavigation = items.QueryIndex<IDocumentationFile>(this, "/index.md", out var navigationItems);
		Index = indexNavigation;
		NavigationItems = navigationItems;
		_ = this.UpdateNavigationIndex(context);
		foreach (var node in UnseenNodes)
		{
			// impossible since unseen nodes are build from _nodes
			if (!_nodes.TryGetValue(node, out var value))
				continue;
			if (!DeclaredPhantoms.Contains(node))
				context.EmitHint(context.ConfigurationPath, $"Navigation does not explicitly declare: {node} as a phantom");

			// ensure the parent of phantom nodes is `SiteNavigation`
			value.Parent = this;
		}

	}

	public HashSet<Uri> DeclaredPhantoms { get; }

	/// <summary> All the table of contents explicitly declared in the navigation</summary>
	public ImmutableHashSet<Uri> DeclaredTableOfContents { get; set; }

	private readonly Dictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> _nodes;
	public IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> Nodes => _nodes;

	private HashSet<Uri> UnseenNodes { get; }

	public IReadOnlyCollection<PhantomRegistration> Phantoms { get; }

	/// <inheritdoc />
	public Uri Identifier { get; } = new Uri("site://");

	//TODO Obsolete?
	public IReadOnlyCollection<INodeNavigationItem<INavigationModel, INavigationItem>> TopLevelItems =>
		NavigationItems.OfType<INodeNavigationItem<INavigationModel, INavigationItem>>().ToList();

	/// <inheritdoc />
	public string Url => string.IsNullOrEmpty(_sitePrefix) ? "/" : _sitePrefix;

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
	public string Id { get; }

	/// <inheritdoc />
	public ILeafNavigationItem<IDocumentationFile> Index { get; }

	/// <inheritdoc />
	public bool IsUsingNavigationDropdown { get; }

	/// <inheritdoc />
	public IReadOnlyCollection<INavigationItem> NavigationItems { get; }

	void IAssignableChildrenNavigation.SetNavigationItems(IReadOnlyCollection<INavigationItem> navigationItems) =>
		throw new NotSupportedException("SetNavigationItems is not supported on SiteNavigation");

	/// <summary>
	/// Normalizes the site prefix to ensure it has a leading slash and no trailing slash.
	/// Returns null for null or empty/whitespace input.
	/// </summary>
	private static string? NormalizeSitePrefix(string? sitePrefix)
	{
		if (string.IsNullOrWhiteSpace(sitePrefix))
			return null;

		var normalized = sitePrefix.Trim();

		// Ensure leading slash
		if (!normalized.StartsWith('/'))
			normalized = "/" + normalized;

		// Remove trailing slash
		normalized = normalized.TrimEnd('/');

		return normalized;
	}

	private INavigationItem? CreateSiteTableOfContentsNavigation(
		SiteTableOfContentsRef tocRef,
		int index,
		IDocumentationContext context,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		IRootNavigationItem<INavigationModel, INavigationItem>? root
	)
	{
		var pathPrefix = tocRef.PathPrefix;
		// Validate that path_prefix is set
		if (string.IsNullOrWhiteSpace(pathPrefix))
		{
			// we allow not setting path prefixes for toc references from the narrative repository
			if (tocRef.Source.Scheme != NarrativeRepository.RepositoryName)
			{
				context.EmitError(context.ConfigurationPath, $"path_prefix is required for TOC reference: {tocRef.Source}");
				pathPrefix += $"bad-mapping-{tocRef.Source.Scheme}-{tocRef.Source.Host}-{tocRef.Source.AbsolutePath}".TrimEnd('/').TrimEnd('-');
				pathPrefix += "/";
			}
			else
			{
				if (!string.IsNullOrEmpty(tocRef.Source.Host))
					pathPrefix += $"/{tocRef.Source.Host}";
				if (!string.IsNullOrEmpty(tocRef.Source.AbsolutePath) && tocRef.Source.AbsolutePath != "/")
					pathPrefix += $"/{tocRef.Source.AbsolutePath}";
			}
		}

		// Normalize pathPrefix to remove leading/trailing slashes for a consistent combination
		pathPrefix = pathPrefix.Trim('/');

		// Combine with site prefix if present, otherwise ensure leading slash
		pathPrefix = !string.IsNullOrWhiteSpace(_sitePrefix) ? $"{_sitePrefix}/{pathPrefix}" : "/" + pathPrefix;

		// Look up the node in the collected nodes
		if (!_nodes.TryGetValue(tocRef.Source, out var node))
		{
			context.EmitError(context.ConfigurationPath, $"Could not find navigation node for identifier: {tocRef.Source} (from source: {tocRef.Source})");
			return null;
		}
		if (node is not INavigationHomeAccessor homeAccessor)
		{
			context.EmitError(context.ConfigurationPath, $"Navigation contains an node navigation that does not implement: {nameof(INavigationHomeAccessor)} (from source: {tocRef.Source})");
			return null;
		}

		root ??= node;

		_ = UnseenNodes.Remove(tocRef.Source);
		// Set the navigation index
		node.Parent = parent;
		node.NavigationIndex = index;
		homeAccessor.HomeProvider = new NavigationHomeProvider(pathPrefix, root);

		var children = new List<INavigationItem>();

		// Always start with the node's existing children and update their HomeProvider
		INavigationItem[] nodeChildren = [node.Index, .. node.NavigationItems];
		foreach (var nodeChild in nodeChildren)
		{
			nodeChild.Parent = node;
			if (nodeChild is INavigationHomeAccessor childAccessor)
				childAccessor.HomeProvider = homeAccessor.HomeProvider;

			// roots are only added if configured by navigation.yml (tocRef)
			if (nodeChild is IRootNavigationItem<INavigationModel, INavigationItem>)
				continue;

			children.Add(nodeChild);
		}

		// If there are additional children defined in the site navigation, add those too
		if (tocRef.Children.Count > 0)
		{
			var childIndex = 0;
			foreach (var child in tocRef.Children)
			{
				var childItem = CreateSiteTableOfContentsNavigation(
					child,
					childIndex++,
					context,
					node,
					root
				);
				if (childItem != null)
					children.Add(childItem);
			}
		}

		// Check for any undeclared nested TOCs in the node's children
		INavigationItem[] allNodeChildren = [.. node.NavigationItems, node.Index];
		foreach (var nodeChild in allNodeChildren)
		{
			if (nodeChild is not IRootNavigationItem<INavigationModel, INavigationItem> rootChild)
				continue;
			if (DeclaredTableOfContents.Contains(rootChild.Identifier) || DeclaredPhantoms.Contains(rootChild.Identifier))
				continue;

			context.EmitWarning(context.ConfigurationPath, $"Navigation does not explicitly declare: {rootChild.Identifier}");
		}

		switch (node)
		{
			case SiteNavigation:
				break;
			case IAssignableChildrenNavigation documentationSetNavigation:
				documentationSetNavigation.SetNavigationItems(children);
				break;
			default:
				throw new Exception($"node is not a known type: {node.GetType().Name}");
		}
		return node;
	}
}
