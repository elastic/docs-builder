// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Diagnostics;
using Elastic.Markdown.Diagnostics;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers.Substitution;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Found: {Found}, Replacement: {Replacement}")]
public class SubstitutionLeaf(string content, bool found, string replacement) : CodeInline(content)
{
	public bool Found { get; } = found;
	public string Replacement { get; } = replacement;
}

public class SubstitutionRenderer : HtmlObjectRenderer<SubstitutionLeaf>
{
	protected override void Write(HtmlRenderer renderer, SubstitutionLeaf obj) =>
		renderer.Write(obj.Found ? obj.Replacement : obj.Content);
}

public class SubstitutionParser : InlineParser
{
	public SubstitutionParser() => OpeningCharacters = ['{'];

	private readonly SearchValues<char> _values = SearchValues.Create(['\r', '\n', ' ', '\t', '}']);

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = slice.CurrentChar;
		if (slice.PeekCharExtra(1) != match)
			return false;

		if (processor.Context is not ParserContext context)
			return false;

		Debug.Assert(match is not ('\r' or '\n'));

		// Match the opened sticks
		var openSticks = slice.CountAndSkipChar(match);

		var span = slice.AsSpan();

		var i = span.IndexOfAny(_values);

		if ((uint)i >= (uint)span.Length)
		{
			// We got to the end of the input before seeing the match character.
			return false;
		}

		var closeSticks = 0;

		while ((uint)i < (uint)span.Length && span[i] == '}')
		{
			closeSticks++;
			i++;
		}

		span = span[i..];

		if (closeSticks != 2)
			return false;

		var rawContent = slice.AsSpan()[..(slice.Length - span.Length)];

		var content = new LazySubstring(slice.Text, slice.Start, rawContent.Length);

		var startPosition = slice.Start;
		slice.Start = startPosition + rawContent.Length;

		// We've already skipped the opening sticks. Account for that here.
		startPosition -= openSticks;
		startPosition = Math.Max(startPosition, 0);

		var key = content.ToString().Trim(['{', '}']).ToLowerInvariant();
		var found = false;
		var replacement = string.Empty;
		if (context.Substitutions.TryGetValue(key, out var value))
		{
			found = true;
			replacement = value;
		}
		else if (context.ContextSubstitutions.TryGetValue(key, out value))
		{
			found = true;
			replacement = value;
		}
		if (found)
			context.Build.Collector.CollectUsedSubstitutionKey(key);

		var start = processor.GetSourcePosition(startPosition, out var line, out var column);
		var end = processor.GetSourcePosition(slice.Start);
		var sourceSpan = new SourceSpan(start, end);

		var substitutionLeaf = new SubstitutionLeaf(content.ToString(), found, replacement)
		{
			Delimiter = '{',
			Span = sourceSpan,
			Line = line,
			Column = column,
			DelimiterCount = openSticks
		};
		if (!found)
			processor.EmitError(line + 1, column + 3, substitutionLeaf.Span.Length - 3, $"Substitution key {{{key}}} is undefined");

		if (processor.TrackTrivia)
		{
			// startPosition and slice.Start include the opening/closing sticks.
			substitutionLeaf.ContentWithTrivia =
				new StringSlice(slice.Text, startPosition + openSticks, slice.Start - openSticks - 1);
		}

		processor.Inline = substitutionLeaf;
		return true;
	}
}
