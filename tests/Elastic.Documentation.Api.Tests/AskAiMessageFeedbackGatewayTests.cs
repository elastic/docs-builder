// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.AskAi;
using Elastic.Documentation.Api.AskAi;
using Elastic.Transport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Tests;

public class AskAiMessageFeedbackGatewayTests
{
	[Test]
	[Arguments(201, true)]
	[Arguments(200, true)]
	[Arguments(400, false)]
	[Arguments(503, false)]
	public async Task RecordFeedbackAsync_ReportsWhetherElasticsearchStoredTheFeedback(int statusCode, bool stored)
	{
		var transport = new DistributedTransport(
			new TransportConfiguration(
				new SingleNodePool(new Uri("http://localhost:9200")),
				new InMemoryRequestInvoker(Encoding.UTF8.GetBytes("""{"result":"created"}"""), statusCode)
			)
		);
		var gateway = new ElasticsearchAskAiMessageFeedbackGateway(
			transport,
			new AppEnvironment { Current = AppEnv.Dev },
			NullLogger<ElasticsearchAskAiMessageFeedbackGateway>.Instance
		);

		var result = await gateway.RecordFeedbackAsync(
			new AskAiMessageFeedbackRecord(Guid.NewGuid(), Guid.NewGuid(), Reaction.ThumbsUp),
			TestContext.Current!.Execution.CancellationToken
		);

		result.Should().Be(stored);
	}
}
