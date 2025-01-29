// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Slices.Directives;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using RazorSlices;

namespace Elastic.Markdown.Myst.CodeBlocks;

public class EnhancedCodeBlockHtmlRenderer : HtmlObjectRenderer<EnhancedCodeBlock>
{
	private const int TabWidth = 4;

	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var html = slice.RenderAsync().GetAwaiter().GetResult();
		var blocks = html.Split("[CONTENT]", 2, StringSplitOptions.RemoveEmptyEntries);
		renderer.Write(blocks[0]);
		RenderCodeBlockLines(renderer, block);
		renderer.Write(blocks[1]);
	}

	/// <summary>
	/// Renders the code block lines while also removing the common indentation level.
	/// Required because EnableTrackTrivia preserves extra indentation.
	/// </summary>
	private static void RenderCodeBlockLines(HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var commonIndent = GetCommonIndent(block);
		var hasCode = false;
		for (var i = 0; i < block.Lines.Count; i++)
		{
			var line = block.Lines.Lines[i];
			var slice = line.Slice;
			//ensure we never emit an empty line at beginning or start
			if ((i == 0 || i == block.Lines.Count - 1) && line.Slice.IsEmptyOrWhitespace())
				continue;
			var indent = CountIndentation(slice);
			if (indent >= commonIndent)
				slice.Start += commonIndent;

			if (!hasCode)
			{
				renderer.Write($"<code class=\"language-{block.Language}\">");
				hasCode = true;
			}
			RenderCodeBlockLine(renderer, block, slice, i);
		}
		if (hasCode)
			renderer.Write($"</code>");
	}

	private static void RenderCodeBlockLine(HtmlRenderer renderer, EnhancedCodeBlock block, StringSlice slice, int lineNumber)
	{
		renderer.WriteEscape(slice);
		RenderCallouts(renderer, block, lineNumber);
		renderer.WriteLine();
	}

	private static void RenderCallouts(HtmlRenderer renderer, EnhancedCodeBlock block, int lineNumber)
	{
		var callOuts = FindCallouts(block.CallOuts ?? [], lineNumber + 1);
		foreach (var callOut in callOuts)
			renderer.Write($"<span class=\"code-callout\" data-index=\"{callOut.Index}\">{callOut.Index}</span>");
	}

	private static IEnumerable<CallOut> FindCallouts(
		IEnumerable<CallOut> callOuts,
		int lineNumber
	) => callOuts.Where(callOut => callOut.Line == lineNumber);

	private static int GetCommonIndent(EnhancedCodeBlock block)
	{
		var commonIndent = int.MaxValue;
		for (var i = 0; i < block.Lines.Count; i++)
		{
			var line = block.Lines.Lines[i].Slice;
			if (line.IsEmptyOrWhitespace())
				continue;
			var indent = CountIndentation(line);
			commonIndent = Math.Min(commonIndent, indent);
		}
		return commonIndent;
	}


	private static int CountIndentation(StringSlice slice)
	{
		var indentCount = 0;
		for (var i = slice.Start; i <= slice.End; i++)
		{
			var c = slice.Text[i];
			if (c == ' ')
				indentCount++;
			else if (c == '\t')
				indentCount += TabWidth;
			else
				break;
		}
		return indentCount;
	}

	protected override void Write(HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		var callOuts = block.UniqueCallOuts;

		var slice = Code.Create(new CodeViewModel
		{
			CrossReferenceName = string.Empty,// block.CrossReferenceName,
			Language = block.Language,
			Caption = block.Caption,
			ApiCallHeader = block.ApiCallHeader
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
