// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers.Icon;

[DebuggerDisplay("Icon: {IconName}")]
public class IconLeaf(string iconName) : CodeInline($":{iconName}:")
{
	public string IconName { get; } = iconName;
}

public partial class IconParser : InlineParser
{

	[GeneratedRegex(@":[a-zA-Z0-9_]+:", RegexOptions.Compiled)]
	public static partial Regex IconRegex();

	public IconParser() => OpeningCharacters = [':'];

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{

		var startPosition = slice.Start;

		if (!slice.PeekCharExtra(-1).IsWhitespace())
			return false;

		// Advance the slice past ":"
		slice.Start += 1;

		var end = slice.AsSpan().IndexOf(':');
		if (end <= 0) // no closing ':' or empty name `::`
		{
			slice.Start = startPosition;
			return false;
		}

		var nameSlice = new StringSlice(slice.Text, slice.Start, slice.Start + end - 1);
		var nameSpan = nameSlice.AsSpan();

		// Validate characters in name
		foreach (var t in nameSpan)
		{
			if (!char.IsLetterOrDigit(t) && t != '_')
			{
				slice.Start = startPosition;
				return false;
			}
		}

		var iconName = nameSlice.ToString();

		var finalPosition = nameSlice.End + 2;
		slice.Start = finalPosition;

		var sourceSpan = new SourceSpan(processor.GetSourcePosition(startPosition, out var line, out var column), processor.GetSourcePosition(finalPosition - 1));

		var leaf = new IconLeaf(iconName)
		{
			Span = sourceSpan,
			Line = line,
			Column = column,
		};

		processor.Inline = leaf;
		return true;
	}
}
