// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Helpers;

namespace Elastic.Markdown.Myst.Directives.Stepper;

public class StepperBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "stepper";

	public override void FinalizeAndValidate(ParserContext context)
	{
	}
}

public class StepBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public override string Directive => "step";
	public string Title { get; private set; } = string.Empty;
	public string Anchor { get; private set; } = string.Empty;

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Arguments ?? string.Empty;
		Anchor = Prop("anchor") ?? Title.Slugify();

		// Set CrossReferenceName so this step can be found by ToC generation
		if (!string.IsNullOrEmpty(Title))
		{
			CrossReferenceName = Anchor;
		}
	}
}
