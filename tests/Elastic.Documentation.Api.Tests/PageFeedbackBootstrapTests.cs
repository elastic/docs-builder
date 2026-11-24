// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.PageFeedback;
using Elastic.Ingest.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.VirtualizedCluster;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elastic.Documentation.Api.Tests;

public class PageFeedbackBootstrapTests
{
	private sealed record Call(string Method, string Path, string Body);

	[Test]
	public async Task StartAsync_IndexExists_PutsCurrentMappingOnIt()
	{
		var (transport, calls) = CreateTransport(mappingStatus: 200);

		await CreateService(transport, AppEnv.Staging).StartAsync(TestContext.Current!.Execution.CancellationToken);

		var mappingCalls = calls.Where(c => c.Path.EndsWith("/_mapping", StringComparison.Ordinal)).ToArray();
		mappingCalls.Should().ContainSingle();
		mappingCalls[0].Method.Should().Be("PUT");
		mappingCalls[0].Path.Should().Be("/page-feedback-v1-staging/_mapping");
		mappingCalls[0].Body.Should().Contain("\"reasons\"");
	}

	[Test]
	public async Task StartAsync_IndexDoesNotExistYet_Completes()
	{
		var (transport, _) = CreateTransport(mappingStatus: 404);

		var start = () => CreateService(transport, AppEnv.Staging).StartAsync(TestContext.Current!.Execution.CancellationToken);

		await start.Should().NotThrowAsync();
	}

	[Test]
	public async Task StartAsync_MappingRejectedInStaging_FailsStartup()
	{
		var (transport, _) = CreateTransport(mappingStatus: 400);

		var start = () => CreateService(transport, AppEnv.Staging).StartAsync(TestContext.Current!.Execution.CancellationToken);

		await start.Should().ThrowAsync<InvalidOperationException>();
	}

	[Test]
	public async Task StartAsync_MappingRejectedInDev_Completes()
	{
		var (transport, _) = CreateTransport(mappingStatus: 400);

		var start = () => CreateService(transport, AppEnv.Dev).StartAsync(TestContext.Current!.Execution.CancellationToken);

		await start.Should().NotThrowAsync();
	}

	private static PageFeedbackBootstrapService CreateService(ITransport transport, AppEnv env)
	{
		var appEnvironment = new AppEnvironment { Current = env };
		var index = new PageFeedbackIndex(appEnvironment);
		var channel = new IngestChannel<PageFeedbackDocument>(
			new IngestChannelOptions<PageFeedbackDocument>(transport, index.MappingContext)
		);
		return new PageFeedbackBootstrapService(
			channel,
			transport,
			index,
			appEnvironment,
			NullLogger<PageFeedbackBootstrapService>.Instance
		);
	}

	private static (ITransport Transport, List<Call> Calls) CreateTransport(int mappingStatus)
	{
		var calls = new List<Call>();
		var transport = Virtual
			.Elasticsearch
			.Bootstrap(1)
			.Ping(call => call.SucceedAlways())
			.ClientCalls(
				call => mappingStatus is 200 ? call.OnPath("_mapping").SucceedAlways() : call.OnPath("_mapping").FailAlways(mappingStatus)
			)
			.ClientCalls(call => call.SucceedAlways())
			.StaticNodePool()
			.Settings(
				settings => settings
					.DisablePing()
					.EnableDebugMode()
					.OnRequestCompleted(
						details => calls.Add(
							new Call(
								details.HttpMethod.ToString(),
								details.Uri?.AbsolutePath ?? string.Empty,
								details.RequestBodyInBytes is { } bytes ? Encoding.UTF8.GetString(bytes) : string.Empty
							)
						)
					)
			)
			.RequestHandler;
		return (transport, calls);
	}
}
