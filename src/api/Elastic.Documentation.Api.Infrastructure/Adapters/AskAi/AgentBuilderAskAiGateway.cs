// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

public class AgentBuilderAskAiGateway(HttpClient httpClient, IParameterProvider parameterProvider, ILogger<AgentBuilderAskAiGateway> logger) : IAskAiGateway<Stream>
{
	/// <summary>
	/// Model name used by Agent Builder (from AgentId)
	/// </summary>
	public const string ModelName = "docs-agent";

	/// <summary>
	/// Provider name for tracing
	/// </summary>
	public const string ProviderName = "agent-builder";
	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx = default)
	{
		var agentBuilderPayload = new AgentBuilderPayload(
			askAiRequest.Message,
			"docs-agent",
			askAiRequest.ConversationId?.ToString());
		var requestBody = JsonSerializer.Serialize(agentBuilderPayload, AgentBuilderContext.Default.AgentBuilderPayload);

		logger.LogInformation("Sending to Agent Builder with conversation_id: \"{ConversationId}\"", askAiRequest.ConversationId?.ToString() ?? "(null - first request)");

		var kibanaUrl = await parameterProvider.GetParam("docs-kibana-url", false, ctx);
		var kibanaApiKey = await parameterProvider.GetParam("docs-kibana-apikey", true, ctx);

		using var request = new HttpRequestMessage(HttpMethod.Post,
			$"{kibanaUrl}/api/agent_builder/converse/async");
		request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
		request.Headers.Add("kbn-xsrf", "true");
		request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", kibanaApiKey);

		var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx);

		// Ensure the response is successful before streaming
		if (!response.IsSuccessStatusCode)
		{
			logger.LogInformation("Body: {Body}", requestBody);
			var errorContent = await response.Content.ReadAsStringAsync(ctx);
			logger.LogInformation("Reason: {Reason}", response.ReasonPhrase);
			throw new HttpRequestException($"Agent Builder returned {response.StatusCode}: {errorContent}");
		}

		// Log response details for debugging
		logger.LogInformation("Response Content-Type: {ContentType}", response.Content.Headers.ContentType?.ToString());
		logger.LogInformation("Response Content-Length: {ContentLength}", response.Content.Headers.ContentLength?.ToString(CultureInfo.InvariantCulture));

		// Agent Builder already returns SSE format, just return the stream directly
		return await response.Content.ReadAsStreamAsync(ctx);
	}
}

internal sealed record AgentBuilderPayload(string Input, string AgentId, string? ConversationId);

[JsonSerializable(typeof(AgentBuilderPayload))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class AgentBuilderContext : JsonSerializerContext;
