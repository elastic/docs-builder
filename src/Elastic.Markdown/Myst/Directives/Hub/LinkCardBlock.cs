// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// A single card with a required title and link, plus an optional badge and
/// description body. Designed to live inside a <see cref="CardGroupBlock"/>
/// but rendered standalone if used outside one.
/// </summary>
/// <example>
/// <code>
/// :::{link-card} Self-managed
/// :link: /deploy-manage/deploy/self-managed
/// :badge: 9.0
/// Run Elasticsearch on your own infrastructure.
/// :::
/// </code>
/// </example>
public class LinkCardBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context), IBlockTitle
{
	public override string Directive => "link-card";

	public string Title { get; private set; } = default!;
	public string? Link { get; private set; }
	public string? Badge { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		if (string.IsNullOrWhiteSpace(Arguments))
			this.EmitError("{link-card} requires a title argument, e.g. `:::{link-card} My title`.");

		Title = (Arguments ?? "{undefined}").ReplaceSubstitutions(context);
		Link = Prop("link");
		Badge = Prop("badge");

		if (string.IsNullOrWhiteSpace(Link))
			this.EmitError("{link-card} requires a `:link:` option.");
	}
}
