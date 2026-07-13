// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Navigation;

namespace Elastic.Documentation.Site.Navigation;

/// <summary>
/// Resolves which sidebar URL should be highlighted for the current page.
/// </summary>
public static class NavigationActiveUrlResolver
{
	/// <summary>
	/// Returns the nav link URL to mark as current. Hidden items (e.g. overload operations in the API tree)
	/// resolve to the nearest visible ancestor.
	/// </summary>
	public static string? Resolve(INavigationItem current)
	{
		if (!current.Hidden)
			return Normalize(current.Url);

		var item = current;
		while (item.Parent is { } parent)
		{
			if (!parent.Hidden)
				return Normalize(parent.Url);

			item = parent;
		}

		return null;
	}

	public static bool IsActive(string? activeUrl, string linkUrl) =>
		activeUrl is not null && Normalize(activeUrl) == Normalize(linkUrl);

	/// <summary>Returns true when {@paramref activeUrl} matches this item or any visible descendant.</summary>
	public static bool TreeContainsActiveUrl(INavigationItem item, string? activeUrl)
	{
		if (activeUrl is null)
			return false;

		if (!item.Hidden && IsActive(activeUrl, item.Url))
			return true;

		if (item is not INodeNavigationItem<INavigationModel, INavigationItem> node)
			return false;

		foreach (var child in node.NavigationItems)
		{
			if (child.Hidden)
				continue;

			if (TreeContainsActiveUrl(child, activeUrl))
				return true;
		}

		return false;
	}

	public static string Normalize(string url)
	{
		var normalized = url.TrimEnd('/');
		return normalized.Length == 0 ? "/" : normalized;
	}
}
