// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.ServiceDefaults;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

try
{
	var process = System.Diagnostics.Process.GetCurrentProcess();
	Console.WriteLine($"Starting Lambda application... Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	var builder = WebApplication.CreateSlimBuilder(args);
	process.Refresh();
	Console.WriteLine($"WebApplication builder created. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	_ = builder.AddDocumentationServiceDefaults(ref args);
	process.Refresh();
	Console.WriteLine($"Documentation service defaults added. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	_ = builder.AddElasticOpenTelemetry(edotBuilder =>
	{
		_ = edotBuilder
			.WithElasticTracing(tracing =>
			{
				_ = tracing
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation();
			})
			.WithElasticLogging()
			.WithElasticMetrics(metrics =>
			{
				_ = metrics
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddProcessInstrumentation()
					.AddRuntimeInstrumentation();
			});
	});

	process.Refresh();
	Console.WriteLine($"Elastic OTel configured. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	_ = builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi, new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>());
	process.Refresh();
	Console.WriteLine($"AWS Lambda hosting configured. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
	Console.WriteLine($"Environment: {environment}");

	builder.Services.AddElasticDocsApiUsecases(environment);
	process.Refresh();
	Console.WriteLine($"Elastic docs API use cases added. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	_ = builder.WebHost.UseKestrelHttpsConfiguration();
	process.Refresh();
	Console.WriteLine($"Kestrel HTTPS configuration applied. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	var app = builder.Build();
	process.Refresh();
	Console.WriteLine($"Application built successfully. Memory: {process.WorkingSet64 / 1024 / 1024} MB");

	var v1 = app.MapGroup("/docs/_api/v1");
	v1.MapElasticDocsApiEndpoints();
	Console.WriteLine("API endpoints mapped");

	Console.WriteLine("Application startup completed successfully");
	app.Run();
}
catch (Exception ex)
{
	Console.WriteLine($"FATAL ERROR during startup: {ex}");
	Console.WriteLine($"Exception type: {ex.GetType().Name}");
	Console.WriteLine($"Stack trace: {ex.StackTrace}");
	throw;
}

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(AskAiRequest))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(SearchResponse))]
internal sealed partial class LambdaJsonSerializerContext : JsonSerializerContext;
