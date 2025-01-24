// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.InlineParsers;
using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;

namespace Elastic.Markdown.Myst.Extensions;

public static class MarkdownPipelineBuilderExtensions
{
	// This is a copy of the DisableHtml extension method with the addition of a HashSet of exceptions
	// to allow certain HTML tags to be parsed.
	// However, as of now it only supports inline HTML tags.
	// If you need to support block HTML tags, you will need to add a custom HtmlBlockParser.
	public static MarkdownPipelineBuilder DisableHtmlWithExceptions(this MarkdownPipelineBuilder pipeline, HashSet<string> exceptions)
	{
		var parser = pipeline.BlockParsers.Find<HtmlBlockParser>();
		if (parser != null)
		{
			pipeline.BlockParsers.Remove(parser);
		}
		pipeline.InlineParsers.ReplaceOrAdd<AutolinkInlineParser>(new RestrictedAutolinkInlineParser { AllowedTags = exceptions });
		return pipeline;
	}
}
