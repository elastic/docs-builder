// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using Elastic.Documentation.Api.Infrastructure.Adapters.Telemetry;
using Elastic.Documentation.Api.IntegrationTests.Fixtures;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elastic.Documentation.Api.IntegrationTests;

public class OtlpProxyIntegrationTests : IAsyncLifetime
{
	private const string OtlpEndpoint = "http://localhost:4318";

	public ValueTask InitializeAsync()
	{
		Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", OtlpEndpoint);
		return ValueTask.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
		return ValueTask.CompletedTask;
	}

	[Fact]
	public async Task OtlpProxyTracesEndpointForwardsToCorrectUrl()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();
		var capturedRequest = (HttpRequestMessage?)null;

		// Create mock response (will be disposed by HttpClient)
		var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("{}")
		};

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Invokes((HttpRequestMessage req, CancellationToken ct) => capturedRequest = req)
			.Returns(Task.FromResult(mockResponse));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
			// Replace the named HttpClient with our mock
			_ = services.AddHttpClient(AdotOtlpGateway.HttpClientName)
				.ConfigurePrimaryHttpMessageHandler(() => mockHandler);
		});

		var client = factory.CreateClient();
		var otlpPayload = /*lang=json,strict*/ """
		{
			"resourceSpans": [{
				"scopeSpans": [{
					"spans": [{
						"traceId": "0123456789abcdef0123456789abcdef",
						"spanId": "0123456789abcdef",
						"name": "test-span"
					}]
				}]
			}]
		}
		""";

		using var content = new StringContent(otlpPayload, Encoding.UTF8, "application/json");

		// Act
		using var response = await client.PostAsync("/docs/_api/v1/o/t", content, TestContext.Current.CancellationToken);

		// Assert - verify the request was forwarded to the correct URL
		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			throw new Exception($"Test failed with {response.StatusCode}: {errorBody}");
		}

		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri.Should().NotBeNull();
		capturedRequest.RequestUri!.ToString().Should().Be($"{OtlpEndpoint}/v1/traces");
		capturedRequest.Method.Should().Be(HttpMethod.Post);
		capturedRequest.Content.Should().NotBeNull();
		capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");

		// Cleanup mock response
		mockResponse.Dispose();
	}

	[Fact]
	public async Task OtlpProxyLogsEndpointForwardsToCorrectUrl()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();
		var capturedRequest = (HttpRequestMessage?)null;

		// Create mock response (will be disposed by HttpClient)
		var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("{}")
		};

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Invokes((HttpRequestMessage req, CancellationToken ct) => capturedRequest = req)
			.Returns(Task.FromResult(mockResponse));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
			_ = services.AddHttpClient(AdotOtlpGateway.HttpClientName)
				.ConfigurePrimaryHttpMessageHandler(() => mockHandler);
		});

		var client = factory.CreateClient();
		var otlpPayload = /*lang=json,strict*/ """
		{
			"resourceLogs": [{
				"scopeLogs": [{
					"logRecords": [{
						"timeUnixNano": "1672531200000000000",
						"severityNumber": 9,
						"severityText": "INFO",
						"body": {
							"stringValue": "Test log"
						}
					}]
				}]
			}]
		}
		""";

		using var content = new StringContent(otlpPayload, Encoding.UTF8, "application/json");

		// Act
		using var response = await client.PostAsync("/docs/_api/v1/o/l", content, TestContext.Current.CancellationToken);

		// Assert - verify the enum ToStringFast() generates "logs" (lowercase)
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.ToString().Should().Be($"{OtlpEndpoint}/v1/logs");

		// Cleanup mock response
		mockResponse.Dispose();
	}

	[Fact]
	public async Task OtlpProxyMetricsEndpointForwardsToCorrectUrl()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();
		var capturedRequest = (HttpRequestMessage?)null;

		// Create mock response (will be disposed by HttpClient)
		var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
		{
			Content = new StringContent("{}")
		};

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Invokes((HttpRequestMessage req, CancellationToken ct) => capturedRequest = req)
			.Returns(Task.FromResult(mockResponse));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
			_ = services.AddHttpClient(AdotOtlpGateway.HttpClientName)
				.ConfigurePrimaryHttpMessageHandler(() => mockHandler);
		});

		var client = factory.CreateClient();
		var otlpPayload = /*lang=json,strict*/ """
		{
			"resourceMetrics": [{
				"scopeMetrics": [{
					"metrics": [{
						"name": "test_metric",
						"unit": "1"
					}]
				}]
			}]
		}
		""";

		using var content = new StringContent(otlpPayload, Encoding.UTF8, "application/json");

		// Act
		using var response = await client.PostAsync("/docs/_api/v1/o/m", content, TestContext.Current.CancellationToken);

		// Assert - verify the enum ToStringFast() generates "metrics" (lowercase)
		response.StatusCode.Should().Be(HttpStatusCode.NoContent);
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.ToString().Should().Be($"{OtlpEndpoint}/v1/metrics");

		// Cleanup mock response
		mockResponse.Dispose();
	}

	[Fact]
	public async Task OtlpProxyReturnsCollectorErrorStatusCode()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();

		// Create mock response (will be disposed by HttpClient)
		var mockResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
		{
			Content = new StringContent("Service unavailable")
		};

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Returns(Task.FromResult(mockResponse));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
#pragma warning disable EXTEXP0001 // Experimental API - needed for test to bypass resilience handlers
			_ = services.AddHttpClient(AdotOtlpGateway.HttpClientName)
				.ConfigurePrimaryHttpMessageHandler(() => mockHandler)
				.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
		});

		var client = factory.CreateClient();
		using var content = new StringContent("{}", Encoding.UTF8, "application/json");

		// Act
		using var response = await client.PostAsync("/docs/_api/v1/o/t", content, TestContext.Current.CancellationToken);

		var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		// Assert - verify error responses are properly forwarded
		response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, "{0}", responseContent);


		// Cleanup mock response
		mockResponse.Dispose();
	}

	[Fact]
	public async Task OtlpProxyInvalidSignalTypeReturns404()
	{
		// Arrange
		using var factory = new ApiWebApplicationFactory();
		using var client = factory.CreateClient();
		using var content = new StringContent("{}", Encoding.UTF8, "application/json");

		// Act - use invalid signal type
		using var response = await client.PostAsync("/docs/_api/v1/o/invalid", content, TestContext.Current.CancellationToken);

		// Assert - route doesn't exist
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
