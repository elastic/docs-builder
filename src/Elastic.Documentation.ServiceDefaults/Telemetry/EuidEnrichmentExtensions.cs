// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Elastic.Documentation.ServiceDefaults.Logging;
using Elastic.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.ServiceDefaults.Telemetry;

public static class EuidEnrichmentExtensions
{
	/// <summary>
	/// Configures Elastic OpenTelemetry (EDOT) with euid enrichment for logging, tracing, and metrics.
	/// Reads service.version from the calling assembly and configures the resource attribute.
	/// No-ops if OTEL_EXPORTER_OTLP_ENDPOINT is not set.
	/// </summary>
	/// <param name="builder">The host application builder</param>
	/// <returns>The builder for chaining</returns>
	public static TBuilder AddEuidEnrichment<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
		if (!useOtlpExporter)
			return builder;

		var options = new ElasticOpenTelemetryOptions
		{
			// In AOT mode, assembly scanning is not supported, so we skip it
			// for consistency with the non-AOT mode
			SkipInstrumentationAssemblyScanning = true
		};

		_ = builder.AddElasticOpenTelemetry(options, edotBuilder =>
		{
			_ = edotBuilder
				.WithLogging(logging => logging.AddProcessor<EuidLogProcessor>())
				.WithTracing(tracing =>
				{
					_ = tracing
						.AddAspNetCoreInstrumentation(aspNetCoreOptions =>
						{
							// Enrich spans with custom attributes from HTTP context
							aspNetCoreOptions.EnrichWithHttpRequest = (activity, httpRequest) =>
							{
								// Add euid cookie value to span attributes and baggage
								if (httpRequest.Cookies.TryGetValue("euid", out var euid) && !string.IsNullOrEmpty(euid))
								{
									_ = activity.SetTag(TelemetryConstants.UserEuidAttributeName, euid);
									// Add to baggage so it propagates to all child spans
									_ = activity.AddBaggage(TelemetryConstants.UserEuidAttributeName, euid);
								}
							};
						})
						.AddProcessor<EuidSpanProcessor>() // Automatically add euid to all child spans
						.AddHttpClientInstrumentation();
				})
				.WithMetrics(metrics =>
				{
					_ = metrics
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation();
				});
		});

		ConfigureServiceVersionAttributes(builder);

		return builder;
	}

	// Configure service.version for ALL signals (traces, metrics, logs)
	// Only set it if we have a valid version from MinVer
	// If null, something is wrong with the build and we should see the missing attribute
	private static void ConfigureServiceVersionAttributes<TBuilder>(TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		var informationalVersion = (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly())
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		if (informationalVersion is null)
		{
			Console.WriteLine($"Unable to determine service.version from {nameof(AssemblyInformationalVersionAttribute)}. Skipping setting it.");
			return;
		}

		// Extract just major.minor.patch by removing prerelease tags (-) and build metadata (+)
		var serviceVersion = informationalVersion.Split(['+', '-'])[0];

		var versionAttribute = new KeyValuePair<string, object>("service.version", serviceVersion);

		_ = builder.Services.ConfigureOpenTelemetryTracerProvider(tracerProviderBuilder =>
		{
			_ = tracerProviderBuilder.ConfigureResource(resourceBuilder =>
				_ = resourceBuilder.AddAttributes([versionAttribute]));
		});

		_ = builder.Services.ConfigureOpenTelemetryMeterProvider(meterProviderBuilder =>
		{
			_ = meterProviderBuilder.ConfigureResource(resourceBuilder =>
				_ = resourceBuilder.AddAttributes([versionAttribute]));
		});

		_ = builder.Services.ConfigureOpenTelemetryLoggerProvider(loggerProviderBuilder =>
		{
			_ = loggerProviderBuilder.ConfigureResource(resourceBuilder =>
				_ = resourceBuilder.AddAttributes([versionAttribute]));
		});
	}
}
