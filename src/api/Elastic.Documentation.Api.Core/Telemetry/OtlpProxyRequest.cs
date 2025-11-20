// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// Request model for OTLP proxy endpoint.
/// Accepts raw OTLP payload from frontend and forwards to configured OTLP endpoint.
/// </summary>
public class OtlpProxyRequest
{
	/// <summary>
	/// The OTLP signal type: traces, logs, or metrics
	/// </summary>
	public required string SignalType { get; init; }
}

