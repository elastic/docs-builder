// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Renders a full-bleed page hero with a product icon, title, description, and
/// up to three call-to-action buttons. All content is supplied via options -- the
/// body is unused. Release cadence lives in {whats-new}.
/// </summary>
/// <example>
/// <code>
/// :::{hero}
/// :icon: kibana
/// :title: Kibana documentation hub
/// :description: The UI for the Elasticsearch platform.
/// :primary-action: [Get started](#get-started)
/// :secondary-action: [What's new](#whats-new)
/// :tertiary-action: [Explore Kibana docs](#explore)
/// :::
/// </code>
/// </example>
public partial class HeroBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "hero";

	public string? Icon { get; private set; }
	public string? IconSvg { get; private set; }
	public string? Title { get; private set; }
	public string? Description { get; private set; }
	public string? PrimaryActionLabel { get; private set; }
	public string? PrimaryActionUrl { get; private set; }
	public string? SecondaryActionLabel { get; private set; }
	public string? SecondaryActionUrl { get; private set; }
	public string? TertiaryActionLabel { get; private set; }
	public string? TertiaryActionUrl { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Icon = Prop("icon");
		IconSvg = ProductIcons.Get(Icon);
		Title = Prop("title");
		Description = Prop("description");
		(PrimaryActionLabel, PrimaryActionUrl) = ParseAction(Prop("primary-action"));
		(SecondaryActionLabel, SecondaryActionUrl) = ParseAction(Prop("secondary-action"));
		(TertiaryActionLabel, TertiaryActionUrl) = ParseAction(Prop("tertiary-action"));

		if (string.IsNullOrWhiteSpace(Title))
			this.EmitError("{hero} requires a `:title:` option.");
	}

	private static (string? Label, string? Url) ParseAction(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return (null, null);
		var match = MarkdownLink().Match(value.Trim());
		return match.Success
			? (match.Groups["label"].Value.Trim(), match.Groups["url"].Value.Trim())
			: (null, null);
	}

	[GeneratedRegex(@"^\[(?<label>[^\]]+)\]\((?<url>[^)]+)\)$")]
	private static partial Regex MarkdownLink();
}
