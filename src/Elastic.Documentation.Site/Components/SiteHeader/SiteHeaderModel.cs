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

	/// <summary>Adds the theme switch: light and dark, and the theme, contrast and motion options. It needs script to store the choice.</summary>
	public bool ThemeSwitch { get; init; } = true;

	/// <summary>A dark band, as the Codex header on the boards.</summary>
	public bool Dark { get; init; }

	public string CssClass => Dark ? "site-header site-header--dark" : "site-header";

	/// <summary>Adds the Ask AI button and panel to the header.</summary>
	public bool AskAi { get; init; }

	public string? LogoSrc { get; init; }
	public IReadOnlyList<SiteHeaderLink> Links { get; init; } = [];

	/// <summary>Where the build came from. Shown next to the links as a button that opens the details.</summary>
	public Elastic.Documentation.Site.Components.DeploymentInfo.DeploymentInfoModel? Deployment { get; init; }
}
