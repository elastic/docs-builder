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
	private static readonly Counter<int> ForwardCounter =
		Meter.CreateCounter<int>("otlp.proxy.forward",
			description: "OTLP batches processed, by outcome (forwarded/stale_drop/collector_unavailable/timeout/error) and signal_type");
	private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientName);

	/// <inheritdoc />
	public async Task<OtlpForwardResult> ForwardOtlp(
		OtlpSignalType signalType,
		Stream requestBody,
		string contentType,
		Cancel ctx = default)
	{
		using var activity = ActivitySource.StartActivity("forward otlp", ActivityKind.Client);
		var signalTag = signalType.ToStringFast(true);
		_ = activity?.SetTag("otlp.signal_type", signalTag);

		try
		{
			var targetUrl = $"{options.Endpoint.TrimEnd('/')}/v1/{signalTag}";
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
				_ = activity?.SetStatus(ActivityStatusCode.Error, $"Collector returned {(int)response.StatusCode}");
				ForwardCounter.Add(1,
					new KeyValuePair<string, object?>("outcome", "error"),
					new KeyValuePair<string, object?>("signal_type", signalTag));
			}
			else
			{
				logger.LogDebug("Successfully forwarded OTLP {SignalType} to ADOT collector", signalType);
				_ = activity?.SetStatus(ActivityStatusCode.Ok);
				ForwardCounter.Add(1,
					new KeyValuePair<string, object?>("outcome", "forwarded"),
					new KeyValuePair<string, object?>("signal_type", signalTag));
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
				// Stale connection: streaming body cannot be replayed under the zero-copy constraint,
				// so this batch is dropped best-effort. Rare after PooledConnectionIdleTimeout tuning.
				// Leave activity status unset (not Error) — this is an expected transient condition.
				_ = activity?.SetTag("otlp.proxy.outcome", "stale_drop");
				logger.LogDebug("Dropped OTLP {SignalType} batch on stale connection; collector will reconnect", signalType);
			}
			else
			{
				_ = activity?.SetStatus(ActivityStatusCode.Error, message);
				logger.LogError(ex, "Error forwarding OTLP {SignalType}: {ErrorMessage}", signalType, message);
			}
			ForwardCounter.Add(1,
				new KeyValuePair<string, object?>("outcome", OutcomeTag(statusCode)),
				new KeyValuePair<string, object?>("signal_type", signalTag));
			return new OtlpForwardResult { StatusCode = statusCode, Content = message };
		}
	}

	private static string OutcomeTag(int statusCode) => statusCode switch
	{
		204 => "stale_drop",
		503 => "collector_unavailable",
		504 => "timeout",
		_ => "error"
	};

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

			// Stale connection reset — streaming body cannot be replayed (zero-copy proxy constraint),
			// so the batch is dropped best-effort. Return 204 so the browser exporter doesn't treat it
			// as a retryable 502. Rare after PooledConnectionIdleTimeout tuning.
			HttpRequestException { InnerException: SocketException { SocketErrorCode: SocketError.ConnectionReset } }
				=> (204, string.Empty),

			HttpRequestException { InnerException: IOException }
				=> (204, string.Empty),

			// Other HTTP/network errors - bad gateway
			HttpRequestException
				=> (502, "Failed to communicate with telemetry collector"),

			// Unknown errors
			_ => (500, $"Internal error: {ex.Message}")
		};
}
