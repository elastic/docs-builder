// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Navigation.Isolated.Node;

namespace Elastic.Documentation.Navigation.V2;

/// <summary>
/// Extends <see cref="SiteNavigation"/> with a V2 label-structured sidebar tree derived from
/// <c>navigation-v2.yml</c>. Content is built at the same URL paths as V1 (the original
/// <paramref name="originalFile"/> is passed to the base constructor unchanged).
/// Only the sidebar presentation changes — <see cref="V2NavigationItems"/> exposes the
/// label/placeholder hierarchy used by <c>_TocTreeNavV2.cshtml</c>.
/// </summary>
public class SiteNavigationV2 : SiteNavigation
{
	public SiteNavigationV2(
		NavigationV2File v2File,
		SiteNavigationFile originalFile,
		IDocumentationContext context,
		IReadOnlyCollection<IDocumentationSetNavigation> documentationSetNavigations,
		string? sitePrefix
	) : base(originalFile, context, documentationSetNavigations, sitePrefix)
		=> V2NavigationItems = BuildV2Items(v2File.Nav, Nodes, this, depth: 0);

	/// <summary>
	/// Label-structured navigation items for V2 sidebar rendering.
	/// Contains <see cref="LabelNavigationNode"/>, <see cref="PlaceholderNavigationLeaf"/>,
	/// and existing <see cref="IRootNavigationItem{TIndex,TChildNavigation}"/> nodes.
	/// </summary>
	public IReadOnlyList<INavigationItem> V2NavigationItems { get; }

	private static IReadOnlyList<INavigationItem> BuildV2Items(
		IReadOnlyList<INavV2Item> v2Items,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		int depth
	)
	{
		var result = new List<INavigationItem>();
		foreach (var item in v2Items)
		{
			var navItem = CreateV2NavigationItem(item, nodes, parent, depth);
			if (navItem is not null)
				result.Add(navItem);
		}
		return result;
	}

	private static INavigationItem? CreateV2NavigationItem(
		INavV2Item item,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		int depth
	) =>
		item switch
		{
			LabelNavV2Item label => CreateLabel(label, nodes, parent, depth),
			TocNavV2Item toc => nodes.TryGetValue(toc.Source, out var node) ? node : null,
			PageNavV2Item { Page: null, Title: var title } => new PlaceholderNavigationLeaf(title ?? "Untitled", parent),
			PageNavV2Item { Title: var title } page => new PlaceholderNavigationLeaf(title ?? page.Page?.ToString() ?? "Unknown", parent),
			_ => null
		};

	private static LabelNavigationNode CreateLabel(
		LabelNavV2Item label,
		IReadOnlyDictionary<Uri, IRootNavigationItem<IDocumentationFile, INavigationItem>> nodes,
		INodeNavigationItem<INavigationModel, INavigationItem> parent,
		int depth
	)
	{
		// Build a temporary label so it can be used as parent for its children
		var placeholder = new LabelNavigationNode(label.Label, label.Expanded, [], parent);
		var children = BuildV2Items(label.Children, nodes, placeholder, depth + 1);
		return new LabelNavigationNode(label.Label, label.Expanded, children, parent);
	}
}
