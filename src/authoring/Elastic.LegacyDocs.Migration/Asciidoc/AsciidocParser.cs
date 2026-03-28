// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;
using Elastic.LegacyDocs.Migration.Asciidoc.Ast;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public record AsciidocParserOptions
{
	public Dictionary<string, string> Attributes { get; init; } = [];
	public int MaxIncludeDepth { get; init; } = 64;
	public Func<string, string?>? FileReader { get; init; }
}

public partial class AsciidocParser(AsciidocParserOptions options)
{
	private readonly Dictionary<string, string> _attributes = new(options.Attributes, StringComparer.OrdinalIgnoreCase);

	private IReadOnlyList<Token> _tokens = [];
	private int _pos;
	private int _includeDepth;
	private string _basePath = "";

	public AsciidocDocument Parse(string filePath)
	{
		var content = ReadFile(filePath) ?? throw new FileNotFoundException($"File not found: {filePath}");
		return Parse(content, Path.GetDirectoryName(filePath) ?? "");
	}

	public AsciidocDocument Parse(string content, string basePath)
	{
		_basePath = basePath;
		var rawTokens = AsciidocLexer.Tokenize(content);
		var processed = ConditionalProcessor.Process(rawTokens, _attributes);
		_tokens = processed;
		_pos = 0;

		var doc = new AsciidocDocument();
		string? pendingId = null;
		string? pendingTitle = null;
		TokenMetadata? pendingBlockAttr = null;

		while (_pos < _tokens.Count)
		{
			var token = Current;

			switch (token.Type)
			{
				case TokenType.AttributeEntry:
					_attributes[token.Metadata!.AttributeName!] = token.Metadata.AttributeValue ?? "";
					_pos++;
					break;

				case TokenType.AttributeUnset:
					_ = _attributes.Remove(token.Metadata!.AttributeName!);
					_pos++;
					break;

				case TokenType.BlockAnchor:
					pendingId = token.Metadata!.Id;
					_pos++;
					break;

				case TokenType.BlockTitle:
					pendingTitle = token.Metadata!.Title;
					_pos++;
					break;

				case TokenType.BlockAttribute:
					pendingBlockAttr = token.Metadata;
					_pos++;
					break;

				case TokenType.SectionTitle:
					if (doc.Title is null && token.Metadata!.Level == 1)
					{
						doc = doc with
						{
							Title = token.Metadata.Title,
							Id = pendingId,
							Attributes = new Dictionary<string, string>(_attributes)
						};
						pendingId = null;
						pendingTitle = null;
						_pos++;
					}
					else
					{
						var section = ParseSection(pendingId, pendingTitle);
						doc.Children.Add(section);
						pendingId = null;
						pendingTitle = null;
						pendingBlockAttr = null;
					}
					break;

				case TokenType.Blank:
				case TokenType.Comment:
					_pos++;
					break;

				case TokenType.IncludeDirective:
					var included = ProcessInclude(token);
					if (included != null)
						doc.Children.AddRange(included);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					_pos++;
					break;

				default:
					var block = ParseBlock(pendingId, pendingTitle, pendingBlockAttr);
					if (block != null)
						doc.Children.Add(block);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;
			}
		}

		return doc with { Attributes = new Dictionary<string, string>(_attributes) };
	}

	private Token Current => _tokens[_pos];

	private SectionNode ParseSection(string? id, string? title)
	{
		var token = Current;
		var level = token.Metadata!.Level!.Value;
		var sectionTitle = title ?? token.Metadata.Title!;
		var sectionId = id ?? ExtractInlineAnchor(sectionTitle);
		_pos++;

		var children = new List<IAsciidocNode>();
		string? pendingId = null;
		string? pendingTitle = null;
		TokenMetadata? pendingBlockAttr = null;

		while (_pos < _tokens.Count)
		{
			var cur = Current;

			if (cur.Type == TokenType.SectionTitle && cur.Metadata!.Level!.Value <= level)
				break;

			switch (cur.Type)
			{
				case TokenType.AttributeEntry:
					_attributes[cur.Metadata!.AttributeName!] = cur.Metadata.AttributeValue ?? "";
					_pos++;
					break;

				case TokenType.AttributeUnset:
					_ = _attributes.Remove(cur.Metadata!.AttributeName!);
					_pos++;
					break;

				case TokenType.BlockAnchor:
					pendingId = cur.Metadata!.Id;
					_pos++;
					break;

				case TokenType.BlockTitle:
					pendingTitle = cur.Metadata!.Title;
					_pos++;
					break;

				case TokenType.BlockAttribute:
					pendingBlockAttr = cur.Metadata;
					_pos++;
					break;

				case TokenType.SectionTitle:
					var childSection = ParseSection(pendingId, null);
					children.Add(childSection);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;

				case TokenType.Blank:
				case TokenType.Comment:
					_pos++;
					break;

				case TokenType.IncludeDirective:
					var included = ProcessInclude(cur);
					if (included != null)
						children.AddRange(included);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					_pos++;
					break;

				default:
					var block = ParseBlock(pendingId, pendingTitle, pendingBlockAttr);
					if (block != null)
						children.Add(block);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;
			}
		}

		return new SectionNode { Level = level, Title = sectionTitle, Id = sectionId, Children = children };
	}

	private IAsciidocNode? ParseBlock(string? id, string? title, TokenMetadata? blockAttr)
	{
		_ = id;
		if (_pos >= _tokens.Count)
			return null;

		var token = Current;

		return token.Type switch
		{
			TokenType.BlockDelimiter => ParseDelimitedBlock(blockAttr),
			TokenType.ListItemUnordered => ParseUnorderedList(),
			TokenType.ListItemOrdered => ParseOrderedList(),
			TokenType.DescriptionListItem => ParseDescriptionList(),
			TokenType.TableDelimiter => ParseTable(blockAttr),
			TokenType.AdmonitionParagraph => ParseAdmonitionParagraph(),
			TokenType.ImageBlock => ParseImageBlock(title),
			TokenType.PageBreak => ParsePageBreak(),
			TokenType.ThematicBreak => ParseThematicBreak(),
			TokenType.Text => ParseParagraph(),
			TokenType.ConditionalStart or TokenType.ConditionalEnd => SkipConditional(),
			_ => SkipToken()
		};
	}

	private IAsciidocNode? ParseDelimitedBlock(TokenMetadata? blockAttr)
	{
		var token = Current;
		var delimChar = token.Metadata?.DelimiterChar ?? "-";
		var openingDelim = token.Raw;
		_pos++;

		var style = blockAttr?.BlockStyle?.ToLowerInvariant();

		var contentLines = new List<string>();
		var children = new List<IAsciidocNode>();

		if (IsVerbatimDelimiter(delimChar))
		{
			while (_pos < _tokens.Count)
			{
				var cur = Current;
				if (cur.Type == TokenType.BlockDelimiter && IsMatchingClose(cur.Raw, openingDelim))
				{
					_pos++;
					break;
				}
				contentLines.Add(cur.Raw);
				_pos++;
			}
		}
		else
		{
			var innerTokens = CollectDelimitedTokens(openingDelim);
			children = ParseTokensAsBlocks(innerTokens);
		}

		return delimChar switch
		{
			"-" when openingDelim.Length >= 4 || style == "source" => new CodeBlockNode
			{
				Language = blockAttr?.Language,
				Source = string.Join('\n', contentLines)
			},
			"." => new LiteralBlockNode(string.Join('\n', contentLines)),
			"=" when IsAdmonitionStyle(style) => new AdmonitionNode
			{
				Type = ParseAdmonitionType(style!),
				Children = children
			},
			"=" => new ExampleNode { Children = children },
			"*" => new SidebarNode { Children = children },
			"+" => new PassthroughNode(string.Join('\n', contentLines)),
			"-" when openingDelim == "--" => style switch
			{
				"source" => new CodeBlockNode { Language = blockAttr?.Language, Source = string.Join('\n', contentLines) },
				_ => new OpenBlockNode { Children = children.Count > 0 ? children : WrapAsBlocks(contentLines) }
			},
			"/" => null,
			_ => new OpenBlockNode { Children = children.Count > 0 ? children : WrapAsBlocks(contentLines) }
		};
	}

	private static bool IsVerbatimDelimiter(string delimChar) =>
		delimChar is "-" or "." or "+" or "/";

	private static bool IsMatchingClose(string line, string openingDelim)
	{
		if (openingDelim.Length < 2)
			return false;

		var delimChar = openingDelim[0];
		return line.Length >= openingDelim.Length && line.All(c => c == delimChar);
	}

	private List<Token> CollectDelimitedTokens(string openingDelim)
	{
		var inner = new List<Token>();
		while (_pos < _tokens.Count)
		{
			var cur = Current;
			if (cur.Type == TokenType.BlockDelimiter && IsMatchingClose(cur.Raw, openingDelim))
			{
				_pos++;
				break;
			}
			inner.Add(cur);
			_pos++;
		}
		return inner;
	}

	private List<IAsciidocNode> ParseTokensAsBlocks(List<Token> innerTokens)
	{
		var savedTokens = _tokens;
		var savedPos = _pos;
		_tokens = innerTokens;
		_pos = 0;

		var blocks = new List<IAsciidocNode>();
		string? pendingId = null;
		string? pendingTitle = null;
		TokenMetadata? pendingBlockAttr = null;

		while (_pos < _tokens.Count)
		{
			var t = Current;
			switch (t.Type)
			{
				case TokenType.BlockAnchor:
					pendingId = t.Metadata!.Id;
					_pos++;
					break;
				case TokenType.BlockTitle:
					pendingTitle = t.Metadata!.Title;
					_pos++;
					break;
				case TokenType.BlockAttribute:
					pendingBlockAttr = t.Metadata;
					_pos++;
					break;
				case TokenType.Blank:
				case TokenType.Comment:
					_pos++;
					break;
				case TokenType.AttributeEntry:
					_attributes[t.Metadata!.AttributeName!] = t.Metadata.AttributeValue ?? "";
					_pos++;
					break;
				case TokenType.AttributeUnset:
					_ = _attributes.Remove(t.Metadata!.AttributeName!);
					_pos++;
					break;
				default:
					var block = ParseBlock(pendingId, pendingTitle, pendingBlockAttr);
					if (block != null)
						blocks.Add(block);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;
			}
		}

		_tokens = savedTokens;
		_pos = savedPos;
		return blocks;
	}

	private static List<IAsciidocNode> WrapAsBlocks(List<string> lines)
	{
		if (lines.Count == 0)
			return [];

		var text = string.Join('\n', lines).Trim();
		if (string.IsNullOrEmpty(text))
			return [];

		return [new ParagraphNode { Inlines = [new TextInline(text)] }];
	}

	private IAsciidocNode ParseUnorderedList()
	{
		var list = new UnorderedListNode();
		while (_pos < _tokens.Count && Current.Type == TokenType.ListItemUnordered)
		{
			var item = ParseListItem(TokenType.ListItemUnordered);
			list.Items.Add(item);
		}
		return list;
	}

	private IAsciidocNode ParseOrderedList()
	{
		var list = new OrderedListNode();
		while (_pos < _tokens.Count && Current.Type == TokenType.ListItemOrdered)
		{
			var item = ParseListItem(TokenType.ListItemOrdered);
			list.Items.Add(item);
		}
		return list;
	}

	private ListItemNode ParseListItem(TokenType listType)
	{
		var token = Current;
		var level = token.Metadata!.Level!.Value;
		var inlines = ParseInlines(SubstituteAttributes(token.Metadata.Content!));
		_pos++;

		var children = new List<IAsciidocNode>();

		while (_pos < _tokens.Count)
		{
			var cur = Current;

			if (cur.Type == TokenType.ListContinuation)
			{
				_pos++;
				if (_pos < _tokens.Count)
				{
					var continued = ParseBlock(null, null, null);
					if (continued != null)
						children.Add(continued);
				}
				continue;
			}

			if (cur.Type == listType && cur.Metadata!.Level!.Value > level)
			{
				var nested = listType == TokenType.ListItemUnordered
					? ParseUnorderedList()
					: ParseOrderedList();
				children.Add(nested);
				continue;
			}

			if (cur.Type == TokenType.Text)
			{
				var textInlines = ParseInlines(SubstituteAttributes(cur.Raw));
				inlines.AddRange(textInlines);
				_pos++;
				continue;
			}

			break;
		}

		return new ListItemNode { Inlines = inlines, Children = children };
	}

	private IAsciidocNode ParseDescriptionList()
	{
		var list = new DescriptionListNode();
		while (_pos < _tokens.Count && Current.Type == TokenType.DescriptionListItem)
		{
			var token = Current;
			var term = ParseInlines(SubstituteAttributes(token.Metadata!.Title!));
			var descText = token.Metadata.Content ?? "";
			_pos++;

			var description = new List<IAsciidocNode>();
			if (!string.IsNullOrWhiteSpace(descText))
				description.Add(new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(descText)) });

			while (_pos < _tokens.Count)
			{
				var cur = Current;
				if (cur.Type == TokenType.ListContinuation)
				{
					_pos++;
					if (_pos < _tokens.Count)
					{
						var continued = ParseBlock(null, null, null);
						if (continued != null)
							description.Add(continued);
					}
					continue;
				}

				if (cur.Type == TokenType.Text)
				{
					description.Add(new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(cur.Raw)) });
					_pos++;
					continue;
				}

				if (cur.Type is TokenType.Blank)
				{
					_pos++;
					break;
				}

				break;
			}

			list.Items.Add(new DescriptionListItemNode { Term = term, Description = description });
		}
		return list;
	}

	private IAsciidocNode ParseTable(TokenMetadata? blockAttr)
	{
		_pos++;

		var format = blockAttr?.NamedAttributes?.GetValueOrDefault("format")?.ToLowerInvariant();
		if (format is "dsv" or "csv" or "tsv")
			return ParseSeparatedTable(blockAttr, format);

		var columns = ParseColumnSpecs(blockAttr);
		var allRows = new List<TableRowNode>();
		var currentCells = new List<string>();
		var firstBlankSeen = false;

		while (_pos < _tokens.Count)
		{
			var cur = Current;

			if (cur.Type == TokenType.TableDelimiter)
			{
				_pos++;
				break;
			}

			if (cur.Type == TokenType.Blank)
			{
				if (currentCells.Count > 0)
				{
					allRows.Add(BuildTableRow(currentCells));
					currentCells = [];
				}
				firstBlankSeen = true;
				_pos++;
				continue;
			}

			if (cur.Type == TokenType.TableRow)
			{
				var cellContent = cur.Metadata?.Content ?? "";
				var cells = SplitTableCells(cellContent);
				currentCells.AddRange(cells);
				_pos++;
				continue;
			}

			if (cur.Type == TokenType.Text)
			{
				if (currentCells.Count > 0)
					currentCells[^1] += " " + cur.Raw.Trim();
				_pos++;
				continue;
			}

			_pos++;
		}

		if (currentCells.Count > 0)
			allRows.Add(BuildTableRow(currentCells));

		var hasHeader = HasHeaderOption(blockAttr);
		if (!hasHeader && allRows.Count > 1 && !firstBlankSeen)
			hasHeader = false;
		else if (!hasHeader && allRows.Count > 1)
			hasHeader = true;

		List<TableRowNode> headerRows = hasHeader && allRows.Count > 0 ? [allRows[0]] : [];
		var bodyRows = hasHeader && allRows.Count > 0 ? allRows[1..] : allRows;

		return new TableNode { Columns = columns, HeaderRows = headerRows, BodyRows = bodyRows };
	}

	private IAsciidocNode ParseSeparatedTable(TokenMetadata? blockAttr, string format)
	{
		var separator = format switch
		{
			"csv" => ',',
			"tsv" => '\t',
			_ => blockAttr?.NamedAttributes?.GetValueOrDefault("separator") is { Length: > 0 } sep ? sep[0] : ':'
		};

		var hasHeader = HasHeaderOption(blockAttr);
		var rows = new List<TableRowNode>();

		while (_pos < _tokens.Count)
		{
			var cur = Current;

			if (cur.Type == TokenType.TableDelimiter)
			{
				_pos++;
				break;
			}

			if (cur.Type == TokenType.Blank)
			{
				_pos++;
				continue;
			}

			var line = cur.Raw;
			if (!string.IsNullOrWhiteSpace(line))
			{
				var cells = line.Split(separator).Select(c => c.Trim()).ToList();
				rows.Add(BuildTableRow(cells));
			}

			_pos++;
		}

		var columns = ParseColumnSpecs(blockAttr);
		List<TableRowNode> headerRows = hasHeader && rows.Count > 0 ? [rows[0]] : [];
		var bodyRows = hasHeader && rows.Count > 0 ? rows[1..] : rows;

		return new TableNode { Columns = columns, HeaderRows = headerRows, BodyRows = bodyRows };
	}

	private static bool HasHeaderOption(TokenMetadata? blockAttr)
	{
		if (blockAttr?.NamedAttributes?.TryGetValue("options", out var opts) == true
			&& opts.Contains("header", StringComparison.OrdinalIgnoreCase))
			return true;

		var content = blockAttr?.Content ?? blockAttr?.BlockStyle ?? "";
		return content.Contains("%header", StringComparison.OrdinalIgnoreCase);
	}

	private static List<ColumnSpec> ParseColumnSpecs(TokenMetadata? blockAttr)
	{
		var colsValue = blockAttr?.NamedAttributes?.GetValueOrDefault("cols");
		if (string.IsNullOrWhiteSpace(colsValue))
			return [];

		var specs = new List<ColumnSpec>();
		var parts = colsValue.Split(',');

		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			if (trimmed.EndsWith('*'))
			{
				var countStr = trimmed[..^1];
				var count = int.TryParse(countStr, out var c) ? c : 1;
				for (var i = 0; i < count; i++)
					specs.Add(new ColumnSpec());
			}
			else
			{
				var spec = ParseSingleColumnSpec(trimmed);
				specs.Add(spec);
			}
		}

		return specs;
	}

	private static ColumnSpec ParseSingleColumnSpec(string spec)
	{
		var hAlign = ColumnHAlign.Left;
		var vAlign = ColumnVAlign.Top;
		int? width = null;
		string? style = null;

		if (spec.StartsWith('<'))
			hAlign = ColumnHAlign.Left;
		else if (spec.StartsWith('^'))
			hAlign = ColumnHAlign.Center;
		else if (spec.StartsWith('>'))
			hAlign = ColumnHAlign.Right;

		var digits = new string(spec.Where(char.IsDigit).ToArray());
		if (int.TryParse(digits, out var w))
			width = w;

		if (spec.EndsWith('a'))
			style = "asciidoc";
		else if (spec.EndsWith('h'))
			style = "header";

		return new ColumnSpec { HAlign = hAlign, VAlign = vAlign, Width = width, Style = style };
	}

	private static List<string> SplitTableCells(string content)
	{
		var cells = new List<string>();
		var parts = content.Split('|');
		foreach (var p in parts)
		{
			var trimmed = p.Trim();
			if (!string.IsNullOrEmpty(trimmed) || cells.Count > 0)
				cells.Add(trimmed);
		}
		return cells;
	}

	private TableRowNode BuildTableRow(List<string> cellTexts)
	{
		var cells = cellTexts
			.Select(text => new TableCellNode
			{
				Content = [new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(text)) }]
			})
			.ToList();
		return new TableRowNode { Cells = cells };
	}

	private IAsciidocNode ParseAdmonitionParagraph()
	{
		var token = Current;
		var type = ParseAdmonitionType(token.Metadata!.BlockStyle!);
		var content = token.Metadata.Content!;
		_pos++;

		var paragraph = new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(content)) };
		return new AdmonitionNode { Type = type, Children = [paragraph] };
	}

	private IAsciidocNode ParseImageBlock(string? title)
	{
		var token = Current;
		var path = SubstituteAttributes(token.Metadata!.Path!);
		var alt = token.Metadata.Title;
		_pos++;

		return new ImageNode { Path = path, Alt = alt, Title = title };
	}

	private IAsciidocNode ParsePageBreak()
	{
		_pos++;
		return new PageBreakNode();
	}

	private IAsciidocNode ParseThematicBreak()
	{
		_pos++;
		return new ThematicBreakNode();
	}

	private IAsciidocNode ParseParagraph()
	{
		var lines = new List<string>();
		while (_pos < _tokens.Count && Current.Type == TokenType.Text)
		{
			lines.Add(Current.Raw);
			_pos++;
		}

		var text = string.Join('\n', lines);
		var inlines = ParseInlines(SubstituteAttributes(text));
		return new ParagraphNode { Inlines = inlines };
	}

	private IAsciidocNode? SkipConditional()
	{
		_pos++;
		return null;
	}

	private IAsciidocNode? SkipToken()
	{
		_pos++;
		return null;
	}

	private static bool IsAdmonitionStyle(string? style) =>
		style is "note" or "tip" or "warning" or "important" or "caution" or
			"NOTE" or "TIP" or "WARNING" or "IMPORTANT" or "CAUTION";

	private static AdmonitionType ParseAdmonitionType(string style) =>
		style.ToUpperInvariant() switch
		{
			"NOTE" => AdmonitionType.Note,
			"TIP" => AdmonitionType.Tip,
			"WARNING" => AdmonitionType.Warning,
			"IMPORTANT" => AdmonitionType.Important,
			"CAUTION" => AdmonitionType.Caution,
			_ => AdmonitionType.Note
		};

	private static string? ExtractInlineAnchor(string title)
	{
		if (title.StartsWith("[[", StringComparison.Ordinal) && title.Contains("]]", StringComparison.Ordinal))
		{
			var end = title.IndexOf("]]", StringComparison.Ordinal);
			return title[2..end];
		}
		return null;
	}

	private List<IAsciidocNode>? ProcessInclude(Token token)
	{
		if (_includeDepth >= options.MaxIncludeDepth)
			throw new InvalidOperationException($"Include depth exceeded maximum of {options.MaxIncludeDepth}");

		var rawPath = SubstituteAttributes(token.Metadata!.Path!);
		var resolvedPath = Path.IsPathRooted(rawPath) ? rawPath : Path.Combine(_basePath, rawPath);
		var content = ReadFile(resolvedPath);
		if (content is null)
			return null;

		var attrs = token.Metadata.NamedAttributes ?? [];

		content = ApplyIncludeFilters(content, attrs);

		if (attrs.TryGetValue("leveloffset", out var offsetStr) && int.TryParse(offsetStr, out var offset))
			content = ApplyLevelOffset(content, offset);

		_includeDepth++;
		var savedTokens = _tokens;
		var savedPos = _pos;
		var savedBase = _basePath;

		_basePath = Path.GetDirectoryName(resolvedPath) ?? _basePath;
		var includeTokens = AsciidocLexer.Tokenize(content);
		var processed = ConditionalProcessor.Process(includeTokens, _attributes);
		_tokens = processed;
		_pos = 0;

		var result = new List<IAsciidocNode>();
		string? pendingId = null;
		string? pendingTitle = null;
		TokenMetadata? pendingBlockAttr = null;

		while (_pos < _tokens.Count)
		{
			var cur = Current;
			switch (cur.Type)
			{
				case TokenType.AttributeEntry:
					_attributes[cur.Metadata!.AttributeName!] = cur.Metadata.AttributeValue ?? "";
					_pos++;
					break;
				case TokenType.AttributeUnset:
					_ = _attributes.Remove(cur.Metadata!.AttributeName!);
					_pos++;
					break;
				case TokenType.BlockAnchor:
					pendingId = cur.Metadata!.Id;
					_pos++;
					break;
				case TokenType.BlockTitle:
					pendingTitle = cur.Metadata!.Title;
					_pos++;
					break;
				case TokenType.BlockAttribute:
					pendingBlockAttr = cur.Metadata;
					_pos++;
					break;
				case TokenType.Blank:
				case TokenType.Comment:
					_pos++;
					break;
				case TokenType.SectionTitle:
					var section = ParseSection(pendingId, null);
					result.Add(section);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;
				case TokenType.IncludeDirective:
					var included = ProcessInclude(cur);
					if (included != null)
						result.AddRange(included);
					_pos++;
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;
				default:
					var block = ParseBlock(pendingId, pendingTitle, pendingBlockAttr);
					if (block != null)
						result.Add(block);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					break;
			}
		}

		_tokens = savedTokens;
		_pos = savedPos;
		_basePath = savedBase;
		_includeDepth--;

		return result;
	}

	private static string ApplyIncludeFilters(string content, Dictionary<string, string> attrs)
	{
		if (attrs.TryGetValue("lines", out var linesSpec))
			content = FilterByLines(content, linesSpec);

		if (attrs.TryGetValue("tag", out var tag))
			content = FilterByTags(content, [tag]);
		else if (attrs.TryGetValue("tags", out var tags))
			content = FilterByTags(content, tags.Split(';'));

		return content;
	}

	private static string FilterByLines(string content, string linesSpec)
	{
		var allLines = content.Split('\n');
		var result = new List<string>();

		foreach (var range in linesSpec.Split(';'))
		{
			var trimmed = range.Trim();
			if (trimmed.Contains("..", StringComparison.Ordinal))
			{
				var parts = trimmed.Split("..");
				var start = int.TryParse(parts[0], out var s) ? s : 1;
				var endStr = parts.Length > 1 ? parts[1] : "";
				var end = endStr == "-1" || string.IsNullOrEmpty(endStr) ? allLines.Length : int.TryParse(endStr, out var e) ? e : allLines.Length;

				for (var i = Math.Max(1, start); i <= Math.Min(end, allLines.Length); i++)
					result.Add(allLines[i - 1]);
			}
			else if (int.TryParse(trimmed, out var lineNum) && lineNum >= 1 && lineNum <= allLines.Length)
			{
				result.Add(allLines[lineNum - 1]);
			}
		}

		return string.Join('\n', result);
	}

	private static string FilterByTags(string content, string[] tagNames)
	{
		var lines = content.Split('\n');
		var result = new List<string>();
		var activeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var targetTags = new HashSet<string>(tagNames, StringComparer.OrdinalIgnoreCase);

		foreach (var line in lines)
		{
			var trimmed = line.TrimStart();

			if (TagStartRegex().Match(trimmed) is { Success: true } startMatch)
			{
				var tagName = startMatch.Groups[1].Value;
				if (targetTags.Contains(tagName))
					_ = activeTags.Add(tagName);
				continue;
			}

			if (TagEndRegex().Match(trimmed) is { Success: true } endMatch)
			{
				_ = activeTags.Remove(endMatch.Groups[1].Value);
				continue;
			}

			if (activeTags.Count > 0)
				result.Add(line);
		}

		return string.Join('\n', result);
	}

	private static string ApplyLevelOffset(string content, int offset)
	{
		if (offset == 0)
			return content;

		var lines = content.Split('\n');
		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			if (line.StartsWith('='))
			{
				var match = LevelOffsetRegex().Match(line);
				if (match.Success)
				{
					var currentLevel = match.Groups[1].Value.Length;
					var newLevel = Math.Max(1, Math.Min(6, currentLevel + offset));
					lines[i] = new string('=', newLevel) + " " + match.Groups[2].Value;
				}
			}
		}
		return string.Join('\n', lines);
	}

	[GeneratedRegex(@"^(={1,6})\s+(.+)$")]
	private static partial Regex LevelOffsetRegex();

	[GeneratedRegex(@"//\s*tag::(\w+)\[\]")]
	private static partial Regex TagStartRegex();

	[GeneratedRegex(@"//\s*end::(\w+)\[\]")]
	private static partial Regex TagEndRegex();

	private string? ReadFile(string path)
	{
		if (options.FileReader is not null)
			return options.FileReader(path);

		return File.Exists(path) ? File.ReadAllText(path) : null;
	}

	[GeneratedRegex(
		@"link:([^\[]+)\[([^\]]*)\]|" +           // groups 1,2: link
		@"<<([^,>]+)(?:,([^>]+))?>>>|" +           // groups 3,4: triple-xref
		@"<<([^,>]+)(?:,([^>]+))?>>|" +            // groups 5,6: xref
		@"image:([^\[]+)\[([^\]]*)\]|" +           // groups 7,8: image
		@"footnote:\[([^\]]*)\]|" +                // group 9: footnote
		@"pass:\[([^\]]*)\]|" +                    // group 10: pass:[] passthrough
		@"\[([a-zA-Z][a-zA-Z0-9_-]*)\]#([^#]+)#|" + // groups 11,12: [role]#text#
		@"\*\*([^\*]+)\*\*|" +                     // group 13: unconstrained bold
		@"\*([^\*]+)\*|" +                         // group 14: constrained bold
		@"_([^_]+)_|" +                            // group 15: italic
		@"`([^`]+)`|" +                            // group 16: mono
		@"\^([^\^]+)\^|" +                         // group 17: superscript
		@"~([^~]+)~|" +                            // group 18: subscript
		@"\{([a-zA-Z0-9_-]+)\}|" +                // group 19: attr-ref
		@"(https?://[^\[\s]+)\[([^\]]*)\]|" +      // groups 20,21: url
		@"\+([^\+]+)\+|" +                         // group 22: +inline+ passthrough
		@"\s*\+\s*$"                               // line-break (no capture)
	)]
	private static partial Regex InlineCombinedRegex();

	[GeneratedRegex(@"\{([a-zA-Z0-9_-]+)\}")]
	private static partial Regex InlineAttrRefRegex();

	public List<IInlineNode> ParseInlines(string text)
	{
		if (string.IsNullOrEmpty(text))
			return [];

		var result = new List<IInlineNode>();
		var lastIndex = 0;

		foreach (Match match in InlineCombinedRegex().Matches(text))
		{
			if (match.Index > lastIndex)
				result.Add(new TextInline(text[lastIndex..match.Index]));

			if (match.Groups[1].Success)
				result.Add(new InlineLinkNode(match.Groups[1].Value, NullIfEmpty(match.Groups[2].Value)));
			else if (match.Groups[5].Success)
				result.Add(new InlineCrossRefNode(match.Groups[5].Value, NullIfEmpty(match.Groups[6].Value)));
			else if (match.Groups[3].Success)
				result.Add(new InlineCrossRefNode(match.Groups[3].Value, NullIfEmpty(match.Groups[4].Value)));
			else if (match.Groups[7].Success)
				result.Add(new InlineImageNode(match.Groups[7].Value, NullIfEmpty(match.Groups[8].Value)));
			else if (match.Groups[9].Success)
				result.Add(new FootnoteInline(ParseInlines(match.Groups[9].Value)));
			else if (match.Groups[10].Success)
				result.Add(new PassthroughInline(match.Groups[10].Value));
			else if (match.Groups[11].Success)
				result.Add(new RoleInline(match.Groups[11].Value, ParseInlines(match.Groups[12].Value)));
			else if (match.Groups[13].Success)
				result.Add(new BoldInline(ParseInlines(match.Groups[13].Value)));
			else if (match.Groups[14].Success)
				result.Add(new BoldInline(ParseInlines(match.Groups[14].Value)));
			else if (match.Groups[15].Success)
				result.Add(new ItalicInline(ParseInlines(match.Groups[15].Value)));
			else if (match.Groups[16].Success)
				result.Add(new MonoInline(match.Groups[16].Value));
			else if (match.Groups[17].Success)
				result.Add(new SuperscriptInline(ParseInlines(match.Groups[17].Value)));
			else if (match.Groups[18].Success)
				result.Add(new SubscriptInline(ParseInlines(match.Groups[18].Value)));
			else if (match.Groups[19].Success)
				result.Add(new AttributeRefInline(match.Groups[19].Value));
			else if (match.Groups[20].Success)
				result.Add(new InlineLinkNode(match.Groups[20].Value, NullIfEmpty(match.Groups[21].Value)));
			else if (match.Groups[22].Success)
				result.Add(new PassthroughInline(match.Groups[22].Value, Backticks: true));
			else if (match.Value.TrimEnd().EndsWith('+'))
				result.Add(new LineBreakInline());

			lastIndex = match.Index + match.Length;
		}

		if (lastIndex < text.Length)
			result.Add(new TextInline(text[lastIndex..]));

		return result;
	}

	private string SubstituteAttributes(string text)
	{
		if (string.IsNullOrEmpty(text))
			return text;

		return InlineAttrRefRegex().Replace(text, match =>
		{
			var name = match.Groups[1].Value;
			return _attributes.TryGetValue(name, out var value) ? value : match.Value;
		});
	}

	private static string? NullIfEmpty(string value) =>
		string.IsNullOrEmpty(value) ? null : value;
}
