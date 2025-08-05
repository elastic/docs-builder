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
					const string envName = "LLM_GATEWAY_SERVICE_ACCOUNT_KEY_PATH";
					var serviceAccountKeyPath = Environment.GetEnvironmentVariable(envName);
					if (string.IsNullOrEmpty(serviceAccountKeyPath))
						throw new ArgumentException($"Environment variable '{envName}' not found.");
					if (!File.Exists(serviceAccountKeyPath))
						throw new ArgumentException($"Service account key file not found at '{serviceAccountKeyPath}'.");
					var serviceAccountKey = await File.ReadAllTextAsync(serviceAccountKeyPath, ctx);
					return serviceAccountKey;
				}
			case "llm-gateway-function-url":
				{
					const string envName = "LLM_GATEWAY_FUNCTION_URL";
					var value = Environment.GetEnvironmentVariable(envName);
					if (string.IsNullOrEmpty(value))
						throw new ArgumentException($"Environment variable '{envName}' not found.");
					return value;
				}
			default:
				{
					throw new ArgumentException($"Parameter '{name}' not found in {nameof(LocalParameterProvider)}");
				}
		}
	}
}
