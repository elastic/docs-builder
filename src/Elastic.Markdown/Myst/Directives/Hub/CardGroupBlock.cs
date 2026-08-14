// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Container directive that renders a titled section housing a grid of
/// <see cref="LinkCardBlock"/> children. Generic and reusable wherever a
/// linked-card grid is appropriate.
/// </summary>
/// <example>
/// <code>
/// ::::{card-group}
/// :title: Install and deploy
/// :intro: Set up Elasticsearch on your platform of choice.
/// :id: install
///
/// :::{link-card} Self-managed
/// :link: /deploy-manage/deploy/self-managed
/// Run on your own infrastructure.
/// :::
/// ::::
/// </code>
/// </example>
public class CardGroupBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "card-group";

	public string? Title { get; private set; }
	public string? Intro { get; private set; }
	public string? Anchor { get; private set; }
	public string? Variant { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Prop("title");
		Intro = Prop("intro");
		Anchor = Prop("id");
		Variant = Prop("variant");
	}

	public override IEnumerable<string> GeneratedAnchors =>
		string.IsNullOrWhiteSpace(Anchor) ? [] : [Anchor];
}
