// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Ingest.Elasticsearch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

internal sealed class PageFeedbackBootstrapService(
	IngestChannel<PageFeedbackDocument> channel,
	PageFeedbackIndex index,
	AppEnvironment appEnvironment,
	ILogger<PageFeedbackBootstrapService> logger
) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var method = appEnvironment.Current is AppEnv.Dev
			? BootstrapMethod.Silent
			: BootstrapMethod.Failure;

		if (await channel.BootstrapElasticsearchAsync(method, cancellationToken))
		{
			logger.LogInformation("Bootstrapped page feedback index template for {IndexName}", index.Name);
			return;
		}

		logger.LogWarning("Unable to bootstrap page feedback index template for {IndexName}", index.Name);
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
