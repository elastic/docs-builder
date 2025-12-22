// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Core.AskAi;

public class AskAiUsecase(
	IAskAiGateway<Stream> askAiGateway,
	IStreamTransformer streamTransformer,
	ILogger<AskAiUsecase> logger)
{
	private static readonly ActivitySource AskAiActivitySource = new(TelemetryConstants.AskAiSourceName);

	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx)
	{
		logger.LogInformation("Starting AskAI chat with {AgentProvider} and {AgentId}", streamTransformer.AgentProvider, streamTransformer.AgentId);
		var activity = AskAiActivitySource.StartActivity($"chat {streamTransformer.AgentProvider}", ActivityKind.Client);
		_ = activity?.SetTag("gen_ai.operation.name", "chat");
		_ = activity?.SetTag("gen_ai.provider.name", streamTransformer.AgentProvider); // agent-builder or llm-gateway
		_ = activity?.SetTag("gen_ai.agent.id", streamTransformer.AgentId); // docs-agent or docs_assistant
		if (askAiRequest.ConversationId is not null)
			_ = activity?.SetTag("gen_ai.conversation.id", askAiRequest.ConversationId.ToString());
		var inputMessages = new[]
		{
			new InputMessage("user", [new MessagePart("text", askAiRequest.Message)])
		};
		var inputMessagesJson = JsonSerializer.Serialize(inputMessages, ApiJsonContext.Default.InputMessageArray);
		_ = activity?.SetTag("gen_ai.input.messages", inputMessagesJson);
		var sanitizedMessage = askAiRequest.Message?.Replace("\r", "").Replace("\n", "");
		logger.LogInformation("AskAI input message: <{ask_ai.input.message}>", sanitizedMessage);
		logger.LogInformation("Streaming AskAI response");
		var rawStream = await askAiGateway.AskAi(askAiRequest, ctx);
		// The stream transformer will handle disposing the activity when streaming completes
		var transformedStream = await streamTransformer.TransformAsync(rawStream, askAiRequest.ConversationId?.ToString(), activity, ctx);
		return transformedStream;
	}
}

public record AskAiRequest(string Message, Guid? ConversationId);
