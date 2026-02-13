// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Mcp.Remote.Gateways;
using Elastic.Documentation.Mcp.Remote.Tools;
using Elastic.Documentation.Search;
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
	Console.WriteLine($"FATAL ERROR during startup: {ex}");
	Console.WriteLine($"Exception type: {ex.GetType().Name}");
	Console.WriteLine($"Stack trace: {ex.StackTrace}");
	throw;
}

// Make the Program class accessible for integration testing
#pragma warning disable ASP0027
public partial class Program { }
#pragma warning restore ASP0027
