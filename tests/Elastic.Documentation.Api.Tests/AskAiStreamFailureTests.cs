// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.AskAi;
using Elastic.Documentation.Api.Tests.Fixtures;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Elastic.Documentation.Api.Tests;

/// <summary>
/// Until the stream transformer takes the chat activity, the handler owns it. A failure before that point must
/// still end the activity, with an error status, or the span never finishes and is never exported.
/// </summary>
// Every factory listens to every Ask AI span in the process; tests that read those spans run one at a time.
[NotInParallel("ask-ai-spans")]
public class AskAiStreamFailureTests : IAsyncInitializer, IAsyncDisposable
{
	// The API exports spans only when an OTLP endpoint is configured, as in EuidEnrichmentTests.
	public Task InitializeAsync()
	{
		Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318");
		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);
		Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
		return ValueTask.CompletedTask;
	}

	[Test]
	[Arguments(true)]
	[Arguments(false)]
	public async Task Stream_FailureBeforeTheTransformerOwnsTheActivity_EndsItWithAnError(bool askAiFails)
	{
		// Every factory's listener sees every Ask AI span in the process, so find this test's span by its failure.
		var failure = $"{(askAiFails ? "provider down" : "transform failed")} {Guid.NewGuid()}";
		using var factory = ApiWebApplicationFactory.WithMockedServices(services =>
		{
			var askAi = A.Fake<IAskAiService>();
			if (askAiFails)
				_ = A.CallTo(() => askAi.AskAi(A<AskAiRequest>._, A<Cancel>._)).ThrowsAsync(new InvalidOperationException(failure));
			else
				_ = A.CallTo(() => askAi.AskAi(A<AskAiRequest>._, A<Cancel>._)).Returns(
					new AskAiGatewayResponse(new MemoryStream(), GeneratedConversationId: Guid.NewGuid())
				);
			_ = services.AddSingleton(askAi);

			var transformer = A.Fake<IStreamTransformer>();
			_ = A.CallTo(() => transformer.AgentProvider).Returns("test-provider");
			_ = A.CallTo(() => transformer.AgentId).Returns("test-agent");
			_ = A.CallTo(() => transformer.TransformAsync(A<Stream>._, A<Guid?>._, A<Activity?>._, A<Cancel>._)).ThrowsAsync(
				new InvalidOperationException(failure)
			);
			_ = services.AddSingleton(transformer);
		});
		using var client = factory.CreateClient();

		using var response = await client.PostAsync(
			"/docs/_api/v1/ask-ai/stream",
			new StringContent("""{"message":"test question","conversationId":null}""", Encoding.UTF8, "application/json"),
			TestContext.Current!.Execution.CancellationToken
		);

		response.IsSuccessStatusCode.Should().BeFalse();
		// Exported means ended: an activity that is never stopped never reaches the exporter.
		factory
			.ExportedActivities
			.Where(a => a.Source.Name == TelemetryConstants.AskAiSourceName && a.StatusDescription == failure)
			.Should()
			.ContainSingle()
			.Which
			.Status
			.Should()
			.Be(ActivityStatusCode.Error);
	}
}
