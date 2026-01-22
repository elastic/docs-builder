// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Comments;

[DebuggerDisplay("{GetType().Name} Line: {Line}, {Lines}")]
public class MultipleLineCommentBlock(BlockParser parser) : LeafBlock(parser);

public class MultipleLineCommentBlockParser : BlockParser
{
	public MultipleLineCommentBlockParser() => OpeningCharacters = ['<'];

	private const string BlockStart = "<!--";
	private const string BlockEnd = "-->";

	public override BlockState TryOpen(BlockProcessor processor)
	{
		var currentLine = processor.Line;
		if (currentLine.Match(BlockStart))
		{
			var block = new MultipleLineCommentBlock(this)
			{
				Column = processor.Column,
				Span =
				{
					Start = processor.Start
				}
			};
			processor.NewBlocks.Push(block);

			// Check if the closing --> is on the same line (single-line comment)
			// Search after the opening <!-- (offset by length of BlockStart)
			if (currentLine.IndexOf(BlockEnd, BlockStart.Length, false) >= 0)
			{
				block.UpdateSpanEnd(currentLine.End);
				return BlockState.BreakDiscard;
			}

			processor.GoToColumn(currentLine.End);
			return BlockState.Continue;
		}
		return BlockState.None;
	}

	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		var currentLine = processor.Line;

		// Check if --> appears anywhere in the line, not just at the start
		if (currentLine.IndexOf(BlockEnd, 0, false) < 0)
			return BlockState.Continue;

		block.UpdateSpanEnd(currentLine.End);
		return BlockState.BreakDiscard;
	}
}
