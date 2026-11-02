// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;
using Markdig.Syntax;

namespace Elastic.Markdown.Myst.Directives.AgentPrompt;

/// <summary>
/// A copyable prompt for AI agents. The prompt is kept in the markdown source as a fenced code
/// block inside the directive but is never rendered inline: readers see a call-to-action that
/// opens a modal with a copy button.
/// </summary>
public class AgentPromptBlock(DirectiveBlockParser parser, ParserContext context) : DirectiveBlock(parser, context)
{
	public const string DefaultTitle = "Get started with your AI agent";
	public const string DefaultDescription =
		"Copy this prompt into an AI coding agent that can run commands. It does the steps on this page for you and reports what it did.";

	public override string Directive => "agent-prompt";

	public string Title { get; private set; } = DefaultTitle;

	public string Description { get; private set; } = DefaultDescription;

	public string? Prompt { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Title = Prop("title") ?? DefaultTitle;
		Description = Prop("description") ?? DefaultDescription;

		var code = this.OfType<FencedCodeBlock>().FirstOrDefault();
		Prompt = code?.Lines.ToString().Trim();

		if (string.IsNullOrWhiteSpace(Prompt))
			this.EmitError("agent-prompt directive requires the prompt in a fenced code block in its body");
	}
}
