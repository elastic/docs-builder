// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;

namespace Elastic.Documentation.Api.Core.Telemetry;

/// <summary>
/// Proxies OTLP telemetry from the frontend to the local ADOT Lambda Layer collector.
/// The ADOT layer handles authentication and forwarding to the backend.
/// </summary>
public class OtlpProxyUsecase(IOtlpGateway gateway)
{
	private static readonly ActivitySource ActivitySource = new(TelemetryConstants.OtlpProxySourceName);

	/// <summary>
	/// Proxies OTLP data from the frontend to the local ADOT collector.
	/// </summary>
	/// <param name="signalType">The OTLP signal type (traces, logs, or metrics)</param>
	/// <param name="requestBody">The raw OTLP payload (JSON or protobuf)</param>
	/// <param name="contentType">Content-Type header from the original request</param>
	/// <param name="ctx">Cancellation token</param>
	/// <returns>HTTP status code and response content</returns>
	public async Task<(int StatusCode, string? Content)> ProxyOtlp(
		OtlpSignalType signalType,
		Stream requestBody,
		string contentType,
		Cancel ctx = default)
	{
		using var activity = ActivitySource.StartActivity("ProxyOtlp", ActivityKind.Client);

		// Forward to gateway
		return await gateway.ForwardOtlp(signalType, requestBody, contentType, ctx);
	}
}
