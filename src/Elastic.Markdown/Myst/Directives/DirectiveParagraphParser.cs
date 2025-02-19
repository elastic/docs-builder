// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using Markdig.Parsers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives;

public class DirectiveParagraphParser : ParagraphBlockParser
{
	public override BlockState TryOpen(BlockProcessor processor)
	{
		var line = processor.Line.AsSpan();

		// TODO Validate properties on directive.
		if (line.StartsWith(":") && processor.CurrentBlock is DirectiveBlock)
			return BlockState.None;
		if (line.StartsWith(":"))
			return BlockState.None;

		return base.TryOpen(processor);
	}

	public override BlockState TryContinue(BlockProcessor processor, Block block)
	{
		if (block is not ParagraphBlock paragraphBlock)
			return base.TryContinue(processor, block);

		if (block.Parent is not DirectiveBlock)
			return base.TryContinue(processor, block);

		var lines = paragraphBlock.Lines.Lines;
		if (lines.Length < 1)
			return base.TryContinue(processor, block);

		var line = lines[0];
		return line.Slice.AsSpan().StartsWith(':')
			? BlockState.BreakDiscard
			: base.TryContinue(processor, block);
	}
}
