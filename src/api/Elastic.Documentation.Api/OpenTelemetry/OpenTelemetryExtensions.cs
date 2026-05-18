// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ServiceDefaults.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.OpenTelemetry;

public static class OpenTelemetryExtensions
{
	/// <summary>
	/// Configures Elastic OpenTelemetry (EDOT) for the Docs API.
	/// Delegates euid enrichment and base configuration to AddEuidEnrichment,
	/// then adds API-specific activity sources and path filters.
	/// </summary>
	/// <param name="builder">The web application builder</param>
	/// <returns>The builder for chaining</returns>
	public static TBuilder AddDocsApiOpenTelemetry<TBuilder>(this TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
		if (!useOtlpExporter)
			return builder;

		// Configure euid enrichment (euid processors, AspNetCore + HttpClient instrumentation, service.version)
		_ = builder.AddEuidEnrichment();

		// Additionally register the 5 API-specific activity sources
		_ = builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
		{
			_ = tracing
				.AddSource(TelemetryConstants.AskAiSourceName)
				.AddSource(TelemetryConstants.StreamTransformerSourceName)
				.AddSource(TelemetryConstants.OtlpProxySourceName)
				.AddSource(TelemetryConstants.CacheSourceName)
				.AddSource(TelemetryConstants.AskAiFeedbackSourceName);
		});

		return builder;
	}

	/// <summary>
	/// Configures logging for the Docs API with euid enrichment.
	/// This is the shared configuration used in both production and tests.
	/// </summary>
	public static global::OpenTelemetry.Logs.LoggerProviderBuilder AddDocsApiLogging(this global::OpenTelemetry.Logs.LoggerProviderBuilder builder)
	{
		_ = builder.AddProcessor<Elastic.Documentation.ServiceDefaults.Logging.EuidLogProcessor>();
		return builder;
	}

	/// <summary>
	/// Configures tracing for the Docs API with sources, instrumentation, and enrichment.
	/// This is the shared configuration used in both production and tests.
	/// </summary>
	public static global::OpenTelemetry.Trace.TracerProviderBuilder AddDocsApiTracing(this global::OpenTelemetry.Trace.TracerProviderBuilder builder)
	{
		_ = builder
			.AddSource(TelemetryConstants.AskAiSourceName)
			.AddSource(TelemetryConstants.StreamTransformerSourceName)
			.AddSource(TelemetryConstants.OtlpProxySourceName)
			.AddSource(TelemetryConstants.CacheSourceName)
			.AddSource(TelemetryConstants.AskAiFeedbackSourceName)
			.AddAspNetCoreInstrumentation(aspNetCoreOptions =>
			{
				// Don't trace root API endpoint (health check)
				aspNetCoreOptions.Filter = (httpContext) =>
				{
					var path = httpContext.Request.Path.Value ?? string.Empty;
					// Exclude root API path: /docs/_api/v1
					return path != "/docs/_api/v1";
				};

				// Enrich spans with custom attributes from HTTP context
				aspNetCoreOptions.EnrichWithHttpRequest = (activity, httpRequest) =>
				{
					// Add euid cookie value to span attributes and baggage
					if (httpRequest.Cookies.TryGetValue("euid", out var euid) && !string.IsNullOrEmpty(euid))
					{
						_ = activity.SetTag(Elastic.Documentation.ServiceDefaults.Telemetry.TelemetryConstants.UserEuidAttributeName, euid);
						// Add to baggage so it propagates to all child spans
						_ = activity.AddBaggage(Elastic.Documentation.ServiceDefaults.Telemetry.TelemetryConstants.UserEuidAttributeName, euid);
					}
				};
			})
			.AddProcessor<Elastic.Documentation.ServiceDefaults.Telemetry.EuidSpanProcessor>() // Automatically add euid to all child spans
			.AddHttpClientInstrumentation();

		return builder;
	}
}
