// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// Inline table-of-contents chip listing every {card-group} and {whats-new}
/// section on the page that has a non-empty id and title. Auto-generated; no
/// options or body required.
/// </summary>
/// <example>
/// <code>
/// :::{on-this-page}
/// :::
/// </code>
/// </example>
public class OnThisPageBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "on-this-page";

	public override void FinalizeAndValidate(ParserContext context) { }

	/// <summary>
	/// Resolved at render time, since later sibling card-groups may not be
	/// validated yet at parse time.
	/// </summary>
	public IReadOnlyList<OnThisPageItem> CollectItems()
	{
		var current = (ContainerBlock)this;
		while (current.Parent is not null)
			current = current.Parent;

		var items = new List<OnThisPageItem>();
		foreach (var descendant in current.Descendants())
		{
			switch (descendant)
			{
				case CardGroupBlock g when !string.IsNullOrWhiteSpace(g.Anchor) && !string.IsNullOrWhiteSpace(g.Title):
					items.Add(new OnThisPageItem(g.Title!, g.Anchor!));
					break;
				case WhatsNewBlock w when !string.IsNullOrWhiteSpace(w.Data.Id) && !string.IsNullOrWhiteSpace(w.Data.Title):
					items.Add(new OnThisPageItem(w.Data.Title!, w.Data.Id!));
					break;
			}
		}
		return items;
	}
}

public readonly record struct OnThisPageItem(string Title, string Anchor);
