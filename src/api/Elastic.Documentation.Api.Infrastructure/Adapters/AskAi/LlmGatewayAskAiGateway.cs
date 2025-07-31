// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Gcp;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

public class LlmGatewayAskAiGateway(HttpClient httpClient, GcpIdTokenProvider tokenProvider, string gcpFunctionUrl) : IAskAiGateway<Stream>
{
	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx = default)
	{
		var llmGatewayRequest = LlmGatewayRequest.CreateFromQuestion(askAiRequest.Message, askAiRequest.ThreadId);
		var requestBody = JsonSerializer.Serialize(llmGatewayRequest, LlmGatewayContext.Default.LlmGatewayRequest);
		var request = new HttpRequestMessage(HttpMethod.Post, gcpFunctionUrl)
		{
			Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
		};
		var authToken = await tokenProvider.GenerateIdTokenAsync(ctx);
		request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
		request.Headers.Add("User-Agent", "elastic-docs-proxy/1.0");
		request.Headers.Add("Accept", "text/event-stream");
		request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
		var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx);
		return await response.Content.ReadAsStreamAsync(ctx);
	}
}

public record LlmGatewayRequest(
	UserContext UserContext,
	PlatformContext PlatformContext,
	ChatInput[] Input,
	string ThreadId
)
{
	public static LlmGatewayRequest CreateFromQuestion(string question, string? threadId = null) =>
		new(
			UserContext: new UserContext("elastic-docs-v3@invalid"),
			PlatformContext: new PlatformContext("support_portal", "support_assistant", []),
			Input:
			[
				new ChatInput("user", question)
			],
			ThreadId: threadId ?? "elastic-docs-" + Guid.NewGuid()
		);
}

public record UserContext(string UserEmail);

public record PlatformContext(
	string Origin,
	string UseCase,
	Dictionary<string, object>? Metadata = null
);

public record ChatInput(string Role, string Message);

[JsonSerializable(typeof(LlmGatewayRequest))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class LlmGatewayContext : JsonSerializerContext;
