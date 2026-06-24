// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Renders a full-bleed page hero with product icon, title, description, and an
/// optional release-status line. All content is supplied via options -- the body
/// is unused.
/// </summary>
/// <example>
/// <code>
/// :::{hero}
/// :icon: kibana
/// :title: Kibana
/// :description: The UI for the Elasticsearch platform.
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
	public string? Releases { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Icon = Prop("icon");
		IconSvg = ProductIcons.Get(Icon);
		Title = Prop("title");
		Description = Prop("description");
		Releases = Prop("releases");

		if (string.IsNullOrWhiteSpace(Title))
			this.EmitError("{hero} requires a `:title:` option.");
	}
}
