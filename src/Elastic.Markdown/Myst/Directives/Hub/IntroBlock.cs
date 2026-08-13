// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// A small "getting started" callout panel shown between the hero and the first
/// section. The body is rendered as inline markdown so authors can use bold,
/// links, and emphasis. Visually a white rounded panel with a teal accent bar.
/// </summary>
/// <example>
/// <code>
/// :::{intro}
/// **New to Kibana?** [Learn data exploration and visualization](/learn), a hands-on
/// walk-through of Discover, ES|QL, visualizations, and dashboards.
/// :::
/// </code>
/// </example>
public class IntroBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "intro";

	public override void FinalizeAndValidate(ParserContext context) { }
}
