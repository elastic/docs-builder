// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.AskAi;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Documentation.Api.Tests;

public class AskAiProviderTests
{
	private static AskAiProviderOptions Options(string? value) =>
		new(
			new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
			{
				[AskAiProviderOptions.ConfigurationKey] = value
			}).Build()
		);

	[Test]
	[Arguments(null)]
	[Arguments("")]
	[Arguments("  ")]
	public async Task Provider_Unset_IsLlmGateway(string? value) => Options(value).Provider.Should().Be(AskAiProvider.LlmGateway);

	[Test]
	[Arguments("AgentBuilder")]
	[Arguments("agentbuilder")]
	public async Task Provider_AgentBuilder_IsAgentBuilder(string value) => Options(value).Provider.Should().Be(AskAiProvider.AgentBuilder);

	[Test]
	[Arguments("Kibana")]
	[Arguments("1")]
	public async Task Provider_UnknownValue_Throws(string value)
	{
		var act = () => Options(value);
		_ = act.Should().Throw<InvalidOperationException>().WithMessage($"*{AskAiProviderOptions.ConfigurationKey}*");
	}

	[Test]
	public async Task Services_ProviderUnset_ResolveLlmGateway()
	{
		using var factory = CreateFactory(provider: null);
		using var scope = factory.Services.CreateScope();

		_ = scope.ServiceProvider.GetRequiredService<IAskAiService>().Should().BeOfType<LlmGatewayAskAiGateway>();
		_ = scope.ServiceProvider.GetRequiredService<IStreamTransformer>().Should().BeOfType<LlmGatewayStreamTransformer>();
	}

	[Test]
	public async Task Services_ProviderAgentBuilder_ResolveAgentBuilder()
	{
		using var factory = CreateFactory(provider: "AgentBuilder");
		using var scope = factory.Services.CreateScope();

		_ = scope.ServiceProvider.GetRequiredService<IAskAiService>().Should().BeOfType<AgentBuilderAskAiGateway>();
		_ = scope.ServiceProvider.GetRequiredService<IStreamTransformer>().Should().BeOfType<AgentBuilderStreamTransformer>();
	}

	private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory(string? provider) =>
		new ApiWebApplicationFactory().WithWebHostBuilder(builder =>
		{
			_ = builder.UseSetting("LLM_GATEWAY_FUNCTION_URL", "https://llm-gateway.example.com/");
			_ = builder.UseSetting("LLM_GATEWAY_SERVICE_ACCOUNT", "{}");
			_ = builder.UseSetting("DOCUMENTATION_KIBANA_URL", "https://kibana.example.com");
			_ = builder.UseSetting("DOCUMENTATION_KIBANA_APIKEY", "test-key");
			_ = builder.UseSetting(AskAiProviderOptions.ConfigurationKey, provider ?? string.Empty);
		});
}
