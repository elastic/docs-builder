// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Elastic.Markdown.Myst.InlineParsers;

public static class AutoLinkBuilderExtensions
{
	public static MarkdownPipelineBuilder UseAutoLinks(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<AutoLinkBuilderExtension>();
		return pipeline;
	}
}

public class AutoLinkBuilderExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.InlineParsers.InsertBefore<LinkInlineParser>(new AutoLinkInlineParser());

	public void Setup(MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer)
	{
		// No custom renderer needed - we create standard LinkInline objects
		// that are rendered by HtmxLinkInlineRenderer
	}
}

/// <summary>
/// Parses bare https:// URLs and converts them to clickable links.
/// URLs containing elastic.co/docs emit a hint suggesting crosslinks or relative links.
/// </summary>
public class AutoLinkInlineParser : InlineParser
{
	public AutoLinkInlineParser() => OpeningCharacters = ['h'];

	public override bool Match(InlineProcessor processor, ref StringSlice slice)
	{
		// Must start with https://
		var span = slice.AsSpan();
		if (!span.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			return false;

		// Find the end of the URL
		var urlLength = FindUrlEnd(span);
		if (urlLength <= "https://".Length)
			return false; // Just "https://" with nothing after is not valid

		var url = span[..urlLength].ToString();

		// Get source position for proper diagnostics
		var startPosition = slice.Start;
		var start = processor.GetSourcePosition(startPosition, out var line, out var column);
		var spanEnd = start + urlLength - 1;

		// Create a LinkInline with the URL as both href and text
		var linkInline = new LinkInline(url, string.Empty)
		{
			IsClosed = true,
			IsAutoLink = true,
			Span = new SourceSpan(start, spanEnd),
			Line = line,
			Column = column
		};
		_ = linkInline.AppendChild(new LiteralInline(url));

		// Store context data for the renderer (same pattern as DiagnosticLinkInlineParser)
		var context = processor.GetContext();
		linkInline.SetData(nameof(context.CurrentUrlPath), context.CurrentUrlPath);
		linkInline.SetData("isCrossLink", false);

		processor.Inline = linkInline;

		// Emit hint for elastic.co/docs URLs (after setting Inline so position is correct)
		if (url.Contains("elastic.co/docs", StringComparison.OrdinalIgnoreCase))
			processor.EmitHint(linkInline, "Autolink points to elastic.co/docs. Consider using a crosslink or relative link instead.");

		// Advance the slice past the URL
		var end = slice.Start + urlLength;
		while (slice.Start < end)
			slice.SkipChar();

		return true;
	}

	/// <summary>
	/// Finds the end of a URL in the given span, handling trailing punctuation correctly.
	/// </summary>
	private static int FindUrlEnd(ReadOnlySpan<char> span)
	{
		var length = 0;
		var parenDepth = 0;
		var bracketDepth = 0;

		for (var i = 0; i < span.Length; i++)
		{
			var c = span[i];

			// URL terminates at whitespace or control characters
			if (char.IsWhiteSpace(c) || char.IsControl(c))
				break;

			// Track balanced parentheses (common in Wikipedia URLs)
			if (c == '(')
				parenDepth++;
			else if (c == ')')
			{
				if (parenDepth > 0)
					parenDepth--;
				else
					break; // Unbalanced closing paren - not part of URL
			}

			// Track balanced brackets
			if (c == '[')
				bracketDepth++;
			else if (c == ']')
			{
				if (bracketDepth > 0)
					bracketDepth--;
				else
					break; // Unbalanced closing bracket - not part of URL
			}

			// These characters end the URL (Markdown syntax)
			if (c is '<' or '>')
				break;

			length = i + 1;
		}

		// Remove trailing punctuation that's likely sentence punctuation, not part of URL
		while (length > 0 && span[length - 1] is '.' or ',' or ';' or ':' or '!' or '?' or '\'' or '"')
			length--;

		return length;
	}
}
