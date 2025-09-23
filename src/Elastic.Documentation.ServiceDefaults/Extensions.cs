// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
	private const string HealthEndpointPath = "/health";
	private const string AlivenessEndpointPath = "/alive";

	public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder
			.ConfigureOpenTelemetry()
			.AddDefaultHealthChecks();

		_ = builder.Services
			.AddServiceDiscovery()
			.ConfigureHttpClientDefaults(http =>
			{
				_ = http.AddStandardResilienceHandler();
				_ = http.AddServiceDiscovery();
			});
		return builder;
	}

	public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder.Logging.AddOpenTelemetry(logging =>
		{
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes = true;
		});

		_ = builder.Services.AddOpenTelemetry()
			.WithMetrics(metrics =>
			{
				_ = metrics.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddRuntimeInstrumentation();
			})
			.WithTracing(tracing =>
			{
				_ = tracing.AddSource(builder.Environment.ApplicationName)
					.AddAspNetCoreInstrumentation(instrumentation =>
						// Exclude health check requests from tracing
						instrumentation.Filter = context =>
							!context.Request.Path.StartsWithSegments(HealthEndpointPath)
							&& !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
					)
					// Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
					//.AddGrpcClientInstrumentation()
					.AddHttpClientInstrumentation();
			});

		_ = builder.AddOpenTelemetryExporters();

		return builder;
	}

	private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

		if (useOtlpExporter)
			_ = builder.Services.AddOpenTelemetry().UseOtlpExporter();

		// Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
		//if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
		//{
		//    builder.Services.AddOpenTelemetry()
		//       .UseAzureMonitor();
		//}

		return builder;
	}

	public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		_ = builder.Services.AddHealthChecks()
			.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

		return builder;
	}

	public static WebApplication MapDefaultEndpoints(this WebApplication app)
	{
		// Adding health checks endpoints to applications in non-development environments has security implications.
		// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
		if (app.Environment.IsDevelopment())
		{
			// All health checks must pass for app to be considered ready to accept traffic after starting
			_ = app.MapHealthChecks(HealthEndpointPath);

			// Only health checks tagged with the "live" tag must pass for app to be considered alive
			_ = app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
			{
				Predicate = r => r.Tags.Contains("live")
			});
		}

		return app;
	}
}
