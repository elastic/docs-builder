// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
namespace Elastic.Markdown.Myst.Directives;

public class CodeBlock(
	DirectiveBlockParser parser,
	string directive,
	Dictionary<string, string> properties,
	ParserContext context)
	: DirectiveBlock(parser, properties, context)
{
	public override string Directive => directive;
	public string? Caption { get; private set; }

	public string Language
	{
		get
		{
			var language = (Directive is "code" or "code-block" or "sourcecode" ? Arguments : Info) ?? "unknown";
			return language;

		}
	}

	public override void FinalizeAndValidate(ParserContext context)
	{
		Caption = Properties.GetValueOrDefault("caption");
		CrossReferenceName = Prop("name", "label");
	}
}
