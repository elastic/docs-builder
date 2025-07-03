// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Myst.CodeBlocks;
using Elastic.Markdown.Myst.FrontMatter;
using Markdig.Parsers;

namespace Elastic.Markdown.Myst.Directives.AppliesTo;


public class AppliesToDirective(BlockParser parser, ParserContext context)
	: EnhancedCodeBlock(parser, context), IApplicableToElement
{
	public ApplicableTo? AppliesTo { get; set; }
}

