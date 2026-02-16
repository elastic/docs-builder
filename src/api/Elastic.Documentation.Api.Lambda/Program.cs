// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.Core.Search;
using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ServiceDefaults;

try
{
	var builder = WebApplication.CreateSlimBuilder(args);
	_ = builder.AddDocumentationServiceDefaults(ref args, (s, p) =>
	{
		_ = s.AddSingleton(AssemblyConfiguration.Create(p));
	});
	// Add logging configuration for Lambda
	_ = builder.AddDocsApiOpenTelemetry();

	// If we are running in Lambda Web Adapter response_stream mode, configure Kestrel to listen on port 8080
	// Otherwise, configure AWS Lambda hosting for API Gateway HTTP API
	if (Environment.GetEnvironmentVariable("AWS_LWA_INVOKE_MODE") == "response_stream")
	{
		// Configure Kestrel to listen on port 8080 for Lambda Web Adapter
		// Lambda Web Adapter expects the app to run as a standard HTTP server on localhost:8080
		_ = builder.WebHost.ConfigureKestrel(serverOptions =>
		{
			serverOptions.ListenLocalhost(8080);
		});
	}
	else
	{
		// Configure AWS Lambda hosting with custom JSON serializer context for API Gateway HTTP API
		_ = builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi, new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>());
		_ = builder.WebHost.UseKestrelHttpsConfiguration();
	}
	var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
	Console.WriteLine($"Docs Environment: {environment}");

	builder.Services.AddElasticDocsApiUsecases(environment);
	var app = builder.Build();

	var logger = app.Services.GetRequiredService<ILogger<Program>>();

	var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
	_ = lifetime.ApplicationStarted.Register(() => logger.LogInformation("Application started"));
	_ = lifetime.ApplicationStopping.Register(() => logger.LogWarning("Application is shutting down"));
	_ = lifetime.ApplicationStopped.Register(() => logger.LogWarning("Application has stopped"));

	if (app.Environment.IsDevelopment())
		_ = app.UseDeveloperExceptionPage();

	var v1 = app.MapGroup("/docs/_api/v1");

	var mapOtlpEndpoints = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
	v1.MapElasticDocsApiEndpoints(mapOtlpEndpoints);
	Console.WriteLine("API endpoints mapped");

	Console.WriteLine("Application startup completed successfully");
	app.Run();
}
catch (Exception ex)
{
	Console.WriteLine($"FATAL ERROR: {ex}");
	Console.WriteLine($"Exception type: {ex.GetType().FullName}");
	Console.WriteLine($"Message: {ex.Message}");
	if (ex.InnerException != null)
		Console.WriteLine($"Inner exception: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
	Console.WriteLine($"Stack trace: {ex.StackTrace}");
	throw;
}

[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
[JsonSerializable(typeof(AskAiRequest))]
[JsonSerializable(typeof(NavigationSearchApiRequest))]
[JsonSerializable(typeof(NavigationSearchApiResponse))]
[JsonSerializable(typeof(NavigationSearchAggregations))]
internal sealed partial class LambdaJsonSerializerContext : JsonSerializerContext;

// Make the Program class accessible for integration testing
#pragma warning disable ASP0027
public partial class Program { }
#pragma warning restore ASP0027
