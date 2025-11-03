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
			// TODO: I don't think we really want to set `SkipOtlpExporter=true`.
			// But without it, EDOT is sending duplicated traces and spans to the OTLP endpoint.
			// Needs investigation.
			// *However*, this makes it work correctly.
			SkipOtlpExporter = true,
			SkipInstrumentationAssemblyScanning = true // Disable instrumentation assembly scanning for AOT
		};

		_ = builder.AddElasticOpenTelemetry(options, edotBuilder =>
		{
			_ = edotBuilder
				.WithElasticLogging()
				.WithElasticTracing(tracing =>
				{
					_ = tracing
						.AddSource("Elastic.Documentation.Api.AskAi")
						.AddSource("Elastic.Documentation.Api.StreamTransformer")
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation();
				})
				.WithElasticMetrics(metrics =>
				{
					_ = metrics
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation();
				});
		});
		return builder;
	}
}
