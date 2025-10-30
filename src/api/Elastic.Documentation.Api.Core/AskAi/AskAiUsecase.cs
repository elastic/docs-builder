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

	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx)
	{
		// Start activity for the chat request - DO NOT use 'using' because the stream is consumed later
		// The activity will be passed to the transformer which will dispose it when the stream completes
		var activity = AskAiActivitySource.StartActivity("chat", ActivityKind.Client);

		// Generate a correlation ID for tracking if this is a new conversation
		// For first messages (no ThreadId), generate a temporary ID that will be updated when the provider responds
		var correlationId = askAiRequest.ThreadId ?? $"temp-{Guid.NewGuid()}";

		// Set GenAI semantic convention attributes
		_ = (activity?.SetTag("gen_ai.operation.name", "chat"));
		_ = (activity?.SetTag("gen_ai.conversation.id", correlationId)); // Will be updated when we receive ConversationStart with actual ID
		_ = (activity?.SetTag("gen_ai.usage.input_tokens", askAiRequest.Message.Length)); // Approximate token count

		// Custom attributes for tracking our abstraction layer
		// We use custom attributes because we don't know the actual GenAI provider (OpenAI, Anthropic, etc.)
		// or model (gpt-4, claude, etc.) - those are abstracted by AgentBuilder/LlmGateway
		_ = (activity?.SetTag("docs.ai.gateway", streamTransformer.AgentProvider)); // agent-builder or llm-gateway
		_ = (activity?.SetTag("docs.ai.agent_name", streamTransformer.AgentId)); // docs-agent or docs_assistant

		// Add GenAI prompt event
		_ = (activity?.AddEvent(new ActivityEvent("gen_ai.content.prompt",
			timestamp: DateTimeOffset.UtcNow,
			tags:
			[
				new KeyValuePair<string, object?>("gen_ai.prompt", askAiRequest.Message),
				new KeyValuePair<string, object?>("gen_ai.system_instructions", AskAiRequest.SystemPrompt)
			])));

		logger.LogDebug("Processing AskAiRequest: {Request}", askAiRequest);

		var rawStream = await askAiGateway.AskAi(askAiRequest, ctx);

		// The stream transformer will handle disposing the activity when streaming completes
		var transformedStream = await streamTransformer.TransformAsync(rawStream, activity, ctx);

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
