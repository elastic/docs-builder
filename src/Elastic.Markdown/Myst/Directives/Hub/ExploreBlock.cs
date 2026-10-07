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
/// :level: 2
///
/// ::::{card-group}
/// :title: Install & admin
/// ... link-cards ...
/// ::::
/// :::::
/// </code>
/// </example>
public class ExploreBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "explore";

	public string? Title { get; private set; }
	public string? Intro { get; private set; }
	public string? Anchor { get; private set; }

	/// <summary>
	/// Heading level of the section title: 2 (default) or 3. The accordions and link columns inside
	/// render one and two levels below it, so a nested section keeps a complete page outline.
	/// </summary>
	public int Level { get; private set; } = 2;

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Prop("title");
		Intro = Prop("intro");
		Anchor = Prop("id");
		Level = ParseLevel(Prop("level"));

		if (string.IsNullOrWhiteSpace(Title))
			this.EmitError("{explore} requires a `:title:` option.");
	}

	private int ParseLevel(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return 2;

		switch (value.Trim())
		{
			case "2":
				return 2;
			case "3":
				return 3;
			default:
				this.EmitWarning($"Invalid {{explore}} level '{value}'. Valid levels are: 2, 3. Defaulting to 2.");
				return 2;
		}
	}

	public override IEnumerable<string> GeneratedAnchors => string.IsNullOrWhiteSpace(Anchor) ? [] : [Anchor];
}
