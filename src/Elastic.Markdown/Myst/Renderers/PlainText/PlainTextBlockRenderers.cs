// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;
using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.Directives;
using Elastic.Markdown.Myst.Directives.Admonition;
using Elastic.Markdown.Myst.Directives.AppliesTo;
using Elastic.Markdown.Myst.Directives.Diagram;
using Elastic.Markdown.Myst.Directives.Image;
using Elastic.Markdown.Myst.Directives.Include;
using Elastic.Markdown.Myst.Directives.Math;
using Elastic.Markdown.Myst.Directives.Settings;
using Elastic.Markdown.Myst.Directives.Tabs;
using Elastic.Markdown.Myst.Renderers.LlmMarkdown;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Renderers.PlainText;

/// <summary>
/// Skips YAML frontmatter entirely
/// </summary>
public class PlainTextYamlFrontMatterRenderer : MarkdownObjectRenderer<PlainTextRenderer, YamlFrontMatterBlock>
{
	protected override void Write(PlainTextRenderer renderer, YamlFrontMatterBlock obj)
	{
		// Skip frontmatter - not searchable content
	}
}

/// <summary>
/// Renders headings as plain text with line breaks
/// </summary>
public class PlainTextHeadingRenderer : MarkdownObjectRenderer<PlainTextRenderer, HeadingBlock>
{
	protected override void Write(PlainTextRenderer renderer, HeadingBlock obj)
	{
		renderer.EnsureBlockSpacing();
		if (obj.Inline is not null)
			renderer.WriteChildren(obj.Inline);
		renderer.EnsureLine();
	}
}

/// <summary>
/// Renders paragraphs as plain text
/// </summary>
public class PlainTextParagraphRenderer : MarkdownObjectRenderer<PlainTextRenderer, ParagraphBlock>
{
	protected override void Write(PlainTextRenderer renderer, ParagraphBlock obj)
	{
		if (obj.Parent is MarkdownDocument)
			renderer.EnsureBlockSpacing();
		renderer.WriteLeafInline(obj);
		renderer.EnsureLine();
	}
}

/// <summary>
/// Renders code blocks as plain text (just the code content without fences)
/// </summary>
public class PlainTextCodeBlockRenderer : MarkdownObjectRenderer<PlainTextRenderer, EnhancedCodeBlock>
{
	protected override void Write(PlainTextRenderer renderer, EnhancedCodeBlock obj)
	{
		// Render applies-to directives as readable text
		if (obj is AppliesToDirective appliesTo)
		{
			var appliesText = LlmApplicabilityHelper.RenderForLlm(
				appliesTo.AppliesTo,
				renderer.BuildContext.VersionsConfiguration,
				useInlineTag: false);
			if (!string.IsNullOrEmpty(appliesText))
			{
				renderer.EnsureBlockSpacing();
				renderer.WriteLine($"({appliesText})");
			}
			return;
		}

		renderer.EnsureBlockSpacing();

		// Include caption if present
		if (!string.IsNullOrEmpty(obj.Caption))
			renderer.WriteLine(obj.Caption);

		// Output code content without fence markers
		var lastNonEmptyIndex = GetLastNonEmptyLineIndex(obj);
		for (var i = 0; i <= lastNonEmptyIndex; i++)
		{
			var line = obj.Lines.Lines[i];
			renderer.WriteLine(line.ToString());
		}
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
/// Renders lists as plain text (items without bullets or numbers)
/// </summary>
public class PlainTextListRenderer : MarkdownObjectRenderer<PlainTextRenderer, ListBlock>
{
	protected override void Write(PlainTextRenderer renderer, ListBlock listBlock)
	{
		if (listBlock.Parent is not ListItemBlock)
			renderer.EnsureBlockSpacing();

		foreach (var item in listBlock.Cast<ListItemBlock>())
		{
			foreach (var block in item)
				_ = renderer.Render(block);
		}
	}
}

/// <summary>
/// Renders blockquotes as plain text (content without > markers)
/// </summary>
public class PlainTextQuoteBlockRenderer : MarkdownObjectRenderer<PlainTextRenderer, QuoteBlock>
{
	protected override void Write(PlainTextRenderer renderer, QuoteBlock obj)
	{
		renderer.EnsureBlockSpacing();
		foreach (var block in obj)
			_ = renderer.Render(block);
	}
}

/// <summary>
/// Renders thematic breaks as blank lines
/// </summary>
public class PlainTextThematicBreakRenderer : MarkdownObjectRenderer<PlainTextRenderer, ThematicBreakBlock>
{
	protected override void Write(PlainTextRenderer renderer, ThematicBreakBlock obj) => renderer.EnsureBlockSpacing();
}

/// <summary>
/// Renders tables as "Header: Value" pairs for search indexing
/// </summary>
public class PlainTextTableRenderer : MarkdownObjectRenderer<PlainTextRenderer, Table>
{
	protected override void Write(PlainTextRenderer renderer, Table table)
	{
		renderer.EnsureBlockSpacing();

		string[]? headers = null;

		// Get headers from first row
		if (table.Count > 0 && table[0] is TableRow headerRow)
		{
			headers = headerRow.Cast<TableCell>()
				.Select(cell => RenderCellContent(renderer, cell))
				.ToArray();
		}

		// Render each data row as header: value pairs
		var isFirstRow = true;
		foreach (var row in table.Skip(1).Cast<TableRow>())
		{
			if (!isFirstRow)
				renderer.EnsureLine();
			isFirstRow = false;

			var cells = row.Cast<TableCell>().ToArray();
			for (var i = 0; i < cells.Length; i++)
			{
				var content = RenderCellContent(renderer, cells[i]);
				if (headers != null && i < headers.Length && !string.IsNullOrEmpty(headers[i]))
				{
					renderer.Write(headers[i]);
					renderer.Write(": ");
				}
				renderer.WriteLine(content);
			}
		}
	}

	private static string RenderCellContent(PlainTextRenderer renderer, TableCell cell) =>
		DocumentationObjectPoolProvider.UsePlainTextRenderer(
			renderer.BuildContext,
			cell,
			static (tmpRenderer, c) => tmpRenderer.WriteChildren(c)
		).Trim();
}

/// <summary>
/// Renders directives as plain text content
/// </summary>
public class PlainTextDirectiveRenderer : MarkdownObjectRenderer<PlainTextRenderer, DirectiveBlock>
{
	protected override void Write(PlainTextRenderer renderer, DirectiveBlock obj)
	{
		switch (obj)
		{
			case ImageBlock imageBlock:
				// Just output alt text
				if (!string.IsNullOrEmpty(imageBlock.Alt))
				{
					renderer.EnsureBlockSpacing();
					renderer.WriteLine(imageBlock.Alt);
				}
				return;

			case IncludeBlock includeBlock:
				WriteIncludeBlock(renderer, includeBlock);
				return;

			case DiagramBlock:
				// Skip diagrams - not text searchable
				return;

			case SettingsBlock settingsBlock:
				WriteSettingsBlock(renderer, settingsBlock);
				return;

			case MathBlock mathBlock:
				// Output math content as-is for search
				if (!string.IsNullOrEmpty(mathBlock.Content))
				{
					renderer.EnsureBlockSpacing();
					renderer.WriteLine(mathBlock.Content);
				}
				return;

			case TabSetBlock tabSetBlock:
				WriteTabSetBlock(renderer, tabSetBlock);
				return;

			case TabItemBlock tabItemBlock:
				WriteTabItemBlock(renderer, tabItemBlock);
				return;
		}

		renderer.EnsureBlockSpacing();

		// For titled blocks (like admonitions), include the title
		if (obj is IBlockTitle { Title: not null } titledBlock)
		{
			// Only output title if it's not a standard admonition type name
			var title = titledBlock.Title;
			if (obj is AdmonitionBlock)
			{
				// Skip standard titles like "Note", "Warning", "Tip", "Important"
				var standardTitles = new[] { "Note", "Warning", "Tip", "Important" };
				if (!standardTitles.Contains(title, StringComparer.OrdinalIgnoreCase))
				{
					renderer.WriteLine(title);
					renderer.EnsureBlockSpacing();
				}
			}
			else
			{
				renderer.WriteLine(title);
				renderer.EnsureBlockSpacing();
			}
		}

		// Render directive content
		renderer.WriteChildren(obj);
		renderer.EnsureLine();
	}

	private static void WriteTabSetBlock(PlainTextRenderer renderer, TabSetBlock tabSet)
	{
		renderer.EnsureBlockSpacing();
		renderer.WriteChildren(tabSet);
	}

	private static void WriteTabItemBlock(PlainTextRenderer renderer, TabItemBlock tabItem)
	{
		renderer.EnsureBlockSpacing();

		// Output tab title
		if (!string.IsNullOrEmpty(tabItem.Title))
		{
			renderer.WriteLine(tabItem.Title);
			renderer.EnsureBlockSpacing();
		}

		// Render tab content
		renderer.WriteChildren(tabItem);
	}

	private static void WriteIncludeBlock(PlainTextRenderer renderer, IncludeBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var snippet = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);
		if (!snippet.Exists)
			return;

		renderer.EnsureLine();

		if (block.Literal)
		{
			var content = block.Build.ReadFileSystem.File.ReadAllText(block.IncludePath);
			renderer.WriteLine(content);
		}
		else
		{
			try
			{
				var parentPath = block.Context.MarkdownParentPath ?? block.Context.MarkdownSourcePath;
				var document = MarkdownParser.ParseSnippetAsync(
					block.Build, block.Context, snippet, parentPath,
					block.Context.YamlFrontMatter, Cancel.None, block.Line
				).GetAwaiter().GetResult();
				_ = renderer.Render(document);
			}
			catch
			{
				// Skip on error
			}
		}
	}

	private static void WriteSettingsBlock(PlainTextRenderer renderer, SettingsBlock block)
	{
		if (!block.Found || block.IncludePath is null)
			return;

		var file = block.Build.ReadFileSystem.FileInfo.New(block.IncludePath);
		if (!file.Exists)
			return;

		YamlSettings? settings;
		try
		{
			var yaml = file.FileSystem.File.ReadAllText(file.FullName);
			settings = YamlSerialization.Deserialize<YamlSettings>(yaml, block.Context.Build.ProductsConfiguration);
		}
		catch
		{
			return;
		}

		renderer.EnsureBlockSpacing();

		foreach (var group in settings.Groups)
		{
			renderer.EnsureLine();
			renderer.WriteLine(group.Name ?? string.Empty);

			foreach (var setting in group.Settings)
			{
				renderer.EnsureLine();
				renderer.WriteLine(setting.Name ?? string.Empty);

				if (!string.IsNullOrEmpty(setting.Description))
				{
					var document = MarkdownParser.ParseMarkdownStringAsync(
						block.Build,
						block.Context,
						setting.Description,
						block.IncludeFrom,
						block.Context.YamlFrontMatter,
						MarkdownParser.Pipeline);
					_ = renderer.Render(document);
					renderer.EnsureBlockSpacing();
				}
			}
		}

		renderer.EnsureLine();
	}
}

/// <summary>
/// Renders definition lists as plain text
/// </summary>
public class PlainTextDefinitionListRenderer : MarkdownObjectRenderer<PlainTextRenderer, DefinitionList>
{
	protected override void Write(PlainTextRenderer renderer, DefinitionList obj)
	{
		renderer.EnsureBlockSpacing();
		foreach (var block in obj)
			_ = renderer.Render(block);
	}
}

/// <summary>
/// Renders definition items as plain text (term followed by definition)
/// </summary>
public class PlainTextDefinitionItemRenderer : MarkdownObjectRenderer<PlainTextRenderer, DefinitionItem>
{
	protected override void Write(PlainTextRenderer renderer, DefinitionItem obj)
	{
		renderer.EnsureBlockSpacing();

		// Render the term (first element)
		var first = obj.Cast<Block>().FirstOrDefault();
		if (first is LeafBlock leafBlock)
		{
			var term = DocumentationObjectPoolProvider.UsePlainTextRenderer(
				renderer.BuildContext,
				leafBlock,
				static (tmpRenderer, block) => tmpRenderer.WriteLeafInline(block)
			).Trim();
			renderer.WriteLine(term);
		}

		// Render the definitions (remaining elements)
		foreach (var block in obj.Skip(1))
			_ = renderer.Render(block);
	}
}
