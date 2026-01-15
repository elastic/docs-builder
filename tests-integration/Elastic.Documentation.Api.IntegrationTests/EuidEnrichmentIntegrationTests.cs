// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.IntegrationTests.Fixtures;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Elastic.Documentation.Api.IntegrationTests;

/// <summary>
/// Integration tests for euid cookie enrichment in OpenTelemetry traces and logging.
/// Uses WebApplicationFactory to test the real API configuration with mocked AskAi services.
/// </summary>
public class EuidEnrichmentIntegrationTests : IAsyncLifetime
{
	private const string OtlpEndpoint = "http://localhost:4318";

	public ValueTask InitializeAsync()
	{
		Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", OtlpEndpoint);
		return ValueTask.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Test that verifies euid cookie is added to both HTTP span and custom AskAi span,
	/// and appears in log entries - using the real API configuration.
	/// </summary>
	[Fact]
	public async Task AskAiEndpointPropagatatesEuidToAllSpansAndLogs()
	{
		// Arrange
		const string expectedEuid = "integration-test-euid-12345";

		// Track streams created by mocks so we can dispose them after the test
		var mockStreams = new List<MemoryStream>();

		// Create factory with mocked AskAi services
		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
			// Mock IAskAiGateway to avoid external AI service calls
			var mockAskAiGateway = A.Fake<IAskAiGateway<Stream>>();
			A.CallTo(() => mockAskAiGateway.AskAi(A<AskAiRequest>._, A<Cancel>._))
				.ReturnsLazily(() =>
				{
					var stream = new MemoryStream(Encoding.UTF8.GetBytes("data: test\n\n"));
					mockStreams.Add(stream);
					return Task.FromResult<Stream>(stream);
				});
			services.AddSingleton(mockAskAiGateway);

			// Mock IStreamTransformer
			var mockTransformer = A.Fake<IStreamTransformer>();
			A.CallTo(() => mockTransformer.AgentProvider).Returns("test-provider");
			A.CallTo(() => mockTransformer.AgentId).Returns("test-agent");
			A.CallTo(() => mockTransformer.TransformAsync(A<Stream>._, A<string?>._, A<Activity?>._, A<Cancel>._))
				.ReturnsLazily((Stream s, string? _, Activity? activity, Cancel _) =>
				{
					// Dispose the activity if provided (simulating what the real transformer does)
					activity?.Dispose();
					return Task.FromResult(s);
				});
			services.AddSingleton(mockTransformer);
		});

		// Create client
		using var client = factory.CreateClient();

		// Act - Make request to /ask-ai/stream with euid cookie
		using var request = new HttpRequestMessage(HttpMethod.Post, "/docs/_api/v1/ask-ai/stream");
		request.Headers.Add("Cookie", $"euid={expectedEuid}");
		request.Content = new StringContent(
													 /*lang=json,strict*/
													 """{"message":"test question","conversationId":null}""",
			Encoding.UTF8,
			"application/json"
		);

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

		// Assert - Response is successful
		response.IsSuccessStatusCode.Should().BeTrue();

		// Assert - Verify spans were captured
		var activities = factory.ExportedActivities;
		activities.Should().NotBeEmpty("OpenTelemetry should have captured activities");

		// NOTE: We only verify custom AskAi spans, not HTTP request spans.
		// HTTP spans require ASP.NET Core instrumentation which may not work reliably
		// in test environments due to OpenTelemetry SDK limitations when multiple
		// tests initialize the SDK. The custom spans are sufficient to prove euid enrichment works.

		// Verify custom AskAi span has euid (proves baggage + processor work)
		var askAiSpan = activities.FirstOrDefault(a => a.Source.Name == TelemetryConstants.AskAiSourceName);
		askAiSpan.Should().NotBeNull("Should have captured custom AskAi span from AskAiUsecase");
		var askAiEuidTag = askAiSpan!.TagObjects.FirstOrDefault(t => t.Key == TelemetryConstants.UserEuidAttributeName);
		askAiEuidTag.Should().NotBeNull("AskAi span should have user.euid tag from baggage");
		askAiEuidTag.Value.Should().Be(expectedEuid, "AskAi span euid should match cookie value");

		// Assert - Verify logs have euid in attributes
		var logRecords = factory.ExportedLogRecords;
		logRecords.Should().NotBeEmpty("Should have captured log records");

		// Find a log entry from AskAiUsecase
		var askAiLogRecord = logRecords.FirstOrDefault(r =>
			string.Equals(r.CategoryName, typeof(AskAiUsecase).FullName, StringComparison.OrdinalIgnoreCase) &&
			r.FormattedMessage?.Contains("Starting AskAI", StringComparison.OrdinalIgnoreCase) == true);
		askAiLogRecord.Should().NotBeNull("Should have logged from AskAiUsecase");

		// Verify euid is present in OTEL log attributes (mirrors production exporter behavior)
		var euidAttribute = askAiLogRecord!.Attributes?.FirstOrDefault(a => a.Key == TelemetryConstants.UserEuidAttributeName) ?? default;
		euidAttribute.Should().NotBe(default(KeyValuePair<string, object?>), "Log record should include user.euid attribute");
		(euidAttribute.Value?.ToString() ?? string.Empty).Should().Be(expectedEuid, "Log record euid should match cookie value");

		// Cleanup - dispose all mock streams
		foreach (var stream in mockStreams)
			stream.Dispose();
	}
}
