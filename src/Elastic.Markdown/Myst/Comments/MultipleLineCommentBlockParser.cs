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
			processor.GoToColumn(currentLine.End);
			return BlockState.Continue;
		}
		return BlockState.None;
	}

	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		var currentLine = processor.Line;

		if (!currentLine.Match(BlockEnd))
			return BlockState.Continue;

		block.UpdateSpanEnd(currentLine.End);
		return BlockState.BreakDiscard;
	}
}
