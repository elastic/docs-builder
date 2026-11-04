// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Site.Components.Navigation.LinkMenu;

namespace Elastic.Documentation.Site.Components.Navigation.DocsBar;

public sealed record DocsBarLink(string Title, string Href);

public sealed record DocsBarGroup(string? Label, IReadOnlyList<DocsBarLink> Links);

public sealed record DocsBarItem
{
	public required string Title { get; init; }

	/// <summary>The target of a plain link. Null for a dropdown.</summary>
	public string? Href { get; init; }

	public bool Active { get; init; }
	public bool External { get; init; }
	public string? Icon { get; init; }

	/// <summary>The groups of a dropdown. Empty for a plain link.</summary>
	public IReadOnlyList<DocsBarGroup> Groups { get; init; } = [];

	public bool IsDropdown => Groups.Count > 0;
}

public sealed record DocsBarModel
{
	public required string HomeLabel { get; init; }
	public required string HomeHref { get; init; }
	public IReadOnlyList<DocsBarItem> Items { get; init; } = [];

	/// <summary>The version picker, when the page has several versions.</summary>
	public LinkMenuModel? Versions { get; init; }

	/// <summary>The theme switch at the right of the bar.</summary>
	public bool ThemeSwitch { get; init; }

	/// <summary>The spec download menu on API pages.</summary>
	public LinkMenuModel? Downloads { get; init; }
}
