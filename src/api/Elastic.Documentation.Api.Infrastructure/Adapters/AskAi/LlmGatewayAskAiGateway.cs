// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Gcp;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

public class LlmGatewayAskAiGateway(HttpClient httpClient, IGcpIdTokenProvider tokenProvider, LlmGatewayOptions options) : IAskAiGateway
{
	/// <summary>
	/// Model name used by LLM Gateway (from PlatformContext.UseCase)
	/// </summary>
	public const string ModelName = "docs_assistant";

	/// <summary>
	/// Provider name for tracing
	/// </summary>
	public const string ProviderName = "llm-gateway";
	public async Task<AskAiGatewayResponse> AskAi(AskAiRequest askAiRequest, Cancel ctx = default)
	{
		// LLM Gateway requires a ThreadId - generate one if not provided
		var generatedId = askAiRequest.ConversationId is null ? Guid.NewGuid() : (Guid?)null;
		var threadId = askAiRequest.ConversationId ?? generatedId!.Value;

		var llmGatewayRequest = LlmGatewayRequest.CreateFromRequest(askAiRequest, threadId);
		var requestBody = JsonSerializer.Serialize(llmGatewayRequest, LlmGatewayContext.Default.LlmGatewayRequest);
		using var request = new HttpRequestMessage(HttpMethod.Post, options.FunctionUrl);
		request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
		var authToken = await tokenProvider.GenerateIdTokenAsync(options.ServiceAccount, options.TargetAudience, ctx);
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
		request.Headers.Add("User-Agent", "elastic-docs-proxy/1.0");
		request.Headers.Add("Accept", "text/event-stream");
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

		// Use HttpCompletionOption.ResponseHeadersRead to get headers immediately
		// This allows us to start streaming as soon as headers are received
		var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx);

		// Ensure the response is successful before streaming
		if (!response.IsSuccessStatusCode)
		{
			var errorContent = await response.Content.ReadAsStringAsync(ctx);
			throw new HttpRequestException($"LLM Gateway returned {response.StatusCode}: {errorContent}");
		}

		// Return the response stream directly - this enables true streaming
		// The stream will be consumed as data arrives from the LLM Gateway
		var stream = await response.Content.ReadAsStreamAsync(ctx);
		return new AskAiGatewayResponse(stream, generatedId);
	}
}

public record LlmGatewayRequest(
	AgentOptions AgentOptions,
	UserContext UserContext,
	PlatformContext PlatformContext,
	ChatInput[] Input,
	string ThreadId
)
{
	public static LlmGatewayRequest CreateFromRequest(AskAiRequest request, Guid conversationId) =>
		new(
			AgentOptions: new AgentOptions(FirstGenerationTimeout: 60000),
			UserContext: new UserContext("elastic-docs-v3@invalid"),
			PlatformContext: new PlatformContext("docs_site", "docs_assistant", []),
			Input:
			[
				new ChatInput("user", request.Message)
			],
			ThreadId: conversationId.ToString()
		);
}

public record UserContext(string UserEmail);

public record AgentOptions(int FirstGenerationTimeout);

public record PlatformContext(
	string Origin,
	string UseCase,
	Dictionary<string, object>? Metadata = null
);

public record ChatInput(string Role, string Message);

[JsonSerializable(typeof(LlmGatewayRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class LlmGatewayContext : JsonSerializerContext;
