// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.OpenTelemetry;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.Infrastructure;

public static class OpenTelemetryExtensions
{
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
				.WithLogging()
				.WithTracing(tracing =>
				{
					_ = tracing
						.AddSource("Elastic.Documentation.Api.AskAi")
						.AddSource("Elastic.Documentation.Api.Search")
						.AddSource("Elastic.Documentation.Api.StreamTransformer")
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation();
				})
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
