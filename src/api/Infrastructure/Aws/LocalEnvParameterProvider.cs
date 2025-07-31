// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Api.Infrastructure.Aws;

public class LocalEnvParameterProvider : IParameterProvider
{
	public Task<string> GetParam(string name, bool withDecryption = true, Cancel ctx = default)
	{
		var env = name switch
		{
			"/elastic-docs-v3/dev/llm-gateway-service-account" => "LLM_GATEWAY_SERVICE_ACCOUNT",
			"/elastic-docs-v3/dev/llm-gateway-function-url" => "LLM_GATEWAY_FUNCTION_URL",
			_ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
		};
		var value = Environment.GetEnvironmentVariable(env);

		if (string.IsNullOrEmpty(value))
			throw new ArgumentException($"Environment variable '{env}' not found.");

		return Task.FromResult(value);
	}
}
