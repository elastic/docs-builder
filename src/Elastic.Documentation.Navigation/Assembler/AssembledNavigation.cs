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
			var childIndex = 0;
			foreach (var child in tocRef.Children)
			{
				_ = CreateSiteTableOfContentsNavigation(
					child,
					childIndex++,
					context
				);
			}
		}

		return node;
	}
}
