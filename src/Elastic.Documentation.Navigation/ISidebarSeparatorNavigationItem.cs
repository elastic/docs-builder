// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Navigation;

/// <summary>
/// Marker for a visual divider in the sidebar. Not a page: generators skip it,
/// and nav templates render <c>nav-v2-separator</c>.
/// </summary>
public interface ISidebarSeparatorNavigationItem;

/// <summary>Inserts a horizontal rule between intro pages and the rest of a sidebar tree.</summary>
public sealed class SidebarSeparatorNavigationItem(
	IRootNavigationItem<INavigationModel, INavigationItem> root,
	INodeNavigationItem<INavigationModel, INavigationItem> parent
) : INavigationItem, ISidebarSeparatorNavigationItem
{
	public string Url { get; } = "";
	public string NavigationTitle { get; } = "";
	public IRootNavigationItem<INavigationModel, INavigationItem> NavigationRoot { get; } = root;
	public INodeNavigationItem<INavigationModel, INavigationItem>? Parent { get; set; } = parent;
	public bool Hidden => false;
	public int NavigationIndex { get; set; }
}
