// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text;
using Elastic.Documentation.Api.Core.Telemetry;
using Elastic.Documentation.Api.IntegrationTests.Fixtures;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Elastic.Documentation.Api.IntegrationTests;

public class OtlpProxyIntegrationTests
{
	[Fact]
	public async Task OtlpProxyTracesEndpointReturnsSuccess()
	{
		// Arrange
		var mockGateway = A.Fake<IOtlpGateway>();
		A.CallTo(() => mockGateway.ForwardOtlp(
			A<string>._,
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.Returns(Task.FromResult<(int StatusCode, string? Content)>((200, "{}")));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
			services.Replace(mockGateway));
		using var client = factory.CreateClient();

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

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify gateway was called
		A.CallTo(() => mockGateway.ForwardOtlp(
			"traces",
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task OtlpProxyLogsEndpointReturnsSuccess()
	{
		// Arrange
		var mockGateway = A.Fake<IOtlpGateway>();
		A.CallTo(() => mockGateway.ForwardOtlp(
			A<string>._,
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.Returns(Task.FromResult<(int StatusCode, string? Content)>((200, "{}")));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
			services.Replace(mockGateway));
		using var client = factory.CreateClient();

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

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify gateway was called
		A.CallTo(() => mockGateway.ForwardOtlp(
			"logs",
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task OtlpProxyMetricsEndpointReturnsSuccess()
	{
		// Arrange
		var mockGateway = A.Fake<IOtlpGateway>();
		A.CallTo(() => mockGateway.ForwardOtlp(
			A<string>._,
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.Returns(Task.FromResult<(int StatusCode, string? Content)>((200, "{}")));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
			services.Replace(mockGateway));
		using var client = factory.CreateClient();

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

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		// Verify gateway was called
		A.CallTo(() => mockGateway.ForwardOtlp(
			"metrics",
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.MustHaveHappenedOnceExactly();
	}

	[Fact]
	public async Task OtlpProxyReturnsGatewayErrorStatusCode()
	{
		// Arrange
		var mockGateway = A.Fake<IOtlpGateway>();
		A.CallTo(() => mockGateway.ForwardOtlp(
			A<string>._,
			A<Stream>._,
			A<string>._,
			A<Cancel>._))
			.Returns(Task.FromResult<(int StatusCode, string? Content)>((503, "Service unavailable")));

		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
			services.Replace(mockGateway));
		using var client = factory.CreateClient();

		var content = new StringContent("{}", Encoding.UTF8, "application/json");

		// Act
		var response = await client.PostAsync("/docs/_api/v1/o/t", content, TestContext.Current.CancellationToken);

		// Assert
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
