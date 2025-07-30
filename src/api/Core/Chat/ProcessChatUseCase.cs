// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using System.Text.Json;
using Core.Interfaces;
using Core.Serialization;

namespace Core.Chat;

public class ProcessChatUseCase(HttpClient httpClient, IGcpTokenGenerator tokenGenerator)
{
	private readonly HttpClient _httpClient = httpClient;
	private readonly IGcpTokenGenerator _tokenGenerator = tokenGenerator;

	public async Task ExecuteAsync(ChatRequest request, Stream responseStream, CancellationToken cancellationToken = default)
	{
		// TODO: Add use case specific logic
		// 1. Validate business rules
		// 2. Log analytics (question tracking)
		// 3. Apply rate limiting if needed
		// 4. Execute chat processing
		// 5. Handle post-processing (caching, etc.)

		try
		{
			// Serialize the ChatRequest to JSON (using AOT-compatible serialization)
			var requestBody = JsonSerializer.Serialize(request, ApiJsonContext.Default.ChatRequest);

			// Load GCP service account credentials
			var serviceAccountKeyPath = Environment.GetEnvironmentVariable("GCP_SERVICE_ACCOUNT_KEY_PATH")
				?? "service-account-key.json";

			if (!File.Exists(serviceAccountKeyPath))
			{
				await WriteErrorToStream(responseStream, "GCP credentials not configured", cancellationToken);
				return;
			}

			// Get GCP function URL
			var gcpFunctionUrl = Environment.GetEnvironmentVariable("GCP_CHAT_FUNCTION_URL");
			if (string.IsNullOrEmpty(gcpFunctionUrl))
			{
				await WriteErrorToStream(responseStream, "GCP function URL not configured", cancellationToken);
				return;
			}

			// Extract base URL for ID token audience (service URL without path)
			var functionUri = new Uri(gcpFunctionUrl);
			var audienceUrl = $"{functionUri.Scheme}://{functionUri.Host}";

			// Generate ID token
			var idToken = await _tokenGenerator.GenerateIdTokenAsync(serviceAccountKeyPath, audienceUrl, cancellationToken);

			// Create HTTP request
			var httpRequest = new HttpRequestMessage(HttpMethod.Post, gcpFunctionUrl)
			{
				Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
			};

			// Add authorization header with ID token
			httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);

			// Add additional headers that GCP functions commonly require
			httpRequest.Headers.Add("User-Agent", "docs-api/1.0");
			httpRequest.Headers.Add("Accept", "text/event-stream, application/json");

			// Ensure Content-Type is set properly for the request body
			httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

			// Send request and get response
			var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
				Console.WriteLine($"[CHAT USE CASE] Error response: {errorContent}");
				await WriteErrorToStream(responseStream, errorContent, cancellationToken);
				return;
			}

			// Stream the response directly
			await using var gcpResponseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
			await gcpResponseStream.CopyToAsync(responseStream, cancellationToken);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[CHAT USE CASE] Exception: {ex.Message}");
			await WriteErrorToStream(responseStream, $"Error processing chat request: {ex.Message}", cancellationToken);
		}
	}

	private static async Task WriteErrorToStream(Stream responseStream, string errorMessage, CancellationToken cancellationToken)
	{
		// Write a simple error message as text
		var errorBytes = Encoding.UTF8.GetBytes(errorMessage);
		await responseStream.WriteAsync(errorBytes, cancellationToken);
	}
}
