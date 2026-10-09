// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.AskAi;

/// <summary>
/// The AI assistant: a button and a side panel with a conversation. The conversation is streamed from the AI endpoint, so the whole
/// component stays hidden until script can talk to it.
/// </summary>
public sealed record AskAiModel
{
	/// <summary>Prefix for element ids.</summary>
	public string Id { get; init; } = "ask-ai";

	/// <summary>A button fixed to the corner of the page. Turn it off to place the button inside the header.</summary>
	public bool Floating { get; init; } = true;

	public string Title { get; init; } = "Ask AI";
	public string Placeholder { get; init; } = "Ask the Elastic Docs AI Assistant";
}
