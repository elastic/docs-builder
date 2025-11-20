// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Api.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Infrastructure.Adapters.Telemetry;

/// <summary>
/// Gateway that forwards OTLP telemetry to the ADOT Lambda Layer collector.
/// </summary>
public class AdotOtlpGateway(
	OtlpProxyOptions options,
	ILogger<AdotOtlpGateway> logger) : IOtlpGateway
{
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromSeconds(30)
	};

	/// <inheritdoc />
	public async Task<(int StatusCode, string? Content)> ForwardOtlp(
		string signalType,
		Stream requestBody,
		string contentType,
		Cancel ctx = default)
	{
		try
		{
			// Build the target URL: http://localhost:4318/v1/{signalType}
			var targetUrl = $"{options.Endpoint.TrimEnd('/')}/v1/{signalType}";

			logger.LogDebug("Forwarding OTLP {SignalType} to ADOT collector at {TargetUrl}", signalType, targetUrl);

			using var request = new HttpRequestMessage(HttpMethod.Post, targetUrl);

			// Forward the content
			request.Content = new StreamContent(requestBody);
			request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

			// No need to add authentication headers - ADOT layer handles auth to backend
			// Just forward the telemetry to the local collector

			// Forward to ADOT collector
			using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, ctx);
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

			return ((int)response.StatusCode, responseContent);
		}
		catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused") || ex.InnerException?.Message?.Contains("Connection refused") == true)
		{
			logger.LogError(ex, "Failed to connect to ADOT collector at {Endpoint}. Is ADOT Lambda Layer enabled?", options.Endpoint);
			return (503, "ADOT collector not available. Ensure AWS_LAMBDA_EXEC_WRAPPER=/opt/otel-instrument is set");
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error forwarding OTLP {SignalType}", signalType);
			return (500, $"Error forwarding OTLP: {ex.Message}");
		}
	}
}
