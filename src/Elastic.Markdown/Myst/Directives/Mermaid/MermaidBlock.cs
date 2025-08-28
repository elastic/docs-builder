// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives.Mermaid;

public class MermaidBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "mermaid";

	public override void FinalizeAndValidate(ParserContext context) =>
		// Call the DirectiveBlock's FinalizeAndValidate
		// for setup common to all the directive blocks
		base.FinalizeAndValidate(context);
}
