// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// The "Explore {product}" hub section: a titled band that houses a stack of
/// <see cref="CardGroupBlock"/> children rendered as collapsible accordion groups.
/// Nested card-groups and their link-cards detect this ancestor and switch to
/// their accordion/column rendering.
/// </summary>
/// <example>
/// <code>
/// :::::{explore}
/// :title: Explore Kibana
/// :intro: Explore the apps and capabilities that help you act on your data.
///
/// ::::{card-group}
/// :title: Install & admin
/// ... link-cards ...
/// ::::
/// :::::
/// </code>
/// </example>
public class ExploreBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "explore";

	public string? Title { get; private set; }
	public string? Intro { get; private set; }
	public string? Anchor { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Prop("title");
		Intro = Prop("intro");
		Anchor = Prop("id");

		if (string.IsNullOrWhiteSpace(Title))
			this.EmitError("{explore} requires a `:title:` option.");
	}

	public override IEnumerable<string> GeneratedAnchors =>
		string.IsNullOrWhiteSpace(Anchor) ? [] : [Anchor];
}
