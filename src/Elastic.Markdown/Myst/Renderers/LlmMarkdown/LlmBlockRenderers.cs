// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.Directives.Admonition;
using Elastic.Markdown.Myst.Directives.Image;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Myst.Directives.Tabs;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using CodeBlock = Markdig.Syntax.CodeBlock;

namespace Elastic.Markdown.Myst.Renderers.LlmMarkdown;

public static class LlmRenderingHelpers
{
	public static void RenderBlockWithIndentation(LlmMarkdownRenderer renderer, MarkdownObject block, string indentation = "  ")
	{
		var content = DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(renderer.BuildContext, block, static (tmpRenderer, obj) =>
		{
			_ = tmpRenderer.Render(obj);
		});

		if (string.IsNullOrEmpty(content))
			return;
		var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
		foreach (var line in lines)
		{
			renderer.Write(indentation);
			renderer.WriteLine(line);
		}
	}

	/// <summary>
	/// Converts relative URLs to absolute URLs using BuildContext.CanonicalBaseUrl for better LLM consumption
	/// </summary>
	public static string? MakeAbsoluteUrl(LlmMarkdownRenderer renderer, string? url)
	{
		if (
			string.IsNullOrEmpty(url)
			|| renderer.BuildContext.CanonicalBaseUrl == null
			|| Uri.IsWellFormedUriString(url, UriKind.Absolute)
			|| !Uri.IsWellFormedUriString(url, UriKind.Relative))
			return url;
		try
		{
			var baseUri = renderer.BuildContext.CanonicalBaseUrl;
			var absoluteUri = new Uri(baseUri, url);
			return absoluteUri.ToString();
		}
		catch
		{
			return url;
		}
	}
}

/// <summary>
/// Skips rendering YAML frontmatter blocks to prevent them from appearing as visible content in LLM output
/// </summary>
public class LlmYamlFrontMatterRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, YamlFrontMatterBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, YamlFrontMatterBlock obj)
	{
		// Intentionally skip rendering YAML frontmatter - it should not appear in LLM output
		// The frontmatter content is currently handled in LlmMarkdownExporter.cs
		// TODO: Handle YAML frontmatter in LLM output here
	}
}

public class LlmHeadingRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, HeadingBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, HeadingBlock obj)
	{
		renderer.EnsureBlockSpacing();
		renderer.WriteLine();

		var headingText = ExtractHeadingText(obj);

		renderer.Write(new string('#', obj.Level));
		renderer.Write(" ");
		renderer.WriteLine(headingText);
	}

	private static string ExtractHeadingText(HeadingBlock heading)
	{
		if (heading.Inline == null)
			return string.Empty;
		return heading.Inline.Descendants()
			.OfType<LiteralInline>()
			.Select(l => l.Content.ToString())
			.Aggregate(string.Empty, (current, text) => current + text);
	}
}

public class LlmParagraphRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, ParagraphBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, ParagraphBlock obj)
	{
		// Only add newline if the paragraph is not within an element
		if (obj.Parent is MarkdownDocument)
			renderer.EnsureBlockSpacing();
		renderer.WriteLeafInline(obj);
		renderer.EnsureLine();
	}
}

public class LlmEnhancedCodeBlockRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, EnhancedCodeBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, EnhancedCodeBlock obj)
	{
		renderer.EnsureBlockSpacing();
		if (!string.IsNullOrEmpty(obj.Caption))
		{
			renderer.Write("<!-- Caption: ");
			renderer.Write(obj.Caption);
			renderer.WriteLine(" -->");
		}
		renderer.Write("```");
		if (!string.IsNullOrEmpty(obj.Language))
			renderer.Write(obj.Language);
		renderer.WriteLine();
		var lastNonEmptyIndex = GetLastNonEmptyLineIndex(obj);
		for (var i = 0; i <= lastNonEmptyIndex; i++)
		{
			var line = obj.Lines.Lines[i];
			renderer.Write(line.ToString());
			renderer.WriteLine();
		}
		renderer.WriteLine("```");
	}

	private static int GetLastNonEmptyLineIndex(EnhancedCodeBlock obj)
	{
		var lastNonEmptyIndex = obj.Lines.Lines.Length - 1;
		while (lastNonEmptyIndex >= 0 && string.IsNullOrWhiteSpace(obj.Lines.Lines[lastNonEmptyIndex].ToString()))
			lastNonEmptyIndex--;
		return lastNonEmptyIndex;
	}
}

/// <summary>
/// Renders lists as standard CommonMark lists using trivia information to preserve original indentation
/// </summary>
public class LlmListRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, ListBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, ListBlock listBlock)
	{
		var baseIndent = CalculateNestedIndentation(listBlock);
		if (listBlock.Parent is not ListItemBlock)
			renderer.EnsureBlockSpacing();

		var isOrdered = listBlock.IsOrdered;
		var itemIndex = 1;
		if (isOrdered && int.TryParse(listBlock.DefaultOrderedStart, out var startIndex))
			itemIndex = startIndex;

		foreach (var item in listBlock.Cast<ListItemBlock>())
		{
			renderer.Write(baseIndent);
			renderer.Write(isOrdered ? $"{itemIndex}. " : "- ");
			foreach (var block in item)
			{
				if (block != item.First())
				{
					var continuationIndent = GetContinuationIndent(baseIndent, isOrdered);
					renderer.Write(continuationIndent);
				}

				if (block != item.First() && block is ListBlock)
					renderer.WriteLine();
				RenderBlockWithIndentation(renderer, block, baseIndent, isOrdered);
			}

			renderer.EnsureLine();
			if (isOrdered)
				itemIndex++;
		}
	}

	private static string GetContinuationIndent(string baseIndent, bool isOrdered) =>
		baseIndent + new string(' ', isOrdered ? 3 : 2);

	private static void RenderBlockWithIndentation(LlmMarkdownRenderer renderer, Block block, string baseIndent, bool isOrdered)
	{
		var blockOutput = DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(renderer.BuildContext, block, static (tmpRenderer, obj) =>
		{
			_ = tmpRenderer.Render(obj);
		});
		var continuationIndent = GetContinuationIndent(baseIndent, isOrdered);
		var lines = blockOutput.Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (i == 0)
				renderer.Write(line);
			else if (!string.IsNullOrWhiteSpace(line))
			{
				renderer.WriteLine();
				renderer.Write(continuationIndent);
				renderer.Write(block is CodeBlock ? line : line.TrimStart());
			}
			else if (i < lines.Length - 1)
				renderer.WriteLine();
		}
	}

	/// <summary>
	/// Calculates the proper indentation for a nested list by traversing up the parent chain
	/// and accounting for parent list marker widths
	/// Used as fallback when trivia information is not available
	/// </summary>
	private static string CalculateNestedIndentation(ListBlock listBlock)
	{
		var indentation = "";
		var parent = listBlock.Parent;
		while (parent != null)
		{
			if (parent is ListItemBlock { Parent: ListBlock parentList })
			{
				var markerWidth = parentList.IsOrdered ? 3 : 2; // Default widths
				indentation = new string(' ', markerWidth) + indentation;
			}

			parent = parent.Parent;
		}

		return indentation;
	}
}

/// <summary>
/// Renders quote blocks as standard CommonMark blockquotes
/// </summary>
public class LlmQuoteBlockRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, QuoteBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, QuoteBlock obj)
	{
		foreach (var block in obj)
		{
			renderer.Writer.Write("> ");
			_ = renderer.Render(block);
		}

		renderer.EnsureLine();
	}
}

public class LlmThematicBreakRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, ThematicBreakBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, ThematicBreakBlock obj)
	{
		renderer.EnsureBlockSpacing();
		renderer.Writer.WriteLine("---");
		renderer.EnsureLine();
	}
}

/// <summary>
/// Renders tables as standard CommonMark tables with improved formatting for readability
/// </summary>
public class LlmTableRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, Table>
{
	protected override void Write(LlmMarkdownRenderer renderer, Table table)
	{
		renderer.EnsureBlockSpacing();
		renderer.Writer.WriteLine();

		// Calculate column widths for better alignment
		var columnWidths = CalculateColumnWidths(renderer, table);

		// Render table header
		if (table.Count > 0 && table[0] is TableRow headerRow)
		{
			RenderTableRowCells(renderer, headerRow, columnWidths);

			// Render separator row with proper alignment
			renderer.Writer.Write("|");
			for (var i = 0; i < headerRow.Count; i++)
			{
				renderer.Writer.Write(new string('-', columnWidths[i] + 2));
				renderer.Writer.Write("|");
			}

			renderer.WriteLine();
		}

		// Render table body with aligned columns
		foreach (var row in table.Skip(1).Cast<TableRow>())
		{
			RenderTableRowCells(renderer, row, columnWidths);
		}
	}

	/// <summary>
	/// Renders a table row with proper cell alignment and padding
	/// </summary>
	private static void RenderTableRowCells(LlmMarkdownRenderer renderer, TableRow row, int[] columnWidths)
	{
		renderer.Writer.Write("|");
		var cellIndex = 0;
		foreach (var cell in row.Cast<TableCell>())
		{
			renderer.Writer.Write(" ");
			var content = RenderTableCellContent(renderer, cell);
			renderer.Writer.Write(content.PadRight(columnWidths[cellIndex]));
			renderer.Writer.Write(" |");
			cellIndex++;
		}
		renderer.WriteLine();
	}

	/// <summary>
	/// Calculate the optimal width for each column based on content
	/// </summary>
	private static int[] CalculateColumnWidths(LlmMarkdownRenderer renderer, Table table)
	{
		if (table.Count == 0)
			return [];

		// Initialize array with column count from header row
		var headerRow = table[0] as TableRow;
		var columnCount = headerRow?.Count ?? 0;
		var widths = new int[columnCount];

		// Process all rows to find maximum width for each column
		foreach (var row in table.Cast<TableRow>())
		{
			for (var cellIndex = 0; cellIndex < row.Count; cellIndex++)
			{
				var cell = row[cellIndex] as TableCell;
				// Capture cell content
				var content = RenderTableCellContent(renderer, cell!);

				// Update width if this cell is wider
				widths[cellIndex] = Math.Max(widths[cellIndex], content.Length);
			}
		}

		return widths;
	}

	/// <summary>
	/// Renders the inline content of a table cell to plain text
	/// </summary>
	private static string RenderTableCellContent(LlmMarkdownRenderer renderer, TableCell cell) =>
		DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(
			renderer.BuildContext,
			cell.Descendants().OfType<Inline>(),
			static (tmpRenderer, obj) =>
			{
				foreach (var inline in obj)
					tmpRenderer.Write(inline);
			});
}

public class LlmDirectiveRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, DirectiveBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, DirectiveBlock obj)
	{
		switch (obj)
		{
			case ImageBlock imageBlock:
				WriteImageBlock(renderer, imageBlock);
				return;
			case IncludeBlock includeBlock:
				WriteIncludeBlock(renderer, includeBlock);
				return;
		}

		// Ensure single empty line before directive
		renderer.EnsureBlockSpacing();

		// Convert directive to structured comment that LLMs can understand
		renderer.Writer.Write("<");
		renderer.Writer.Write(obj.Directive);

		switch (obj)
		{
			case DropdownBlock dropdown:
				renderer.Writer.Write($" title=\"{dropdown.Title}\"");
				break;
			case TabItemBlock tabItem:
				renderer.Writer.Write($" title=\"{tabItem.Title}\"");
				break;
			case AdmonitionBlock admonition when obj.Directive is "admonition":
				renderer.Writer.Write($" title=\"{admonition.Title}\"");
				break;
		}

		renderer.WriteLine(">");
		renderer.EnsureLine();

		// Render directive content as regular markdown with indentation
		WriteChildrenWithIndentation(renderer, obj, "  ");

		// Add clear end marker
		renderer.EnsureLine();
		renderer.Writer.Write("</");
		renderer.Writer.Write(obj.Directive);
		renderer.Writer.WriteLine(">");
		renderer.EnsureLine();
	}

	private static void WriteImageBlock(LlmMarkdownRenderer renderer, ImageBlock imageBlock)
	{
		renderer.EnsureBlockSpacing();

		// Make image URL absolute for better LLM consumption
		var absoluteImageUrl = LlmRenderingHelpers.MakeAbsoluteUrl(renderer, imageBlock.ImageUrl);
		renderer.WriteLine($"![{imageBlock.Alt}]({absoluteImageUrl})");
		renderer.EnsureLine();
	}

	private void WriteIncludeBlock(LlmMarkdownRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
		{
			renderer.BuildContext.Collector.EmitError(block.IncludePath ?? string.Empty, "File not found or invalid path");
			return;
		}

		renderer.EnsureLine();

		// Get the file content
		var snippet = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);
		if (!snippet.Exists)
		{
			renderer.BuildContext.Collector.EmitError(block.IncludePath ?? string.Empty, "File not found or invalid path");
			return;
		}

		// Handle differently based on whether it's a literal include or regular include
		if (block.Literal)
		{
			// For literal includes, output the content as a code block
			// Read the file content
			var content = block.Build.ReadFileSystem.File.ReadAllText(block.IncludePath);
			renderer.Write("```");
			if (!string.IsNullOrEmpty(block.Language))
				renderer.Write(block.Language);
			renderer.WriteLine();
			renderer.Write(content);
			renderer.WriteLine();
			renderer.WriteLine("```");
		}
		else
		{
			try
			{
				var parentPath = block.Context.MarkdownParentPath ?? block.Context.MarkdownSourcePath;
				var document = MarkdownParser.ParseSnippetAsync(block.Build, block.Context, snippet, parentPath, block.Context.YamlFrontMatter, Cancel.None)
					.GetAwaiter().GetResult();
				_ = renderer.Render(document);
			}
			catch (Exception ex)
			{
				renderer.BuildContext.Collector.EmitError(block.IncludePath ?? string.Empty, "Failed to parse included content", ex);
			}
		}

		renderer.EnsureLine();
	}

	private static void WriteChildrenWithIndentation(LlmMarkdownRenderer renderer, Block container, string indent)
	{
		// Capture output and manually add indentation
		var content = DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(renderer.BuildContext, container, static (tmpRenderer, obj) =>
		{
			switch (obj)
			{
				case ContainerBlock containerBlock:
					tmpRenderer.WriteChildren(containerBlock);
					break;
				case LeafBlock leafBlock:
					tmpRenderer.WriteLeafInline(leafBlock);
					break;
			}
		});

		if (string.IsNullOrEmpty(content))
			return;
		var reader = new StringReader(content);
		while (reader.ReadLine() is { } line)
			renderer.WriteLine(string.IsNullOrWhiteSpace(line) ? string.Empty : indent + line);
	}
}

public class LlmDefinitionItemRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, DefinitionItem>
{
	protected override void Write(LlmMarkdownRenderer renderer, DefinitionItem obj)
	{

		var first = obj.Cast<LeafBlock>().First();
		renderer.EnsureBlockSpacing();
		renderer.Write("<definition");
		renderer.Write(" term=\"");
		renderer.Write(GetPlainTextFromLeafBlock(renderer, first));
		renderer.WriteLine("\">");
		for (var index = 0; index < obj.Count; index++)
		{
			if (index == 0)
				continue;
			var block = obj[index];
			LlmRenderingHelpers.RenderBlockWithIndentation(renderer, block);
		}
		renderer.WriteLine("</definition>");
	}

	private static string GetPlainTextFromLeafBlock(LlmMarkdownRenderer renderer, LeafBlock leafBlock)
	{
		var markdownText = DocumentationObjectPoolProvider.UseLlmMarkdownRenderer(renderer.BuildContext, leafBlock, static (tmpRenderer, obj) =>
		{
			tmpRenderer.WriteLeafInline(obj);
		});
		return markdownText.StripMarkdown();
	}
}

public class LlmDefinitionListRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, DefinitionList>
{
	protected override void Write(LlmMarkdownRenderer renderer, DefinitionList obj)
	{
		renderer.EnsureBlockSpacing();
		renderer.WriteLine("<definitions>");
		foreach (var block in obj)
			LlmRenderingHelpers.RenderBlockWithIndentation(renderer, block);
		renderer.WriteLine("</definitions>");
	}
}
