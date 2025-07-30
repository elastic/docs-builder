// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.RegularExpressions;
using Elastic.Markdown.Myst.Roles.Icons;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
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

public partial class HeadingBlockWithSlugParser : HeadingBlockParser
{
	private static readonly Regex IconSyntax = IconParser.IconRegex();
	private static readonly Regex AppliesToSyntax = AppliesToSyntaxRegex();

	private static string StripAppliesToAnnotations(string text)
	{
		// Remove applies_to inline annotations from the text
		return AppliesToSyntax.Replace(text, "").Trim();
	}

	[GeneratedRegex(@"\{applies_to\}`[^`]*`", RegexOptions.Compiled)]
	private static partial Regex AppliesToSyntaxRegex();

	public override bool Close(BlockProcessor processor, Block block)
	{
		if (block is not HeadingBlock headingBlock)
			return base.Close(processor, block);

		var text = headingBlock.Lines.Lines[0].Slice.AsSpan();
		// Remove icon syntax and applies_to annotations from the heading text
		var cleanText = IconSyntax.Replace(text.ToString(), "");
		cleanText = StripAppliesToAnnotations(cleanText);
		headingBlock.SetData("header", cleanText);

		if (!HeadingAnchorParser.MatchAnchorLine().IsMatch(text))
			return base.Close(processor, block);

		var splits = HeadingAnchorParser.MatchAnchor().EnumerateMatches(text);

		foreach (var match in splits)
		{
			var header = text[..match.Index];
			var anchor = text.Slice(match.Index, match.Length);

			var newSlice = new StringSlice(header.ToString());
			headingBlock.Lines.Lines[0] = new StringLine(ref newSlice);

			if (header.IndexOf('$') >= 0)
				anchor = HeadingAnchorParser.MatchAnchor().Replace(anchor.ToString(), "");
			headingBlock.SetData("anchor", anchor.ToString());
			// Remove icon syntax and applies_to annotations from the header text when setting it as data
			var headerText = IconSyntax.Replace(header.ToString(), "");
			headerText = StripAppliesToAnnotations(headerText);
			headingBlock.SetData("header", headerText.Trim());
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

	[GeneratedRegex(@"\$\$\$[^\$]+\$\$\$", RegexOptions.IgnoreCase, "en-US")]
	public static partial Regex InlineAnchors();
}
