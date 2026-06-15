// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Telemetry;

/// <summary>
/// Service that forwards OTLP telemetry to the ADOT Lambda Layer collector.
/// </summary>
public class AdotOtlpService(
	IHttpClientFactory httpClientFactory,
	OtlpProxyOptions options,
	ILogger<AdotOtlpService> logger) : IOtlpService
{
	public const string HttpClientName = "OtlpProxy";
	private static readonly ActivitySource ActivitySource = new(TelemetryConstants.OtlpProxySourceName);
	private static readonly Meter Meter = new(TelemetryConstants.OtlpProxySourceName);
	internal static readonly Counter<int> StaleConnectionDrops =
		Meter.CreateCounter<int>("otlp.proxy.stale_connection.dropped",
			description: "OTLP batches silently dropped due to a stale pooled connection to the ADOT collector");
	private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientName);

	/// <inheritdoc />
	public async Task<OtlpForwardResult> ForwardOtlp(
		OtlpSignalType signalType,
		Stream requestBody,
		string contentType,
		Cancel ctx = default)
	{
		using var activity = ActivitySource.StartActivity("forward otlp", ActivityKind.Client);

		try
		{
			var targetUrl = $"{options.Endpoint.TrimEnd('/')}/v1/{signalType.ToStringFast(true)}";
			logger.LogDebug("Forwarding OTLP {SignalType} to ADOT collector at {TargetUrl}", signalType, targetUrl);

			using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);
			request.Content = new StreamContent(requestBody);
			_ = request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);

			using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ctx);
			var responseContent = response.Content.Headers.ContentLength > 0
				? await response.Content.ReadAsStringAsync(ctx)
				: string.Empty;

			if (!response.IsSuccessStatusCode)
			{
				logger.LogError("OTLP forward to ADOT failed with status {StatusCode}: {Content}",
					response.StatusCode, responseContent);
			}
			else
			{
				logger.LogDebug("Successfully forwarded OTLP {SignalType} to ADOT collector", signalType);
			}

			return new OtlpForwardResult
			{
				StatusCode = (int)response.StatusCode,
				Content = responseContent
			};
		}
		catch (Exception ex)
		{
			var (statusCode, message) = MapExceptionToStatusCode(ex);
			if (statusCode == 204)
			{
				StaleConnectionDrops.Add(1);
				logger.LogDebug("Dropped OTLP {SignalType} batch on stale connection; collector will reconnect", signalType);
			}
			else
				logger.LogError(ex, "Error forwarding OTLP {SignalType}: {ErrorMessage}", signalType, message);
			return new OtlpForwardResult { StatusCode = statusCode, Content = message };
		}
	}

	private static (int StatusCode, string Message) MapExceptionToStatusCode(Exception ex) =>
		ex switch
		{
			// Connection refused - downstream service not available
			HttpRequestException { InnerException: SocketException { SocketErrorCode: SocketError.ConnectionRefused } }
				=> (503, "Telemetry collector unavailable"),

			// Timeout - gateway timeout
			HttpRequestException { InnerException: SocketException { SocketErrorCode: SocketError.TimedOut } }
				=> (504, "Telemetry collector timeout"),

			TaskCanceledException or OperationCanceledException
				=> (504, "Request to telemetry collector timed out"),

			// Stale pooled connection — SocketsHttpHandler sets AllowRetry=false for non-seekable
			// StreamContent, so it throws rather than retrying. OTLP is best-effort; return 204
			// so the browser exporter doesn't treat this as a retryable 502.
			HttpRequestException { InnerException: IOException }
				=> (204, string.Empty),

			// Other HTTP/network errors - bad gateway
			HttpRequestException
				=> (502, "Failed to communicate with telemetry collector"),

			// Unknown errors
			_ => (500, $"Internal error: {ex.Message}")
		};
}
