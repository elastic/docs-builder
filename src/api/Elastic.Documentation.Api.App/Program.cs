// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Infrastructure;
using Elastic.Documentation.Api.Infrastructure.OpenTelemetry;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.ServiceDefaults;
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

	if (app.Environment.IsDevelopment())
		_ = app.UseDeveloperExceptionPage();

	var api = app.MapGroup("/docs/_api");

	_ = api.MapHealthChecks("/health");
	_ = api.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
	
	var v1 = api.MapGroup("/docs/_api/v1");
	

	var mapOtlpEndpoints = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
	v1.MapElasticDocsApiEndpoints(mapOtlpEndpoints);
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

// Make the Program class accessible for integration testing
#pragma warning disable ASP0027
public partial class Program { }
#pragma warning restore ASP0027
