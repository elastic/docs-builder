// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;
using AwesomeAssertions;
using Elastic.Documentation.Api.Adapters.PageFeedback;
using Elastic.Ingest.Elasticsearch;
using Elastic.Transport;
using Elastic.Transport.VirtualizedCluster;
using Microsoft.Extensions.Logging;
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

		var mappingCall = calls.Should().ContainSingle(c => c.Path.EndsWith("/_mapping", StringComparison.Ordinal)).Subject;
		mappingCall.Path.Should().Be("/page-feedback-v1-staging/_mapping");
		mappingCall.Body.Should().Contain("\"reasons\"");
	}

	// A missing index is only safe when the templates are current: the first write creates the index from them.
	[Test]
	public async Task StartAsync_IndexExists_InstallsTemplatesBeforeTheMapping()
	{
		var (transport, calls) = CreateTransport(mappingStatus: 200);

		await CreateService(transport, AppEnv.Staging).StartAsync(TestContext.Current!.Execution.CancellationToken);

		calls
			.Where(c => c.Method == "PUT")
			.Select(c => c.Path)
			.Should()
			.ContainInOrder(
				"/_component_template/page-feedback-v1-staging-template-settings",
				"/_component_template/page-feedback-v1-staging-template-mappings",
				"/_index_template/page-feedback-v1-staging-template",
				"/page-feedback-v1-staging/_mapping"
			);
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
	public async Task StartAsync_MappingRejectedInDev_CompletesAndLogsTheRejection()
	{
		var (transport, _) = CreateTransport(mappingStatus: 400);
		var logger = new ListLogger();

		await CreateService(transport, AppEnv.Dev, logger).StartAsync(TestContext.Current!.Execution.CancellationToken);

		logger.Messages.Should().ContainSingle(m => m.Contains("page-feedback-v1-dev") && m.Contains("HTTP 400"));
	}

	[Test]
	public async Task StartAsync_MappingNotAcknowledgedInStaging_CompletesAndWarnsAboutTheIndexingGap()
	{
		var (transport, _) = CreateTransport(mappingStatus: 200, acknowledged: false);
		var logger = new ListLogger();

		await CreateService(transport, AppEnv.Staging, logger).StartAsync(TestContext.Current!.Execution.CancellationToken);

		logger.Messages.Should().ContainSingle(m => m.Contains("page-feedback-v1-staging") && m.Contains("_update_by_query"));
	}

	[Test]
	public async Task StartAsync_MappingAcknowledged_LogsNothing()
	{
		var (transport, _) = CreateTransport(mappingStatus: 200);
		var logger = new ListLogger();

		await CreateService(transport, AppEnv.Staging, logger).StartAsync(TestContext.Current!.Execution.CancellationToken);

		logger.Messages.Should().BeEmpty();
	}

	[Test]
	public async Task StartAsync_NoResponseToTheMappingInDev_LogsTheTransportFailure()
	{
		var (transport, _) = CreateTransport(mappingStatus: null);
		var logger = new ListLogger();

		await CreateService(transport, AppEnv.Dev, logger).StartAsync(TestContext.Current!.Execution.CancellationToken);

		logger.Messages.Should().ContainSingle(m => m.Contains("No response from Elasticsearch") && m.Contains("page-feedback-v1-dev"));
		logger.Exceptions.Should().ContainSingle().Which.Should().NotBeNull();
	}

	private sealed class ListLogger : ILogger
	{
		public List<string> Messages { get; } = [];

		public List<Exception?> Exceptions { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter
		)
		{
			Messages.Add(formatter(state, exception));
			Exceptions.Add(exception);
		}
	}

	private static PageFeedbackBootstrapService CreateService(ITransport transport, AppEnv env, ILogger? stepLogger = null)
	{
		var appEnvironment = new AppEnvironment { Current = env };
		var index = new PageFeedbackIndex(appEnvironment);
		return new PageFeedbackBootstrapService(
			index.CreateChannel(transport, stepLogger ?? NullLogger.Instance),
			index,
			appEnvironment,
			NullLogger<PageFeedbackBootstrapService>.Instance
		);
	}

	private static (ITransport Transport, List<Call> Calls) CreateTransport(int? mappingStatus, bool acknowledged = true)
	{
		var calls = new List<Call>();
		var transport = Virtual
			.Elasticsearch
			.Bootstrap(1)
			.Ping(call => call.SucceedAlways())
			.ClientCalls(
				call =>
					mappingStatus switch
					{
						200 => call.OnPath("_mapping").SucceedAlways().ReturnResponse(new { acknowledged }),
						{ } status => call.OnPath("_mapping").FailAlways(status),
						null => call.OnPath("_mapping").FailAlways()
					}
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
