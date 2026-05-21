// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.V2;

namespace Elastic.Documentation.Site.Navigation;

internal static class NavV2LabelListKind
{
	/// <summary>
	/// True when every direct child under this label renders as a non-folder row (nested labels, plain links).
	/// False when any child renders as <c>li.group-navigation</c> (accordion folder rows from _TocTreeNavV2).
	/// </summary>
	public static bool IsSubsectionList(LabelNavigationNode label) =>
		label.NavigationItems.Count > 0 && label.NavigationItems.All(i => !RendersAsGroupNavigationRow(i));

	private static bool RendersAsGroupNavigationRow(INavigationItem item)
	{
		if (item is PlaceholderNavigationNode)
			return true;
		if (item is LabelNavigationNode)
			return false;
		if (item is INodeNavigationItem<INavigationModel, INavigationItem> node && node.NavigationItems.Count > 0)
			return true;

		return false;
	}
}
