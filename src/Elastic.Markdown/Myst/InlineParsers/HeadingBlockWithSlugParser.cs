// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.InlineParsers;

public static class HeadingBlockWithSlugBuilderExtensions
{
	public static MarkdownPipelineBuilder UseHeadingsWithSlugs(this MarkdownPipelineBuilder pipeline)
	{
		pipeline.Extensions.AddIfNotAlready<HeadingBlockWithSlugBuilderExtension>();
		return pipeline;
	}
}

public class HeadingBlockWithSlugBuilderExtension : IMarkdownExtension
{
	public void Setup(MarkdownPipelineBuilder pipeline) =>
		pipeline.BlockParsers.Replace<HeadingBlockParser>(new HeadingBlockWithSlugParser());

	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) { }
}

public class HeadingBlockWithSlugParser : HeadingBlockParser
{
	public override bool Close(BlockProcessor processor, Block block)
	{
		if (block is not HeadingBlock headerBlock)
			return base.Close(processor, block);

		var text = headerBlock.Lines.Lines[0].Slice.AsSpan();
		headerBlock.SetData("header", text.ToString());

		if (!HeadingAnchorParser.MatchAnchorLine().IsMatch(text))
			return base.Close(processor, block);

		var splits = HeadingAnchorParser.MatchAnchor().EnumerateMatches(text);

		foreach (var match in splits)
		{
			var header = text.Slice(0, match.Index);
			var anchor = text.Slice(match.Index, match.Length);

			var newSlice = new StringSlice(header.ToString());
			headerBlock.Lines.Lines[0] = new StringLine(ref newSlice);
			headerBlock.SetData("anchor", anchor.ToString());
			headerBlock.SetData("header", header.ToString());
			return base.Close(processor, block);
		}

		return base.Close(processor, block);
	}
}

public static partial class HeadingAnchorParser
{
	[GeneratedRegex(@"^.*(?:\[[^[]+\])\s*$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchAnchorLine();

	[GeneratedRegex(@"(?:\[[^[]+\])\s*$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex MatchAnchor();
}
