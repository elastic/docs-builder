// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.CallOutCode;

public static class CallOutCodeBuilderExtensions
{
	public static MarkdownPipelineBuilder UseCallOutAwareCodeBlocks(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<CallOutCodeMarkdownExtension>();
		return pipeline;
	}
}

/// <summary>
/// Extension to allow custom containers.
/// </summary>
/// <seealso cref="IMarkdownExtension" />
public class CallOutCodeMarkdownExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		pipeline.BlockParsers.Replace<FencedCodeBlockParser>(new CallOutAwareFencedCodeBlockParser());
	}

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (!renderer.ObjectRenderers.Contains<CodeBlockRenderer>())
		{
			// Must be inserted before CodeBlockRenderer
			renderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new DirectiveHtmlRenderer());
		}

		renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new CallOutCodeRenderer());
	}
}

public class CallOutCodeRenderer : HtmlObjectRenderer<CodeBlockWithCallOuts>
{
	protected override void Write(HtmlRenderer renderer, CodeBlockWithCallOuts block)
	{
		var callOuts = block.CallOuts ?? [];

		renderer.WriteLine("<code><pre>");
		renderer.WriteLeafRawLines(block, true, false, false);
		renderer.WriteLine("</pre></code>");

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
				if (siblingBlock is ListBlock l && l.Count != callOuts.Count)
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
			foreach (var c in block.CallOuts ?? [])
			{
				renderer.WriteLine("<li>");
				renderer.WriteLine(c.Text);
				renderer.WriteLine("</li>");
			}

			renderer.WriteLine("</ol>");
		}
	}
}
