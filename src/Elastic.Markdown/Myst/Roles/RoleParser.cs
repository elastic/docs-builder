// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Diagnostics;
using Elastic.Markdown.Myst.InlineParsers;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Roles;

[DebuggerDisplay("{GetType().Name} Line: {Line}, Role: {Role}, Content: {Content}")]
public abstract class RoleLeaf(string role, string content) : CodeInline(content)
{
	public string Role => role;
}

public abstract class RoleParser<TRole> : InlineParser
	where TRole : RoleLeaf
{
	protected RoleParser() => OpeningCharacters = ['{'];

	private readonly SearchValues<char> _values = SearchValues.Create(['\r', '\n', ' ', '\t', '}']);

	protected abstract TRole CreateRole(string role, string content, InlineProcessor parserContext);

	protected abstract bool Matches(ReadOnlySpan<char> role);

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var match = slice.CurrentChar;

		if (processor.Context is not ParserContext)
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
		if (!Matches(roleContent))
			return false;

		// {role} has to be followed by `content`
		if ((uint)i >= (uint)span.Length || span[i] != '`')
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

		var leaf = CreateRole(roleContent.ToString(), contentSpan.Trim('`').ToString(), processor);
		leaf.Delimiter = '{';
		leaf.Span = sourceSpan;
		leaf.Line = line;
		leaf.Column = column;
		leaf.DelimiterCount = openSticks;

		if (processor.TrackTrivia)
		{
			// startPosition and slice.Start include the opening/closing sticks.
			leaf.ContentWithTrivia =
				new StringSlice(slice.Text, startPosition + openSticks, slice.Start - openSticks - 1);
		}

		processor.Inline = leaf;
		return true;
	}
}
