// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.ServiceDefaults.Logging;
using Elastic.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.ServiceDefaults.Telemetry;

public record OtelRegistration(string ServiceName)
{
	public Action<ElasticOpenTelemetryOptions, TracerProviderBuilder>? Tracing { get; init; }
	public Action<ElasticOpenTelemetryOptions, MeterProviderBuilder>? Metrics { get; init; }
	public Action<ElasticOpenTelemetryOptions, LoggerProviderBuilder>? Logging { get; init; }
}

public static class OpenTelemetryRegistrationExtensions
{
	/// <summary>
	/// Configures Elastic OpenTelemetry (EDOT) with euid enrichment for logging, tracing, and metrics.
	/// Reads service.version from the calling assembly and configures the resource attribute.
	/// No-ops if OTEL_EXPORTER_OTLP_ENDPOINT is not set.
	/// </summary>
	/// <returns>The builder for chaining</returns>
	public static TBuilder AddDocumentationOpenTelemetry<TBuilder>(this TBuilder builder, OtelRegistration registration)
		where TBuilder : IHostApplicationBuilder
	{
		var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
		if (!useOtlpExporter)
			return builder;

		var environment = builder.Configuration["ENVIRONMENT"];
		var serviceName = registration.ServiceName;

		var options = new ElasticOpenTelemetryOptions
		{
			// In AOT mode, assembly scanning is not supported, so we skip it
			// for consistency with the non-AOT mode
			SkipInstrumentationAssemblyScanning = true
		};

		// Configure delta temporality for Elasticsearch compatibility
		// See: https://www.elastic.co/docs/reference/opentelemetry/compatibility/limitations#histograms-in-delta-temporality-only
		_ = builder.Services.Configure<MetricReaderOptions>(mo =>
		{
			mo.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
		});
		_ = builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
		{
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes = true;
		});

		_ = builder.AddElasticOpenTelemetry(options, edotBuilder =>
		{
			_ = edotBuilder.ConfigureResource(r => ResourceBuilderExtensions.AddService(r, serviceName: serviceName, serviceVersion: VersionHelper.InformationalVersion)
				.AddAttributes(new Dictionary<string, object>
				{
					["deployment.environment"] = !string.IsNullOrWhiteSpace(environment) ? environment : builder.Environment.EnvironmentName,
				})
			);
			_ = edotBuilder
				.WithLogging(logging =>
				{
					_ = logging.AddProcessor<EuidLogProcessor>();
					registration.Logging?.Invoke(options, logging);
				})
				.WithTracing(tracing =>
				{
					_ = tracing
						.AddSource(builder.Environment.ApplicationName)
						.AddAspNetCoreInstrumentation(aspNetCoreOptions =>
						{
							// Exclude requests from our own synthetics monitors from tracing
							aspNetCoreOptions.Filter = httpContext =>
								!httpContext.Request.Headers.ContainsKey(TelemetryConstants.SyntheticMonitorHeaderName);
							// Enrich spans with custom attributes from HTTP context
							aspNetCoreOptions.EnrichWithHttpRequest = (activity, httpRequest) =>
							{
								// Add euid cookie value to span attributes and baggage
								if (!httpRequest.Cookies.TryGetValue("euid", out var euid) || string.IsNullOrEmpty(euid))
									return;
								_ = activity.SetTag(TelemetryConstants.UserEuidAttributeName, euid);
								// Add to baggage so it propagates to all child spans
								_ = activity.AddBaggage(TelemetryConstants.UserEuidAttributeName, euid);
							};
						})
						.AddProcessor<EuidSpanProcessor>() // Automatically add euid to all child spans
						.AddHttpClientInstrumentation();
					registration.Tracing?.Invoke(options, tracing);
				})
				.WithMetrics(metrics =>
				{
					_ = metrics
						.AddAspNetCoreInstrumentation()
						.AddRuntimeInstrumentation()
						.AddHttpClientInstrumentation();
					registration.Metrics?.Invoke(options, metrics);
				});
		});

		return builder;
	}
}
