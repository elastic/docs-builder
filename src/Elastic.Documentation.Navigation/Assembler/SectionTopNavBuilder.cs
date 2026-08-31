// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Navigation.Assembler;

/// <summary>
/// Builds a <see cref="TopNavRenderModel"/> from the top-level navigation entries in
/// <c>navigation_preview.yml</c> when the <c>navigation-preview</c> feature flag is on.
/// Supports three <c>section:</c> shapes:
/// <list type="bullet">
/// <item><c>external:</c> — external-link tab, never active.</item>
/// <item><c>dropdown:</c> — a panel of links, never active (no tree membership).</item>
/// <item><c>children:</c> — maps to a <see cref="SectionNavigation"/> tree node; active when the
///   current page's NavigationRoot.Id equals the section's Id.</item>
/// </list>
/// Leftover top-level <c>toc:</c> entries are not tabs (they stay in the tree).
/// Active state is determined by comparing the current page's NavigationRoot.Id to each
/// tab's stored <see cref="TopNavLinkItem.SectionId"/>.
/// </summary>
public static class SectionTopNavBuilder
{
	public static TopNavRenderModel? Build(SiteNavigation navigation, SiteNavigationFile navFile)
	{
		var topLevel = navigation.TopLevelItems;
		if (navFile.TableOfContents.Count == 0)
			return null;

		// Sections with children live in the tree as SectionNavigation nodes and
		// are looked up by title.
		var sectionsByTitle = topLevel.OfType<SectionNavigation>().ToDictionary(s => s.Title, StringComparer.OrdinalIgnoreCase);

		var items = new List<TopNavRenderItem>();

		foreach (var entry in navFile.TableOfContents)
		{
			if (entry is SiteSectionRef section)
			{
				if (section.IsExternal)
				{
					items.Add(new TopNavLinkItem(section.Title, section.ExternalUrl!, IsExternal: true));
				}
				else if (section.IsDropdown)
				{
					// Resolve each link URL against the site prefix so hrefs in the template are site-absolute.
					var sitePrefix = navigation.Url.TrimEnd('/');
					var links = section
						.DropdownLinks
						.Select(l => new TopNavLinkItem(l.Title, sitePrefix + "/" + l.Url.TrimStart('/'), IsExternal: false))
						.ToArray();
					items.Add(new TopNavDropdownItem(section.Title, [new TopNavGroup(null, links)]));
				}
				else if (sectionsByTitle.TryGetValue(section.Title, out var sectionNav))
				{
					// All pages within the section have NavigationRoot = sectionNav,
					// so a single SectionId match is sufficient for active-tab detection.
					var tabUrl = sectionNav
						.NavigationItems
						.OfType<IRootNavigationItem<INavigationModel, INavigationItem>>()
						.FirstOrDefault()?.Index.Url;

					if (tabUrl is not null)
					{
						items.Add(new TopNavLinkItem(section.Title, tabUrl, IsExternal: false, SectionId: sectionNav.Id));
					}
				}
			}
			else if (entry is SiteTableOfContentsRef)
			{
				// Preview tabs come from section: entries only. A leftover top-level
				// toc: (the local docs-builder inject) stays in the tree, not the top bar.
				continue;
			}
		}

		return items.Count > 0 ? new TopNavRenderModel(items) : null;
	}
}
