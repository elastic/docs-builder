// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Configuration.Toc;

/// <summary>
/// The resolved top navigation handed to the layout. Every URL here is final: cross links are
/// resolved and the environment path prefix is already applied, so templates render hrefs as is.
/// The active tab is determined by comparing each item's <see cref="TopNavLinkItem.SectionId"/>
/// against the current page's <c>NavigationRoot.Id</c> — no URL prefix matching.
/// </summary>
public record TopNavRenderModel(IReadOnlyList<TopNavRenderItem> Items);

public abstract record TopNavRenderItem(string Title)
{
	/// <summary>
	/// Whether this tab is active given the current page's navigation root id.
	/// Pass <c>null</c> when the page has no section (e.g. the homepage).
	/// </summary>
	public abstract bool IsActive(string? currentSectionId);
}

/// <summary>
/// A link tab. <see cref="SectionId"/> is the ID of a single navigation root that owns this tab.
/// <see cref="SectionIds"/> overrides <see cref="SectionId"/> when a tab groups multiple roots (section:
/// entries in navigation.yml). The tab is active when <c>currentSectionId</c> matches any owned ID.
/// External link tabs carry neither field and are never active.
/// </summary>
public record TopNavLinkItem(string Title, string Url, bool IsExternal, string? SectionId = null) : TopNavRenderItem(Title)
{
	/// <summary>When non-null, overrides <see cref="SectionId"/> for active-state matching.</summary>
	public IReadOnlySet<string>? SectionIds { get; init; }

	public override bool IsActive(string? currentSectionId)
	{
		if (currentSectionId is null)
			return false;
		if (SectionIds is not null)
			return SectionIds.Contains(currentSectionId);
		return SectionId is not null && currentSectionId == SectionId;
	}
}

/// <summary>
/// A dropdown tab with labelled link groups. Dropdowns have no tree-section membership and are never
/// marked active.
/// </summary>
public record TopNavDropdownItem(string Title, IReadOnlyList<TopNavGroup> Groups) : TopNavRenderItem(Title)
{
	public override bool IsActive(string? currentSectionId) => false;
}

/// <summary>A run of links inside a dropdown. A null <see cref="Label"/> means the links are ungrouped.</summary>
public record TopNavGroup(string? Label, IReadOnlyList<TopNavLinkItem> Links);
