// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Renders a full-bleed page hero with product icon, title, description,
/// search box, version chip, quick-link pills, and an optional release-status
/// line. All content is supplied via options -- the body is unused.
/// </summary>
/// <example>
/// <code>
/// :::{hero}
/// :icon: kibana
/// :title: Kibana
/// :description: The UI for the Elasticsearch platform.
/// :version: v9 / Serverless (current)
/// :quick-links: Install=/install,Tutorial=/tutorial
/// :releases: Latest&#58; [Stack 9.4.1](/rn) (Mar 28, 2026)
/// :::
/// </code>
/// </example>
public class HeroBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "hero";

	public string? Icon { get; private set; }
	public string? IconSvg { get; private set; }
	public string? Title { get; private set; }
	public string? Description { get; private set; }
	public string? Version { get; private set; }
	public bool ShowSearch { get; private set; }
	public IReadOnlyList<HeroQuickLink> QuickLinks { get; private set; } = [];
	public IReadOnlyList<HeroVersion> OtherVersions { get; private set; } = [];
	public string? Releases { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Icon = Prop("icon");
		IconSvg = ProductIcons.Get(Icon);
		Title = Prop("title");
		Description = Prop("description");
		Version = Prop("version");
		// search defaults to true; explicit ":search: false" hides it
		ShowSearch = TryPropBool("search") ?? true;
		QuickLinks = ParsePairs(Prop("quick-links"), allowEmptyUrl: false)
			.Select(p => new HeroQuickLink(p.Label, p.Url!)).ToList();
		OtherVersions = ParsePairs(Prop("versions"), allowEmptyUrl: true)
			.Select(p => new HeroVersion(p.Label, p.Url)).ToList();
		Releases = Prop("releases");

		if (string.IsNullOrWhiteSpace(Title))
			this.EmitError("{hero} requires a `:title:` option.");
	}

	private static IReadOnlyList<(string Label, string? Url)> ParsePairs(string? raw, bool allowEmptyUrl)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return [];

		var entries = new List<(string, string?)>();
		foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var separator = part.IndexOf('=');
			string label;
			string? url;
			if (separator < 0)
			{
				if (!allowEmptyUrl)
					continue;
				label = part.Trim();
				url = null;
			}
			else
			{
				label = part[..separator].Trim();
				url = part[(separator + 1)..].Trim();
				if (string.IsNullOrEmpty(url))
				{
					if (!allowEmptyUrl)
						continue;
					url = null;
				}
			}
			if (label.Length > 0)
				entries.Add((label, url));
		}

		return entries;
	}
}

public readonly record struct HeroQuickLink(string Label, string Url);

public readonly record struct HeroVersion(string Label, string? Url);
