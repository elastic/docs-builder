// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Mcp.Remote;
using Elastic.Documentation.Search;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

	var env = SystemEnvironmentVariables.Instance;
	var profile = McpServerProfile.Resolve(env.McpServerProfile);

	profile.RegisterAllServices(builder.Services);

	// CreateSlimBuilder disables reflection-based JSON serialization.
	// The MCP SDK's legacy SSE handler uses Results.BadRequest(string) which needs
	// ASP.NET Core's HTTP JSON options to have type metadata for System.String.
	_ = builder.Services.ConfigureHttpJsonOptions(options =>
	{
		options.SerializerOptions.TypeInfoResolverChain.Insert(0, McpJsonUtilities.DefaultOptions.TypeInfoResolver!);
	});

	// Stateless mode: no Mcp-Session-Id header is issued or expected, which avoids a known
	// Cursor bug where it opens the SSE stream without the session header and receives 400.
	// Stateless mode is appropriate here because all tools are pure request/response (no
	// server-initiated push) and the server runs behind a load balancer without session affinity.
	var mcpBuilder = builder.Services
		.AddMcpServer(options => options.ServerInstructions = profile.ComposeServerInstructions())
		.WithHttpTransport(o => o.Stateless = true);

	var prefixedTools = McpToolRegistration.CreatePrefixedTools(profile);
	mcpBuilder = mcpBuilder.WithTools(prefixedTools);

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

	_ = app.UseMiddleware<McpBearerAuthMiddleware>();
	_ = app.UseMiddleware<SseKeepAliveMiddleware>();

	var mcpPrefix = SystemEnvironmentVariables.Instance.McpPrefix;
	var mcp = app.MapGroup(mcpPrefix);

	if (SystemEnvironmentVariables.Instance.McpOAuthIssuer is not null)
		McpOAuthMetadata.MapEndpoints(mcp);

	_ = mcp.MapHealthChecks("/health");
	_ = mcp.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
	_ = mcp.MapMcp("");

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
