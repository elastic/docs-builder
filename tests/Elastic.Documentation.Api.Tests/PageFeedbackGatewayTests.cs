// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.PageFeedback;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Documentation.PageFeedback;
using Elastic.Ingest.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.VirtualizedCluster;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Tests;

public class PageFeedbackGatewayTests
{
	[Test]
	public async Task UpsertFeedbackAsync_AllItemsPersisted_ReturnsTrue()
	{
		var transport = CreateTransport(201);
		var index = CreateIndex();
		using var channel = CreateChannel(transport, index);
		var gateway = new ElasticsearchPageFeedbackGateway(channel, NullLogger<ElasticsearchPageFeedbackGateway>.Instance);

		var result = await gateway.UpsertFeedbackAsync(CreateRecord(), TestContext.Current!.Execution.CancellationToken);

		result.Should().BeTrue();
	}

	[Test]
	public async Task UpsertFeedbackAsync_ItemRejected_ReturnsFalse()
	{
		var transport = CreateTransport(400);
		var index = CreateIndex();
		using var channel = CreateChannel(transport, index);
		var gateway = new ElasticsearchPageFeedbackGateway(channel, NullLogger<ElasticsearchPageFeedbackGateway>.Instance);

		var result = await gateway.UpsertFeedbackAsync(CreateRecord(), TestContext.Current!.Execution.CancellationToken);

		result.Should().BeFalse();
	}

	[Test]
	public async Task UpsertFeedbackAsync_ValidRecord_SendsDocumentWithoutEuid()
	{
		var requests = new List<(string Path, string Body)>();
		var transport = CreateTransport(201, requests);
		var index = CreateIndex();
		using var channel = CreateChannel(transport, index);
		var gateway = new ElasticsearchPageFeedbackGateway(channel, NullLogger<ElasticsearchPageFeedbackGateway>.Instance);

		_ = await gateway.UpsertFeedbackAsync(CreateRecord(), TestContext.Current!.Execution.CancellationToken);

		var (_, body) = requests.Should().ContainSingle().Subject;
		body.Should().Contain("page_url").And.NotContainEquivalentOf("euid");
	}

	[Test]
	public async Task UpsertFeedbackAsync_ValidRecord_IndexesByFeedbackIdIntoTheEnvironmentIndex()
	{
		var requests = new List<(string Path, string Body)>();
		var transport = CreateTransport(201, requests);
		var index = CreateIndex();
		using var channel = CreateChannel(transport, index);
		var gateway = new ElasticsearchPageFeedbackGateway(channel, NullLogger<ElasticsearchPageFeedbackGateway>.Instance);

		_ = await gateway.UpsertFeedbackAsync(CreateRecord(), TestContext.Current!.Execution.CancellationToken);

		var (path, body) = requests.Should().ContainSingle().Subject;
		path.Should().Be("/page-feedback-v1-dev/_bulk");
		body.Split('\n')[0].Should().Be("""{"index":{"_id":"00000000-0000-4000-8000-000000000001"}}""");
	}

	private static ITransport CreateTransport(int itemStatus, List<(string Path, string Body)>? requests = null) =>
		Virtual
			.Elasticsearch
			.Bootstrap(1)
			.Ping(call => call.SucceedAlways())
			.ClientCalls(
				call => call
					.OnPath("_bulk")
					.SucceedAlways()
					.ReturnResponse(new
					{
						errors = itemStatus is < 200 or > 299,
						items = new[]
						{
							new
							{
								index = new
								{
									_index = "page-feedback-v1-dev",
									_id = "00000000-0000-4000-8000-000000000001",
									status = itemStatus
								}
							}
						}
					})
			)
			.StaticNodePool()
			.Settings(
				settings => settings
					.DisablePing()
					.EnableDebugMode()
					.OnRequestCompleted(
						details => requests?.Add(
							(details.Uri?.AbsolutePath ?? string.Empty, details.RequestBodyInBytes is { } bytes
								? Encoding.UTF8.GetString(bytes)
								: string.Empty)
						)
					)
			)
			.RequestHandler;

	private static PageFeedbackIndex CreateIndex() => new(new AppEnvironment { Current = AppEnv.Dev });

	// The factory the API registers, so a change to its strategy is exercised by these writes.
	private static IngestChannel<PageFeedbackDocument> CreateChannel(ITransport transport, PageFeedbackIndex index) =>
		index.CreateChannel(transport, NullLogger.Instance);

	private static PageFeedbackRecord CreateRecord() =>
		new(
			Guid.Parse("00000000-0000-4000-8000-000000000001"),
			"/docs/test-page",
			"Test page",
			PageFeedbackReaction.ThumbsUp,
			[PageFeedbackReason.Accurate],
			2,
			"Clear and useful."
		);
}
