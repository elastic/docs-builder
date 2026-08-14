// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Text.RegularExpressions;

namespace Elastic.LegacyDocs.Migration.Asciidoc;

public enum TokenType
{
	SectionTitle,
	AttributeEntry,
	AttributeUnset,
	BlockDelimiter,
	BlockAnchor,
	BlockTitle,
	BlockAttribute,
	ListItemUnordered,
	ListItemOrdered,
	ListContinuation,
	DescriptionListItem,
	TableDelimiter,
	TableRow,
	IncludeDirective,
	ConditionalStart,
	ConditionalEnd,
	ImageBlock,
	AdmonitionParagraph,
	Comment,
	CommentBlockDelim,
	PageBreak,
	ThematicBreak,
	Blank,
	Text
}

public record Token(TokenType Type, string Raw, int LineNumber, TokenMetadata? Metadata = null);

public record TokenMetadata
{
	public int? Level { get; init; }
	public string? Id { get; init; }
	public string? Language { get; init; }
	public string? Path { get; init; }
	public string? Title { get; init; }
	public string? Content { get; init; }
	public string? DelimiterChar { get; init; }
	public string? AttributeName { get; init; }
	public string? AttributeValue { get; init; }
	public string? Condition { get; init; }
	public string? BlockStyle { get; init; }
	public Dictionary<string, string>? NamedAttributes { get; init; }
}

public static partial class AsciidocLexer
{
	private static readonly Regex SectionRegex = GetSectionRegex();
	private static readonly Regex AttributeEntryRegex = GetAttributeEntryRegex();
	private static readonly Regex AttributeUnsetRegex = GetAttributeUnsetRegex();
	private static readonly Regex BlockAnchorRegex = GetBlockAnchorRegex();
	private static readonly Regex BlockTitleRegex = GetBlockTitleRegex();
	private static readonly Regex BlockAttributeRegex = GetBlockAttributeRegex();
	private static readonly Regex BlockDelimiterRegex = GetBlockDelimiterRegex();
	private static readonly Regex UnorderedListRegex = GetUnorderedListRegex();
	private static readonly Regex OrderedListRegex = GetOrderedListRegex();
	private static readonly Regex ListContinuationRegex = GetListContinuationRegex();
	private static readonly Regex DescriptionListRegex = GetDescriptionListRegex();
	private static readonly Regex TableDelimiterRegex = GetTableDelimiterRegex();
	private static readonly Regex IncludeRegex = GetIncludeRegex();
	private static readonly Regex ConditionalStartRegex = GetConditionalStartRegex();
	private static readonly Regex ConditionalEndRegex = GetConditionalEndRegex();
	private static readonly Regex ImageBlockRegex = GetImageBlockRegex();
	private static readonly Regex AdmonitionRegex = GetAdmonitionRegex();
	private static readonly Regex CommentBlockDelimRegex = GetCommentBlockDelimRegex();
	private static readonly Regex CommentRegex = GetCommentRegex();
	private static readonly Regex PageBreakRegex = GetPageBreakRegex();
	private static readonly Regex ThematicBreakRegex = GetThematicBreakRegex();
	private static readonly Regex BlankRegex = GetBlankRegex();

	[GeneratedRegex(@"^(={1,6})\s+(.+)$")]
	private static partial Regex GetSectionRegex();

	[GeneratedRegex(@"^:([^!:][^:]*?):\s*(.*)$")]
	private static partial Regex GetAttributeEntryRegex();

	[GeneratedRegex(@"^:!([^:]+):$")]
	private static partial Regex GetAttributeUnsetRegex();

	[GeneratedRegex(@"^\[\[([^\]]+)\]\]$")]
	private static partial Regex GetBlockAnchorRegex();

	[GeneratedRegex(@"^\.(\S.*)$")]
	private static partial Regex GetBlockTitleRegex();

	[GeneratedRegex(@"^\[(.+)\]\s*$")]
	private static partial Regex GetBlockAttributeRegex();

	[GeneratedRegex(@"^(-{4,}|\.{4,}|={4,}|\*{4,}|\+{4,}|/{4,}|-{2})\s*$")]
	private static partial Regex GetBlockDelimiterRegex();

	[GeneratedRegex(@"^(\*{1,5})\s+(.+)$")]
	private static partial Regex GetUnorderedListRegex();

	[GeneratedRegex(@"^(\.{1,5})\s+(.+)$")]
	private static partial Regex GetOrderedListRegex();

	[GeneratedRegex(@"^\+\s*$")]
	private static partial Regex GetListContinuationRegex();

	[GeneratedRegex(@"^(.+?)(:{2,4})\s*(.*)$")]
	private static partial Regex GetDescriptionListRegex();

	[GeneratedRegex(@"^\|={3,}\s*$")]
	private static partial Regex GetTableDelimiterRegex();

	[GeneratedRegex(@"^include::(.+?)\[(.*?)?\]$")]
	private static partial Regex GetIncludeRegex();

	[GeneratedRegex(@"^(ifdef|ifndef|ifeval)::(.*?)\[(.*?)?\]$")]
	private static partial Regex GetConditionalStartRegex();

	[GeneratedRegex(@"^endif::(.*?)?\[(.*?)?\]$")]
	private static partial Regex GetConditionalEndRegex();

	[GeneratedRegex(@"^image::(.+?)\[(.*?)?\]$")]
	private static partial Regex GetImageBlockRegex();

	[GeneratedRegex(@"^(NOTE|TIP|WARNING|IMPORTANT|CAUTION):\s+(.+)$")]
	private static partial Regex GetAdmonitionRegex();

	[GeneratedRegex(@"^/{4,}$")]
	private static partial Regex GetCommentBlockDelimRegex();

	[GeneratedRegex(@"^//\s*(.*)$")]
	private static partial Regex GetCommentRegex();

	[GeneratedRegex(@"^<<<\s*$")]
	private static partial Regex GetPageBreakRegex();

	[GeneratedRegex(@"^'{3,}\s*$")]
	private static partial Regex GetThematicBreakRegex();

	[GeneratedRegex(@"^\s*$")]
	private static partial Regex GetBlankRegex();

	public static IReadOnlyList<Token> Tokenize(string content)
	{
		var lines = content.Split('\n');
		var tokens = new List<Token>();
		var inCommentBlock = false;
		var inVerbatimBlock = false;
		var verbatimDelimiter = "";
		var inTable = false;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i].TrimEnd('\r');
			var lineNumber = i + 1;

			if (inCommentBlock)
			{
				if (CommentBlockDelimRegex.IsMatch(line))
					inCommentBlock = false;
				continue;
			}

			if (inVerbatimBlock)
			{
				if (IsMatchingDelimiter(line, verbatimDelimiter))
				{
					inVerbatimBlock = false;
					tokens.Add(new Token(TokenType.BlockDelimiter, line, lineNumber, new TokenMetadata { DelimiterChar = verbatimDelimiter[..1] }));
				}
				else
				{
					tokens.Add(new Token(TokenType.Text, line, lineNumber));
				}
				continue;
			}

			if (CommentBlockDelimRegex.IsMatch(line))
			{
				inCommentBlock = true;
				continue;
			}

			var match = BlockDelimiterRegex.Match(line);
			if (match.Success)
			{
				var delim = match.Groups[1].Value;
				var delimChar = delim[..1];

				if (delimChar is "-" or "." or "/" or "+")
				{
					// `--` (length 2) is an open block delimiter, not verbatim.
					// Only `----`, `....`, `++++`, `////` (length >= 4) are verbatim.
					if (delim.Length < 4)
					{
						tokens.Add(new Token(TokenType.BlockDelimiter, line, lineNumber, new TokenMetadata { DelimiterChar = delimChar }));
						continue;
					}

					if (delimChar == "/")
					{
						inCommentBlock = true;
						continue;
					}

					inVerbatimBlock = true;
					verbatimDelimiter = delim;
					tokens.Add(new Token(TokenType.BlockDelimiter, line, lineNumber, new TokenMetadata { DelimiterChar = delimChar }));
					continue;
				}

				if (delimChar is "=" or "*")
				{
					tokens.Add(new Token(TokenType.BlockDelimiter, line, lineNumber, new TokenMetadata { DelimiterChar = delimChar }));
					continue;
				}
			}

			if (inTable)
			{
				if (TableDelimiterRegex.IsMatch(line))
				{
					inTable = false;
					tokens.Add(new Token(TokenType.TableDelimiter, line, lineNumber));
					continue;
				}

				if (BlankRegex.IsMatch(line))
				{
					tokens.Add(new Token(TokenType.Blank, line, lineNumber));
					continue;
				}

				var condStart = ConditionalStartRegex.Match(line);
				if (condStart.Success)
				{
					tokens.Add(new Token(TokenType.ConditionalStart, line, lineNumber, new TokenMetadata
					{
						Condition = condStart.Groups[2].Value,
						Content = condStart.Groups[3].Value,
						BlockStyle = condStart.Groups[1].Value
					}));
					continue;
				}

				var condEnd = ConditionalEndRegex.Match(line);
				if (condEnd.Success)
				{
					tokens.Add(new Token(TokenType.ConditionalEnd, line, lineNumber, new TokenMetadata
					{
						Condition = condEnd.Groups[1].Value
					}));
					continue;
				}

				if (CommentRegex.IsMatch(line))
				{
					tokens.Add(new Token(TokenType.Comment, line, lineNumber));
					continue;
				}

				if (line.StartsWith('|'))
				{
					tokens.Add(new Token(TokenType.TableRow, line, lineNumber, new TokenMetadata { Content = line[1..] }));
					continue;
				}

				tokens.Add(new Token(TokenType.Text, line, lineNumber));
				continue;
			}

			if (TableDelimiterRegex.IsMatch(line))
			{
				inTable = true;
				tokens.Add(new Token(TokenType.TableDelimiter, line, lineNumber));
				continue;
			}

			if (TryMatchToken(line, lineNumber, out var token))
			{
				tokens.Add(token);
				continue;
			}

			tokens.Add(new Token(TokenType.Text, line, lineNumber));
		}

		return tokens;
	}

	private static bool IsMatchingDelimiter(string line, string openDelimiter)
	{
		if (openDelimiter.Length < 2)
			return false;

		var delimChar = openDelimiter[0];
		// Trim trailing whitespace so source bugs like `---- ` still close a `----` block
		var trimmed = line.TrimEnd();
		// Require exact length so e.g. `--------` does not close a `----` block
		return trimmed.Length == openDelimiter.Length && trimmed.All(c => c == delimChar);
	}

	private static bool TryMatchToken(string line, int lineNumber, out Token token)
	{
		token = default!;

		var m = SectionRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.SectionTitle, line, lineNumber, new TokenMetadata
			{
				Level = m.Groups[1].Value.Length - 1,
				Title = m.Groups[2].Value.Trim()
			});
			return true;
		}

		m = AttributeUnsetRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.AttributeUnset, line, lineNumber, new TokenMetadata
			{
				AttributeName = m.Groups[1].Value
			});
			return true;
		}

		m = AttributeEntryRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.AttributeEntry, line, lineNumber, new TokenMetadata
			{
				AttributeName = m.Groups[1].Value,
				AttributeValue = m.Groups[2].Value
			});
			return true;
		}

		m = BlockAnchorRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.BlockAnchor, line, lineNumber, new TokenMetadata
			{
				Id = m.Groups[1].Value
			});
			return true;
		}

		m = ConditionalStartRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.ConditionalStart, line, lineNumber, new TokenMetadata
			{
				Condition = m.Groups[2].Value,
				Content = m.Groups[3].Value,
				BlockStyle = m.Groups[1].Value
			});
			return true;
		}

		m = ConditionalEndRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.ConditionalEnd, line, lineNumber, new TokenMetadata
			{
				Condition = m.Groups[1].Value
			});
			return true;
		}

		m = IncludeRegex.Match(line);
		if (m.Success)
		{
			var attrs = ParseBlockAttributeContent(m.Groups[2].Value);
			token = new Token(TokenType.IncludeDirective, line, lineNumber, new TokenMetadata
			{
				Path = m.Groups[1].Value,
				NamedAttributes = attrs
			});
			return true;
		}

		m = ImageBlockRegex.Match(line);
		if (m.Success)
		{
			var attrs = ParseInlineAttributes(m.Groups[2].Value);
			token = new Token(TokenType.ImageBlock, line, lineNumber, new TokenMetadata
			{
				Path = m.Groups[1].Value,
				Title = attrs.GetValueOrDefault("alt") ?? attrs.GetValueOrDefault("0"),
				NamedAttributes = attrs
			});
			return true;
		}

		m = AdmonitionRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.AdmonitionParagraph, line, lineNumber, new TokenMetadata
			{
				BlockStyle = m.Groups[1].Value,
				Content = m.Groups[2].Value
			});
			return true;
		}

		if (PageBreakRegex.IsMatch(line))
		{
			token = new Token(TokenType.PageBreak, line, lineNumber);
			return true;
		}

		if (ThematicBreakRegex.IsMatch(line))
		{
			token = new Token(TokenType.ThematicBreak, line, lineNumber);
			return true;
		}

		m = ListContinuationRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.ListContinuation, line, lineNumber);
			return true;
		}

		m = UnorderedListRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.ListItemUnordered, line, lineNumber, new TokenMetadata
			{
				Level = m.Groups[1].Value.Length,
				Content = m.Groups[2].Value
			});
			return true;
		}

		m = OrderedListRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.ListItemOrdered, line, lineNumber, new TokenMetadata
			{
				Level = m.Groups[1].Value.Length,
				Content = m.Groups[2].Value
			});
			return true;
		}

		m = CommentRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.Comment, line, lineNumber, new TokenMetadata
			{
				Content = m.Groups[1].Value
			});
			return true;
		}

		if (BlankRegex.IsMatch(line))
		{
			token = new Token(TokenType.Blank, line, lineNumber);
			return true;
		}

		m = BlockAttributeRegex.Match(line);
		if (m.Success && !BlockAnchorRegex.IsMatch(line))
		{
			var content = m.Groups[1].Value;
			var parsed = ParseBlockAttributeContent(content);
			string? style = null;
			string? language = null;

			var positional = content.Split(',');
			if (positional.Length > 0)
			{
				var first = positional[0].Trim().Trim('"');
				if (!first.Contains('='))
					style = first;
			}
			if (positional.Length > 1 && style?.Equals("source", StringComparison.OrdinalIgnoreCase) == true)
			{
				var second = positional[1].Trim().Trim('"');
				if (!second.Contains('='))
					language = second;
			}

			token = new Token(TokenType.BlockAttribute, line, lineNumber, new TokenMetadata
			{
				BlockStyle = style,
				Language = language,
				Content = content,
				NamedAttributes = parsed
			});
			return true;
		}

		m = BlockTitleRegex.Match(line);
		if (m.Success)
		{
			token = new Token(TokenType.BlockTitle, line, lineNumber, new TokenMetadata
			{
				Title = m.Groups[1].Value
			});
			return true;
		}

		m = DescriptionListRegex.Match(line);
		if (m.Success)
		{
			var term = m.Groups[1].Value;
			var separator = m.Groups[2].Value;
			var desc = m.Groups[3].Value;
			if (!term.Contains("://") && !term.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				token = new Token(TokenType.DescriptionListItem, line, lineNumber, new TokenMetadata
				{
					Title = term,
					Content = desc,
					Level = separator.Length
				});
				return true;
			}
		}

		return false;
	}

	private static Dictionary<string, string> ParseBlockAttributeContent(string content)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(content))
			return result;

		var parts = SplitRespectingQuotes(content);
		var positionalIndex = 0;
		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			var eqIndex = trimmed.IndexOf('=');
			if (eqIndex > 0)
			{
				var key = trimmed[..eqIndex].Trim();
				var value = trimmed[(eqIndex + 1)..].Trim().Trim('"');
				result[key] = value;
			}
			else
			{
				result[positionalIndex.ToString(CultureInfo.InvariantCulture)] = trimmed.Trim('"');
				positionalIndex++;
			}
		}
		return result;
	}

	private static Dictionary<string, string> ParseInlineAttributes(string content)
	{
		var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(content))
			return result;

		var parts = SplitRespectingQuotes(content);
		var positionalIndex = 0;
		foreach (var part in parts)
		{
			var trimmed = part.Trim();
			var eqIndex = trimmed.IndexOf('=');
			if (eqIndex > 0)
			{
				var key = trimmed[..eqIndex].Trim();
				var value = trimmed[(eqIndex + 1)..].Trim().Trim('"');
				result[key] = value;
			}
			else
			{
				if (positionalIndex == 0)
					result["alt"] = trimmed.Trim('"');
				result[positionalIndex.ToString(CultureInfo.InvariantCulture)] = trimmed.Trim('"');
				positionalIndex++;
			}
		}
		return result;
	}

	private static List<string> SplitRespectingQuotes(string input)
	{
		var parts = new List<string>();
		var current = new System.Text.StringBuilder();
		var inQuotes = false;

		foreach (var c in input)
		{
			if (c == '"')
			{
				inQuotes = !inQuotes;
				_ = current.Append(c);
			}
			else if (c == ',' && !inQuotes)
			{
				parts.Add(current.ToString());
				_ = current.Clear();
			}
			else
			{
				_ = current.Append(c);
			}
		}

		if (current.Length > 0)
			parts.Add(current.ToString());

		return parts;
	}
}
