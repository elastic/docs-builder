// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Documentation.Site.Navigation;
using Elastic.Markdown.Links.CrossLinks;

namespace Elastic.Markdown.IO.Navigation;

public static class NavigationCrossLinkValidator
{
	public static async Task ValidateNavigationCrossLinksAsync(
		INavigationItem root,
		ICrossLinkResolver crossLinkResolver,
		Action<string> errorEmitter)
	{
		// Ensure cross-links are fetched before validation
		_ = await crossLinkResolver.FetchLinks(new Cancel());

		// Collect all navigation items that contain cross-repo links
		var itemsWithCrossLinks = FindNavigationItemsWithCrossLinks(root);

		foreach (var item in itemsWithCrossLinks)
		{
			if (item is CrossLinkNavigationItem crossLinkItem)
			{
				var url = crossLinkItem.Url;
				if (url != null && Uri.TryCreate(url, UriKind.Absolute, out var crossUri) &&
					crossUri.Scheme != "http" && crossUri.Scheme != "https")
				{
					// Try to resolve the cross-link URL
					if (crossLinkResolver.TryResolve(errorEmitter, crossUri, out var resolvedUri))
					{
						// If resolved successfully, set the resolved URL
						crossLinkItem.ResolvedUrl = resolvedUri.ToString();
					}
					else
					{
						// Error already emitted by CrossLinkResolver
						// But we won't fail the build - just display the original URL
					}
				}
			}
			else if (item is FileNavigationItem fileItem &&
					fileItem.Url != null &&
					Uri.TryCreate(fileItem.Url, UriKind.Absolute, out var fileUri) &&
					fileUri.Scheme != "http" &&
					fileUri.Scheme != "https")
			{
				// Cross-link URL detected in a FileNavigationItem, but we're not validating it yet
			}
		}

		return;
	}

	private static List<INavigationItem> FindNavigationItemsWithCrossLinks(INavigationItem item)
	{
		var results = new List<INavigationItem>();

		// Check if this item has a cross-link
		if (item is CrossLinkNavigationItem crossLinkItem)
		{
			var url = crossLinkItem.Url;
			if (url != null &&
				Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
				uri.Scheme != "http" &&
				uri.Scheme != "https")
			{
				results.Add(item);
			}
		}
		else if (item is FileNavigationItem fileItem &&
				fileItem.Url != null &&
				Uri.TryCreate(fileItem.Url, UriKind.Absolute, out var fileUri) &&
				fileUri.Scheme != "http" &&
				fileUri.Scheme != "https")
		{
			results.Add(item);
		}       // Recursively check children if this is a container
		if (item is INodeNavigationItem<INavigationModel, INavigationItem> containerItem)
		{
			foreach (var child in containerItem.NavigationItems)
			{
				results.AddRange(FindNavigationItemsWithCrossLinks(child));
			}
		}

		return results;
	}
}
