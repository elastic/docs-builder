// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net.Sockets;
using Elastic.Documentation.Api.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Telemetry;

/// <summary>
/// Gateway that forwards OTLP telemetry to the ADOT Lambda Layer collector.
/// </summary>
public class AdotOtlpGateway(
	IHttpClientFactory httpClientFactory,
	OtlpProxyOptions options,
	ILogger<AdotOtlpGateway> logger) : IOtlpGateway
{
	public const string HttpClientName = "OtlpProxy";
	private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientName);

	/// <inheritdoc />
	public async Task<OtlpForwardResult> ForwardOtlp(
		OtlpSignalType signalType,
		Stream requestBody,
		string contentType,
		Cancel ctx = default)
	{
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
			logger.LogError(ex, "Error forwarding OTLP {SignalType}: {ErrorMessage}", signalType, message);
			return new OtlpForwardResult
			{
				StatusCode = statusCode,
				Content = message
			};
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

			// Other HTTP/network errors - bad gateway
			HttpRequestException
				=> (502, "Failed to communicate with telemetry collector"),

			// Unknown errors
			_ => (500, $"Internal error: {ex.Message}")
		};
}
