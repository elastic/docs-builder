// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

try
{
	var builder = WebApplication.CreateSlimBuilder(args);
	_ = builder.AddDocumentationServiceDefaults(ref args);
	_ = builder.AddDefaultHealthChecks();

	var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
	Console.WriteLine($"Docs Environment: {environment}");

	_ = builder.Services.AddSearchServices();

	_ = builder.Services.AddScoped<IDocumentGateway, DocumentGateway>();

	_ = builder.Services
		.AddMcpServer()
		.WithHttpTransport()
		.WithTools<SearchTools>()
		.WithTools<CoherenceTools>()
		.WithTools<DocumentTools>();

	var app = builder.Build();

	var logger = app.Services.GetRequiredService<ILogger<Program>>();

	LogElasticsearchConfiguration(app, logger);

	var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
	_ = lifetime.ApplicationStarted.Register(() => logger.LogInformation("Application started"));
	_ = lifetime.ApplicationStopping.Register(() => logger.LogWarning("Application is shutting down"));
	_ = lifetime.ApplicationStopped.Register(() => logger.LogWarning("Application has stopped"));

	if (app.Environment.IsDevelopment())
		_ = app.UseDeveloperExceptionPage();

	var mcp = app.MapGroup("/docs/_mcp");
	_ = mcp.MapHealthChecks("/health");
	_ = mcp.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
	_ = mcp.MapMcp("/");

	Console.WriteLine("MCP server startup completed successfully");
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

static void LogElasticsearchConfiguration(WebApplication app, ILogger logger)
{
	try
	{
		var esOptions = app.Services.GetService<ElasticsearchOptions>();
		if (esOptions != null)
		{
			logger.LogInformation(
				"Elasticsearch configuration - Url: {Url}, Index: {Index}",
				esOptions.Url,
				esOptions.IndexName
			);
		}
		else
			logger.LogWarning("ElasticsearchOptions could not be resolved from DI");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Failed to resolve Elasticsearch configuration");
	}
}

// Make the Program class accessible for integration testing
#pragma warning disable ASP0027
public partial class Program { }
#pragma warning restore ASP0027
