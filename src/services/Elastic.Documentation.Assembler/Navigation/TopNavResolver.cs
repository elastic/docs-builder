// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Links.CrossLinks;

namespace Elastic.Documentation.Assembler.Navigation;

/// <summary>
/// Turns the <c>top_nav:</c> entries of navigation.yml into a <see cref="TopNavRenderModel"/> with final hrefs.
/// Runs once per assemble, before any page is rendered.
/// </summary>
public static class TopNavResolver
{
	/// <summary>
	/// Resolves the configured top navigation, or returns null when nothing is configured, in which
	/// case the layout keeps rendering its built-in links.
	/// </summary>
	public static TopNavRenderModel? Resolve(
		SiteNavigationFile navigationFile,
		ICrossLinkResolver crossLinkResolver,
		string? pathPrefix,
		IDiagnosticsCollector collector,
		IFileInfo navigationFileInfo
	)
	{
		if (navigationFile.TopNav.Count == 0)
			return null;

		var items = new List<TopNavRenderItem>();
		foreach (var config in navigationFile.TopNav)
		{
			if (ResolveItem(config, crossLinkResolver, pathPrefix, collector, navigationFileInfo) is { } item)
				items.Add(item);
		}

		return items.Count == 0 ? null : new TopNavRenderModel(items);
	}

	private static TopNavRenderItem? ResolveItem(
		TopNavItemConfig config,
		ICrossLinkResolver crossLinkResolver,
		string? pathPrefix,
		IDiagnosticsCollector collector,
		IFileInfo navigationFileInfo
	)
	{
		if (string.IsNullOrWhiteSpace(config.Title))
		{
			collector.EmitError(navigationFileInfo, "top_nav entry is missing a 'title'");
			return null;
		}

		if (config.Children.Count == 0)
			return ResolveLink(config, config.Title, crossLinkResolver, pathPrefix, collector, navigationFileInfo);

		if (config.Url is not null || config.Page is not null)
		{
			collector.EmitWarning(navigationFileInfo,
				$"top_nav entry '{config.Title}' has children, so its 'url'/'page' is ignored: the label only toggles the dropdown");
		}

		var groups = ResolveGroups(config, crossLinkResolver, pathPrefix, collector, navigationFileInfo);
		if (groups.Count == 0)
		{
			collector.EmitWarning(navigationFileInfo, $"top_nav dropdown '{config.Title}' has no resolvable links and is not rendered");
			return null;
		}

		return new TopNavDropdownItem(config.Title, groups);
	}

	/// <summary>
	/// Flattens a dropdown's children into groups. A child with children of its own becomes a labelled
	/// group; consecutive childless children are collected into a single unlabelled group.
	/// </summary>
	private static List<TopNavGroup> ResolveGroups(
		TopNavItemConfig dropdown,
		ICrossLinkResolver crossLinkResolver,
		string? pathPrefix,
		IDiagnosticsCollector collector,
		IFileInfo navigationFileInfo
	)
	{
		var groups = new List<TopNavGroup>();
		var ungrouped = new List<TopNavLinkItem>();

		foreach (var child in dropdown.Children)
		{
			if (string.IsNullOrWhiteSpace(child.Title))
			{
				collector.EmitError(navigationFileInfo, $"top_nav entry under '{dropdown.Title}' is missing a 'title'");
				continue;
			}

			if (child.Children.Count == 0)
			{
				if (ResolveLink(child, child.Title, crossLinkResolver, pathPrefix, collector, navigationFileInfo) is { } link)
					ungrouped.Add(link);
				continue;
			}

			if (ungrouped.Count > 0)
			{
				groups.Add(new TopNavGroup(null, ungrouped.ToArray()));
				ungrouped.Clear();
			}

			var links = new List<TopNavLinkItem>();
			foreach (var grandChild in child.Children)
			{
				if (string.IsNullOrWhiteSpace(grandChild.Title))
				{
					collector.EmitError(navigationFileInfo, $"top_nav entry under '{child.Title}' is missing a 'title'");
					continue;
				}

				if (grandChild.Children.Count > 0)
				{
					collector.EmitError(navigationFileInfo,
						$"top_nav entry '{grandChild.Title}' nests too deeply: a dropdown supports one level of groups only");
					continue;
				}

				if (ResolveLink(grandChild, grandChild.Title, crossLinkResolver, pathPrefix, collector, navigationFileInfo) is { } link)
					links.Add(link);
			}

			if (links.Count > 0)
				groups.Add(new TopNavGroup(child.Title, links));
			else
				collector.EmitWarning(navigationFileInfo, $"top_nav group '{child.Title}' has no resolvable links and is not rendered");
		}

		if (ungrouped.Count > 0)
			groups.Add(new TopNavGroup(null, ungrouped.ToArray()));

		return groups;
	}

	private static TopNavLinkItem? ResolveLink(
		TopNavItemConfig config,
		string title,
		ICrossLinkResolver crossLinkResolver,
		string? pathPrefix,
		IDiagnosticsCollector collector,
		IFileInfo navigationFileInfo
	)
	{
		if (config.Url is not null && config.Page is not null)
		{
			collector.EmitError(navigationFileInfo, $"top_nav entry '{title}' sets both 'url' and 'page', use one of them");
			return null;
		}

		if (config.Page is { } page)
		{
			var errors = new List<string>();
			if (!crossLinkResolver.TryResolve(errors.Add, page, out var resolved))
			{
				collector.EmitError(navigationFileInfo,
					$"top_nav entry '{title}' could not resolve page '{page}': {string.Join("; ", errors)}");
				return null;
			}

			return new TopNavLinkItem(title, EnsureTrailingSlash(resolved.AbsolutePath), false);
		}

		if (string.IsNullOrWhiteSpace(config.Url))
		{
			collector.EmitError(navigationFileInfo, $"top_nav entry '{title}' needs a 'url', a 'page' or 'children'");
			return null;
		}

		if (config.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| config.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			return new TopNavLinkItem(title, config.Url, true);

		var prefix = string.IsNullOrWhiteSpace(pathPrefix) ? string.Empty : $"/{pathPrefix.Trim('/')}";
		return new TopNavLinkItem(title, EnsureTrailingSlash($"{prefix}/{config.Url.TrimStart('/')}"), false);
	}

	private static string EnsureTrailingSlash(string url) =>
		url.Contains('#') || url.EndsWith('/') ? url : url + '/';
}
