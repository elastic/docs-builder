// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Renders a full-bleed page hero with product icon, title (from inner H1),
/// description, search box, version chip, quick-link pills, and an optional
/// release-status line. Designed for landing-style pages such as product hubs.
/// </summary>
/// <example>
/// <code>
/// :::{hero}
/// :icon: kibana
/// :version: v9 / Serverless (current)
/// :quick-links: Install=/install,Tutorial=/tutorial,API reference=/api,Release notes=/release-notes
/// :releases: Latest&#58; [Stack 9.4.1](/rn) (Mar 28, 2026) · [Serverless deployed](/srn) Apr 1, 2026
///
/// # Kibana
///
/// The UI for the Elasticsearch platform.
/// :::
/// </code>
/// </example>
public class HeroBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "hero";

	public string? Icon { get; private set; }
	public string? IconSvg { get; private set; }
	public string? Version { get; private set; }
	public bool ShowSearch { get; private set; }
	public IReadOnlyList<HeroQuickLink> QuickLinks { get; private set; } = [];
	public string? Releases { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Icon = Prop("icon");
		IconSvg = ProductIcons.Get(Icon);
		Version = Prop("version");
		// search defaults to true; explicit ":search: false" hides it
		ShowSearch = TryPropBool("search") ?? true;
		QuickLinks = ParseQuickLinks(Prop("quick-links"));
		Releases = Prop("releases");
	}

	private static IReadOnlyList<HeroQuickLink> ParseQuickLinks(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return [];

		var entries = new List<HeroQuickLink>();
		foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var separator = part.IndexOf('=');
			if (separator <= 0 || separator == part.Length - 1)
				continue;

			var label = part[..separator].Trim();
			var url = part[(separator + 1)..].Trim();
			if (label.Length > 0 && url.Length > 0)
				entries.Add(new HeroQuickLink(label, url));
		}

		return entries;
	}
}

public readonly record struct HeroQuickLink(string Label, string Url);
