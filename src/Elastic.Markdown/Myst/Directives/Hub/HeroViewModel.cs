// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class HeroViewModel : DirectiveViewModel
{
	public required string? IconKey { get; init; }
	public required string? IconSvg { get; init; }
	public required string? Title { get; init; }
	public required string? DescriptionHtml { get; init; }
	public required string? PrimaryActionLabel { get; init; }
	public required string? PrimaryActionUrl { get; init; }
	public required string? SecondaryActionLabel { get; init; }
	public required string? SecondaryActionUrl { get; init; }
	public required string? TertiaryActionLabel { get; init; }
	public required string? TertiaryActionUrl { get; init; }
	public required string? SitePathPrefix { get; init; }
	public string? PrefixUrl(string? url) => HubUrl.Prefix(url, SitePathPrefix);

	public IReadOnlyList<HeroAction> Actions
	{
		get
		{
			var actions = new List<HeroAction>(3);
			Add(actions, PrimaryActionLabel, PrimaryActionUrl, isPrimary: true);
			Add(actions, SecondaryActionLabel, SecondaryActionUrl, isPrimary: false);
			Add(actions, TertiaryActionLabel, TertiaryActionUrl, isPrimary: false);
			return actions;
		}
	}

	private static void Add(List<HeroAction> actions, string? label, string? url, bool isPrimary)
	{
		if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(url))
			actions.Add(new HeroAction(label, url, isPrimary, url[0] == '#'));
	}
}

public sealed record HeroAction(string Label, string Url, bool IsPrimary, bool IsAnchor);
