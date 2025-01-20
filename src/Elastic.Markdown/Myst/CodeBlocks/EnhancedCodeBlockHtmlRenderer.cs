// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Slices.Directives;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using RazorSlices;

namespace Elastic.Markdown.Myst.CodeBlocks;

public class EnhancedCodeBlockHtmlRenderer : HtmlObjectRenderer<EnhancedCodeBlock>
{

	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		renderer.Write(blocks[0]);
		renderer.WriteLeafRawLines(block, true, true, false);
		renderer.Write(blocks[1]);
	}
	protected override void Write(HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var callOuts = block.UniqueCallOuts;

		var slice = Code.Create(new CodeViewModel
		{
			CrossReferenceName = string.Empty,// block.CrossReferenceName,
			Language = block.Language,
			Caption = string.Empty
		});

		RenderRazorSlice(slice, renderer, block);

		if (!block.InlineAnnotations && callOuts.Count > 0)
		{
			var index = block.Parent!.IndexOf(block);
			if (index == block.Parent!.Count - 1)
				block.EmitError("Code block with annotations is not followed by any content, needs numbered list");
			else
			{
				var siblingBlock = block.Parent[index + 1];
				if (siblingBlock is not ListBlock)
					block.EmitError("Code block with annotations is not followed by a list");
				if (siblingBlock is ListBlock l && l.Count < callOuts.Count)
				{
					block.EmitError(
						$"Code block has {callOuts.Count} callouts but the following list only has {l.Count}");
				}
				else if (siblingBlock is ListBlock listBlock)
				{
					block.Parent.Remove(listBlock);
					renderer.WriteLine("<ol class=\"code-callouts\">");
					foreach (var child in listBlock)
					{
						var listItem = (ListItemBlock)child;
						var previousImplicit = renderer.ImplicitParagraph;
						renderer.ImplicitParagraph = !listBlock.IsLoose;

						renderer.EnsureLine();
						if (renderer.EnableHtmlForBlock)
						{
							renderer.Write("<li");
							renderer.WriteAttributes(listItem);
							renderer.Write('>');
						}

						renderer.WriteChildren(listItem);

						if (renderer.EnableHtmlForBlock)
							renderer.WriteLine("</li>");

						renderer.EnsureLine();
						renderer.ImplicitParagraph = previousImplicit;
					}
					renderer.WriteLine("</ol>");
				}
			}
		}
		else if (block.InlineAnnotations)
		{
			renderer.WriteLine("<ol class=\"code-callouts\">");
			foreach (var c in block.UniqueCallOuts)
			{
				renderer.WriteLine("<li>");
				renderer.WriteLine(c.Text);
				renderer.WriteLine("</li>");
			}

			renderer.WriteLine("</ol>");
		}
	}
}
