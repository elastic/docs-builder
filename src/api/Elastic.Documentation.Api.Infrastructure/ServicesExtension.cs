// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Core.Telemetry;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.Search;
using Elastic.Documentation.Api.Infrastructure.Adapters.Telemetry;
using Elastic.Documentation.Api.Infrastructure.Aws;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using Microsoft.Extensions.Configuration;
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
			AddElasticDocsApiUsecases(services, parsedEnvironment);
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

		// Configure HttpClient for streaming optimization
		_ = services.AddHttpClient("StreamingHttpClient", client =>
		{
			// Disable response buffering for streaming
			client.DefaultRequestHeaders.Connection.Add("keep-alive");
			client.Timeout = TimeSpan.FromMinutes(10); // Longer timeout for streaming
		});
		// Register AppEnvironment as a singleton for dependency injection
		_ = services.AddSingleton(new AppEnvironment { Current = appEnv });
		AddParameterProvider(services, appEnv);
		AddAskAiUsecase(services, appEnv);
		AddSearchUsecase(services, appEnv);
		AddOtlpProxyUsecase(services, appEnv);
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
					try
					{
						_ = services.AddHttpClient(LambdaExtensionParameterProvider.HttpClientName, client =>
						{
							client.BaseAddress = new Uri("http://localhost:2773");
							client.DefaultRequestHeaders.Add("X-Aws-Parameters-Secrets-Token", Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN"));
						});
						logger?.LogInformation("Lambda extension HTTP client configured");

						_ = services.AddSingleton<IParameterProvider, LambdaExtensionParameterProvider>();
						logger?.LogInformation("LambdaExtensionParameterProvider registered successfully");
					}
					catch (Exception ex)
					{
						logger?.LogError(ex, "Failed to configure LambdaExtensionParameterProvider for environment {AppEnvironment}", appEnv);
						throw;
					}
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

		try
		{
			_ = services.AddSingleton<GcpIdTokenProvider>();
			logger?.LogInformation("GcpIdTokenProvider registered successfully");

			_ = services.AddScoped<LlmGatewayOptions>();
			logger?.LogInformation("LlmGatewayOptions registered successfully");

			_ = services.AddScoped<AskAiUsecase>();
			logger?.LogInformation("AskAiUsecase registered successfully");

			// Register HttpContextAccessor for provider resolution
			_ = services.AddHttpContextAccessor();
			logger?.LogInformation("HttpContextAccessor registered successfully");

			// Register provider resolver
			_ = services.AddScoped<AskAiProviderResolver>();
			logger?.LogInformation("AskAiProviderResolver registered successfully");

			// Register both gateways as concrete types
			_ = services.AddScoped<LlmGatewayAskAiGateway>();
			_ = services.AddScoped<AgentBuilderAskAiGateway>();
			logger?.LogInformation("Both AI gateways registered as concrete types");

			// Register both transformers as concrete types
			_ = services.AddScoped<LlmGatewayStreamTransformer>();
			_ = services.AddScoped<AgentBuilderStreamTransformer>();
			logger?.LogInformation("Both stream transformers registered as concrete types");

			// Register factories as interface implementations
			_ = services.AddScoped<IAskAiGateway<Stream>, AskAiGatewayFactory>();
			_ = services.AddScoped<IStreamTransformer, StreamTransformerFactory>();
			logger?.LogInformation("Gateway and transformer factories registered successfully - provider switchable via X-AI-Provider header");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Failed to configure AskAi use case for environment {AppEnvironment}", appEnv);
			throw;
		}
	}
	private static void AddSearchUsecase(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring Search use case for environment {AppEnvironment}", appEnv);
		_ = services.AddScoped<ElasticsearchOptions>();
		_ = services.AddScoped<ISearchGateway, ElasticsearchGateway>();
		_ = services.AddScoped<SearchUsecase>();
	}

	private static void AddOtlpProxyUsecase(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring OTLP proxy use case for environment {AppEnvironment}", appEnv);

		_ = services.AddSingleton(sp =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			return new OtlpProxyOptions(config);
		});
		_ = services.AddScoped<IOtlpGateway, AdotOtlpGateway>();
		_ = services.AddScoped<OtlpProxyUsecase>();
		logger?.LogInformation("OTLP proxy configured to forward to ADOT Lambda Layer collector");
	}
}
