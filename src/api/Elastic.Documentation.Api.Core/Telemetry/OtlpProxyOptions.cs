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
public class OtlpProxyOptions(IConfiguration configuration)
{
	/// <summary>
	/// OTLP endpoint URL for the local ADOT collector.
	/// Defaults to localhost:4318 when running in Lambda with ADOT layer.
	/// </summary>
	public string Endpoint { get; } = ResolveEndpoint(configuration);

	private static string ResolveEndpoint(IConfiguration configuration)
	{
		const string configKey = "OtlpProxy:Endpoint";
		const string envVarKey = "OTLP_PROXY_ENDPOINT";
		const string defaultEndpoint = "http://localhost:4318";

		// Priority 1: Explicit configuration (for tests or custom deployments)
		if (!string.IsNullOrEmpty(configuration[configKey]))
			return configuration[configKey]!;

		// Priority 2: Environment variable (ADOT Lambda Layer standard)
		if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVarKey)))
			return Environment.GetEnvironmentVariable(envVarKey)!;

		// Priority 3: Default (ADOT Lambda Layer collector)
		return defaultEndpoint;
	}
}
