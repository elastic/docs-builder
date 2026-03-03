// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Search;
using Elastic.Documentation.ServiceDefaults;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

try
{
	var builder = WebApplication.CreateSlimBuilder(args);
	_ = builder.AddDocumentationServiceDefaults(ref args, (s, p) =>
	{
		_ = s.AddSingleton(AssemblyConfiguration.Create(p));
	});

	_ = builder.AddDefaultHealthChecks();
	_ = builder.AddDocsApiOpenTelemetry();

	// Configure Kestrel to listen on port 8080 (standard container port)
	_ = builder.WebHost.ConfigureKestrel(serverOptions =>
	{
		serverOptions.ListenAnyIP(8080);
	});

	var environment = Environment.GetEnvironmentVariable("ENVIRONMENT");
	Console.WriteLine($"Docs Environment: {environment}");

	builder.Services.AddElasticDocsApiUsecases(environment);
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

	var api = app.MapGroup(SystemEnvironmentVariables.Instance.ApiPrefix);

	_ = api.MapHealthChecks("/health");
	_ = api.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });

	var v1 = api.MapGroup("/v1");

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
