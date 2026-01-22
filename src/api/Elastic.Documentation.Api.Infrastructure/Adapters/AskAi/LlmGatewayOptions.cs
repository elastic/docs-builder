// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

public class LlmGatewayOptions(IConfiguration configuration)
{
	public string ServiceAccount { get; } = ResolveServiceAccount(configuration);
	public string FunctionUrl { get; } = configuration["LLM_GATEWAY_FUNCTION_URL"]
		?? throw new InvalidOperationException("LLM_GATEWAY_FUNCTION_URL not configured");
	public string TargetAudience { get; } = GetTargetAudience(configuration["LLM_GATEWAY_FUNCTION_URL"]
		?? throw new InvalidOperationException("LLM_GATEWAY_FUNCTION_URL not configured"));

	private static string ResolveServiceAccount(IConfiguration configuration)
	{
		// Auto-detect: if value is a file path that exists, read file content
		// Otherwise use the value directly (for Lambda with env var containing the JSON)
		var serviceAccountValue = configuration["LLM_GATEWAY_SERVICE_ACCOUNT"]
			?? configuration["LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH"];

		if (string.IsNullOrEmpty(serviceAccountValue))
			throw new InvalidOperationException("LLM_GATEWAY_SERVICE_ACCOUNT not configured");

		return File.Exists(serviceAccountValue)
			? File.ReadAllText(serviceAccountValue)
			: serviceAccountValue;
	}

	private static string GetTargetAudience(string functionUrl)
	{
		var uri = new Uri(functionUrl);
		return $"{uri.Scheme}://{uri.Host}";
	}
}
