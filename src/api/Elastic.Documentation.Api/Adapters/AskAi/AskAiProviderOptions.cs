// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Adapters.AskAi;

public enum AskAiProvider
{
	LlmGateway,
	AgentBuilder
}

/// <summary>The Ask AI provider is a deployment setting, never a per-request choice. Unset means LlmGateway.</summary>
public class AskAiProviderOptions(IConfiguration configuration)
{
	public const string ConfigurationKey = "DOCUMENTATION_ASK_AI_PROVIDER";

	public AskAiProvider Provider { get; } = Parse(configuration[ConfigurationKey]);

	private static AskAiProvider Parse(string? value) => value?.Trim().ToLowerInvariant() switch
	{
		null or "" or "llmgateway" => AskAiProvider.LlmGateway,
		"agentbuilder" => AskAiProvider.AgentBuilder,
		_ =>
			throw new InvalidOperationException(
				$"{ConfigurationKey} has unknown value '{value}'. Valid values are 'LlmGateway' or 'AgentBuilder'"
			)
	};
}
