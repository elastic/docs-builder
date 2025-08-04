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
public enum AppEnv
{
	[Display(Name = "dev")] Dev,
	[Display(Name = "staging")] Staging,
	[Display(Name = "edge")] Edge,
	[Display(Name = "prod")] Prod
}

public class AppEnvironment
{
	public AppEnv Current { get; init; }
}

public static class ServicesExtension
{
	private static ILogger? GetLogger(IServiceCollection services)
	{
		using var serviceProvider = services.BuildServiceProvider();
		var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
		return loggerFactory?.CreateLogger(typeof(ServicesExtension));
	}

	public static void AddElasticDocsApiUsecases(this IServiceCollection services, string? appEnvironment)
	{
		if (AppEnvExtensions.TryParse(appEnvironment, out var parsedEnvironment, true))
		{
			AddElasticDocsApiUsecases(services, parsedEnvironment);
		}
		else
		{
			var logger = GetLogger(services);
			logger?.LogWarning("Unable to parse environment {AppEnvironment} into AppEnvironment. Using default AppEnvironment.Dev", appEnvironment);
			AddElasticDocsApiUsecases(services, AppEnv.Dev);
		}
	}


	private static void AddElasticDocsApiUsecases(this IServiceCollection services, AppEnv appEnv)
	{
		_ = services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
		});
		_ = services.AddHttpClient();
		// Register AppEnvironment as a singleton for dependency injection
		_ = services.AddSingleton(new AppEnvironment { Current = appEnv });
		AddParameterProvider(services, appEnv);
		AddAskAiUsecase(services, appEnv);
	}

	// https://docs.aws.amazon.com/systems	-manager/latest/userguide/ps-integration-lambda-extensions.html
	private static void AddParameterProvider(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);

		switch (appEnv)
		{
			case AppEnv.Prod:
			case AppEnv.Staging:
			case AppEnv.Edge:
				{
					logger?.LogInformation("Configuring LambdaExtensionParameterProvider for environment {AppEnvironment}", appEnv);
					_ = services.AddHttpClient(LambdaExtensionParameterProvider.HttpClientName, client =>
					{
						client.BaseAddress = new Uri("http://localhost:2773");
						client.DefaultRequestHeaders.Add("X-Aws-Parameters-Secrets-Token", Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN"));
					});
					_ = services.AddSingleton<IParameterProvider, LambdaExtensionParameterProvider>();
					break;
				}
			case AppEnv.Dev:
				{
					logger?.LogInformation("Configuring LocalParameterProvider for environment {AppEnvironment}", appEnv);
					_ = services.AddSingleton<IParameterProvider, LocalParameterProvider>();
					break;
				}
			default:
				{
					throw new ArgumentOutOfRangeException(nameof(appEnv), appEnv,
						"Unsupported environment for parameter provider.");
				}
		}
	}

	private static void AddAskAiUsecase(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring AskAi use case for environment {AppEnvironment}", appEnv);
		_ = services.AddSingleton<GcpIdTokenProvider>();
		_ = services.AddScoped<LlmGatewayOptions>();
		_ = services.AddScoped<AskAiUsecase>();
		_ = services.AddScoped<IAskAiGateway<Stream>, LlmGatewayAskAiGateway>();
	}
}
