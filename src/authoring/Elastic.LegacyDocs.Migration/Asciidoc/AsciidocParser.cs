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
	public Action<string>? OnDiagnostic { get; init; }
}

public partial class AsciidocParser(AsciidocParserOptions options)
{
	private readonly Dictionary<string, string> _attributes = new(options.Attributes, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Returns the resolved attributes after parsing. Useful for extracting attribute definitions
	/// from a shared attributes file to seed into subsequent parsers.
	/// </summary>
	public IReadOnlyDictionary<string, string> ResolvedAttributes => _attributes;

	/// <summary>
	/// Parses a shared AsciiDoc attributes file (`:name: value` definitions) with the given
	/// seed attributes and returns the fully-resolved attribute map, excluding ProductNames keys.
	/// </summary>
	public static Dictionary<string, string> LoadAttributeFile(string path, Dictionary<string, string> seedAttributes)
	{
		if (!File.Exists(path))
			return [];
		var content = File.ReadAllText(path);
		var parser = new AsciidocParser(new AsciidocParserOptions { Attributes = seedAttributes });
		_ = parser.Parse(content, Path.GetDirectoryName(path) ?? "");
		return new Dictionary<string, string>(parser._attributes, StringComparer.OrdinalIgnoreCase);
	}

	private IReadOnlyList<Token> _tokens = [];
	private int _pos;
	private int _includeDepth;
	private string _basePath = "";

	public AsciidocDocument Parse(string filePath)
	{
		var content = ReadFile(filePath) ?? throw new FileNotFoundException($"File not found: {filePath}");
		return Parse(content, Path.GetDirectoryName(filePath) ?? "");
	}

	/// <summary>
	/// Sets an attribute with eager expansion of its value (Asciidoctor semantics).
	/// Product-name keys defined in <see cref="SharedAttributes.ProductNames"/> are intentionally
	/// kept unresolved so the emitter can emit them as docs-builder {{sub}} placeholders.
	/// </summary>
	private void SetAttribute(string name, string? value)
	{
		if (value is null)
		{
			_ = _attributes.Remove(name);
			return;
		}
		// Product name subs stay unresolved so the emitter emits {{name}} for docs-builder substitution
		if (SharedAttributes.ProductNames.ContainsKey(name))
			return;
		// Eagerly expand attribute values at definition time (Asciidoctor semantics)
		_attributes[name] = SubstituteAttributes(value);
	}

	/// <summary>
	/// Sets the base path and updates the <c>docdir</c> attribute, which is per-file in Asciidoctor.
	/// </summary>
	private void SetBasePath(string basePath)
	{
		_basePath = basePath;
		_attributes["docdir"] = basePath;
	}

	public AsciidocDocument Parse(string content, string basePath)
	{
		SetBasePath(basePath);
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
					SetAttribute(token.Metadata!.AttributeName!, token.Metadata.AttributeValue);
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
							Title = SubstituteAttributes(token.Metadata.Title!),
							Id = pendingId,
							Attributes = new(_attributes, StringComparer.OrdinalIgnoreCase)
						};
						pendingId = null;
						pendingTitle = null;
						_pos++;
					}
					else
					{
						var section = ParseSection(pendingId, pendingTitle, pendingBlockAttr);
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

		return doc with { Attributes = new(_attributes, StringComparer.OrdinalIgnoreCase) };
	}

	private Token Current => _tokens[_pos];

	private SectionNode ParseSection(string? id, string? title, TokenMetadata? blockAttr = null)
	{
		var token = Current;
		var level = token.Metadata!.Level!.Value;
		var sectionTitle = SubstituteAttributes(title ?? token.Metadata.Title!);
		var sectionId = id ?? ExtractInlineAnchor(sectionTitle);
		var isDiscrete = string.Equals(blockAttr?.BlockStyle, "discrete", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(blockAttr?.BlockStyle, "float", StringComparison.OrdinalIgnoreCase);
		_pos++;

		var children = new List<IAsciidocNode>();
		string? pendingId = null;
		string? pendingTitle = null;
		TokenMetadata? pendingBlockAttr = null;
		var pendingStart = _pos;

		while (_pos < _tokens.Count)
		{
			var cur = Current;

			if (cur.Type == TokenType.SectionTitle && cur.Metadata!.Level!.Value <= level)
			{
				_pos = pendingStart;
				break;
			}

			switch (cur.Type)
			{
				case TokenType.AttributeEntry:
					SetAttribute(cur.Metadata!.AttributeName!, cur.Metadata.AttributeValue);
					_pos++;
					pendingStart = _pos;
					break;

				case TokenType.AttributeUnset:
					_ = _attributes.Remove(cur.Metadata!.AttributeName!);
					_pos++;
					pendingStart = _pos;
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
					var childSection = ParseSection(pendingId, null, pendingBlockAttr);
					children.Add(childSection);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					pendingStart = _pos;
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
					pendingStart = _pos;
					break;

				default:
					var block = ParseBlock(pendingId, pendingTitle, pendingBlockAttr);
					if (block != null)
						children.Add(block);
					pendingId = null;
					pendingTitle = null;
					pendingBlockAttr = null;
					pendingStart = _pos;
					break;
			}
		}

		return new SectionNode { Level = level, Title = sectionTitle, Id = sectionId, Children = children, IsDiscrete = isDiscrete };
	}

	private IAsciidocNode? ParseBlock(string? id, string? title, TokenMetadata? blockAttr)
	{
		if (_pos >= _tokens.Count)
			return null;

		var token = Current;

		var block = token.Type switch
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
			TokenType.Text when CalloutItemRegex().IsMatch(token.Raw) => ParseCalloutList(),
			TokenType.Text => ParseParagraph(),
			TokenType.IncludeDirective => ParseIncludeBlock(),
			TokenType.ConditionalStart or TokenType.ConditionalEnd => SkipConditional(),
			_ => SkipToken()
		};

		return id is not null && block is not null ? new AnchoredBlock(id, block) : block;
	}

	private IAsciidocNode? ParseDelimitedBlock(TokenMetadata? blockAttr)
	{
		var token = Current;
		var delimChar = token.Metadata?.DelimiterChar ?? "-";
		// Trim trailing whitespace from the raw delimiter so `--  ` matches `--` as its close
		var openingDelim = token.Raw.TrimEnd();
		_pos++;

		var style = blockAttr?.BlockStyle?.ToLowerInvariant();

		var contentLines = new List<string>();
		var children = new List<IAsciidocNode>();

		if (IsVerbatimDelimiter(delimChar, openingDelim))
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

			// Resolve include-tagged:: directives embedded inside verbatim blocks
			contentLines = ResolveVerbatimIncludes(contentLines);
		}
		else
		{
			var innerTokens = CollectDelimitedTokens(openingDelim);
			children = ParseTokensAsBlocks(innerTokens);
		}

		// Collect callout annotations that follow a code block (e.g. <1> First step).
		// Skip any trailing comment lines (// TEST[...]) and one optional blank line;
		// restore position if what follows is not callout markers.
		var callouts = new List<string>();
		if (delimChar is "-" && openingDelim.Length >= 4)
		{
			var savedPos = _pos;
			while (_pos < _tokens.Count && Current.Type == TokenType.Comment)
				_pos++;
			if (_pos < _tokens.Count && Current.Type == TokenType.Blank)
				_pos++;
			if (_pos >= _tokens.Count || Current.Type != TokenType.Text || !CalloutItemRegex().IsMatch(Current.Raw))
				_pos = savedPos; // not callouts — restore so the normal token handling runs
			while (_pos < _tokens.Count && Current.Type == TokenType.Text)
			{
				var calloutMatch = CalloutItemRegex().Match(Current.Raw);
				if (!calloutMatch.Success)
					break;
				if (int.TryParse(calloutMatch.Groups[1].Value, out var idx) && idx >= 1)
				{
					while (callouts.Count < idx)
						callouts.Add("");
					var text = calloutMatch.Groups[2].Value;
					_pos++;
					// Collect any continuation lines (Text tokens without <n> prefix)
					while (_pos < _tokens.Count && Current.Type == TokenType.Text && !CalloutItemRegex().IsMatch(Current.Raw))
					{
						text += " " + Current.Raw.Trim();
						_pos++;
					}
					callouts[idx - 1] = text;
					continue;
				}
				_pos++;
			}
		}

		return delimChar switch
		{
			"-" when openingDelim.Length >= 4 || style == "source" =>
				new CodeBlockNode { Language = blockAttr?.Language, Source = string.Join('\n', contentLines), Callouts = callouts },
			"." => new LiteralBlockNode(string.Join('\n', contentLines)),
			"=" when IsAdmonitionStyle(style) => new AdmonitionNode { Type = ParseAdmonitionType(style!), Children = children },
			"=" => new ExampleNode { Children = children },
			"*" => new SidebarNode { Children = children },
			"+" => new PassthroughNode(string.Join('\n', contentLines)),
			"-" when openingDelim == "--" =>
				style switch
				{
					"source" => new CodeBlockNode { Language = blockAttr?.Language, Source = string.Join('\n', contentLines) },
					_ when IsAdmonitionStyle(style) =>
						new AdmonitionNode
						{
							Type = ParseAdmonitionType(style!),
							Children = children.Count > 0 ? children : WrapAsBlocks(contentLines)
						},
					"sidebar" => new SidebarNode { Children = children.Count > 0 ? children : WrapAsBlocks(contentLines) },
					_ => new OpenBlockNode { Children = children.Count > 0 ? children : WrapAsBlocks(contentLines) }
				},
			"/" => null,
			_ => new OpenBlockNode { Children = children.Count > 0 ? children : WrapAsBlocks(contentLines) }
		};
	}

	[GeneratedRegex(@"^<(\d+)>\s+(.+)$")]
	private static partial Regex CalloutItemRegex();

	[GeneratedRegex(@"^include::([^\[]+)\[([^\]]*)\]\s*$")]
	private static partial Regex TableIncludeRegex();

	[GeneratedRegex(@"^include-tagged::(.+?)\[([^\]]*)\]\s*$")]
	private static partial Regex IncludeTaggedInVerbatimRegex();

	// Standard AsciiDoc tagged include inside a verbatim block: include::path[tag=name]
	[GeneratedRegex(@"^include::(.+?)\[tag=([^\]]+)\]\s*$")]
	private static partial Regex IncludeTaggedStandardInVerbatimRegex();

	// Full-file include inside a verbatim block: include::path[] or include::path[indent=0] etc.
	[GeneratedRegex(@"^include::(.+?)\[[^\]]*\]\s*$")]
	private static partial Regex IncludeFullFileInVerbatimRegex();

	// Conditional markers that can appear inside verbatim blocks (e.g. ifeval::[...]/endif::[])
	[GeneratedRegex(@"^(?:ifdef|ifndef|ifeval|endif)::[^\[]*\[.*?\]\s*$")]
	private static partial Regex VerbatimConditionalRegex();

	/// <summary>
	/// Resolves include directives and strips stray conditional markers inside verbatim blocks.
	/// Handles both the Elastic <c>include-tagged::path[tag]</c> extension and the standard
	/// AsciiDoc <c>include::path[tag=name]</c> syntax with a tag qualifier.
	/// </summary>
	private List<string> ResolveVerbatimIncludes(List<string> lines)
	{
		// Fast path: nothing to do
		if (
			!lines.Any(
				l => l.Contains("include-tagged::", StringComparison.Ordinal) || l.Contains(
					"include::",
					StringComparison.Ordinal
				) || l.Contains("ifeval::", StringComparison.Ordinal) || l.Contains("ifdef::", StringComparison.Ordinal) || l.Contains(
					"ifndef::",
					StringComparison.Ordinal
				) || l.Contains("endif::", StringComparison.Ordinal)
			)
		)
			return lines;

		var result = new List<string>(lines.Count);
		foreach (var line in lines)
		{
			var trimmed = line.TrimStart();

			// Strip stray conditional markers (ifeval/ifdef/ifndef/endif) from verbatim content.
			// The surrounding content is always kept since we can't evaluate conditions here.
			if (VerbatimConditionalRegex().IsMatch(trimmed))
				continue;

			var taggedMatch = IncludeTaggedInVerbatimRegex().Match(trimmed);
			if (taggedMatch.Success)
			{
				result.AddRange(ResolveTaggedInclude(taggedMatch.Groups[1].Value, taggedMatch.Groups[2].Value.Trim(), line));
				continue;
			}

			var standardMatch = IncludeTaggedStandardInVerbatimRegex().Match(trimmed);
			if (standardMatch.Success)
			{
				result.AddRange(ResolveTaggedInclude(standardMatch.Groups[1].Value, standardMatch.Groups[2].Value.Trim(), line));
				continue;
			}

			var fullFileMatch = IncludeFullFileInVerbatimRegex().Match(trimmed);
			if (fullFileMatch.Success)
			{
				result.AddRange(ResolveFullFileInclude(fullFileMatch.Groups[1].Value, line));
				continue;
			}

			result.Add(line);
		}
		return result;
	}

	private IEnumerable<string> ResolveTaggedInclude(string rawPathToken, string tag, string originalLine)
	{
		var rawPath = SubstituteAttributes(rawPathToken);
		var resolvedPath = Path.IsPathRooted(rawPath) ? Path.GetFullPath(rawPath) : Path.GetFullPath(Path.Combine(_basePath, rawPath));

		var fileContent = ReadFile(resolvedPath);
		if (fileContent is null)
		{
			options.OnDiagnostic?.Invoke($"include not resolved: {rawPath} (resolved to {resolvedPath})");
			return [originalLine];
		}

		return ExtractTaggedLines(fileContent, tag);
	}

	private IEnumerable<string> ResolveFullFileInclude(string rawPathToken, string originalLine)
	{
		var rawPath = SubstituteAttributes(rawPathToken);
		var resolvedPath = Path.IsPathRooted(rawPath) ? Path.GetFullPath(rawPath) : Path.GetFullPath(Path.Combine(_basePath, rawPath));

		var fileContent = ReadFile(resolvedPath);
		if (fileContent is null)
		{
			options.OnDiagnostic?.Invoke($"include not resolved: {rawPath} (resolved to {resolvedPath})");
			return [originalLine];
		}

		return fileContent.TrimEnd('\n', '\r').Split('\n');
	}

	private static List<string> ExtractTaggedLines(string content, string tag)
	{
		var lines = content.Split('\n');
		var result = new List<string>();
		var inTag = false;
		int? dedentWidth = null;

		var escapedTag = Regex.Escape(tag);
		var startPattern = new Regex($@"tag::{escapedTag}\[\]");
		var endPattern = new Regex($@"end::{escapedTag}\[\]");

		foreach (var line in lines)
		{
			var trimmed = line.TrimStart();
			if (!inTag && startPattern.IsMatch(trimmed))
			{
				inTag = true;
				dedentWidth = line.Length - trimmed.Length;
				continue;
			}
			if (inTag && endPattern.IsMatch(trimmed))
			{
				inTag = false;
				continue;
			}
			if (!inTag)
				continue;

			// Dedent by the leading whitespace of the tag:: line
			result.Add(dedentWidth is > 0 && line.Length >= dedentWidth ? line[dedentWidth.Value..] : line);
		}
		return result;
	}

	/// <summary>
	/// Returns true when the opening delimiter's content should be collected verbatim (raw text lines).
	/// Open blocks (<c>--</c>) are structural, not verbatim, even though their delimChar is <c>-</c>.
	/// </summary>
	private static bool IsVerbatimDelimiter(string delimChar, string openingDelim) =>
		delimChar is "-" or "." or "+" or "/" && openingDelim.Length >= 4;

	private static bool IsMatchingClose(string line, string openingDelim)
	{
		if (openingDelim.Length < 2)
			return false;

		var delimChar = openingDelim[0];
		// Trim trailing whitespace so source bugs like `---- ` still close a `----` block
		var trimmed = line.TrimEnd();
		// Require exact length so `--------` does not close a `----` block
		return trimmed.Length == openingDelim.Length && trimmed.All(c => c == delimChar);
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
					SetAttribute(t.Metadata!.AttributeName!, t.Metadata.AttributeValue);
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

	private List<IAsciidocNode> WrapAsBlocks(List<string> lines)
	{
		if (lines.Count == 0)
			return [];

		var text = string.Join('\n', lines).Trim();
		if (string.IsNullOrEmpty(text))
			return [];

		return [new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(text)) }];
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
		// Collect the first line and any trailing Text tokens together before parsing,
		// so that multi-line xrefs (<<anchor,text\nspanning lines>>) are matched correctly.
		var firstLineText = token.Metadata.Content!;
		_pos++;

		var children = new List<IAsciidocNode>();
		List<IInlineNode>? inlines = null;

		while (_pos < _tokens.Count)
		{
			var cur = Current;

			if (inlines is null && cur.Type == TokenType.Text)
			{
				// Collect the first line + consecutive continuation text lines, then parse once
				var textLines = new List<string> { firstLineText };
				while (_pos < _tokens.Count && Current.Type == TokenType.Text)
				{
					textLines.Add(Current.Raw);
					_pos++;
				}
				inlines = ParseInlines(SubstituteAttributes(string.Join('\n', textLines)));
				continue;
			}

			inlines ??= ParseInlines(SubstituteAttributes(firstLineText));

			if (cur.Type == TokenType.ListContinuation)
			{
				_pos++;
				string? contId = null;
				string? contTitle = null;
				TokenMetadata? contBlockAttr = null;

				while (_pos < _tokens.Count)
				{
					var peek = Current;
					if (peek.Type == TokenType.BlockAnchor)
					{
						contId = peek.Metadata!.Id;
						_pos++;
					}
					else if (peek.Type == TokenType.BlockTitle)
					{
						contTitle = peek.Metadata!.Title;
						_pos++;
					}
					else if (peek.Type == TokenType.BlockAttribute)
					{
						contBlockAttr = peek.Metadata;
						_pos++;
					}
					else if (peek.Type == TokenType.Blank)
					{
						_pos++;
					}
					else
					{
						break;
					}
				}

				if (_pos < _tokens.Count)
				{
					var continued = ParseBlock(contId, contTitle, contBlockAttr);
					if (continued != null)
						children.Add(continued);
				}
				continue;
			}

			if (cur.Type == listType && cur.Metadata!.Level!.Value > level)
			{
				var nested = listType == TokenType.ListItemUnordered ? ParseUnorderedList() : ParseOrderedList();
				children.Add(nested);
				continue;
			}

			if (cur.Type == TokenType.Text)
			{
				// Collect consecutive text lines and parse together so multi-line xrefs (<<anchor,text\nspanning lines>>) match
				var textLines = new List<string>();
				while (_pos < _tokens.Count && Current.Type == TokenType.Text)
				{
					textLines.Add(Current.Raw);
					_pos++;
				}
				inlines.AddRange(ParseInlines(SubstituteAttributes(string.Join('\n', textLines))));
				continue;
			}

			break;
		}

		inlines ??= ParseInlines(SubstituteAttributes(firstLineText));
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
			{
				// If the inline description contains an unclosed <<, the xref spans to the next
				// line(s) — join with continuation Text tokens until the xref is closed.
				var openCount = descText.Split("<<").Length - 1;
				var closeCount = descText.Split(">>").Length - 1;
				while (openCount > closeCount && _pos < _tokens.Count && Current.Type == TokenType.Text)
				{
					descText += "\n" + Current.Raw;
					closeCount = descText.Split(">>").Length - 1;
					_pos++;
				}
				description.Add(new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(descText)) });
			}

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
					var lines = new List<string> { cur.Raw };
					_pos++;
					while (_pos < _tokens.Count && Current.Type == TokenType.Text)
					{
						// Stop before a callout marker so ParseCalloutList can handle the <n> run
						if (CalloutItemRegex().IsMatch(Current.Raw))
							break;
						lines.Add(Current.Raw);
						_pos++;
					}
					description.Add(new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(string.Join('\n', lines))) });
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

			if (cur.Type is TokenType.Text or TokenType.IncludeDirective)
			{
				// Inside tables the lexer produces Text tokens for include:: directives
				var includeMatch = TableIncludeRegex().Match(cur.Raw);
				if (includeMatch.Success)
				{
					// Flush any accumulated row, then inline the included table rows
					if (currentCells.Count > 0)
					{
						allRows.Add(BuildTableRow(currentCells));
						currentCells = [];
					}
					var rawPath = includeMatch.Groups[1].Value;
					var resolvedPath = rawPath.StartsWith("{docdir}", StringComparison.Ordinal)
						? rawPath.Replace("{docdir}", _basePath)
						: Path.GetFullPath(Path.Combine(_basePath, rawPath));
					if (File.Exists(resolvedPath))
					{
						foreach (var fileLine in File.ReadAllLines(resolvedPath))
						{
							var trimmedLine = fileLine.Trim();
							if (trimmedLine.StartsWith('|'))
							{
								var cells = SplitTableCells(trimmedLine[1..]);
								if (cells.Count > 0)
									allRows.Add(BuildTableRow(cells));
							}
						}
					}
				}
				else if (currentCells.Count > 0)
				{
					currentCells[^1] += " " + cur.Raw.Trim();
				}
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
		if (
			blockAttr?.NamedAttributes?.TryGetValue("options", out var opts) == true
			&& opts.Contains("header", StringComparison.OrdinalIgnoreCase)
		)
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
		var cells = cellTexts.Select(
			text => new TableCellNode { Content = [new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(text)) }] }
		).ToList();
		return new TableRowNode { Cells = cells };
	}

	// Collects a run of <n> callout description lines into an ordered list.
	// Called when a Text token at block level matches the callout pattern.
	private IAsciidocNode ParseCalloutList()
	{
		var items = new List<ListItemNode>();
		while (_pos < _tokens.Count && Current.Type == TokenType.Text)
		{
			var calloutMatch = CalloutItemRegex().Match(Current.Raw);
			if (!calloutMatch.Success)
				break;
			var text = calloutMatch.Groups[2].Value;
			_pos++;
			// Collect any continuation lines (Text tokens without <n> prefix)
			while (_pos < _tokens.Count && Current.Type == TokenType.Text && !CalloutItemRegex().IsMatch(Current.Raw))
			{
				text += " " + Current.Raw.Trim();
				_pos++;
			}
			items.Add(new ListItemNode { Inlines = ParseInlines(SubstituteAttributes(text)), Children = [] });
		}
		return new OrderedListNode { Items = items };
	}

	private IAsciidocNode ParseAdmonitionParagraph()
	{
		var token = Current;
		var type = ParseAdmonitionType(token.Metadata!.BlockStyle!);
		var contentParts = new List<string> { token.Metadata.Content! };
		_pos++;

		// Collect continuation lines (non-blank Text tokens that follow the admonition header)
		while (_pos < _tokens.Count && Current.Type == TokenType.Text)
		{
			contentParts.Add(Current.Raw);
			_pos++;
		}

		var content = string.Join('\n', contentParts);
		var paragraph = new ParagraphNode { Inlines = ParseInlines(SubstituteAttributes(content)) };
		return new AdmonitionNode { Type = type, Children = [paragraph] };
	}

	private IAsciidocNode ParseImageBlock(string? title)
	{
		var token = Current;
		var path = SubstituteAttributes(token.Metadata!.Path!);
		var alt = token.Metadata.Title is not null ? SubstituteAttributes(token.Metadata.Title) : null;
		var resolvedTitle = title is not null ? SubstituteAttributes(title) : null;
		_pos++;

		return new ImageNode { Path = path, Alt = alt, Title = resolvedTitle };
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
			// Stop before a callout marker so ParseCalloutList can handle the <n> run
			if (CalloutItemRegex().IsMatch(Current.Raw))
				break;
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

	private IAsciidocNode? ParseIncludeBlock()
	{
		var token = Current;
		_pos++;
		var included = ProcessInclude(token);
		if (included is null or { Count: 0 })
			return null;
		if (included.Count == 1)
			return included[0];
		return new OpenBlockNode { Children = included };
	}

	private static bool IsAdmonitionStyle(string? style) =>
		style is "note" or "tip" or "warning" or "important" or "caution" or "NOTE" or "TIP" or "WARNING" or "IMPORTANT" or "CAUTION";

	private static AdmonitionType ParseAdmonitionType(string style) => style.ToUpperInvariant() switch
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
		var resolvedPath = Path.IsPathRooted(rawPath) ? Path.GetFullPath(rawPath) : Path.GetFullPath(Path.Combine(_basePath, rawPath));
		var content = ReadFile(resolvedPath);
		if (content is null)
		{
			options.OnDiagnostic?.Invoke($"Include not resolved: {rawPath} (resolved to {resolvedPath})");
			return null;
		}

		var attrs = token.Metadata.NamedAttributes ?? [];

		content = ApplyIncludeFilters(content, attrs);

		if (attrs.TryGetValue("leveloffset", out var offsetStr) && int.TryParse(offsetStr, out var offset))
			content = ApplyLevelOffset(content, offset);

		_includeDepth++;
		var savedTokens = _tokens;
		var savedPos = _pos;
		var savedBase = _basePath;
		_ = _attributes.TryGetValue("docdir", out var savedDocDir);

		SetBasePath(Path.GetDirectoryName(resolvedPath) ?? _basePath);
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
					SetAttribute(cur.Metadata!.AttributeName!, cur.Metadata.AttributeValue);
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
					var section = ParseSection(pendingId, null, pendingBlockAttr);
					result.Add(section with { IsIncludeRoot = true });
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
		if (savedDocDir is not null)
			_attributes["docdir"] = savedDocDir;
		else
			_ = _attributes.Remove("docdir");
		_includeDepth--;

		// Re-nest sections by heading level: a section at level N that follows a section at
		// level < N in the flat result list becomes a child of that ancestor, replicating
		// AsciiDoc's natural hierarchical nesting (e.g. migration/index.asciidoc includes a
		// Level-0 intro file followed by Level-1 version files — those version sections should
		// be children of the Level-0 section, not top-level siblings).
		return NestSectionsByLevel(result);
	}

	private static List<IAsciidocNode> NestSectionsByLevel(List<IAsciidocNode> nodes)
	{
		// Fast path: no section can be nested under another (all same level or no L0 present).
		var hasNestableStructure = false;
		var firstSectionLevel = int.MaxValue;
		foreach (var n in nodes)
		{
			if (n is not SectionNode s)
				continue;
			if (s.Level < firstSectionLevel)
				firstSectionLevel = s.Level;
			else if (s.Level > firstSectionLevel)
			{
				hasNestableStructure = true;
				break;
			}
		}
		if (!hasNestableStructure)
			return nodes;

		// Stack entries: the original section node + extra children accumulated from later siblings.
		var stack = new List<(SectionNode Section, List<IAsciidocNode> Extra)>();
		var result = new List<IAsciidocNode>();

		void AddToContext(IAsciidocNode node)
		{
			if (stack.Count > 0)
				stack[^1].Extra.Add(node);
			else
				result.Add(node);
		}

		IAsciidocNode CloseTop()
		{
			var (sec, extra) = stack[^1];
			stack.RemoveAt(stack.Count - 1);
			return extra.Count == 0 ? sec : sec with { Children = [.. sec.Children, .. extra] };
		}

		foreach (var node in nodes)
		{
			if (node is SectionNode section)
			{
				// Close any open sections that are at the same level or deeper.
				while (stack.Count > 0 && stack[^1].Section.Level >= section.Level)
					AddToContext(CloseTop());
				stack.Add((section, []));
			}
			else
			{
				AddToContext(node);
			}
		}

		// Drain remaining open sections.
		while (stack.Count > 0)
			AddToContext(CloseTop());

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
				var end = endStr == "-1" || string.IsNullOrEmpty(endStr)
					? allLines.Length
					: int.TryParse(endStr, out var e) ? e : allLines.Length;

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

	// Allow hyphens in tag names (e.g. tag::my-tag[]) and any comment prefix (// # --)
	[GeneratedRegex(@"tag::([\w-]+)\[\]")]
	private static partial Regex TagStartRegex();

	[GeneratedRegex(@"end::([\w-]+)\[\]")]
	private static partial Regex TagEndRegex();

	private string? ReadFile(string path)
	{
		if (options.FileReader is not null)
		{
			// Normalize to Unix-style paths so test FileReader lambdas receive consistent
			// forward-slash paths regardless of OS. On Windows, Path.GetFullPath turns
			// "/base/foo.adoc" into "C:\base\foo.adoc"; strip the drive letter and flip slashes.
			var normalized = path.Replace('\\', '/');
			if (normalized.Length >= 2 && normalized[1] == ':')
				normalized = normalized[2..];
			return options.FileReader(normalized);
		}

		return File.Exists(path) ? File.ReadAllText(path) : null;
	}

	[GeneratedRegex(@"link:([^\[]+)\[([^\]]*)\]|"
		+ // groups 1,2: link
		 @"<<([^,>]+)(?:,([\s\S]+?))?>>>|"
		+ // groups 3,4: triple-xref (allow newlines in text)
		 @"<<([^,>]+)(?:,([\s\S]+?))?>>"
		+ "|"
		+ // groups 5,6: xref (allow newlines in text)
		 @"image:([^\[]+)\[([^\]]*)\]|"
		+ // groups 7,8: image
		 @"footnote:\[([^\]]*)\]|"
		+ // group 9: footnote
		 @"pass:\[([^\]]*)\]|"
		+ // group 10: pass:[] passthrough
		 @"\[([a-zA-Z][a-zA-Z0-9_-]*)\]#([^#]+)#|"
		+ // groups 11,12: [role]#text#
		 @"\*\*([^\*<]+)\*\*|"
		+ // group 13: unconstrained bold (no < prevents spanning xref markers)
		 @"\*([^\*<]+)\*|"
		+ // group 14: constrained bold (no < prevents spanning xref markers)
		 @"_([^_<]+)_|"
		+ // group 15: italic (no < prevents spanning xref markers)
		 @"(?<!`)`([^`]+)`|"
		+ // group 16: mono (lookbehind prevents ``curly-quote'' from opening a code span)
		 @"\^([^\^]+)\^|"
		+ // group 17: superscript
		 @"~([^~]+)~|"
		+ // group 18: subscript
		 @"\{([a-zA-Z0-9_-]+)\}|"
		+ // group 19: attr-ref
		 @"(https?://[^\[\s]+)\[([^\]]*)\]|"
		+ // groups 20,21: url
		 @"(?<![a-zA-Z0-9.])\+([^\+]+)\+"
		+ "|"
		+ // group 22: +inline+ passthrough (constrained: not after alphanum)
		 @"``([^`']+)''|"
		+ // group 23: AsciiDoc typographic quotes -> "text"
		 @"\s*\+\s*$" // line-break (no capture)
		)]
	private static partial Regex InlineCombinedRegex();

	[GeneratedRegex(@"\{([a-zA-Z0-9_-]+)\}")]
	private static partial Regex InlineAttrRefRegex();

	// Normalize `<<<<target>>, text>>` (double `<<` source bug) → `<<target, text>>`
	[GeneratedRegex(@"<<<<([^,>]+)>>(,[^>]*)?>")]
	private static partial Regex DoubleXrefRegex();

	public List<IInlineNode> ParseInlines(string text)
	{
		if (string.IsNullOrEmpty(text))
			return [];

		// Normalize `<<<<target>>, text>>` (double-nested xref source bug) to `<<target, text>>`
		if (text.Contains("<<<<", StringComparison.Ordinal))
			text = DoubleXrefRegex().Replace(text, "<<$1$2>>");

		var result = new List<IInlineNode>();
		var lastIndex = 0;

		foreach (Match match in InlineCombinedRegex().Matches(text))
		{
			if (match.Index > lastIndex)
				result.Add(new TextInline(text[lastIndex..match.Index]));

			if (match.Groups[1].Success)
				result.Add(new InlineLinkNode(match.Groups[1].Value, NullIfEmpty(match.Groups[2].Value)));
			else if (match.Groups[5].Success)
				result.Add(new InlineCrossRefNode(match.Groups[5].Value, NullIfEmpty(NormalizeWhitespace(match.Groups[6].Value))));
			else if (match.Groups[3].Success)
				result.Add(new InlineCrossRefNode(match.Groups[3].Value, NullIfEmpty(NormalizeWhitespace(match.Groups[4].Value))));
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
			else if (match.Groups[23].Success)
				result.Add(new TextInline($"\"{match.Groups[23].Value}\""));
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

	private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

	// Collapse newlines and surrounding whitespace in captured inline text to a single space.
	private static string NormalizeWhitespace(string value) =>
		string.IsNullOrEmpty(value) ? value : WhitespaceCollapseRegex().Replace(value.Trim(), " ");

	[GeneratedRegex(@"\s*\n\s*")]
	private static partial Regex WhitespaceCollapseRegex();
}
