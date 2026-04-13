// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.RegularExpressions;
using Elastic.LegacyDocs.Migration.Asciidoc.Ast;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public record MarkdownEmitterOptions
{
	public string ImagePathPrefix { get; init; } = "images/";
	public string? BookPrefix { get; init; }
	public string? Version { get; init; }
	public Dictionary<string, string> AnchorToSlugMap { get; init; } = [];
}

public static partial class GuideUrlRewriter
{
	private const string GuidePrefix = "https://www.elastic.co/guide/";

	[GeneratedRegex(@"^https://www\.elastic\.co/guide/(.+?)(?:\.html)?(?:#(.+))?$")]
	private static partial Regex GuideUrlRegex();

	public static string? TryRewriteToInternal(string url)
	{
		if (!url.StartsWith(GuidePrefix, StringComparison.OrdinalIgnoreCase))
			return null;

		var match = GuideUrlRegex().Match(url);
		if (!match.Success)
			return null;

		var path = match.Groups[1].Value.TrimEnd('/');
		var fragment = match.Groups[2].Success ? $"#{match.Groups[2].Value}" : "";

		if (path.EndsWith("/index", StringComparison.Ordinal))
			return $"/{path}.md{fragment}";

		return $"/{path}.md{fragment}";
	}
}

public class MarkdownEmitter(MarkdownEmitterOptions options)
{
	private StringBuilder _sb = new();
	private int _footnoteCounter;
	private readonly List<(int Index, string Content)> _footnotes = [];

	public void UpdateAnchorMap(Dictionary<string, string> anchorMap) =>
		options = options with { AnchorToSlugMap = anchorMap };

	public string Emit(AsciidocDocument document)
	{
		Reset();

		if (document.Title is not null)
		{
			var anchor = document.Id is not null ? $" [{document.Id}]" : "";
			WriteLine($"# {document.Title}{anchor}");
			WriteLine();
		}

		EmitChildren(document.Children);
		AppendFootnotes();

		return Finalize();
	}

	public string Emit(IAsciidocNode node)
	{
		Reset();
		EmitNode(node);
		AppendFootnotes();
		return Finalize();
	}

	public string EmitInlines(IReadOnlyList<IInlineNode> inlines)
	{
		Reset();
		foreach (var inline in inlines)
			EmitInline(inline);
		return _sb.ToString();
	}

	private void Reset()
	{
		_sb = new StringBuilder();
		_footnoteCounter = 0;
		_footnotes.Clear();
	}

	private string Finalize() => _sb.ToString().TrimEnd() + "\n";

	private void Write(string value) => _ = _sb.Append(value);

	private void Write(char value) => _ = _sb.Append(value);

	private void WriteLine() => _ = _sb.AppendLine();

	private void WriteLine(string value) => _ = _sb.AppendLine(value);

	private string CaptureOutput(Action action)
	{
		var saved = _sb;
		_sb = new StringBuilder();
		action();
		var result = _sb.ToString();
		_sb = saved;
		return result;
	}

	private void EmitChildren(IReadOnlyList<IAsciidocNode> children)
	{
		foreach (var child in children)
			EmitNode(child);
	}

	private void EmitNode(IAsciidocNode node)
	{
		switch (node)
		{
			case SectionNode section:
				EmitSection(section);
				break;
			case ParagraphNode paragraph:
				EmitParagraph(paragraph);
				break;
			case CodeBlockNode codeBlock:
				EmitCodeBlock(codeBlock);
				break;
			case LiteralBlockNode literal:
				EmitLiteralBlock(literal);
				break;
			case AdmonitionNode admonition:
				EmitDirective(MapAdmonitionType(admonition.Type), null, admonition.Children);
				break;
			case UnorderedListNode ul:
				EmitUnorderedList(ul, indent: 0);
				WriteLine();
				break;
			case OrderedListNode ol:
				EmitOrderedList(ol, indent: 0);
				WriteLine();
				break;
			case DescriptionListNode dl:
				EmitDescriptionList(dl);
				break;
			case TableNode table:
				EmitTable(table);
				break;
			case ImageNode image:
				EmitBlockImage(image);
				break;
			case SidebarNode sidebar:
				EmitDirective("admonition", "Sidebar", sidebar.Children);
				break;
			case ExampleNode example:
				EmitDirective("admonition", "Example", example.Children);
				break;
			case OpenBlockNode open:
				EmitChildren(open.Children);
				break;
			case PassthroughNode passthrough:
				WriteLine(passthrough.Content);
				WriteLine();
				break;
			case AnchoredBlock anchored:
				WriteLine($"$$${anchored.Id}$$$");
				EmitNode(anchored.Inner);
				break;
			case ThematicBreakNode:
			case PageBreakNode:
				WriteLine("---");
				WriteLine();
				break;
		}
	}

	private void EmitSection(SectionNode section)
	{
		var hashes = new string('#', section.Level + 1);
		var anchor = section.Id is not null ? $" [{section.Id}]" : "";
		WriteLine($"{hashes} {section.Title}{anchor}");
		WriteLine();

		EmitChildren(section.Children);
	}

	private void EmitParagraph(ParagraphNode paragraph)
	{
		foreach (var inline in paragraph.Inlines)
			EmitInline(inline);
		WriteLine();
		WriteLine();
	}

	private void EmitCodeBlock(CodeBlockNode codeBlock)
	{
		var lang = codeBlock.Language ?? "";
		WriteLine($"```{lang}");
		Write(codeBlock.Source.TrimEnd());
		WriteLine();
		WriteLine("```");
		WriteLine();

		if (codeBlock.Callouts.Count <= 0)
			return;

		for (var i = 0; i < codeBlock.Callouts.Count; i++)
			WriteLine($"{i + 1}. {codeBlock.Callouts[i]}");
		WriteLine();
	}

	private void EmitLiteralBlock(LiteralBlockNode literal)
	{
		foreach (var line in literal.Content.Split('\n'))
			WriteLine($"    {line}");
		WriteLine();
	}

	private static string MapAdmonitionType(AdmonitionType type) =>
		type switch
		{
			AdmonitionType.Caution => "warning",
			_ => type.ToString().ToLowerInvariant()
		};

	private void EmitDirective(string name, string? argument, IReadOnlyList<IAsciidocNode> children)
	{
		var header = argument is not null ? $":::{{{name}}} {argument}" : $":::{{{name}}}";
		WriteLine(header);
		EmitChildren(children);
		WriteLine(":::");
		WriteLine();
	}

	private void EmitUnorderedList(UnorderedListNode list, int indent)
	{
		foreach (var item in list.Items)
			EmitListItem(item, "- ", indent);
	}

	private void EmitOrderedList(OrderedListNode list, int indent)
	{
		foreach (var item in list.Items)
			EmitListItem(item, "1. ", indent);
	}

	private void EmitListItem(ListItemNode item, string marker, int indent)
	{
		var prefix = new string(' ', indent);
		Write($"{prefix}{marker}");

		foreach (var inline in item.Inlines)
			EmitInline(inline);
		WriteLine();

		if (item.Children.Count <= 0)
			return;

		var continuationIndent = indent + marker.Length;
		foreach (var child in item.Children)
		{
			switch (child)
			{
				case UnorderedListNode nestedUl:
					EmitUnorderedList(nestedUl, continuationIndent);
					break;
				case OrderedListNode nestedOl:
					EmitOrderedList(nestedOl, continuationIndent);
					break;
				default:
					EmitIndentedBlock(child, continuationIndent);
					break;
			}
		}
	}

	private void EmitIndentedBlock(IAsciidocNode node, int indent)
	{
		var content = CaptureOutput(() => EmitNode(node));
		var continuationPrefix = new string(' ', indent);
		foreach (var line in content.TrimEnd().Split('\n'))
			WriteLine(string.IsNullOrWhiteSpace(line) ? "" : $"{continuationPrefix}{line}");
		WriteLine();
	}

	private void EmitDescriptionList(DescriptionListNode list)
	{
		foreach (var item in list.Items)
		{
			foreach (var inline in item.Term)
				EmitInline(inline);
			WriteLine();

			var content = CaptureOutput(() =>
			{
				foreach (var descNode in item.Description)
					EmitNode(descNode);
			}).TrimEnd();

			var lines = content.Split('\n');
			for (var i = 0; i < lines.Length; i++)
			{
				var linePrefix = i == 0 ? ": " : "  ";
				if (string.IsNullOrWhiteSpace(lines[i]))
					WriteLine();
				else
					WriteLine($"{linePrefix}{lines[i]}");
			}
			WriteLine();
		}
	}

	private void EmitTable(TableNode table)
	{
		if (IsComplexTable(table))
			EmitListTable(table);
		else
			EmitPipeTable(table);
	}

	private static bool IsComplexTable(TableNode table) =>
		table.HeaderRows
			.Concat(table.BodyRows)
			.SelectMany(r => r.Cells)
			.Any(c => c.ColSpan > 1 || c.RowSpan > 1 || c.Content.Count > 1
				|| (c.Content.Count == 1 && c.Content[0] is not ParagraphNode));

	private void EmitPipeTable(TableNode table)
	{
		var colCount = ResolveColumnCount(table);

		if (table.HeaderRows.Count > 0)
		{
			foreach (var row in table.HeaderRows)
				EmitPipeRow(row);
		}
		else
		{
			Write('|');
			for (var i = 0; i < colCount; i++)
				Write("  |");
			WriteLine();
		}

		Write('|');
		for (var i = 0; i < colCount; i++)
			Write("---|");
		WriteLine();

		foreach (var row in table.BodyRows)
			EmitPipeRow(row);

		WriteLine();
	}

	private void EmitPipeRow(TableRowNode row)
	{
		Write('|');
		foreach (var cell in row.Cells)
		{
			Write(' ');
			EmitCellContent(cell);
			Write(" |");
		}
		WriteLine();
	}

	private void EmitListTable(TableNode table)
	{
		WriteLine(":::{list-table}");
		if (table.HeaderRows.Count > 0)
			WriteLine($":header-rows: {table.HeaderRows.Count}");
		WriteLine();

		foreach (var row in table.HeaderRows.Concat(table.BodyRows))
		{
			for (var i = 0; i < row.Cells.Count; i++)
			{
				Write(i == 0 ? "* - " : "  - ");
				EmitCellContent(row.Cells[i]);
				WriteLine();
			}
		}

		WriteLine(":::");
		WriteLine();
	}

	private void EmitCellContent(TableCellNode cell)
	{
		if (cell.Content.Count == 0)
			return;

		if (cell.Content is [ParagraphNode para])
		{
			foreach (var inline in para.Inlines)
				EmitInline(inline);
			return;
		}

		var content = CaptureOutput(() =>
		{
			foreach (var node in cell.Content)
				EmitNode(node);
		});
		Write(content.TrimEnd().Replace("\n", " "));
	}

	private static int ResolveColumnCount(TableNode table)
	{
		if (table.Columns.Count > 0)
			return table.Columns.Count;
		if (table.HeaderRows.Count > 0)
			return table.HeaderRows[0].Cells.Count;
		if (table.BodyRows.Count > 0)
			return table.BodyRows[0].Cells.Count;
		return 0;
	}

	private void EmitBlockImage(ImageNode image)
	{
		var alt = image.Alt ?? "";
		var path = image.Path.StartsWith(options.ImagePathPrefix, StringComparison.OrdinalIgnoreCase)
			? image.Path
			: $"{options.ImagePathPrefix}{image.Path}";
		WriteLine($"![{alt}]({path})");
		WriteLine();
	}

	private void EmitInline(IInlineNode inline)
	{
		switch (inline)
		{
			case TextInline text:
				Write(text.Text);
				break;
			case BoldInline bold:
				Write("**");
				EmitInlineChildren(bold.Children);
				Write("**");
				break;
			case ItalicInline italic:
				Write('*');
				EmitInlineChildren(italic.Children);
				Write('*');
				break;
			case MonoInline mono:
				Write($"`{mono.Text}`");
				break;
			case AttributeRefInline attrRef:
				Write($"{{{{{attrRef.Name}}}}}");
				break;
			case InlineLinkNode link:
				EmitLink(link);
				break;
			case InlineCrossRefNode xref:
				EmitCrossRef(xref);
				break;
			case InlineImageNode img:
				Write($"![{img.Alt ?? ""}]({img.Path})");
				break;
			case FootnoteInline footnote:
				EmitFootnote(footnote);
				break;
			case SuperscriptInline sup:
				Write("<sup>");
				EmitInlineChildren(sup.Children);
				Write("</sup>");
				break;
			case SubscriptInline sub:
				Write("<sub>");
				EmitInlineChildren(sub.Children);
				Write("</sub>");
				break;
			case PassthroughInline passthrough:
				if (passthrough.Backticks)
					Write($"`{passthrough.Content}`");
				else
					Write(passthrough.Content);
				break;
			case RoleInline role:
				EmitInlineChildren(role.Children);
				break;
			case LineBreakInline:
				Write('\\');
				WriteLine();
				break;
		}
	}

	private void EmitInlineChildren(List<IInlineNode> children)
	{
		foreach (var child in children)
			EmitInline(child);
	}

	private void EmitLink(InlineLinkNode link)
	{
		var rewritten = GuideUrlRewriter.TryRewriteToInternal(link.Url);
		var url = rewritten ?? link.Url;

		if (link.Text is not null)
			Write($"[{link.Text}]({url})");
		else
			Write($"<{url}>");
	}

	private void EmitCrossRef(InlineCrossRefNode xref)
	{
		var text = xref.Text ?? xref.Target;

		if (!xref.Target.Contains('/') && !xref.Target.Contains("::"))
		{
			if (options.AnchorToSlugMap.TryGetValue(xref.Target, out var slug))
			{
				Write($"[{text}]({slug}.md)");
				return;
			}

			Write($"[{text}](#{xref.Target})");
			return;
		}

		var prefix = options.BookPrefix ?? "";
		var version = options.Version ?? "current";
		var guideUrl = $"https://www.elastic.co/guide/{prefix}/{version}/{xref.Target}.html";
		var rewritten = GuideUrlRewriter.TryRewriteToInternal(guideUrl);
		Write($"[{text}]({rewritten ?? guideUrl})");
	}

	private void EmitFootnote(FootnoteInline footnote)
	{
		_footnoteCounter++;
		var index = _footnoteCounter;
		Write($"[^{index}]");

		var content = CaptureOutput(() =>
		{
			foreach (var child in footnote.Content)
				EmitInline(child);
		});
		_footnotes.Add((index, content));
	}

	private void AppendFootnotes()
	{
		if (_footnotes.Count == 0)
			return;

		WriteLine();
		foreach (var (index, content) in _footnotes)
			WriteLine($"[^{index}]: {content}");
	}
}
