// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

public class LlmGatewayOptions
{
	public string ServiceAccount { get; set; } = string.Empty;
	public string FunctionUrl { get; set; } = string.Empty;
	public string TargetAudience { get; set; } = string.Empty;
}

public static class ServicesExtension
{
	public static void AddApiUsecases(this IServiceCollection services, string? appEnvironment)
	{
		if (AppEnvironmentExtensions.TryParse(appEnvironment, out var parsedEnvironment, true))
		{
			AddApiUsecases(
				services,
				parsedEnvironment
			);
		}
		else
		{
			var logger = services.BuildServiceProvider().GetRequiredService<ILogger>();
			logger.LogWarning("Unable to parse environment {Environment} into AppEnvironment. Using default AppEnvironment.Dev", appEnvironment);
			AddApiUsecases(
				services,
				AppEnvironment.Dev
			);
		}
	}


	private static void AddApiUsecases(this IServiceCollection services, AppEnvironment appEnvironment)
	{
		_ = services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
		});
		_ = services.AddHttpClient();
		AddParameterProvider(services, appEnvironment);
		AddAskAiUsecase(services, appEnvironment);
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

	private static void AddAskAiUsecase(IServiceCollection services, AppEnvironment appEnvironment)
	{
		_ = services.Configure<LlmGatewayOptions>(options =>
		{
			var serviceProvider = services.BuildServiceProvider();
			var parameterProvider = serviceProvider.GetRequiredService<IParameterProvider>();
			var appEnvString = appEnvironment.ToStringFast(true);

			options.ServiceAccount = parameterProvider.GetParam($"/elastic-docs-v3/{appEnvString}/llm-gateway-service-account").GetAwaiter().GetResult();
			options.FunctionUrl = parameterProvider.GetParam($"/elastic-docs-v3/{appEnvString}/llm-gateway-function-url").GetAwaiter().GetResult();

			var functionUri = new Uri(options.FunctionUrl);
			options.TargetAudience = $"{functionUri.Scheme}://{functionUri.Host}";
		});
		_ = services.AddScoped<GcpIdTokenProvider>();
		_ = services.AddScoped<IAskAiGateway<Stream>, LlmGatewayAskAiGateway>();
		_ = services.AddScoped<AskAiUsecase>();
	}
}
