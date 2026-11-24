// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Elastic.Ingest.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

internal sealed class PageFeedbackBootstrapService(
	IngestChannel<PageFeedbackDocument> channel,
	ITransport transport,
	PageFeedbackIndex index,
	AppEnvironment appEnvironment,
	ILogger<PageFeedbackBootstrapService> logger
) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var method = appEnvironment.Current is AppEnv.Dev ? BootstrapMethod.Silent : BootstrapMethod.Failure;

		if (await channel.BootstrapElasticsearchAsync(method, cancellationToken))
		{
			logger.LogInformation("Bootstrapped page feedback index template for {IndexName}", index.Name);
			await ApplyMappingToExistingIndexAsync(method, cancellationToken);
			return;
		}

		logger.LogWarning("Unable to bootstrap page feedback index template for {IndexName}", index.Name);
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	// Templates only apply when an index is created. The index has a fixed name, so one created before a field was added
	// keeps its old mapping, and with dynamic mapping off the new field is stored but never indexed.
	private async Task ApplyMappingToExistingIndexAsync(BootstrapMethod method, CancellationToken cancellationToken)
	{
		var response = await transport.RequestAsync<StringResponse>(
			Elastic.Transport.HttpMethod.PUT,
			$"{index.Name}/_mapping",
			PostData.String(index.MappingContext.GetMappingsJson()),
			cancellationToken: cancellationToken
		);

		if (response.ApiCallDetails.HttpStatusCode == (int)HttpStatusCode.NotFound)
		{
			// No index yet: the first write creates it from the template.
			logger.LogDebug("Page feedback index {IndexName} does not exist yet; its template applies on first write", index.Name);
			return;
		}

		if (response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			logger.LogInformation("Applied the page feedback mapping to {IndexName}", index.Name);
			return;
		}

		var status = response.ApiCallDetails.HttpStatusCode;
		logger.LogWarning(
			"Unable to apply the page feedback mapping to {IndexName}: HTTP {StatusCode}. {Body}",
			index.Name,
			status,
			response.Body
		);
		if (method is BootstrapMethod.Failure)
			throw new InvalidOperationException($"Unable to apply the page feedback mapping to {index.Name}: HTTP {status}.");
	}
}
