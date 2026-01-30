// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Infrastructure.Aws;

public class LocalParameterProvider : IParameterProvider
{
	private readonly string? _elasticUrlFromSecret;
	private readonly string? _elasticApiKeyFromSecret;
	private readonly string? _llmGatewayUrlFromSecret;
	private readonly string? _llmGatewayServiceAccountPath;

	public LocalParameterProvider()
	{
		// Build a new ConfigurationBuilder to read user secrets
		var configBuilder = new ConfigurationBuilder();
		_ = configBuilder.AddUserSecrets("72f50f33-6fb9-4d08-bff3-39568fe370b3");
		var userSecretsConfig = configBuilder.Build();

		_elasticUrlFromSecret = userSecretsConfig["Parameters:DocumentationElasticUrl"];
		_elasticApiKeyFromSecret = userSecretsConfig["Parameters:DocumentationElasticApiKey"];
		_llmGatewayUrlFromSecret = userSecretsConfig["Parameters:LlmGatewayUrl"];
		_llmGatewayServiceAccountPath = userSecretsConfig["Parameters:LlmGatewayServiceAccountPath"];
	}

	public async Task<string> GetParam(string name, bool withDecryption = true, Cancel ctx = default)
	{
		switch (name)
		{
			case "llm-gateway-service-account":
				{
					var serviceAccountKeyPath = GetEnv("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH", _llmGatewayServiceAccountPath);
					if (!File.Exists(serviceAccountKeyPath))
						throw new ArgumentException($"Service account key file not found at '{serviceAccountKeyPath}'.");
					var serviceAccountKey = await File.ReadAllTextAsync(serviceAccountKeyPath, ctx);
					return serviceAccountKey;
				}
			case "llm-gateway-function-url":
				{
					return GetEnv("LLM_GATEWAY_FUNCTION_URL", _llmGatewayUrlFromSecret);
				}
			case "docs-elasticsearch-url":
				{
					return GetEnv("DOCUMENTATION_ELASTIC_URL", _elasticUrlFromSecret);
				}
			case "docs-elasticsearch-apikey":
				{
					return GetEnv("DOCUMENTATION_ELASTIC_APIKEY", _elasticApiKeyFromSecret);
				}
			case "docs-kibana-url":
				{
					return GetEnv("DOCUMENTATION_KIBANA_URL");
				}
			case "docs-kibana-apikey":
				{
					return GetEnv("DOCUMENTATION_KIBANA_APIKEY");
				}
			case "docs-elasticsearch-index":
				{
					return GetEnv("DOCUMENTATION_ELASTIC_INDEX", "semantic-docs-dev-latest");
				}
			default:
				{
					throw new ArgumentException($"Parameter '{name}' not found in {nameof(LocalParameterProvider)}");
				}
		}
	}

	private static string GetEnv(string name, string? defaultValue = null)
	{
		var value = Environment.GetEnvironmentVariable(name);
		if (!string.IsNullOrEmpty(value))
			return value;
		if (defaultValue != null)
			return defaultValue;
		throw new ArgumentException($"Environment variable '{name}' not found.");
	}
}
