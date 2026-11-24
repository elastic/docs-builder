// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Strategies;
using Elastic.Transport;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

/// <summary>
/// Puts the current mapping on a fixed-name index that already exists. Templates only apply when an index is created,
/// so without this step a field added later is stored but, with dynamic mapping off, never indexed.
/// </summary>
// TODO: this belongs in Elastic.Ingest.Elasticsearch as a built-in bootstrap step; remove it once upstream ships one.
internal sealed class ExistingIndexMappingStep(string indexName) : IBootstrapStep
{
	public string Name => "ExistingIndexMapping";

	public async Task<bool> ExecuteAsync(BootstrapContext context, CancellationToken ctx = default)
	{
		if (context.GetMappingsJson is null)
			return true;

		var response = await context
			.Transport
			.RequestAsync<StringResponse>(
				HttpMethod.PUT,
				$"{indexName}/_mapping",
				PostData.String(context.GetMappingsJson()),
				cancellationToken: ctx
			)
			.ConfigureAwait(false);
		return Applied(context, response);
	}

	public bool Execute(BootstrapContext context)
	{
		if (context.GetMappingsJson is null)
			return true;

		var response = context.Transport.Request<StringResponse>(
			HttpMethod.PUT,
			$"{indexName}/_mapping",
			PostData.String(context.GetMappingsJson())
		);
		return Applied(context, response);
	}

	private bool Applied(BootstrapContext context, StringResponse response)
	{
		// No index yet: the first write creates it from the templates.
		if (response.ApiCallDetails.HasSuccessfulStatusCode || response.ApiCallDetails.HttpStatusCode == (int)HttpStatusCode.NotFound)
			return true;

		return context.BootstrapMethod == BootstrapMethod.Silent
			? false
			: throw new InvalidOperationException(
				$"Failure to apply the mapping to the existing index {indexName}: {response}",
				response.ApiCallDetails.OriginalException
			);
	}
}
