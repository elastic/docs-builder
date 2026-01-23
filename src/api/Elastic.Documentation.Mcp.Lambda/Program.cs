// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Elastic.Documentation.Mcp.Lambda.Gateways;
using Elastic.Documentation.Mcp.Lambda.Tools;
using Elastic.Documentation.Search;
using Elastic.Documentation.ServiceDefaults;

try
{
	var builder = WebApplication.CreateSlimBuilder(args);
	_ = builder.AddDocumentationServiceDefaults(ref args);

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

	// Add search services
	_ = builder.Services.AddSearchServices();

	// Add MCP-specific gateways
	_ = builder.Services.AddScoped<IDocumentGateway, DocumentGateway>();

	// Configure MCP server with HTTP transport
	// Use generic WithTools method for AOT compatibility
	_ = builder.Services
		.AddMcpServer()
		.WithHttpTransport()
		.WithTools<SearchTools>()
		.WithTools<CoherenceTools>()
		.WithTools<DocumentTools>();

	var app = builder.Build();

	if (app.Environment.IsDevelopment())
		_ = app.UseDeveloperExceptionPage();

	// Map MCP endpoint
	_ = app.MapMcp();

	Console.WriteLine("MCP Lambda startup completed successfully");
	app.Run();
}
catch (Exception ex)
{
	Console.WriteLine($"FATAL ERROR during startup: {ex}");
	Console.WriteLine($"Exception type: {ex.GetType().Name}");
	Console.WriteLine($"Stack trace: {ex.StackTrace}");
	throw;
}

[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
internal sealed partial class LambdaJsonSerializerContext : JsonSerializerContext;

// Make the Program class accessible for integration testing
#pragma warning disable ASP0027
public partial class Program { }
#pragma warning restore ASP0027
