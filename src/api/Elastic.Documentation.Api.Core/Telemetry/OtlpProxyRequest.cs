// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using NetEscapades.EnumGenerators;

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// OTLP signal types supported by the proxy.
/// The Display names match the OTLP path segments (lowercase).
/// </summary>
[EnumExtensions]
public enum OtlpSignalType
{
	/// <summary>
	/// Distributed traces - maps to /v1/traces
	/// </summary>
	[Display(Name = "traces")]
	Traces,

	/// <summary>
	/// Log records - maps to /v1/logs
	/// </summary>
	[Display(Name = "logs")]
	Logs,

	/// <summary>
	/// Metrics data - maps to /v1/metrics
	/// </summary>
	[Display(Name = "metrics")]
	Metrics
}

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
