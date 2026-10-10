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

	/// <summary>Adds a button that cycles system, reduced and full motion, so large movement can be previewed or switched off whatever the system says.</summary>
	public bool MotionToggle { get; init; } = true;

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
