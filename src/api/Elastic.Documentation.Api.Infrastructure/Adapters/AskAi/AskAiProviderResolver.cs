// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;

/// <summary>
/// Resolves which AI provider to use based on HTTP headers
/// </summary>
public class AskAiProviderResolver(IHttpContextAccessor httpContextAccessor, ILogger<AskAiProviderResolver> logger)
{
	private const string ProviderHeader = "X-AI-Provider";
	private const string DefaultProvider = "LlmGateway";

	/// <summary>
	/// Resolves the AI provider to use.
	/// If X-AI-Provider header is present, uses that value.
	/// Otherwise, defaults to LlmGateway.
	/// Valid values: "AgentBuilder", "LlmGateway"
	/// </summary>
	public string ResolveProvider()
	{
		var httpContext = httpContextAccessor.HttpContext;

		// Check for X-AI-Provider header (set by frontend)
		if (httpContext?.Request.Headers.TryGetValue(ProviderHeader, out var headerValue) == true)
		{
			var provider = headerValue.FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(provider))
			{
				logger.LogInformation("AI Provider from header: {Provider}", provider);
				return provider;
			}
		}

		// Default to LLM Gateway
		logger.LogDebug("Using default AI Provider: {Provider}", DefaultProvider);
		return DefaultProvider;
	}
}
