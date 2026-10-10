// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SiteFooter;

public sealed record FooterLink(string Label, string Href);

public sealed record SiteFooterModel
{
	/// <summary>The logo for dark surfaces.</summary>
	public required string LogoSrc { get; init; }

	/// <summary>The coloured logo for light surfaces. Without it the light footer uses <see cref="LogoSrc" /> in both modes.</summary>
	public string? ColorLogoSrc { get; init; }

	/// <summary>A footer that stays dark in both themes, as on the archive and Codex pages.</summary>
	public bool Dark { get; init; }

	public string CssClass => Dark ? "site-footer site-footer--dark" : "site-footer";
	public int Year { get; init; } = DateTime.Today.Year;

	public IReadOnlyList<FooterLink> Links { get; init; } =
	[
		new("Terms of Use", "https://www.elastic.co/legal/terms-of-use"),
		new("Privacy", "https://www.elastic.co/legal/privacy-statement")
	];

	/// <summary>Extra legal paragraphs shown next to the links, such as trademark notices.</summary>
	public IReadOnlyList<string> Notices { get; init; } = [];

	/// <summary>Shows the consent links. Their classes are the hooks the privacy script binds to, so they must not change.</summary>
	public bool PrivacyChoices { get; init; }

	/// <summary>Branded builds drop the Elastic footer and keep only the diagnostics slot.</summary>
	public bool Branded { get; init; }

	/// <summary>The copyright line of a branded build. A branded build shows a footer only when it has one.</summary>
	public string? Copyright { get; init; }

	/// <summary>The site icon of a branded build, shown beside its copyright line.</summary>
	public string? BrandIconSrc { get; init; }

	/// <summary>Reserves room for the developer diagnostics panel so the full footer stays visible.</summary>
	public bool DiagnosticsPanel { get; init; }
}
