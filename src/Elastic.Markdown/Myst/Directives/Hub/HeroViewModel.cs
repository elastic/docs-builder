// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

public class HeroViewModel : HubDirectiveViewModel
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
	public IReadOnlyList<HeroAction> Actions
	{
		get
		{
			var actions = new List<HeroAction>(3);
			Add(actions, PrimaryActionLabel, PrimaryActionUrl);
			Add(actions, SecondaryActionLabel, SecondaryActionUrl);
			Add(actions, TertiaryActionLabel, TertiaryActionUrl);
			return actions;
		}
	}

	private void Add(List<HeroAction> actions, string? label, string? url)
	{
		if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(url))
			return;

		actions.Add(new HeroAction(label, url));
	}
}

/// <summary>
/// One hero call to action. The three actions carry equal weight and render as neutral
/// buttons, so the option a label came from does not change its appearance. The href and
/// its link attributes come from <see cref="HubDirectiveViewModel.LinkAttributes"/>, which
/// owns the anchor, external, and cross-link rules.
/// </summary>
public sealed record HeroAction(string Label, string Url);
