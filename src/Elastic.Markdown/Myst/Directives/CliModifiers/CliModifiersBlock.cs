// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.CliModifiers;

public class CliModifiersBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "cli-modifiers";

	public bool Destructive { get; private set; }
	public string? DestructiveDescription { get; private set; }
	public bool RequiresConfirmation { get; private set; }
	public string? RequiresConfirmationDescription { get; private set; }
	public bool RequiresAuth { get; private set; }
	public string? RequiresAuthDescription { get; private set; }
	public bool Idempotent { get; private set; }
	public string? IdempotentDescription { get; private set; }
	public string? Scope { get; private set; }
	public string? ScopeDescription { get; private set; }
	public bool Streaming { get; private set; }
	public string? StreamingDescription { get; private set; }
	public bool LongRunning { get; private set; }
	public string? LongRunningDescription { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Destructive = PropBool("destructive");
		DestructiveDescription = Prop("destructive-description");
		RequiresConfirmation = PropBool("requires-confirmation");
		RequiresConfirmationDescription = Prop("requires-confirmation-description");
		RequiresAuth = PropBool("requires-auth");
		RequiresAuthDescription = Prop("requires-auth-description");
		Idempotent = PropBool("idempotent");
		IdempotentDescription = Prop("idempotent-description");
		Scope = Prop("scope");
		ScopeDescription = Prop("scope-description");
		Streaming = PropBool("streaming");
		StreamingDescription = Prop("streaming-description");
		LongRunning = PropBool("long-running");
		LongRunningDescription = Prop("long-running-description");
	}
}
