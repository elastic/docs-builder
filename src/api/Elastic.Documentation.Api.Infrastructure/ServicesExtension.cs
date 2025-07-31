// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using Microsoft.Extensions.DependencyInjection;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Api.Infrastructure;

[EnumExtensions]
public enum AppEnvironment
{
	[Display(Name = "dev")] Dev,
	[Display(Name = "staging")] Staging,
	[Display(Name = "edge")] Edge,
	[Display(Name = "prod")] Prod
}

public static class ServicesExtension
{
	public static void AddUsecases(this IServiceCollection services, string? appEnvironment) =>
		AddUsecases(
			services,
			AppEnvironmentExtensions.TryParse(appEnvironment, out var parsedEnvironment, true)
				? parsedEnvironment
				: AppEnvironment.Dev
		);

	private static void AddUsecases(this IServiceCollection services, AppEnvironment appEnvironment)
	{
		AddParameterProvider(services, appEnvironment);
		AddAskAiUsecases(services, appEnvironment);
	}

	// https://docs.aws.amazon.com/systems-manager/latest/userguide/ps-integration-lambda-extensions.html
	private static void AddParameterProvider(IServiceCollection services, AppEnvironment appEnvironment)
	{
		switch (appEnvironment)
		{
			case AppEnvironment.Prod:
			case AppEnvironment.Staging:
			case AppEnvironment.Edge:
				{
					_ = services.AddHttpClient(LambdaExtensionParameterProvider.HttpClientName, client =>
					{
						client.BaseAddress = new Uri("http://localhost:2773");
						client.DefaultRequestHeaders.Add("X-Aws-Parameters-Secrets-Token", Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN"));
					});
					_ = services.AddSingleton<IParameterProvider, LambdaExtensionParameterProvider>();
					break;
				}
			case AppEnvironment.Dev:
				{
					_ = services.AddSingleton<IParameterProvider, LocalParameterProvider>();
					break;
				}
			default:
				{
					throw new ArgumentOutOfRangeException(nameof(appEnvironment), appEnvironment,
						"Unsupported environment for parameter provider.");
				}
		}
	}

	private static void AddAskAiUsecases(IServiceCollection services, AppEnvironment appEnvironment)
	{
		_ = services.AddScoped<GcpIdTokenProvider>(serviceProvider =>
		{
			var httpClient = serviceProvider.GetRequiredService<HttpClient>();
			var parameterProvider = serviceProvider.GetRequiredService<IParameterProvider>();
			var appEnvString = appEnvironment.ToStringFast(true);

			var serviceAccount = parameterProvider
				.GetParam($"/elastic-docs-v3/{appEnvString}/llm-gateway-service-account")
				.GetAwaiter()
				.GetResult();
			var functionUrl = parameterProvider
				.GetParam($"/elastic-docs-v3/{appEnvString}/llm-gateway-function-url")
				.GetAwaiter()
				.GetResult();

			var functionUri = new Uri(functionUrl);
			var targetAudience = $"{functionUri.Scheme}://{functionUri.Host}";

			return new GcpIdTokenProvider(httpClient, serviceAccount, targetAudience);
		});

		_ = services.AddScoped<IAskAiGateway<Stream>>(serviceProvider =>
		{
			var parameterProvider = serviceProvider.GetRequiredService<IParameterProvider>();
			var tokenProvider = serviceProvider.GetRequiredService<GcpIdTokenProvider>();
			var httpClient = serviceProvider.GetRequiredService<HttpClient>();
			var functionUrl = parameterProvider.GetParam("/elastic-docs-v3/dev/llm-gateway-function-url").GetAwaiter().GetResult();
			return new LlmGatewayAskAiGateway(httpClient, tokenProvider, functionUrl);
		});

		_ = services.AddScoped<AskAiUsecase>();
	}
}
