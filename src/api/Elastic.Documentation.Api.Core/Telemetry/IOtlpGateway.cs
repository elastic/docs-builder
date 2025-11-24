// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// Gateway for forwarding OTLP telemetry to a collector.
/// </summary>
public interface IOtlpGateway
{
	/// <summary>
	/// Forwards OTLP telemetry data to the collector.
	/// </summary>
	/// <param name="signalType">The OTLP signal type (traces, logs, or metrics)</param>
	/// <param name="requestBody">The raw OTLP payload stream</param>
	/// <param name="contentType">Content-Type of the payload</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>HTTP status code and response content</returns>
	Task<(int StatusCode, string? Content)> ForwardOtlp(
		OtlpSignalType signalType,
		Stream requestBody,
		string contentType,
		Cancel ctx = default);
}
