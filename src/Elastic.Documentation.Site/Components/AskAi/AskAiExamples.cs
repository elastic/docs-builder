// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Site.Components.AskAi;

public static class AskAiExamples
{
	public static ComponentSection Assistants { get; } = new(
		"Ask AI",
		"The assistant: a button and a side panel with a streamed conversation. Without script, or without the AI endpoint, the whole thing stays hidden. The specimen is the panel drawn in place; the live one slides in from the right edge and is revealed by script. A rudimentary design, waiting for a board.",
		"ask-ai",
		[ComponentExample.Of("Panel, mid-conversation", _AskAiPreview.Create, new AskAiModel { Id = "example-ask-ai" })]
	);
}
