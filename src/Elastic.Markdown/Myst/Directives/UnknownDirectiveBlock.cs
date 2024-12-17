// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives;

public class UnknownDirectiveBlock(
	DirectiveBlockParser parser,
	string directive,
	Dictionary<string, string> properties,
	ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public override string Directive => directive;

	public override void FinalizeAndValidate(ParserContext context)
	{
	}
}
