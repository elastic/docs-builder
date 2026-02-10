// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.AskAi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Factory that creates the appropriate IStreamTransformer based on the resolved provider
/// </summary>
public class StreamTransformerFactory(
	IServiceProvider serviceProvider,
	AskAiProviderResolver providerResolver,
	ILogger<StreamTransformerFactory> logger) : IStreamTransformer
{
	private IStreamTransformer? _resolvedTransformer;

	private IStreamTransformer GetTransformer()
	{
		if (_resolvedTransformer != null)
			return _resolvedTransformer;

		var provider = providerResolver.ResolveProvider();

		_resolvedTransformer = provider switch
		{
			"LlmGateway" => serviceProvider.GetRequiredService<LlmGatewayStreamTransformer>(),
			"AgentBuilder" => serviceProvider.GetRequiredService<AgentBuilderStreamTransformer>(),
			_ => throw new InvalidOperationException($"Unknown AI provider: {provider}. Valid values are 'AgentBuilder' or 'LlmGateway'")
		};

		logger.LogDebug("Resolved stream transformer for provider: {Provider}", provider);
		return _resolvedTransformer;
	}

	public string AgentId => GetTransformer().AgentId;
	public string AgentProvider => GetTransformer().AgentProvider;

	public async Task<Stream> TransformAsync(Stream rawStream, Guid? generatedConversationId, System.Diagnostics.Activity? parentActivity, Cancel cancellationToken = default)
	{
		var transformer = GetTransformer();
		return await transformer.TransformAsync(rawStream, generatedConversationId, parentActivity, cancellationToken);
	}
}
