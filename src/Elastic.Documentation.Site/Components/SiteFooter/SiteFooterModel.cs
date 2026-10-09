// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.SiteFooter;

public sealed record FooterLink(string Label, string Href);

public sealed record SiteFooterModel
{
	public required string LogoSrc { get; init; }
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

	/// <summary>Reserves room for the developer diagnostics panel so the full footer stays visible.</summary>
	public bool DiagnosticsPanel { get; init; }
}
