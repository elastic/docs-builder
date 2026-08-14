// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Navigation;
using Elastic.Documentation.Navigation.Assembler;

namespace Elastic.Documentation.Assembler.Navigation;

/// <summary>
/// Builds a <see cref="TopNavRenderModel"/> from the top-level navigation entries in
/// <c>navigation.yml</c>. Supports two entry shapes:
/// <list type="bullet">
/// <item><c>toc:</c> — a single navigation root, becomes one tab.</item>
/// <item><c>section:</c> — a named group of toc: refs, becomes one tab whose active state
///   matches any of the grouped roots. External sections become external-link tabs.</item>
/// </list>
/// Active state is determined by comparing the current page's NavigationRoot.Id to each
/// tab's stored <see cref="TopNavLinkItem.SectionIds"/> (or <see cref="TopNavLinkItem.SectionId"/>
/// for single-root tabs).
/// </summary>
public static class SectionTopNavBuilder
{
	public static TopNavRenderModel? Build(SiteNavigation navigation, SiteNavigationFile navFile)
	{
		var topLevel = navigation.TopLevelItems;
		if (navFile.TableOfContents.Count == 0)
			return null;

		// Build an index from Identifier → navigation item for fast lookup.
		// TopLevelItems are IRootNavigationItem at runtime; cast via OfType to access Identifier.
		var byIdentifier = topLevel
			.OfType<IRootNavigationItem<INavigationModel, INavigationItem>>()
			.ToDictionary(item => item.Identifier);

		var items = new List<TopNavRenderItem>();

		foreach (var entry in navFile.TableOfContents)
		{
			if (entry is SiteSectionRef section)
			{
				if (section.External)
				{
					// External link tab — never active
					items.Add(new TopNavLinkItem(section.Title, section.Url ?? "#", IsExternal: true));
				}
				else
				{
					// Section tab: URL = explicit url: or first resolved child's index URL
					// Active when current NavigationRoot matches any of the section's toc: children
					var sectionIds = new HashSet<string>();
					var tabUrl = section.Url;

					foreach (var childRef in section.Children)
					{
						if (!byIdentifier.TryGetValue(childRef.Source, out var navItem))
							continue;
						_ = sectionIds.Add(navItem.Id);
						tabUrl ??= navItem.Index.Url;
					}

					if (tabUrl is not null)
					{
						items.Add(new TopNavLinkItem(section.Title, tabUrl, IsExternal: false)
						{
							SectionIds = sectionIds
						});
					}
				}
			}
			else if (entry is SiteTableOfContentsRef tocRef)
			{
				// Plain toc: entry — one tab, active when NavigationRoot.Id == item.Id
				if (byIdentifier.TryGetValue(tocRef.Source, out var navItem))
				{
					items.Add(new TopNavLinkItem(
						navItem.NavigationTitle,
						navItem.Index.Url,
						IsExternal: false,
						SectionId: navItem.Id));
				}
			}
		}

		return items.Count > 0 ? new TopNavRenderModel(items) : null;
	}
}
