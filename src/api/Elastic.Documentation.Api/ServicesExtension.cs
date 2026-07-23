// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Amazon.DynamoDBv2;
using Elastic.Documentation.Api;
using Elastic.Documentation.Api.Adapters.AskAi;
using Elastic.Documentation.Api.Adapters.PageFeedback;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.Caching;
using Elastic.Documentation.Api.Gcp;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Api;

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

	public static void AddElasticDocsApiServices(this IServiceCollection services, string? appEnvironment)
	{
		if (AppEnvExtensions.TryParse(appEnvironment, out var parsedEnvironment, true))
			AddElasticDocsApiServices(services, parsedEnvironment);
		else
		{
			var logger = GetLogger(services);
			logger?.LogWarning("Unable to parse environment {AppEnvironment} into AppEnvironment. Using default AppEnvironment.Dev", appEnvironment);
			AddElasticDocsApiServices(services, AppEnv.Dev);
		}
	}


	private static void AddElasticDocsApiServices(this IServiceCollection services, AppEnv appEnv)
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
		AddAskAiServices(services, appEnv);
		AddPageFeedbackServices(services);
		AddSearchServices(services, appEnv);
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

	private static void AddAskAiServices(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring AskAi services for environment {AppEnvironment}", appEnv);

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

			// Register HttpContextAccessor for provider resolution
			_ = services.AddHttpContextAccessor();
			logger?.LogInformation("HttpContextAccessor registered successfully");

			// Register provider resolver
			_ = services.AddScoped<AskAiProviderResolver>();
			logger?.LogInformation("AskAiProviderResolver registered successfully");

			// Register both service implementations as concrete types
			_ = services.AddScoped<LlmGatewayAskAiGateway>();
			_ = services.AddScoped<AgentBuilderAskAiGateway>();
			logger?.LogInformation("Both AI service implementations registered as concrete types");

			// Register both transformers as concrete types
			_ = services.AddScoped<LlmGatewayStreamTransformer>();
			_ = services.AddScoped<AgentBuilderStreamTransformer>();
			logger?.LogInformation("Both stream transformers registered as concrete types");

			// Register factory as interface implementation
			_ = services.AddScoped<IAskAiService, AskAiGatewayFactory>();
			_ = services.AddScoped<IStreamTransformer, StreamTransformerFactory>();
			logger?.LogInformation("Service and transformer factories registered successfully - provider switchable via X-AI-Provider header");

			// Register message feedback service (singleton for connection reuse)
			_ = services.AddSingleton<IAskAiMessageFeedbackService, ElasticsearchAskAiMessageFeedbackGateway>();
			logger?.LogInformation("AskAiMessageFeedbackService (Elasticsearch) registered successfully");
		}
		catch (Exception ex)
		{
			logger?.LogError(ex, "Failed to configure AskAi services for environment {AppEnvironment}", appEnv);
			throw;
		}
	}

	private static void AddPageFeedbackServices(IServiceCollection services)
	{
		_ = services.AddSingleton<PageFeedbackTransport>();
		_ = services.AddSingleton<PageFeedbackIndex>();
		_ = services.AddSingleton<IPageFeedbackService, ElasticsearchPageFeedbackGateway>();
		_ = services.AddHostedService<PageFeedbackBootstrapService>();
	}

	private static void AddSearchServices(IServiceCollection services, AppEnv appEnv)
	{
		var logger = GetLogger(services);
		logger?.LogInformation("Configuring Search services for environment {AppEnvironment}", appEnv);

		// Use the shared search service for DI registration
		_ = services.AddSearchServices();
		logger?.LogInformation("Full search service registered with hybrid RRF support");
	}

}
