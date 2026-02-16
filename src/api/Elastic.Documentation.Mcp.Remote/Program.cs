// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ModelContextProtocol;

try
{
	var builder = WebApplication.CreateSlimBuilder(args);
	_ = builder.AddDocumentationServiceDefaults(ref args);
	_ = builder.AddDefaultHealthChecks();
	_ = builder.AddDocsApiOpenTelemetry();

	// Configure Kestrel to listen on port 8080 (standard container port)
	_ = builder.WebHost.ConfigureKestrel(serverOptions =>
	{
		serverOptions.ListenAnyIP(8080);
	});

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

	_ = builder.Services.ConfigureHttpJsonOptions(options =>
	{
		if (McpJsonUtilities.DefaultOptions.TypeInfoResolver is not null)
			options.SerializerOptions.TypeInfoResolverChain.Insert(0, McpJsonUtilities.DefaultOptions.TypeInfoResolver);
		else
			logger.LogWarning("McpJsonUtilities.DefaultOptions.TypeInfoResolver is null");
	});

	var app = builder.Build();

	var logger = app.Services.GetRequiredService<ILogger<Program>>();

	LogElasticsearchConfiguration(app, logger);

	var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
	_ = lifetime.ApplicationStarted.Register(() => logger.LogInformation("Application started"));
	_ = lifetime.ApplicationStopping.Register(() => logger.LogWarning("Application is shutting down"));
	_ = lifetime.ApplicationStopped.Register(() => logger.LogWarning("Application has stopped"));

	_ = app.Environment.IsDevelopment()
		? app.UseDeveloperExceptionPage()
		: app.UseExceptionHandler(err => err.Run(context =>
		{
			var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
			if (ex != null)
				logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
			context.Response.StatusCode = 500;
			return Task.CompletedTask;
		}));

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
