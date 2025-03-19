// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using Elastic.Markdown.Myst.Substitution;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Slices;

public interface IDescriptionGenerator
{
	string GenerateDescription(MarkdownDocument document);
}


public class DescriptionGenerator : IDescriptionGenerator
{
	private const int MaxLength = 150;

	public string GenerateDescription(MarkdownDocument document)
	{
		var description = new StringBuilder();
		foreach (var block in document.TakeWhile(_ => description.Length < MaxLength))
		{
			// TODO: Add support for IncludeBlock
			// This is needed when the first block is an IncludeBlock.
			switch (block)
			{
				case ParagraphBlock paragraph:
					{
						ProcessParagraph(paragraph, description);
						break;
					}
				case ListBlock listBlock:
					{
						ProcessListBlock(listBlock, description);
						break;
					}
			}
		}

		var result = description.ToString().TrimEnd('.');
		// It can happen that the last parsed block is longer, hence the result is longer than maxLength
		// Hence we need to shorten it. In this case it will be shorted to until the next space after `MaxLength`
		if (result.Length > MaxLength)
		{
			var endIndex = result.IndexOf(' ', MaxLength - 1);
			if (endIndex == -1)
				endIndex = MaxLength;
			result = string.Concat(result.AsSpan(0, endIndex + 1).Trim(), "...");
		}

		return result;
	}

	private static void ProcessParagraph(ParagraphBlock paragraph, StringBuilder description)
	{
		if (paragraph.Inline == null)
			return;

		var paragraphText = GetInlineText(paragraph.Inline);
		if (string.IsNullOrEmpty(paragraphText))
			return;

		_ = description.Append(paragraphText);
		_ = description.Append(' ');
	}

	private static void ProcessListBlock(ListBlock listBlock, StringBuilder description)
	{
		foreach (var item in listBlock)
		{
			if (item is not ListItemBlock listItem)
				continue;

			foreach (var listItemBlock in listItem)
			{
				if (listItemBlock is not ParagraphBlock listItemParagraph || listItemParagraph.Inline == null)
					continue;

				var paragraphText = GetInlineText(listItemParagraph.Inline);
				_ = description.Append(paragraphText);
				var lastChar = paragraphText[^1];
				if (lastChar is not '.' and not ',' and not '!' and not '?')
					_ = description.Append(listItem == listBlock.LastChild ? ". " : ", ");
			}
		}
	}

	private static string GetInlineText(ContainerInline inline)
	{
		var builder = new StringBuilder();
		foreach (var item in inline)
		{
			switch (item)
			{
				case SubstitutionLeaf subs:
					_ = builder.Append(subs.Replacement);
					break;
				case LiteralInline literal:
					_ = builder.Append(literal.Content.ToString());
					break;
				case EmphasisInline emphasis:
					_ = builder.Append(GetInlineText(emphasis));
					break;
				case LinkInline link:
					_ = builder.Append(GetInlineText(link));
					break;
				case CodeInline code:
					_ = builder.Append(code.Content);
					break;
				case LineBreakInline:
					_ = builder.Append(' ');
					break;
				case ContainerInline container:
					_ = builder.Append(GetInlineText(container));
					break;
			}
		}
		return builder.ToString();
	}
}
