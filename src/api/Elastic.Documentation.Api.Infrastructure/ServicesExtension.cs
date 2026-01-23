// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Amazon.DynamoDBv2;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Telemetry;
using Elastic.Documentation.Api.Infrastructure.Adapters.AskAi;
using Elastic.Documentation.Api.Infrastructure.Adapters.Telemetry;
using Elastic.Documentation.Api.Infrastructure.Caching;
using Elastic.Documentation.Api.Infrastructure.Gcp;
using Elastic.Documentation.Search;
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
		AddDistributedCache(services, appEnv);
		AddAskAiUsecase(services, appEnv);
		AddSearchUsecase(services, appEnv);
		AddOtlpProxyUsecase(services, appEnv);
	}

	// Note: IParameterProvider is no longer needed - all options now read from IConfiguration (env vars)
	// The LambdaExtensionParameterProvider and LocalParameterProvider can be removed in a future cleanup

	private static void AddDistributedCache(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);

		switch (appEnv)
		{
			case AppEnv.Dev:
				{
					logger?.LogInformation("Configuring InMemoryDistributedCache for environment {AppEnvironment}", appEnv);
					_ = services.AddSingleton<IDistributedCache, InMemoryDistributedCache>();
					logger?.LogInformation("InMemoryDistributedCache registered for local development");
					break;
				}
			case AppEnv.Prod:
			case AppEnv.Staging:
			case AppEnv.Edge:
				{
					logger?.LogInformation("Configuring DynamoDB distributed cache for environment {AppEnvironment}", appEnv);
					try
					{
						// Register AWS DynamoDB client
						_ = services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
						logger?.LogInformation("AmazonDynamoDB client registered");

						// Register multi-layer cache (L1: in-memory + L2: DynamoDB)
						_ = services.AddSingleton<IDistributedCache>(sp =>
						{
							var dynamoDb = sp.GetRequiredService<IAmazonDynamoDB>();
							var tableName = $"docs-api-cache-{appEnv.ToStringFast(true)}";
							var dynamoLogger = sp.GetRequiredService<ILogger<DynamoDbDistributedCache>>();
							var multiLogger = sp.GetRequiredService<ILogger<MultiLayerCache>>();

							var dynamoCache = new DynamoDbDistributedCache(dynamoDb, tableName, dynamoLogger);
							var multiLayerCache = new MultiLayerCache(dynamoCache, multiLogger);
							logger?.LogInformation("Multi-layer cache registered with DynamoDB table: {TableName}", tableName);
							return multiLayerCache;
						});
					}
					catch (Exception ex)
					{
						logger?.LogError(ex, "Failed to configure distributed cache for environment {AppEnvironment}", appEnv);
						throw;
					}
					break;
				}
			default:
				{
					throw new ArgumentOutOfRangeException(nameof(appEnv), appEnv, "Unsupported environment for distributed cache.");
				}
		}
	}

	private static void AddAskAiUsecase(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring AskAi use case for environment {AppEnvironment}", appEnv);

		try
		{
			// Register GcpIdTokenProvider with distributed cache dependency
			// Clean: Let DI handle everything automatically
			_ = services.AddSingleton<IGcpIdTokenProvider, GcpIdTokenProvider>();
			logger?.LogInformation("GcpIdTokenProvider registered with distributed cache support");

			// Register options - DI auto-resolves IConfiguration from primary constructor
			_ = services.AddSingleton<LlmGatewayOptions>();
			logger?.LogInformation("LlmGatewayOptions registered successfully");

			_ = services.AddSingleton<KibanaOptions>();
			logger?.LogInformation("KibanaOptions registered successfully");

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

			// Register message feedback components (gateway is singleton for connection reuse)
			_ = services.AddSingleton<IAskAiMessageFeedbackGateway, ElasticsearchAskAiMessageFeedbackGateway>();
			_ = services.AddScoped<AskAiMessageFeedbackUsecase>();
			logger?.LogInformation("AskAiMessageFeedbackUsecase and Elasticsearch gateway registered successfully");
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

		// Use the shared search service for DI registration
		_ = services.AddSearchServices();
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

		// Register named HttpClient for OTLP proxy
		_ = services.AddHttpClient(AdotOtlpGateway.HttpClientName)
			.ConfigureHttpClient(client =>
			{
				client.Timeout = TimeSpan.FromSeconds(30);
			});

		_ = services.AddScoped<IOtlpGateway, AdotOtlpGateway>();
		_ = services.AddScoped<OtlpProxyUsecase>();
		logger?.LogInformation("OTLP proxy configured to forward to ADOT Lambda Layer collector");
	}
}
