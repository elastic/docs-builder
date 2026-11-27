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
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Search;
using Elastic.Transport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetEscapades.EnumGenerators;
using OpenApiInfo = Microsoft.OpenApi.OpenApiInfo;
using OpenApiServer = Microsoft.OpenApi.OpenApiServer;

namespace Elastic.Documentation.Api;

[EnumExtensions]
public enum AppEnv
{
	[Display(Name = "dev")]
	Dev,
	[Display(Name = "staging")]
	Staging,
	[Display(Name = "edge")]
	Edge,
	[Display(Name = "prod")]
	Prod
}

public class AppEnvironment
{
	public AppEnv Current { get; init; }
}

public static class ServicesExtension
{
	public static void AddElasticDocsApiServices(this IServiceCollection services, string? appEnvironment)
	{
		if (AppEnvExtensions.TryParse(appEnvironment, out var parsedEnvironment, true))
			AddElasticDocsApiServices(services, parsedEnvironment);
		else
		{
			// No logger exists before the host is built; Program.cs reports startup on the console too.
			Console.WriteLine($"Unable to parse environment '{appEnvironment}' into AppEnvironment. Using default AppEnvironment.Dev");
			AddElasticDocsApiServices(services, AppEnv.Dev);
		}
	}

	private static void AddElasticDocsApiServices(this IServiceCollection services, AppEnv appEnv)
	{
		_ = services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJsonContext.Default);
		});
		// Every error answers RFC 9457 problem details; binding failures throw, so they get a precise detail.
		_ = services.AddProblemDetails(options => options.CustomizeProblemDetails = ApiProblems.Customize);
		_ = services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
		_ = services.AddOpenApi(options => options
			.AddOperationTransformer((operation, _, _) =>
			{
				OpenApi.ApiDescriptions.ApplyToOperation(operation);
				return Task.CompletedTask;
			})
			.AddOperationTransformer(OpenApi.ApiExamples.Apply)
			.AddSchemaTransformer(OpenApi.ApiDescriptions.ApplyToSchema)
			.AddDocumentTransformer((document, _, _) =>
			{
				OpenApi.ApiDescriptions.ApplyToDocument(document);
				return Task.CompletedTask;
			})
			.AddDocumentTransformer((document, _, _) =>
			{
				document.Info = new OpenApiInfo
				{
					Title = "Elastic Docs API",
					Version = "v1",
					Description = "Search, Ask AI, change feeds, feedback and the API reference behind elastic.co/docs."
				};
				document.Servers = [new OpenApiServer { Url = OpenApi.ApiExamples.Server }];
				OpenApi.ApiExamples.RelativizePaths(document);
				return Task.CompletedTask;
			}));

		// Register AppEnvironment as a singleton for dependency injection
		_ = services.AddSingleton(new AppEnvironment { Current = appEnv });
		_ = services.AddSingleton<ITransport>(
			serviceProvider => ElasticsearchTransportFactory.Create(
				serviceProvider.GetRequiredService<DocumentationEndpoints>().Elasticsearch
			)
		);
		AddDistributedCache(services, appEnv);
		AddAskAiServices(services);
		AddPageFeedbackServices(services);
		_ = services.AddSearchServices();
		_ = services.AddApiReferenceServices();
	}

	private static void AddDistributedCache(IServiceCollection services, AppEnv appEnv)
	{
		switch (appEnv)
		{
			case AppEnv.Dev:
				_ = services.AddSingleton<IDistributedCache, InMemoryDistributedCache>();
				break;
			case AppEnv.Prod:
			case AppEnv.Staging:
			case AppEnv.Edge:
				_ = services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
				// L1: in-memory, L2: DynamoDB
				_ = services.AddSingleton<IDistributedCache>(sp =>
				{
					var tableName = $"docs-api-cache-{appEnv.ToStringFast(true)}";
					var dynamoCache = new DynamoDbDistributedCache(
						sp.GetRequiredService<IAmazonDynamoDB>(),
						tableName,
						sp.GetRequiredService<ILogger<DynamoDbDistributedCache>>()
					);
					return new MultiLayerCache(dynamoCache, sp.GetRequiredService<ILogger<MultiLayerCache>>());
				});
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(appEnv), appEnv, "Unsupported environment for distributed cache.");
		}
	}

	private static void AddAskAiServices(IServiceCollection services)
	{
		_ = services.AddSingleton<IGcpIdTokenProvider, GcpIdTokenProvider>();
		_ = services.AddSingleton<LlmGatewayOptions>();
		_ = services.AddSingleton<KibanaOptions>();
		_ = services.AddSingleton<AskAiProviderOptions>();

		_ = services.AddScoped<LlmGatewayAskAiGateway>();
		_ = services.AddScoped<AgentBuilderAskAiGateway>();
		_ = services.AddScoped<LlmGatewayStreamTransformer>();
		_ = services.AddScoped<AgentBuilderStreamTransformer>();

		// The provider comes from AskAiProviderOptions.ConfigurationKey and is fixed per deployment.
		_ = services.AddScoped<IAskAiService>(
			sp =>
				sp.GetRequiredService<AskAiProviderOptions>().Provider switch
				{
					AskAiProvider.AgentBuilder => sp.GetRequiredService<AgentBuilderAskAiGateway>(),
					_ => sp.GetRequiredService<LlmGatewayAskAiGateway>()
				}
		);
		_ = services.AddScoped<IStreamTransformer>(
			sp =>
				sp.GetRequiredService<AskAiProviderOptions>().Provider switch
				{
					AskAiProvider.AgentBuilder => sp.GetRequiredService<AgentBuilderStreamTransformer>(),
					_ => sp.GetRequiredService<LlmGatewayStreamTransformer>()
				}
		);

		// Singleton for connection reuse
		_ = services.AddSingleton<IAskAiMessageFeedbackService, ElasticsearchAskAiMessageFeedbackGateway>();
	}

	private static void AddPageFeedbackServices(IServiceCollection services)
	{
		_ = services.AddSingleton<PageFeedbackIndex>();
		_ = services.AddSingleton(
			serviceProvider => serviceProvider.GetRequiredService<PageFeedbackIndex>().CreateChannel(
				serviceProvider.GetRequiredService<ITransport>(),
				serviceProvider.GetRequiredService<ILogger<PageFeedbackIndex>>()
			)
		);
		_ = services.AddSingleton<IPageFeedbackService, ElasticsearchPageFeedbackGateway>();
		_ = services.AddHostedService<PageFeedbackBootstrapService>();
	}
}
