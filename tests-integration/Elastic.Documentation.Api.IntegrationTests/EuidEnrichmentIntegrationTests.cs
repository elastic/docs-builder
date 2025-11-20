// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using Elastic.Documentation.Api.Core;
using Elastic.Documentation.Api.Core.AskAi;
using Elastic.Documentation.Api.IntegrationTests.Fixtures;
using FluentAssertions;

namespace Elastic.Documentation.Api.IntegrationTests;

/// <summary>
/// Integration tests for euid cookie enrichment in OpenTelemetry traces and logging.
/// Uses WebApplicationFactory to test the real API configuration with mocked services.
/// </summary>
public class EuidEnrichmentIntegrationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
	private readonly ApiWebApplicationFactory _factory = factory;

	/// <summary>
	/// Test that verifies euid cookie is added to both HTTP span and custom AskAi span,
	/// and appears in log entries - using the real API configuration.
	/// </summary>
	[Fact]
	public async Task AskAiEndpointPropagatatesEuidToAllSpansAndLogs()
	{
		// Arrange
		const string expectedEuid = "integration-test-euid-12345";

		// Create client
		using var client = _factory.CreateClient();

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
		var activities = _factory.ExportedActivities;
		activities.Should().NotBeEmpty("OpenTelemetry should have captured activities");

		// Verify HTTP span has euid
		var httpSpan = activities.FirstOrDefault(a =>
			a.DisplayName.Contains("POST") && a.DisplayName.Contains("ask-ai"));
		httpSpan.Should().NotBeNull("Should have captured HTTP request span");
		var httpEuidTag = httpSpan!.TagObjects.FirstOrDefault(t => t.Key == TelemetryConstants.UserEuidAttributeName);
		httpEuidTag.Should().NotBeNull("HTTP span should have user.euid tag");
		httpEuidTag.Value.Should().Be(expectedEuid, "HTTP span euid should match cookie value");

		// Verify custom AskAi span has euid (proves baggage + processor work)
		var askAiSpan = activities.FirstOrDefault(a => a.Source.Name == TelemetryConstants.AskAiSourceName);
		askAiSpan.Should().NotBeNull("Should have captured custom AskAi span from AskAiUsecase");
		var askAiEuidTag = askAiSpan!.TagObjects.FirstOrDefault(t => t.Key == TelemetryConstants.UserEuidAttributeName);
		askAiEuidTag.Should().NotBeNull("AskAi span should have user.euid tag from baggage");
		askAiEuidTag.Value.Should().Be(expectedEuid, "AskAi span euid should match cookie value");

		// Assert - Verify logs have euid in attributes
		var logRecords = _factory.ExportedLogRecords;
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
	}
}
