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

public class SiteNavigation : IRootNavigationItem<SiteModel, SiteTableOfContentsNavigation>
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
		Git = context.Git;
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
		var items = new List<SiteTableOfContentsNavigation>();
		var index = 0;
		foreach (var tocRef in siteNavigationFile.TableOfContents)
		{
			var navItem = CreateSiteTableOfContentsNavigation(
				tocRef,
				index++,
				context,
				parent: null, // Top-level items have no parent
				root: this,
				urlRoot: this,
				depth: Depth
			);

			if (navItem != null)
				items.Add(navItem);
		}

		NavigationItems = items;
	}

	public GitCheckoutInformation Git { get; }

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
		// Convert docs-content:// URI to repository identifier URI
		// Example: docs-content://platform/deployment-guide -> platform://deployment-guide
		var identifier = ConvertSourceToIdentifier(tocRef.Source);

		// Try to look up the node in the collected nodes
		if (_nodes.TryGetValue(identifier, out var node))
		{
			// Get the TableOfContentsNavigation properties from the node
			if (node is not TableOfContentsNavigation and not DocumentationSetNavigation)
			{
				context.EmitError(context.ConfigurationPath, $"Node {identifier} is not a TableOfContentsNavigation or DocumentationSetNavigation, found: {node.GetType().Name}");
				return null;
			}

			// Handle both TableOfContentsNavigation and DocumentationSetNavigation
			IDirectoryInfo tocDirectory;
			string parentPath;
			GitCheckoutInformation git;

			if (node is TableOfContentsNavigation tocNav)
			{
				tocDirectory = tocNav.TableOfContentsDirectory;
				parentPath = tocNav.ParentPath;
				// Use Git from the repository name in the identifier
				git = context.Git;
			}
			else // DocumentationSetNavigation
			{
				var docSetNav = (DocumentationSetNavigation)node;
				// For DocumentationSetNavigation, use the source directory
				tocDirectory = context.ReadFileSystem.DirectoryInfo.New(
					context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, GetTocPath(tocRef))
				);
				parentPath = string.Empty;
				git = docSetNav.Git;
			}

			// Determine navigation items based on whether children are defined in tocRef
			IReadOnlyCollection<INavigationItem> navigationItems;

			if (tocRef.Children.Count > 0)
			{
				// Create placeholder to use as parent and urlRoot for children
				var placeholder = new SiteTableOfContentsNavigation(
					tocDirectory,
					depth + 1,
					parentPath,
					parent,
					urlRoot,
					[],
					git,
					_nodes
				)
				{
					NavigationIndex = index,
					PathPrefix = tocRef.PathPrefix
				};

				// Use children from tocRef
				var children = new List<SiteTableOfContentsNavigation>();
				var childIndex = 0;
				foreach (var child in tocRef.Children)
				{
					var childNav = CreateSiteTableOfContentsNavigation(
						child,
						childIndex++,
						context,
						parent: placeholder,
						root,
						placeholder, // Each SiteTableOfContentsNavigation becomes a new URL root
						depth + 1
					);

					if (childNav != null)
						children.Add(childNav);
				}

				navigationItems = children;

				// Create the final SiteTableOfContentsNavigation with actual children
				var siteTableOfContentsNavigation = new SiteTableOfContentsNavigation(
					tocDirectory,
					depth + 1,
					parentPath,
					parent,
					urlRoot,
					navigationItems,
					git,
					_nodes
				)
				{
					NavigationIndex = index,
					PathPrefix = tocRef.PathPrefix
				};

				// Update children's Parent and UrlRoot to point to final navigation
				foreach (var child in children)
				{
					child.Parent = siteTableOfContentsNavigation;
				}

				return siteTableOfContentsNavigation;
			}
			else
			{
				// Use navigation items from the looked-up node
				navigationItems = node is TableOfContentsNavigation tNav
					? tNav.NavigationItems
					: ((DocumentationSetNavigation)node).NavigationItems;

				// Create the SiteTableOfContentsNavigation
				return new SiteTableOfContentsNavigation(
					tocDirectory,
					depth + 1,
					parentPath,
					parent,
					urlRoot,
					navigationItems,
					git,
					_nodes
				)
				{
					NavigationIndex = index,
					PathPrefix = tocRef.PathPrefix
				};
			}
		}

		// Node not found in dictionary - create from directory structure
		var tocPath = GetTocPath(tocRef);
		var tocDir = context.ReadFileSystem.DirectoryInfo.New(
			context.ReadFileSystem.Path.Combine(context.DocumentationSourceDirectory.FullName, tocPath)
		);

		// Process children recursively (create temp placeholder first, then actual)
		var tempPlaceholder = new SiteTableOfContentsNavigation(
			tocDir,
			depth + 1,
			tocPath,
			parent,
			urlRoot,
			[],
			context.Git,
			_nodes
		)
		{
			NavigationIndex = index,
			PathPrefix = tocRef.PathPrefix
		};

		var directoryChildren = new List<SiteTableOfContentsNavigation>();
		var childIdx = 0;
		foreach (var child in tocRef.Children)
		{
			var childNav = CreateSiteTableOfContentsNavigation(
				child,
				childIdx++,
				context,
				parent: tempPlaceholder,
				root,
				tempPlaceholder, // Each SiteTableOfContentsNavigation becomes a new URL root
				depth + 1
			);

			if (childNav != null)
				directoryChildren.Add(childNav);
		}

		// Create final navigation with children
		var directoryNavigation = new SiteTableOfContentsNavigation(
			tocDir,
			depth + 1,
			tocPath,
			parent,
			urlRoot,
			directoryChildren,
			context.Git,
			_nodes
		)
		{
			NavigationIndex = index,
			PathPrefix = tocRef.PathPrefix
		};

		// Update children's Parent to point to final navigation
		foreach (var child in directoryChildren)
			child.Parent = directoryNavigation;

		return directoryNavigation;
	}

	private static string GetTocPath(SiteTableOfContentsRef tocRef)
	{
		// Determine the TOC path from the URI
		// For URIs like docs-content://elasticsearch/reference, we need both host and path
		return string.IsNullOrEmpty(tocRef.Source.Host)
			? tocRef.Source.AbsolutePath.TrimStart('/')
			: $"{tocRef.Source.Host}{tocRef.Source.AbsolutePath}";
	}

	private static Uri ConvertSourceToIdentifier(Uri source)
	{
		// Convert docs-content://platform/deployment-guide to platform://deployment-guide
		// Convert docs-content://platform to platform://
		var repositoryName = source.Host;
		var path = source.AbsolutePath.TrimStart('/');

		var identifierString = string.IsNullOrEmpty(path)
			? $"{repositoryName}://"
			: $"{repositoryName}://{path}";

		return new Uri(identifierString);
	}
}

/// <inheritdoc />
public class SiteTableOfContentsNavigation(
	IDirectoryInfo tableOfContentsDirectory,
	int depth,
	string parentPath,
	INodeNavigationItem<INavigationModel, INavigationItem>? parent,
	IRootNavigationItem<INavigationModel, INavigationItem> urlRoot,
	IReadOnlyCollection<INavigationItem> navigationItems,
	GitCheckoutInformation git,
	Dictionary<Uri, INodeNavigationItem<INavigationModel, INavigationItem>> tocNodes
)
	: TableOfContentsNavigation(tableOfContentsDirectory, depth, parentPath, parent, urlRoot, navigationItems, git, tocNodes)
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
