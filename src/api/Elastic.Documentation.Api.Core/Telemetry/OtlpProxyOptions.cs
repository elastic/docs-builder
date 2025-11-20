// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Microsoft.Extensions.Configuration;

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// Configuration options for the OTLP proxy.
/// When using ADOT Lambda Layer, the proxy forwards to the local collector at localhost:4318.
/// The ADOT layer handles authentication and forwarding to the backend (Elastic APM, etc).
/// </summary>
/// <remarks>
/// ADOT Lambda Layer runs a local OpenTelemetry Collector that accepts OTLP/HTTP on:
/// - localhost:4318 (HTTP/JSON and HTTP/protobuf)
/// - localhost:4317 (gRPC)
/// 
/// The ADOT layer is configured via environment variables:
/// - OTEL_EXPORTER_OTLP_ENDPOINT: Where ADOT forwards telemetry
/// - OTEL_EXPORTER_OTLP_HEADERS: Authentication headers ADOT uses
/// - AWS_LAMBDA_EXEC_WRAPPER: /opt/otel-instrument (enables ADOT)
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
		// Check for test override first (for integration tests with WireMock)
		var configEndpoint = configuration["OtlpProxy:Endpoint"];
		if (!string.IsNullOrEmpty(configEndpoint))
		{
			Endpoint = configEndpoint;
			return;
		}

		// Check if we're in Lambda with ADOT layer
		var execWrapper = Environment.GetEnvironmentVariable("AWS_LAMBDA_EXEC_WRAPPER");
		var isAdotEnabled = execWrapper?.Contains("otel-instrument") == true;

		if (isAdotEnabled)
		{
			// ADOT Lambda Layer runs collector on localhost:4318
			Endpoint = "http://localhost:4318";
		}
		else
		{
			// Fallback to configured endpoint for local development
			Endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
				?? "http://localhost:4318";
		}
	}
}
