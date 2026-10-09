// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SiteHeader;

public sealed record SiteHeaderLink(string Label, string Href, string? Icon = null);

public sealed record SiteHeaderModel
{
	public required string Title { get; init; }
	public required string HomeHref { get; init; }

	/// <summary>Where the search form submits, or <c>null</c> to leave search out. Search works without JavaScript through a plain GET.</summary>
	public string? SearchAction { get; init; }

	/// <summary>Adds the search dialog and a trigger that opens it. The trigger stays hidden until script can open the dialog.</summary>
	public bool Search { get; init; }

	/// <summary>Adds a button that cycles system, light and dark. It stays hidden until script can store the choice.</summary>
	public bool ThemeToggle { get; init; } = true;

	public string? LogoSrc { get; init; }
	public IReadOnlyList<SiteHeaderLink> Links { get; init; } = [];

	/// <summary>The branch and commit shown next to the links, for example "main · 65728c5".</summary>
	public string? Revision { get; init; }
}
