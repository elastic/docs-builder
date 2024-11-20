// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Buffers;
using System.Diagnostics;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using Elastic.Markdown.Diagnostics;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.Substitution;

public static class StringSliceExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int CountAndSkipChar(this StringSlice slice, char matchChar)
	{
		var text = slice.Text;
		var end = slice.End;
		var current = slice.Start;

		while (current <= end && (uint)current < (uint)text.Length && text[current] == matchChar)
			current++;

		var count = current - slice.Start;
		slice.Start = current;
		return count;
	}
}

internal struct LazySubstring
{
    private string _text;
    public int Offset;
    public int Length;

    public LazySubstring(string text)
    {
        _text = text;
        Offset = 0;
        Length = text.Length;
    }

    public LazySubstring(string text, int offset, int length)
    {
        Debug.Assert((ulong)offset + (ulong)length <= (ulong)text.Length, $"{offset}-{length} in {text}");
        _text = text;
        Offset = offset;
        Length = length;
    }

    public ReadOnlySpan<char> AsSpan() => _text.AsSpan(Offset, Length);

    public override string ToString()
    {
        if (Offset != 0 || Length != _text.Length)
        {
            _text = _text.Substring(Offset, Length);
            Offset = 0;
        }

        return _text;
    }
}

[DebuggerDisplay("{GetType().Name} Line: {Line}, {Lines} Level: {Level}")]
public class SubstitutionLeaf(string content, bool found, string replacement) : CodeInline(content)
{
	public bool Found { get; } = found;
	public string Replacement { get; } = replacement;
}

public class SubstitutionRenderer : HtmlObjectRenderer<SubstitutionLeaf>
{
	protected override void Write(HtmlRenderer renderer, SubstitutionLeaf obj)
	{
		if (obj.Found)
			renderer.Write(obj.Replacement);
		else
			renderer.Write(obj.Content);
	}
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

		var rawContent = slice.AsSpan().Slice(0, slice.Length - span.Length);

		var content = new LazySubstring(slice.Text, slice.Start, rawContent.Length);

		var startPosition = slice.Start;
		slice.Start = startPosition + rawContent.Length;

		// We've already skipped the opening sticks. Account for that here.
		startPosition -= openSticks;

		var key = content.ToString().Trim(['{', '}']);
		var found = false;
		var replacement = string.Empty;
		if (processor.Context?.Properties.TryGetValue(key, out var value) ?? false)
		{
			found = true;
			replacement = value.ToString() ?? string.Empty;
		}

		var substitutionLeaf = new SubstitutionLeaf(content.ToString(), found, replacement)
		{
			Delimiter = slice.Text[startPosition],
			Span = new SourceSpan(processor.GetSourcePosition(startPosition, out var line, out var column),
				processor.GetSourcePosition(slice.Start)),
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
