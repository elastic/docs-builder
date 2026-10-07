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
public class ExploreBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "explore";

	public string? Title { get; private set; }
	public string? Intro { get; private set; }
	public string? Anchor { get; private set; }

	/// <summary>Which accordions render open on load. Defaults to <see cref="ExploreMode.Collapsed"/>.</summary>
	public ExploreMode Mode { get; private set; } = ExploreMode.Collapsed;

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Prop("title");
		Intro = Prop("intro");
		Anchor = Prop("id");
		Mode = ParseMode(Prop("mode"));

		if (string.IsNullOrWhiteSpace(Title))
			this.EmitError("{explore} requires a `:title:` option.");
	}

	private ExploreMode ParseMode(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return ExploreMode.Collapsed;

		switch (value.Trim().ToLowerInvariant())
		{
			case "collapsed":
				return ExploreMode.Collapsed;
			case "first":
				return ExploreMode.First;
			case "expanded":
				return ExploreMode.Expanded;
			default:
				this.EmitWarning(
					$"Invalid {{explore}} mode '{value}'. Valid modes are: collapsed, first, expanded. Defaulting to 'collapsed'."
				);
				return ExploreMode.Collapsed;
		}
	}

	public override IEnumerable<string> GeneratedAnchors => string.IsNullOrWhiteSpace(Anchor) ? [] : [Anchor];
}

/// <summary>Controls which accordions in an <see cref="ExploreBlock"/> render open on load.</summary>
public enum ExploreMode
{
	/// <summary>Every accordion is closed.</summary>
	Collapsed,

	/// <summary>Only the first accordion is open.</summary>
	First,

	/// <summary>Every accordion is open.</summary>
	Expanded
}
