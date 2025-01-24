// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.ObjectModel;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers;

// This class is a copy of the AutolinkInlineParser with the addition of a check for allowed tags
public class RestrictedAutolinkInlineParser : InlineParser
{

	public RestrictedAutolinkInlineParser() => OpeningCharacters = ['<'];

	public required IReadOnlySet<string> AllowedTags { get; init; }

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		var saved = slice;
		int line;
		int column;
		if (LinkHelper.TryParseAutolink(ref slice, out var link, out var isEmail))
		{
			processor.Inline = new AutolinkInline(link)
			{
				IsEmail = isEmail,
				Span = new SourceSpan(processor.GetSourcePosition(saved.Start, out line, out column), processor.GetSourcePosition(slice.Start - 1)),
				Line = line,
				Column = column
			};
		}
		else
		{
			slice = saved;
			if (!HtmlHelper.TryParseHtmlTag(ref slice, out var htmlTag))
				return false;
			if (!AllowedTags.Contains(htmlTag))
				return false;
			processor.Inline = new HtmlInline(htmlTag)
			{
				Span = new SourceSpan(processor.GetSourcePosition(saved.Start, out line, out column), processor.GetSourcePosition(slice.Start - 1)),
				Line = line,
				Column = column
			};
		}
		return true;
	}
}
