// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.PageFeedback;
using Elastic.Documentation.Api.PageFeedback;
using Elastic.Ingest.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.VirtualizedCluster;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Tests;

public class PageFeedbackGatewayTests
{
	[Fact]
	public async Task UpsertFeedbackAsync_AllItemsPersisted_ReturnsTrue()
	{
		var transport = CreateTransport(201);
		using var pageFeedbackTransport = new PageFeedbackTransport(transport);
		var index = CreateIndex();
		using var channel = CreateChannel(transport, index);
		var gateway = new ElasticsearchPageFeedbackGateway(
			pageFeedbackTransport,
			index,
			channel,
			NullLogger<ElasticsearchPageFeedbackGateway>.Instance
		);

		var result = await gateway.UpsertFeedbackAsync(CreateRecord(), TestContext.Current.CancellationToken);

		result.Should().BeTrue();
	}

	[Fact]
	public async Task UpsertFeedbackAsync_ItemRejected_ReturnsFalse()
	{
		var transport = CreateTransport(400);
		using var pageFeedbackTransport = new PageFeedbackTransport(transport);
		var index = CreateIndex();
		using var channel = CreateChannel(transport, index);
		var gateway = new ElasticsearchPageFeedbackGateway(
			pageFeedbackTransport,
			index,
			channel,
			NullLogger<ElasticsearchPageFeedbackGateway>.Instance
		);

		var result = await gateway.UpsertFeedbackAsync(CreateRecord(), TestContext.Current.CancellationToken);

		result.Should().BeFalse();
	}

	private static ITransport CreateTransport(int itemStatus) =>
		Virtual.Elasticsearch
			.Bootstrap(1)
			.Ping(call => call.SucceedAlways())
			.ClientCalls(call => call
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
				}))
			.StaticNodePool()
			.Settings(settings => settings.DisablePing().EnableDebugMode())
			.RequestHandler;

	private static PageFeedbackIndex CreateIndex() => new(new AppEnvironment { Current = AppEnv.Dev });

	private static IngestChannel<PageFeedbackDocument> CreateChannel(ITransport transport, PageFeedbackIndex index) =>
		new(new IngestChannelOptions<PageFeedbackDocument>(transport, index.MappingContext));

	private static PageFeedbackRecord CreateRecord() => new(
		Guid.Parse("00000000-0000-4000-8000-000000000001"),
		"/docs/test-page",
		"Test page",
		PageFeedbackReaction.ThumbsUp,
		PageFeedbackReason.Accurate,
		1,
		"Clear and useful.",
		"test-euid"
	);
}
