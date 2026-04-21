// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Markdown.Diagnostics;

namespace Elastic.Markdown.Myst.Directives.AgentSkill;

public class AgentSkillBlock(DirectiveBlockParser parser, ParserContext context)
	: DirectiveBlock(parser, context)
{
	public override string Directive => "agent-skill";

	public string? Url { get; private set; }

	public override void FinalizeAndValidate(ParserContext context)
	{
		Url = Prop("url");
		if (string.IsNullOrEmpty(Url))
			this.EmitError("agent-skill directive requires a :url: property");
		else if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
			this.EmitError($"agent-skill :url: must be an absolute URL, got '{Url}'");
	}
}
