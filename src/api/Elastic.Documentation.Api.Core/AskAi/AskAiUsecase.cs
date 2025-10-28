// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.AskAi;

public class AskAiUsecase(
	IAskAiGateway<Stream> askAiGateway,
	IStreamTransformer streamTransformer,
	ILogger<AskAiUsecase> logger)
{
	private static readonly ActivitySource AskAiActivitySource = new("Elastic.Documentation.Api.AskAi");

	private static string GetModelNameFromProvider(string provider) => provider switch
	{
		"LlmGateway" => "docs_assistant",
		"AgentBuilder" => "docs-agent",
		_ => "elastic-docs-rag"
	};

	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx)
	{
		using var activity = AskAiActivitySource.StartActivity("gen_ai.agent");

		// We'll determine the actual agent name after we know which provider is being used
		_ = (activity?.SetTag("gen_ai.request.input", askAiRequest.Message));
		_ = (activity?.SetTag("gen_ai.request.conversation_id", askAiRequest.ThreadId ?? "new-conversation"));

		// Add GenAI inference operation details event
		_ = (activity?.AddEvent(new ActivityEvent("gen_ai.client.inference.operation.details",
			timestamp: DateTimeOffset.UtcNow,
			tags:
			[
				new KeyValuePair<string, object?>("gen_ai.operation.name", "chat"),
				new KeyValuePair<string, object?>("gen_ai.request.model", GetModelNameFromProvider("Unknown")), // Will be updated by transformer
				new KeyValuePair<string, object?>("gen_ai.conversation.id", askAiRequest.ThreadId ?? "pending"), // Will be updated when we receive ConversationStart
				new KeyValuePair<string, object?>("gen_ai.input.messages", $"[{{\"role\":\"user\",\"content\":\"{askAiRequest.Message}\"}}]"),
				new KeyValuePair<string, object?>("gen_ai.system_instructions", $"[{{\"type\":\"text\",\"content\":\"{AskAiRequest.SystemPrompt}\"}}]")
			])));

		logger.LogDebug("Processing AskAiRequest: {Request}", askAiRequest);

		var rawStream = await askAiGateway.AskAi(askAiRequest, ctx);

		// The stream transformer will set the correct agent name, model name and provider
		var transformedStream = await streamTransformer.TransformAsync(rawStream, ctx);

		return transformedStream;
	}
}

public record AskAiRequest(string Message, string? ThreadId)
{
	public static string SystemPrompt =>
"""
You are an expert documentation assistant. Your primary task is to answer user questions using **only** the provided documentation.

## Task Overview
Synthesize information from the provided text to give a direct, comprehensive, and self-contained answer to the user's query.

---

## Critical Rules
1.  **Strictly Adhere to Provided Sources:** Your ONLY source of information is the document content provided with by your RAG search. **DO NOT** use any of your pre-trained knowledge or external information.
2.  **Handle Unanswerable Questions:** If the answer is not in the documents, you **MUST** state this explicitly (e.g., "The answer to your question could not be found in the provided documentation."). Do not infer, guess, or provide a general knowledge answer. As a helpful fallback, you may suggest a few related topics that *are* present in the documentation.
3.  **Be Direct and Anonymous:** Answer the question directly without any preamble like "Based on the documents..." or "In the provided text...". **DO NOT** mention that you are an AI or language model.

---

## Response Formatting

### 1. User-Visible Answer
* The final response must be a single, coherent block of text.
* Format your answer using Markdown (headings, bullet points, etc.) for clarity.
* Use sentence case for all headings.
* Do not use `---` or any other section dividers in your answer.
* Keep your answers concise yet complete. Answer the user's question fully, but link to the source documents for more extensive details.

### 2. Hidden Source References (*Crucial*)
* At the end of your response, you **MUST** **ALWAYS** provide a list of all documents you used to formulate the answer.
* Also include links that you used in your answer.
* This list must be a JSON array wrapped inside a specific multi-line comment delimiter.
* DO NOT add any headings, preamble, or explanations around the reference block. The JSON must be invisible to the end-user.

**Delimiter and JSON Schema:**

Use this exact format. The JSON array goes inside the comment block like the example below:

```markdown
<!--REFERENCES

[]

-->
```

**JSON Schema Definition:**
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "List of Documentation Resources",
  "description": "A list of objects, each representing a documentation resource with a URL, title, and description.",
  "type": "array",
  "items": {
    "type": "object",
    "properties": {
      "url": {
        "description": "The URL of the resource.",
        "type": "string",
        "format": "uri"
      },
      "title": {
        "description": "The title of the resource.",
        "type": "string"
      },
      "description": {
        "description": "A brief description of the resource.",
        "type": "string"
      }
    },
    "required": [
      "url",
      "title",
      "description"
    ]
  }
}
""";
}
