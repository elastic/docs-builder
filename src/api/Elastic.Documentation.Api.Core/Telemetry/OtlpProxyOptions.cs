// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// Configuration options for the OTLP proxy.
/// The proxy forwards telemetry to a local OTLP collector (typically ADOT Lambda Layer).
/// </summary>
/// <remarks>
/// ADOT Lambda Layer runs a local OpenTelemetry Collector that accepts OTLP/HTTP on:
/// - localhost:4318 (HTTP/JSON and HTTP/protobuf)
/// - localhost:4317 (gRPC)
/// 
/// Configuration priority:
/// 1. OtlpProxy:Endpoint in IConfiguration (for tests/overrides)
/// 2. OTEL_EXPORTER_OTLP_ENDPOINT environment variable
/// 3. Default: http://localhost:4318
/// 
/// The proxy will return 503 if the collector is not available.
/// </remarks>
public class OtlpProxyOptions
{
	/// <summary>
	/// OTLP endpoint URL for the local ADOT collector.
	/// Defaults to localhost:4318 when running in Lambda with ADOT layer.
	/// </summary>
	public string Endpoint { get; }

	public OtlpProxyOptions(IConfiguration configuration)
	{
		// Check for explicit configuration override first (for tests or custom deployments)
		var configEndpoint = configuration["OtlpProxy:Endpoint"];
		if (!string.IsNullOrEmpty(configEndpoint))
		{
			Endpoint = configEndpoint;
			return;
		}

		// Default to localhost:4318 - this is where ADOT Lambda Layer collector runs
		// If ADOT layer is not present, the proxy will fail gracefully and return 503
		Endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
			?? "http://localhost:4318";
	}
}
