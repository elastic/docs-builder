// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Factory that creates the appropriate IAskAiGateway based on the resolved provider
/// </summary>
public class AskAiGatewayFactory(
	IServiceProvider serviceProvider,
	AskAiProviderResolver providerResolver,
	ILogger<AskAiGatewayFactory> logger) : IAskAiGateway<Stream>
{
	public async Task<Stream> AskAi(AskAiRequest askAiRequest, Cancel ctx = default)
	{
		var provider = providerResolver.ResolveProvider();

		IAskAiGateway<Stream> gateway = provider switch
		{
			"LlmGateway" => serviceProvider.GetRequiredService<LlmGatewayAskAiGateway>(),
			"AgentBuilder" => serviceProvider.GetRequiredService<AgentBuilderAskAiGateway>(),
			_ => throw new InvalidOperationException($"Unknown AI provider: {provider}. Valid values are 'AgentBuilder' or 'LlmGateway'")
		};

		logger.LogInformation("Using AI provider: {Provider}", provider);
		return await gateway.AskAi(askAiRequest, ctx);
	}
}
