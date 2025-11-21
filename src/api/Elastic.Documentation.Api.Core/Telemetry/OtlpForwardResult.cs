// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// Result of forwarding OTLP telemetry to a collector.
/// </summary>
public record OtlpForwardResult
{
	/// <summary>
	/// HTTP status code from the collector response.
	/// </summary>
	public required int StatusCode { get; init; }

	/// <summary>
	/// Response content from the collector, if any.
	/// </summary>
	public string? Content { get; init; }

	/// <summary>
	/// Whether the forward operation was successful (2xx status code).
	/// </summary>
	public bool IsSuccess => StatusCode is >= 200 and < 300;
}
