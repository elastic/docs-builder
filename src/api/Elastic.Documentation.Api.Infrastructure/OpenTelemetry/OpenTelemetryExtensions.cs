// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Elastic.Documentation.Api.Core;
using Elastic.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.Infrastructure.OpenTelemetry;

public static class OpenTelemetryExtensions
{
	/// <summary>
	/// Configures logging for the Docs API with euid enrichment.
	/// This is the shared configuration used in both production and tests.
	/// </summary>
	public static LoggerProviderBuilder AddDocsApiLogging(this LoggerProviderBuilder builder)
	{
		_ = builder.AddProcessor<EuidLogProcessor>();
		return builder;
	}

	/// <summary>
	/// Configures tracing for the Docs API with sources, instrumentation, and enrichment.
	/// This is the shared configuration used in both production and tests.
	/// </summary>
	public static TracerProviderBuilder AddDocsApiTracing(this TracerProviderBuilder builder)
	{
		_ = builder
			.AddSource(TelemetryConstants.AskAiSourceName)
			.AddSource(TelemetryConstants.StreamTransformerSourceName)
			.AddSource(TelemetryConstants.OtlpProxySourceName)
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
						_ = activity.SetTag(TelemetryConstants.UserEuidAttributeName, euid);
						// Add to baggage so it propagates to all child spans
						_ = activity.AddBaggage(TelemetryConstants.UserEuidAttributeName, euid);
					}
				};
			})
			.AddProcessor<EuidSpanProcessor>() // Automatically add euid to all child spans
			.AddHttpClientInstrumentation();

		return builder;
	}

	/// <summary>
	/// Configures Elastic OpenTelemetry (EDOT) for the Docs API.
	/// </summary>
	/// <param name="builder">The web application builder</param>
	/// <returns>The builder for chaining</returns>
	public static TBuilder AddDocsApiOpenTelemetry<TBuilder>(
		this TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		var options = new ElasticOpenTelemetryOptions
		{
			// In AOT mode, we cannot scan the assembly for attributes, so we skip it
			// for consistency with the non-AOT mode
			SkipInstrumentationAssemblyScanning = true
		};

		_ = builder.AddElasticOpenTelemetry(options, edotBuilder =>
		{
			_ = edotBuilder
				.WithLogging(logging => logging.AddDocsApiLogging())
				.WithTracing(tracing => tracing.AddDocsApiTracing())
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

		var serviceVersion = Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		if (serviceVersion is null)
		{
			Console.WriteLine($"Unable to determine service.version from {nameof(AssemblyInformationalVersionAttribute)}. Skipping setting it.");
			return;
		}

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
