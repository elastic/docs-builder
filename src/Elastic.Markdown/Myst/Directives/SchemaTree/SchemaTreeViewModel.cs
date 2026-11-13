// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.AspNetCore.Html;

namespace Elastic.Markdown.Myst.Directives.SchemaTree;

public sealed record SchemaTreeNode(HtmlString Label, HtmlString Description, IReadOnlyList<SchemaTreeNode> Children);

public class SchemaTreeViewModel : DirectiveViewModel
{
	public string Label => ((SchemaTreeBlock)DirectiveBlock).Arguments ?? "Schema";
	public IReadOnlyList<SchemaTreeNode> Nodes =>
		((SchemaTreeBlock)DirectiveBlock).Tree is { } tree
			? tree.OfType<ListItemBlock>().Where(item => item.Count > 0 && item[0] is ParagraphBlock).Select(BuildNode).ToArray()
			: [];

	private static SchemaTreeNode BuildNode(ListItemBlock item) =>
		new(
			Render(renderer => renderer.WriteLeafInline((ParagraphBlock)item[0])),
			Render(renderer =>
			{
				foreach (var block in item.Skip(1).Where(block => block is not ListBlock))
					_ = renderer.Render(block);
			}),
			item
				.OfType<ListBlock>()
				.SelectMany(list => list.OfType<ListItemBlock>())
				.Where(child => child.Count > 0 && child[0] is ParagraphBlock)
				.Select(BuildNode)
				.ToArray()
		);

	private static HtmlString Render(Action<HtmlRenderer> write)
	{
		var subscription = DocumentationObjectPoolProvider.HtmlRendererPool.Get();
		try
		{
			write(subscription.HtmlRenderer);
			return new HtmlString(subscription.RentedStringBuilder?.ToString() ?? string.Empty);
		}
		finally
		{
			DocumentationObjectPoolProvider.HtmlRendererPool.Return(subscription);
		}
	}
}
