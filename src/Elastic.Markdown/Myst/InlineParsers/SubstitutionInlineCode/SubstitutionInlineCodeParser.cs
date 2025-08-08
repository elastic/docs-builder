// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Documentation;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.Myst.InlineParsers.Substitution;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers.SubstitutionInlineCode;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Content: {Content}, ProcessedContent: {ProcessedContent}")]
public class SubstitutionInlineCodeLeaf(string content, string processedContent) : CodeInline(content)
{
	public string ProcessedContent { get; } = processedContent;
}

public class SubstitutionInlineCodeRenderer : HtmlObjectRenderer<SubstitutionInlineCodeLeaf>
{
	protected override void Write(HtmlRenderer renderer, SubstitutionInlineCodeLeaf leaf)
	{
		// Render as a code element with the processed content (substitutions applied)
		_ = renderer.Write("<code");
		_ = renderer.WriteAttributes(leaf);
		_ = renderer.Write(">");
		_ = renderer.WriteEscape(leaf.ProcessedContent);
		_ = renderer.Write("</code>");
	}
}

public partial class SubstitutionInlineCodeParser : InlineParser
{
	public SubstitutionInlineCodeParser() => OpeningCharacters = ['{'];

	private readonly SearchValues<char> _values = SearchValues.Create(['\r', '\n', ' ', '\t', '}']);
	private static readonly Regex SubstitutionPattern = SubstitutionRegex();

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = slice.CurrentChar;

		if (processor.Context is not ParserContext context)
			return false;

		Debug.Assert(match is not ('\r' or '\n'));

		// Match the opened sticks
		var openSticks = slice.CountAndSkipChar(match);
		if (openSticks > 1)
			return false;

		var span = slice.AsSpan();

		var i = span.IndexOfAny(_values);

		// We got to the end of the input before seeing the match character.
		if ((uint)i >= (uint)span.Length)
			return false;

		var closeSticks = 0;
		while ((uint)i < (uint)span.Length && span[i] == '}')
		{
			closeSticks++;
			i++;
		}

		if (closeSticks > 1)
			return false;

		var roleContent = slice.AsSpan()[..i];

		// Check if this matches the "subs=true" pattern
		if (!roleContent.SequenceEqual("{subs=true}".AsSpan()))
			return false;

		// Check if the next character is a backtick
		if (i >= span.Length || span[i] != '`')
			return false;

		var openingBacktickPos = i;
		var contentStartPos = i + 1; // Skip the opening backtick

		var closingBacktickIndex = -1;
		for (var j = contentStartPos; j < span.Length; j++)
		{
			if (span[j] != '`')
				continue;
			closingBacktickIndex = j;
			break;
		}

		if (closingBacktickIndex == -1)
			return false;

		var contentSpan = span[openingBacktickPos..(closingBacktickIndex + 1)];

		var startPosition = slice.Start;
		slice.Start = startPosition + roleContent.Length + contentSpan.Length;

		// We've already skipped the opening sticks. Account for that here.
		startPosition -= openSticks;
		startPosition = Math.Max(startPosition, 0);

		var start = processor.GetSourcePosition(startPosition, out var line, out var column);
		var end = processor.GetSourcePosition(slice.Start);
		var sourceSpan = new SourceSpan(start, end);

		// Extract the actual code content (without backticks)
		var codeContent = contentSpan.Trim('`').ToString();

		// Process substitutions in the code content
		var processedContent = ProcessSubstitutions(codeContent, context, processor, line, column);

		var leaf = new SubstitutionInlineCodeLeaf(codeContent, processedContent)
		{
			Delimiter = '{',
			Span = sourceSpan,
			Line = line,
			Column = column,
			DelimiterCount = openSticks
		};

		if (processor.TrackTrivia)
		{
			// startPosition and slice.Start include the opening/closing sticks.
			leaf.ContentWithTrivia =
				new StringSlice(slice.Text, startPosition + openSticks, slice.Start - openSticks - 1);
		}

		processor.Inline = leaf;
		return true;
	}

	private static string ProcessSubstitutions(string content, ParserContext context, InlineProcessor processor, int line, int column)
	{
		var result = new StringBuilder(content);
		var substitutions = new List<(int Start, int Length, string Replacement)>();

		// Find all substitution patterns
		foreach (Match match in SubstitutionPattern.Matches(content))
		{
			var rawKey = match.Groups[1].Value.Trim().ToLowerInvariant();
			var found = false;
			var replacement = string.Empty;

			// Use shared mutation parsing logic
			var (key, mutationStrings) = SubstitutionMutationHelper.ParseKeyWithMutations(rawKey);

			if (context.Substitutions.TryGetValue(key, out var value) && value is not null)
			{
				found = true;
				replacement = value;
			}
			else if (context.ContextSubstitutions.TryGetValue(key, out value) && value is not null)
			{
				found = true;
				replacement = value;
			}

			if (found)
			{
				context.Build.Collector.CollectUsedSubstitutionKey(key);

				// Apply mutations if any
				if (mutationStrings.Length > 0)
				{
					if (mutationStrings.Length >= 10)
					{
						processor.EmitError(line + 1, column + match.Index, match.Length, $"Substitution key {{{key}}} defines too many mutations, none will be applied");
						replacement = value; // Use original value without mutations
					}
					else
					{
						var mutations = new List<SubstitutionMutation>();
						foreach (var mutationStr in mutationStrings)
						{
							var trimmedMutation = mutationStr.Trim();
							if (SubstitutionMutationExtensions.TryParse(trimmedMutation, out var mutation, true, true))
							{
								mutations.Add(mutation);
							}
							else
							{
								processor.EmitError(line + 1, column + match.Index, match.Length, $"Mutation '{trimmedMutation}' on {{{key}}} is undefined");
							}
						}

						if (mutations.Count > 0)
							replacement = SubstitutionMutationHelper.ApplyMutations(replacement, mutations);
					}
				}

				substitutions.Add((match.Index, match.Length, replacement ?? string.Empty));
			}
			else
			{
				// We temporarily diagnose variable spaces as hints. We used to not read this at all.
				processor.Emit(key.Contains(' ') ? Severity.Hint : Severity.Error, line + 1, column + match.Index, match.Length, $"Substitution key {{{key}}} is undefined");
			}
		}

		// Apply substitutions in reverse order to maintain correct indices
		foreach (var (start, length, replacement) in substitutions.OrderByDescending(s => s.Start))
		{
			_ = result.Remove(start, length);
			_ = result.Insert(start, replacement);
		}

		return result.ToString();
	}

	[GeneratedRegex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled)]
	private static partial Regex SubstitutionRegex();
}
