// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Strategies;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using HttpMethod = Elastic.Transport.HttpMethod;

namespace Elastic.Documentation.Api.Adapters.PageFeedback;

/// <summary>
/// Puts the current mapping on a fixed-name index that already exists. Templates only apply when an index is created,
/// so without this step a field added later is stored but, with dynamic mapping off, never indexed.
/// </summary>
// TODO: this belongs in Elastic.Ingest.Elasticsearch as a built-in bootstrap step; remove it once upstream ships one.
internal sealed class ExistingIndexMappingStep(string indexName, ILogger logger) : IBootstrapStep
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
		if (response.ApiCallDetails.HttpStatusCode == (int)HttpStatusCode.NotFound)
			return true;

		if (response.ApiCallDetails.HasSuccessfulStatusCode)
		{
			// The update is applied either way; only the wait for every node to confirm it timed out.
			if (IsUnacknowledged(response.Body))
				logger.LogWarning(
					"Not every node acknowledged the mapping update for {IndexName} in time. Documents written before they do are stored without the added fields indexed; run _update_by_query on the index to index them",
					indexName
				);
			return true;
		}

		if (context.BootstrapMethod != BootstrapMethod.Silent)
			throw new InvalidOperationException(
				$"Failure to apply the mapping to the existing index {indexName}: {response}",
				response.ApiCallDetails.OriginalException
			);

		if (response.ApiCallDetails.HttpStatusCode is not { } status)
		{
			logger.LogWarning(
				response.ApiCallDetails.OriginalException,
				"No response from Elasticsearch to the mapping update for {IndexName}",
				indexName
			);
			return false;
		}

		logger.LogWarning(
			response.ApiCallDetails.OriginalException,
			"Elasticsearch rejected the mapping update for {IndexName}: HTTP {StatusCode}. {Response}",
			indexName,
			status,
			response.Body
		);
		return false;
	}

	private static bool IsUnacknowledged(string? body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return false;

		try
		{
			using var json = JsonDocument.Parse(body);
			return json.RootElement.ValueKind is JsonValueKind.Object
				&& json.RootElement.TryGetProperty("acknowledged", out var acknowledged)
				&& acknowledged.ValueKind is JsonValueKind.False;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}
