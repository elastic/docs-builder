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
using CodeBlock = Markdig.Syntax.CodeBlock;

namespace Elastic.Markdown.Myst.Renderers.LlmMarkdown;

/// <summary>
/// Helper methods for common rendering patterns in LLM renderers
/// </summary>
public static class LlmRenderingHelpers
{
	/// <summary>
	/// Renders a block with fixed indentation applied to each line
	/// </summary>
	/// <param name="renderer">The target renderer to write to</param>
	/// <param name="block">The block to render</param>
	/// <param name="indentation">The indentation string to apply to each line (default: 2 spaces)</param>
	public static void RenderBlockWithIndentation(LlmMarkdownRenderer renderer, MarkdownObject block, string indentation = "  ")
	{
		using var tempWriter = new StringWriter();
		var tempRenderer = new LlmMarkdownRenderer(tempWriter)
		{
			BuildContext = renderer.BuildContext // Copy BuildContext for URL transformation
		};
		_ = tempRenderer.Render(block);

		// Get the rendered content and add indentation to each line
		var content = tempWriter.ToString().TrimEnd();
		if (!string.IsNullOrEmpty(content))
		{
			var lines = content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				renderer.Write(indentation);
				renderer.WriteLine(line);
			}
		}
	}

	/// <summary>
	/// Converts relative URLs to absolute URLs using BuildContext.CanonicalBaseUrl for better LLM consumption
	/// </summary>
	public static string? MakeAbsoluteUrl(LlmMarkdownRenderer renderer, string? url)
	{
		if (string.IsNullOrEmpty(url) || renderer.BuildContext.CanonicalBaseUrl == null)
			return url;

		// If URL is already absolute, return as-is
		if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
			return url;

		// If URL is relative, prepend canonical base URL
		if (Uri.IsWellFormedUriString(url, UriKind.Relative))
		{
			try
			{
				var baseUri = renderer.BuildContext.CanonicalBaseUrl;
				var absoluteUri = new Uri(baseUri, url);
				return absoluteUri.ToString();
			}
			catch
			{
				// If URI construction fails, return original URL
				return url;
			}
		}

		return url;
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
		// The frontmatter content is already processed and available through context.SourceFile.YamlFrontMatter
	}
}

/// <summary>
/// Renders headings as clean CommonMark headings with improved spacing for readability
/// </summary>
public class LlmHeadingRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, HeadingBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, HeadingBlock obj)
	{
		// Add extra spacing before headings - always add multiple newlines
		renderer.Writer.WriteLine();
		renderer.Writer.WriteLine();

		// Extract just the text content for clean headings
		var headingText = ExtractHeadingText(obj);

		// Output as standard markdown heading
		renderer.Writer.Write(new string('#', obj.Level));
		renderer.Writer.Write(' ');
		renderer.WriteLine(headingText);
	}

	private static string ExtractHeadingText(HeadingBlock heading)
	{
		if (heading.Inline == null)
			return string.Empty;

		// Extract plain text from inline elements
		return heading.Inline.Descendants()
			.OfType<Markdig.Syntax.Inlines.LiteralInline>()
			.Select(l => l.Content.ToString())
			.Aggregate(string.Empty, (current, text) => current + text);
	}
}

/// <summary>
/// Renders paragraphs with proper spacing for LLM readability
/// </summary>
public class LlmParagraphRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, ParagraphBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, ParagraphBlock obj)
	{
		if (obj.Parent is MarkdownDocument)
			renderer.EnsureBlockSpacing();
		renderer.WriteLeafInline(obj);
		renderer.EnsureLine();
	}
}

/// <summary>
/// Renders enhanced code blocks (your custom extension) as standard code blocks with optional metadata
/// and improved spacing for readability
/// </summary>
public partial class LlmEnhancedCodeBlockRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, EnhancedCodeBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, EnhancedCodeBlock obj)
	{
		// Ensure single empty line before code block
		renderer.EnsureBlockSpacing();

		// Add caption as comment if present
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

		// Close code block
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

	/// <summary>
	/// Gets the continuation indent for multi-line list items
	/// </summary>
	private static string GetContinuationIndent(string baseIndent, bool isOrdered) =>
		baseIndent + new string(' ', isOrdered ? 3 : 2);

	/// <summary>
	/// Renders any block type with proper list continuation indentation
	/// </summary>
	private static void RenderBlockWithIndentation(LlmMarkdownRenderer renderer, Block block, string baseIndent, bool isOrdered)
	{
		using var tempWriter = new StringWriter();
		var tempRenderer = new LlmMarkdownRenderer(tempWriter)
		{
			BuildContext = renderer.BuildContext
		};
		_ = tempRenderer.Render(block);
		var blockOutput = tempWriter.ToString();

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

/// <summary>
/// Renders thematic breaks as standard markdown horizontal rules
/// </summary>
public class LlmThematicBreakRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, ThematicBreakBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, ThematicBreakBlock obj)
	{
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
			renderer.Writer.Write("|");
			var cellIndex = 0;
			foreach (var cell in headerRow.Cast<TableCell>())
			{
				renderer.Writer.Write(" ");

				// Capture cell content
				var cellContent = new StringWriter();
				var tempRenderer = new LlmMarkdownRenderer(cellContent)
				{
					BuildContext = renderer.BuildContext // Copy BuildContext for URL transformation
				};

				// Render cell content to temporary writer
				foreach (var inline in cell.Descendants().OfType<Markdig.Syntax.Inlines.Inline>())
				{
					tempRenderer.Write(inline);
				}

				// Write padded content
				var content = cellContent.ToString();
				renderer.Writer.Write(content.PadRight(columnWidths[cellIndex]));
				renderer.Writer.Write(" |");
				cellIndex++;
			}

			renderer.WriteLine();

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
			renderer.Writer.Write("|");
			var cellIndex = 0;
			foreach (var cell in row.Cast<TableCell>())
			{
				renderer.Writer.Write(" ");

				// Capture cell content
				var cellContent = new StringWriter();
				var tempRenderer = new LlmMarkdownRenderer(cellContent)
				{
					BuildContext = renderer.BuildContext // Copy BuildContext for URL transformation
				};

				// Render cell content to temporary writer
				foreach (var inline in cell.Descendants().OfType<Markdig.Syntax.Inlines.Inline>())
				{
					tempRenderer.Write(inline);
				}

				// Write padded content
				var content = cellContent.ToString();
				renderer.Writer.Write(content.PadRight(columnWidths[cellIndex]));
				renderer.Writer.Write(" |");
				cellIndex++;
			}

			renderer.WriteLine();
		}
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
			var cellIndex = 0;
			foreach (var cell in row.Cast<TableCell>())
			{
				// Capture cell content
				var cellContent = new StringWriter();
				var tempRenderer = new LlmMarkdownRenderer(cellContent)
				{
					BuildContext = renderer.BuildContext // Copy BuildContext for URL transformation
				};

				// Render cell content to temporary writer
				foreach (var inline in cell.Descendants().OfType<Markdig.Syntax.Inlines.Inline>())
				{
					tempRenderer.Write(inline);
				}

				// Update width if this cell is wider
				var content = cellContent.ToString();
				widths[cellIndex] = Math.Max(widths[cellIndex], content.Length);
				cellIndex++;
			}
		}

		return widths;
	}
}

/// <summary>
/// Renders MyST directives as structured comments for LLM understanding with improved visual separation
/// </summary>
public class LlmDirectiveRenderer : MarkdownObjectRenderer<LlmMarkdownRenderer, DirectiveBlock>
{
	protected override void Write(LlmMarkdownRenderer renderer, DirectiveBlock obj)
	{
		switch (obj)
		{
			case ImageBlock imageBlock:
				WriteImageBlock(renderer, imageBlock);
				return;
			// Special handling for include directives
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

	/// <summary>
	/// Renders definition lists as structured XML for better LLM comprehension
	/// </summary>
	/// <summary>
	/// Renders include directives by fetching and rendering the included content
	/// </summary>
	private void WriteIncludeBlock(LlmMarkdownRenderer renderer, IncludeBlock block)
	{
		// If the include wasn't found or path is null, just write a comment
		if (!block.Found || block.IncludePath is null)
		{
			renderer.Writer.WriteLine($"<!-- INCLUDE ERROR: File not found or invalid path -->");
			return;
		}

		renderer.EnsureLine();

		// Get the file content
		var snippet = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);
		if (!snippet.Exists)
		{
			renderer.Writer.WriteLine($"<!-- INCLUDE ERROR: File does not exist: {block.IncludePath} -->");
			return;
		}

		// Handle differently based on whether it's a literal include or regular include
		if (block.Literal)
		{
			// For literal includes, output the content as a code block
			// Read the file content
			var content = block.Build.ReadFileSystem.File.ReadAllText(block.IncludePath);

			// Add language if specified
			renderer.Writer.Write("```");
			if (!string.IsNullOrEmpty(block.Language))
			{
				renderer.Writer.Write(block.Language);
			}

			renderer.WriteLine();
			// Add an extra newline after the opening code block marker
			renderer.Writer.WriteLine();

			// Write the content
			renderer.Writer.Write(content);

			// Close the code block
			renderer.Writer.WriteLine();
			renderer.Writer.WriteLine("```");

			// Add multiple line breaks after code blocks for better readability
		}
		else
		{
			// For regular includes, parse as markdown and render
			try
			{
				var parentPath = block.Context.MarkdownParentPath ?? block.Context.MarkdownSourcePath;
				var document = MarkdownParser.ParseSnippetAsync(block.Build, block.Context, snippet, parentPath, block.Context.YamlFrontMatter, default)
					.GetAwaiter().GetResult();

				// Use the same renderer to render the included content
				_ = renderer.Render(document);
			}
			catch (Exception ex)
			{
				renderer.Writer.WriteLine($"<!-- INCLUDE ERROR: Failed to parse included content: {ex.Message} -->");
			}
		}

		renderer.EnsureLine();
	}

	/// <summary>
	/// Writes children with the specified indentation applied to each line
	/// </summary>
	private static void WriteChildrenWithIndentation(LlmMarkdownRenderer renderer, Block container, string indent)
	{
		// Simple approach: capture output and manually add indentation
		using var sw = new StringWriter();
		var originalWriter = renderer.Writer;
		renderer.Writer = sw;

		try
		{
			// Render children to our temporary writer

			switch (container)
			{
				case ContainerBlock containerBlock:
					renderer.WriteChildren(containerBlock);
					break;
				case LeafBlock leafBlock:
					renderer.WriteLeafInline(leafBlock);
					break;
			}


			// Get the output and add indentation to each non-empty line
			var content = sw.ToString();
			if (string.IsNullOrEmpty(content))
				return;
			var reader = new StringReader(content);
			while (reader.ReadLine() is { } line)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					// Empty line - write as-is
					originalWriter.WriteLine();
				}
				else
				{
					// Non-empty line - add indentation
					originalWriter.WriteLine(indent + line);
				}
			}
		}
		finally
		{
			// Restore original writer
			renderer.Writer = originalWriter;
		}
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
		using var tempWriter = new StringWriter();
		var tempRenderer = new LlmMarkdownRenderer(tempWriter) { BuildContext = renderer.BuildContext };
		tempRenderer.WriteLeafInline(leafBlock);
		var markdownText = tempWriter.ToString();
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
