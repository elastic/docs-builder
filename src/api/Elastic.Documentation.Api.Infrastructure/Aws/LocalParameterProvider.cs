// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Infrastructure.Aws;

public class LocalParameterProvider : IParameterProvider
{
	public async Task<string> GetParam(string name, bool withDecryption = true, Cancel ctx = default)
	{
		switch (name)
		{
			case "llm-gateway-service-account":
				{
					var serviceAccountKeyPath = GetEnv("LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH");
					if (!File.Exists(serviceAccountKeyPath))
						throw new ArgumentException($"Service account key file not found at '{serviceAccountKeyPath}'.");
					var serviceAccountKey = await File.ReadAllTextAsync(serviceAccountKeyPath, ctx);
					return serviceAccountKey;
				}
			case "llm-gateway-function-url":
				{
					return GetEnv("LLM_GATEWAY_FUNCTION_URL");
				}
			case "docs-elasticsearch-url":
				{
					return GetEnv("DOCUMENTATION_ELASTIC_URL");
				}
			case "docs-elasticsearch-apikey":
				{
					return GetEnv("DOCUMENTATION_ELASTIC_APIKEY");
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
