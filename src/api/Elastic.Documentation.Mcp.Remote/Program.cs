// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Mcp.Remote;
using Elastic.Documentation.Mcp.Remote.Telemetry;
using Elastic.Documentation.Search.Common;
using Elastic.Documentation.ServiceDefaults;
using Elastic.Documentation.ServiceDefaults.Telemetry;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ModelContextProtocol;
using OpenTelemetry;
using OpenTelemetry.Trace;

try
{
	var builder = WebApplication.CreateSlimBuilder(args)
		.AddDocumentationServiceDefaults()
		.HealthCheckBuilderExtensions()
		.AddDocumentationOpenTelemetry(new OtelRegistration("docs-mcp")
		{
			Tracing = (_, t) => t
				.WithElasticDefaults()
				.AddSource(McpToolTelemetry.McpToolSourceName)
				.AddProcessor(new McpSpanRenameProcessor()),
			Metrics = (_, m) => m
				.WithElasticDefaults()
				.AddMeter(McpToolTelemetry.McpMeterName)
		});

	// Only hardcode port 8080 when not running under Aspire/orchestration.
	// Use builder.Configuration so both ASPNETCORE_* and DOTNET_* prefix variants are covered.
	if (string.IsNullOrEmpty(builder.Configuration["HTTP_PORTS"])
		&& string.IsNullOrEmpty(builder.Configuration["HTTPS_PORTS"])
		&& string.IsNullOrEmpty(builder.Configuration["URLS"]))
	{
		_ = builder.WebHost.ConfigureKestrel(serverOptions =>
		{
			serverOptions.ListenAnyIP(8080);
		});
	}

	var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
	Console.WriteLine($"Docs Environment: {environment}");

	var env = SystemEnvironmentVariables.Instance;
	var profile = McpServerProfile.Resolve(env.McpServerProfile);

	profile.RegisterAllServices(builder.Services);

	// CreateSlimBuilder disables reflection-based JSON serialization.
	// McpJsonUtilities registers System.String so the SDK's error responses can serialize.
	_ = builder.Services.ConfigureHttpJsonOptions(options =>
	{
		options.SerializerOptions.TypeInfoResolverChain.Insert(0, McpJsonUtilities.DefaultOptions.TypeInfoResolver!);
	});

	// Stateless Streamable HTTP transport: each request is an independent POST / — no session
	// affinity, no Mcp-Session-Id header, no server-initiated push (sampling/elicitation/roots).
	// This is the correct posture for a load-balanced service whose tools are pure request/response.
	// In SDK 1.4+, stateless and SSE are mutually exclusive; EnableLegacySse (default false)
	// cannot be combined with Stateless = true. SSE-only clients should use the mcp-remote bridge:
	// npx -y mcp-remote https://<host>/docs/_mcp
	var mcpBuilder = builder.Services
		.AddMcpServer(options => options.ServerInstructions = profile.ComposeServerInstructions())
		.WithHttpTransport(o => o.Stateless = true);

	var prefixedTools = McpToolRegistration.CreatePrefixedTools(profile);
	_ = mcpBuilder.WithTools(prefixedTools);

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
		var clientAccessor = app.Services.GetService<ElasticsearchClientAccessor>();
		if (clientAccessor is not null)
		{
			logger.LogInformation(
				"Elasticsearch configuration - Url: {Url}, SearchIndex: {SearchIndex}",
				clientAccessor.Endpoint.Uri,
				clientAccessor.SearchIndex
			);
		}
		else
			logger.LogWarning("ElasticsearchClientAccessor could not be resolved from DI");
	}
	catch (Exception ex)
	{
		logger.LogError(ex, "Failed to resolve Elasticsearch configuration");
	}
}

// Make the Program class accessible for integration testing
#pragma warning disable ASP0027
public partial class Program
{
}
#pragma warning restore ASP0027
