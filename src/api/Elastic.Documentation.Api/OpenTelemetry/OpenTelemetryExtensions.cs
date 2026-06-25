// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using OpenTelemetry.Trace;

namespace Elastic.Documentation.Api.OpenTelemetry;

public static class OpenTelemetryExtensions
{
	/// <summary>
	/// Configures tracing for the Docs API with sources, instrumentation, and enrichment.
	/// This is the shared configuration used in both production and tests.
	/// </summary>
	public static TracerProviderBuilder AddDocsApiTracing(this TracerProviderBuilder builder)
	{
		_ = builder
			.AddSource(TelemetryConstants.AskAiSourceName)
			.AddSource(TelemetryConstants.StreamTransformerSourceName)
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
			});

		return builder;
	}
}
