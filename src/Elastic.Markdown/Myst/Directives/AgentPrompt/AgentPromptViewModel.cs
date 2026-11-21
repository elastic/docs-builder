// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Markdown.Myst.Directives.AgentPrompt;

public class AgentPromptViewModel : DirectiveViewModel
{
	public required string Title { get; init; }
	public required string Description { get; init; }
	public required string Prompt { get; init; }
}
