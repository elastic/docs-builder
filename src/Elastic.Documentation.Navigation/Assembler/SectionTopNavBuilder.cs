// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;

namespace Elastic.Documentation.Navigation.Assembler;

/// <summary>
/// Builds a <see cref="TopNavRenderModel"/> from the top-level navigation entries in
/// <c>navigation_preview.yml</c> when the <c>navigation-preview</c> feature flag is on.
/// Supports two entry shapes:
/// <list type="bullet">
/// <item><c>toc:</c> — a single navigation root, becomes one tab.</item>
/// <item><c>section:</c> — a named group of toc: refs, becomes one tab whose active state
///   matches the section's navigation root. External sections become external-link tabs.</item>
/// </list>
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

		// Index plain toc: items by Identifier for fast lookup.
		// Sections with children now live in the tree as SectionNavigation nodes and
		// are looked up by title instead.
		var byIdentifier = topLevel
			.OfType<IRootNavigationItem<INavigationModel, INavigationItem>>()
			.Where(item => item is not SectionNavigation)
			.ToDictionary(item => item.Identifier);

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
			else if (entry is SiteTableOfContentsRef tocRef)
			{
				// Plain toc: entry — one tab, active when NavigationRoot.Id == item.Id
				if (byIdentifier.TryGetValue(tocRef.Source, out var navItem))
				{
					items.Add(new TopNavLinkItem(navItem.NavigationTitle, navItem.Index.Url, IsExternal: false, SectionId: navItem.Id));
				}
			}
		}

		return items.Count > 0 ? new TopNavRenderModel(items) : null;
	}
}
