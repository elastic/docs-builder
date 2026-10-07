// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.Hub;

/// <summary>
/// A row of in-page links to the hub sections that render as an H2, placed under the hero.
/// The links are collected from the page, so the directive takes no options and no body.
/// </summary>
/// <example>
/// <code>
/// :::{on-this-page}
/// :::
/// </code>
/// </example>
public class OnThisPageBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	/// <summary>The id that <c>{get-started}</c> renders on its section.</summary>
	private const string GetStartedAnchor = "get-started";

	public override string Directive => "on-this-page";

	public override void FinalizeAndValidate(ParserContext context) { }

	/// <summary>
	/// Resolved at render time, because sections after this block are not validated yet when
	/// this block is parsed.
	/// </summary>
	public IReadOnlyList<OnThisPageItem> CollectItems()
	{
		var root = (ContainerBlock)this;
		while (root.Parent is not null)
			root = root.Parent;

		var items = new List<OnThisPageItem>();
		foreach (var descendant in root.Descendants())
		{
			switch (descendant)
			{
				case GetStartedBlock g when !string.IsNullOrWhiteSpace(g.Data.Title):
					items.Add(new OnThisPageItem(g.Data.Title, GetStartedAnchor));
					break;
				case WhatsNewBlock w when !string.IsNullOrWhiteSpace(w.Data.Id) && !string.IsNullOrWhiteSpace(w.Data.Title):
					items.Add(new OnThisPageItem(w.Data.Title, w.Data.Id));
					break;
				// A card group only renders an H2 on its own. Inside {explore} it is an accordion.
				case CardGroupBlock c when HubExplore.FindAncestor(c) is null
					&& !string.IsNullOrWhiteSpace(c.Anchor)
					&& !string.IsNullOrWhiteSpace(c.Title):
					items.Add(new OnThisPageItem(c.Title, c.Anchor));
					break;
				case ExploreBlock e when e.Level == 2 && !string.IsNullOrWhiteSpace(e.Anchor) && !string.IsNullOrWhiteSpace(e.Title):
					items.Add(new OnThisPageItem(e.Title, e.Anchor));
					break;
			}
		}
		return items;
	}
}

public readonly record struct OnThisPageItem(string Title, string Anchor);
