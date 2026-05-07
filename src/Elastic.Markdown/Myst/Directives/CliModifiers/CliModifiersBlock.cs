// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.CliModifiers;

public class CliModifiersBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "cli-modifiers";

	public bool Destructive { get; private set; }
	public bool RequiresConfirmation { get; private set; }
	public bool RequiresAuth { get; private set; }
	public bool Idempotent { get; private set; }
	public string? Scope { get; private set; }
	public bool Streaming { get; private set; }
	public bool LongRunning { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Destructive = PropBool("destructive");
		RequiresConfirmation = PropBool("requires-confirmation");
		RequiresAuth = PropBool("requires-auth");
		Idempotent = PropBool("idempotent");
		Scope = Prop("scope");
		Streaming = PropBool("streaming");
		LongRunning = PropBool("long-running");
	}
}
