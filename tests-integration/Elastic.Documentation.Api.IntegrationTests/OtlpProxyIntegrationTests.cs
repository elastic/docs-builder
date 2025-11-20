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

public class OtlpProxyIntegrationTests
{
	[Fact]
	public async Task OtlpProxyTracesEndpointForwardsToCorrectUrl()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();
		var capturedRequest = (HttpRequestMessage?)null;

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Invokes((HttpRequestMessage req, CancellationToken ct) => capturedRequest = req)
			.Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));

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
		var content = new StringContent(otlpPayload, Encoding.UTF8, "application/json");

		// Act
		var response = await client.PostAsync("/docs/_api/v1/o/t", content, TestContext.Current.CancellationToken);

		// Assert - verify the request was forwarded to the correct URL
		if (!response.IsSuccessStatusCode)
		{
			var errorBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			throw new Exception($"Test failed with {response.StatusCode}: {errorBody}");
		}

		response.StatusCode.Should().Be(HttpStatusCode.OK);
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri.Should().NotBeNull();
		capturedRequest.RequestUri!.ToString().Should().Be("http://localhost:4318/v1/traces");
		capturedRequest.Method.Should().Be(HttpMethod.Post);
		capturedRequest.Content.Should().NotBeNull();
		capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
	}

	[Fact]
	public async Task OtlpProxyLogsEndpointForwardsToCorrectUrl()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();
		var capturedRequest = (HttpRequestMessage?)null;

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Invokes((HttpRequestMessage req, CancellationToken ct) => capturedRequest = req)
			.Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));

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
		var content = new StringContent(otlpPayload, Encoding.UTF8, "application/json");

		// Act
		var response = await client.PostAsync("/docs/_api/v1/o/l", content, TestContext.Current.CancellationToken);

		// Assert - verify the enum ToStringFast() generates "logs" (lowercase)
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.ToString().Should().Be("http://localhost:4318/v1/logs");
	}

	[Fact]
	public async Task OtlpProxyMetricsEndpointForwardsToCorrectUrl()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();
		var capturedRequest = (HttpRequestMessage?)null;

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Invokes((HttpRequestMessage req, CancellationToken ct) => capturedRequest = req)
			.Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }));

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
		var content = new StringContent(otlpPayload, Encoding.UTF8, "application/json");

		// Act
		var response = await client.PostAsync("/docs/_api/v1/o/m", content, TestContext.Current.CancellationToken);

		// Assert - verify the enum ToStringFast() generates "metrics" (lowercase)
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		capturedRequest.Should().NotBeNull();
		capturedRequest!.RequestUri!.ToString().Should().Be("http://localhost:4318/v1/metrics");
	}

	[Fact]
	public async Task OtlpProxyReturnsCollectorErrorStatusCode()
	{
		// Arrange
		var mockHandler = A.Fake<HttpMessageHandler>();

		A.CallTo(mockHandler)
			.Where(call => call.Method.Name == "SendAsync")
			.WithReturnType<Task<HttpResponseMessage>>()
			.Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
			{
				Content = new StringContent("Service unavailable")
			}));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
			_ = services.AddHttpClient(AdotOtlpGateway.HttpClientName)
				.ConfigurePrimaryHttpMessageHandler(() => mockHandler);
		});

		var client = factory.CreateClient();
		var content = new StringContent("{}", Encoding.UTF8, "application/json");

		// Act
		var response = await client.PostAsync("/docs/_api/v1/o/t", content, TestContext.Current.CancellationToken);

		// Assert - verify error responses are properly forwarded
		response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
		var responseBody = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		responseBody.Should().Contain("Service unavailable");
	}

	[Fact]
	public async Task OtlpProxyInvalidSignalTypeReturns404()
	{
		// Arrange
		using var factory = new ApiWebApplicationFactory();
		using var client = factory.CreateClient();
		var content = new StringContent("{}", Encoding.UTF8, "application/json");

		// Act - use invalid signal type
		var response = await client.PostAsync("/docs/_api/v1/o/invalid", content, TestContext.Current.CancellationToken);

		// Assert - route doesn't exist
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
