// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.Comments;
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

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderRazorSlice<T>(RazorSlice<T> slice, HtmlRenderer renderer) =>
		slice.RenderAsync(renderer.Writer).GetAwaiter().GetResult();

	/// <summary>
	/// Renders the code block lines while also removing the common indentation level.
	/// Required because EnableTrackTrivia preserves extra indentation.
	/// </summary>
	public static void RenderCodeBlockLines(HtmlRenderer renderer, EnhancedCodeBlock block)
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

			if (!hasCode && block.Language != "mermaid")
			{
				_ = renderer.Write($"<code class=\"language-{block.Language}\">");
				hasCode = true;
			}
			RenderCodeBlockLine(renderer, block, slice, i);
		}
		if (hasCode)
			_ = renderer.Write($"</code>");
	}

	private static void RenderCodeBlockLine(HtmlRenderer renderer, EnhancedCodeBlock block, StringSlice slice, int lineNumber)
	{
		var originalLength = slice.Length;
		_ = slice.TrimEnd();
		var removedSpaces = originalLength - slice.Length;
		_ = renderer.WriteEscape(slice);
		RenderCallouts(renderer, block, lineNumber, removedSpaces);
		_ = renderer.WriteLine();
	}

	private static void RenderCallouts(HtmlRenderer renderer, EnhancedCodeBlock block, int lineNumber, int indent)
	{
		var callOuts = FindCallouts(block.CallOuts, lineNumber + 1);
		foreach (var callOut in callOuts)
		{
			// This adds a span with the same width as the removed spaces
			// to ensure the callout number is aligned with the code
			_ = renderer.Write($"<span style=\"display: inline-block; width: {indent}ch\"></span>");
			_ = renderer.Write($"<span class=\"code-callout\" data-index=\"{callOut.Index}\"></span>");
		}
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

	private static bool IsCommentBlock(Block block) => block is CommentBlock;

	protected override void Write(HtmlRenderer renderer, EnhancedCodeBlock block)
	{
		if (block is AppliesToCodeBlock appliesToDirective)
		{
			RenderAppliesToHtml(renderer, appliesToDirective);
			return;
		}

		var callOuts = block.UniqueCallOuts;

		var slice = Code.Create(new CodeViewModel
		{
			CrossReferenceName = string.Empty,// block.CrossReferenceName,
			Language = block.Language,
			Caption = block.Caption,
			ApiCallHeader = block.ApiCallHeader,
			EnhancedCodeBlock = block
		});

		RenderRazorSlice(slice, renderer);
		if (!block.InlineAnnotations && callOuts.Count > 0)
		{
			var index = block.Parent!.IndexOf(block);
			if (index == block.Parent!.Count - 1)
			{
				block.EmitError("Code block with annotations is not followed by any content, needs numbered list");
				return;
			}

			var nonCommentNonListCount = 0;
			ListBlock? listBlock = null;
			var currentIndex = index + 1;

			// Process blocks between the code block and ordered list, removing comments and allowing only one non-comment block
			while (currentIndex < block.Parent.Count)
			{
				var nextBlock = block.Parent[currentIndex];

				if (nextBlock is ListBlock lb)
				{
					listBlock = lb;
					break;
				}
				else if (IsCommentBlock(nextBlock))
				{
					_ = renderer.Render(nextBlock);
					_ = block.Parent.Remove(nextBlock);
				}
				else
				{
					nonCommentNonListCount++;
					if (nonCommentNonListCount > 1)
					{
						block.EmitError("More than one content block between code block with annotations and its list");
						return;
					}

					_ = renderer.Render(nextBlock);
					_ = block.Parent.Remove(nextBlock);
				}
			}

			if (listBlock == null)
			{
				block.EmitError("Code block with annotations is not followed by a list");
				return;
			}

			if (listBlock.Count < callOuts.Count)
			{
				block.EmitError($"Code block has {callOuts.Count} callouts but the following list only has {listBlock.Count}");
				return;
			}

			_ = block.Parent.Remove(listBlock);

			_ = renderer.WriteLine("<ol class=\"code-callouts\">");
			foreach (var child in listBlock)
			{
				var listItem = (ListItemBlock)child;
				var previousImplicit = renderer.ImplicitParagraph;
				renderer.ImplicitParagraph = !listBlock.IsLoose;

				_ = renderer.EnsureLine();
				if (renderer.EnableHtmlForBlock)
				{
					_ = renderer.Write("<li");
					_ = renderer.WriteAttributes(listItem);
					_ = renderer.Write('>');
				}

				renderer.WriteChildren(listItem);

				if (renderer.EnableHtmlForBlock)
					_ = renderer.WriteLine("</li>");

				_ = renderer.EnsureLine();
				renderer.ImplicitParagraph = previousImplicit;
			}
			_ = renderer.WriteLine("</ol>");
		}
		else if (block.InlineAnnotations)
		{
			_ = renderer.WriteLine("<ol class=\"code-callouts\">");
			foreach (var c in block.UniqueCallOuts)
			{
				_ = renderer.WriteLine("<li>");
				_ = renderer.WriteLine(c.Text);
				_ = renderer.WriteLine("</li>");
			}

			_ = renderer.WriteLine("</ol>");
		}
	}

	[SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]
	private static void RenderAppliesToHtml(HtmlRenderer renderer, AppliesToCodeBlock appliesToCodeBlock)
	{
		var appliesTo = appliesToCodeBlock.AppliesTo;
		var slice = ApplicableToDirective.Create(appliesTo);
		if (appliesTo is null || appliesTo == FrontMatter.ApplicableTo.All)
			return;
		slice.RenderAsync(renderer.Writer).GetAwaiter().GetResult();
	}
}
