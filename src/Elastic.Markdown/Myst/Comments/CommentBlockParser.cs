// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System.Diagnostics;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Comments;

public class CommentRenderer : HtmlObjectRenderer<CommentBlock>
{
	protected override void Write(HtmlRenderer renderer, CommentBlock obj)
	{
	}
}

[DebuggerDisplay("{GetType().Name} Line: {Line}, {Lines} Level: {Level}")]
public class CommentBlock : LeafBlock
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HeadingBlock"/> class.
	/// </summary>
	/// <param name="parser">The parser.</param>
	public CommentBlock(BlockParser parser) : base(parser) => ProcessInlines = true;

	/// <summary>
	/// Gets or sets the header character used to defines this heading (usually #)
	/// </summary>
	public char CommentChar { get; set; }

	/// <summary>
	/// Gets or sets the level of heading (starting at 1 for the lowest level).
	/// </summary>
	public int Level { get; set; }
}

/// <summary>
/// Block parser for a <see cref="HeadingBlock"/>.
/// </summary>
/// <seealso cref="BlockParser" />
public class CommentBlockParser : BlockParser
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HeadingBlockParser"/> class.
	/// </summary>
	public CommentBlockParser() => OpeningCharacters = ['%'];

	/// <summary>
	/// Gets or sets the max count of the leading unescaped # characters
	/// </summary>
	public int MaxLeadingCount { get; set; } = 1;

	public override BlockState TryOpen(BlockProcessor processor)
	{
		// If we are in a CodeIndent, early exit
		if (processor.IsCodeIndent)
			return BlockState.None;

		var column = processor.Column;
		var line = processor.Line;
		var sourcePosition = line.Start;
		var c = line.CurrentChar;
		var matchingChar = c;

		Debug.Assert(MaxLeadingCount > 0);
		var leadingCount = 0;
		while (c != '\0' && leadingCount <= MaxLeadingCount)
		{
			if (c != matchingChar)
				break;
			c = processor.NextChar();
			leadingCount++;
		}

		// A space is required after leading %
		if (leadingCount > 0 && leadingCount <= MaxLeadingCount && (c.IsSpaceOrTab() || c == '\0'))
		{
			if (processor.TrackTrivia && c.IsSpaceOrTab())
				processor.NextChar();

			// Move to the content
			var headingBlock = new CommentBlock(this)
			{
				CommentChar = matchingChar,
				Level = leadingCount,
				Column = column,
				Span = { Start = sourcePosition },
			};

			if (processor.TrackTrivia)
			{
				headingBlock.TriviaBefore = processor.UseTrivia(sourcePosition - 1);

				var linesBefore = processor.LinesBefore;
				processor.LinesBefore = null;
				headingBlock.LinesBefore = linesBefore;
				headingBlock.NewLine = processor.Line.NewLine;
			}
			else
				processor.GoToColumn(column + leadingCount + 1);

			processor.NewBlocks.Push(headingBlock);

			// The optional closing sequence of #s must be preceded by a space and may be followed by spaces only.
			var endState = 0;
			var countClosingTags = 0;
			for (var i = processor.Line.End;
				 i >= processor.Line.Start - 1;
				 i--) // Go up to Start - 1 in order to match the space after the first ###
			{
				c = processor.Line.Text[i];
				if (endState == 0)
				{
					if (c.IsSpaceOrTab())
						continue;
					endState = 1;
				}

				if (endState == 1)
				{
					if (c == matchingChar)
					{
						countClosingTags++;
						continue;
					}

					if (countClosingTags > 0)
					{
						if (c.IsSpaceOrTab())
							processor.Line.End = i - 1;
					}

					break;
				}
			}

			// Set up the source end position of this element
			headingBlock.Span.End = processor.Line.End;

			// We expect a single line, so don't continue
			return BlockState.Break;
		}

		// Else we don't have an header
		processor.Line.Start = sourcePosition;
		processor.Column = column;
		return BlockState.None;
	}

	public override bool Close(BlockProcessor processor, Block block)
	{
		if (!processor.TrackTrivia)
		{
			var heading = (CommentBlock)block;
			heading.Lines.Trim();
		}

		return true;
	}
}
