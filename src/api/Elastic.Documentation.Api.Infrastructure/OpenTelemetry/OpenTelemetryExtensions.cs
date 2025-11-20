// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Documentation.Api.Core;
using Elastic.OpenTelemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
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

		return builder;
	}

	/// <summary>
	/// Configures Elastic OpenTelemetry (EDOT) for the Docs API.
	/// Only enables if OTEL_EXPORTER_OTLP_ENDPOINT environment variable is set.
	/// </summary>
	/// <param name="builder">The web application builder</param>
	/// <returns>The builder for chaining</returns>
	public static TBuilder AddDocsApiOpenTelemetry<TBuilder>(
		this TBuilder builder)
		where TBuilder : IHostApplicationBuilder
	{
		var options = new ElasticOpenTelemetryOptions
		{
			SkipInstrumentationAssemblyScanning = true // Disable instrumentation assembly scanning for AOT
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
		return builder;
	}
}
