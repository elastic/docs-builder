// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Nodes;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

/// <summary>
/// Manages content-date tracking via an Elasticsearch enrich policy and ingest pipeline.
/// Instead of draining the lookup index into memory, the pipeline compares content hashes
/// at index time and preserves or updates <c>content_last_updated</c> accordingly.
/// </summary>
public class ContentDateEnrichment(
	DistributedTransport transport,
	ElasticsearchOperations operations,
	ILogger logger,
	string buildType,
	string environment)
{
	private readonly string _lookupIndex = $"docs-{buildType}-content-dates-{environment}";

	public string PipelineName => $"{_lookupIndex}-pipeline";

	private string PolicyName => $"{_lookupIndex}-policy";

	/// <summary>
	/// Creates the lookup index (if needed), enrich policy, executes it, and creates the ingest pipeline.
	/// Must be called before indexing begins.
	/// </summary>
	public async Task InitializeAsync(Cancel ct)
	{
		await EnsureLookupIndexAsync(ct);
		await PutEnrichPolicyAsync(ct);
		await ExecutePolicyAsync(ct);
		await PutPipelineAsync(ct);
	}

	/// <summary>
	/// After indexing completes, syncs the lookup index from the lexical index and re-executes the enrich policy.
	/// This replaces all lookup entries with current data (implicitly removing orphans) and ensures the next
	/// run's pipeline sees up-to-date content hashes.
	/// </summary>
	public async Task SyncLookupIndexAsync(string lexicalAlias, Cancel ct)
	{
		logger.LogInformation("Syncing content date lookup index {Index} from {Source}", _lookupIndex, lexicalAlias);

		await DeleteLookupContentsAsync(ct);
		await ReindexToLookupAsync(lexicalAlias, ct);
		await RefreshLookupIndexAsync(ct);
		await ExecutePolicyAsync(ct);

		logger.LogInformation("Content date lookup sync complete");
	}

	private async Task EnsureLookupIndexAsync(Cancel ct)
	{
		var head = await operations.WithRetryAsync(
			() => transport.HeadAsync(_lookupIndex, ct),
			$"HEAD {_lookupIndex}",
			ct
		);
		if (head.ApiCallDetails.HttpStatusCode == 200)
		{
			logger.LogInformation("Content date lookup index {Index} already exists", _lookupIndex);
			return;
		}

		var mapping = new JsonObject
		{
			["settings"] = new JsonObject { ["number_of_shards"] = 1, ["number_of_replicas"] = 0 },
			["mappings"] = new JsonObject
			{
				["properties"] = new JsonObject
				{
					["url"] = new JsonObject { ["type"] = "keyword" },
					["content_hash"] = new JsonObject { ["type"] = "keyword" },
					["content_last_updated"] = new JsonObject { ["type"] = "date" }
				}
			}
		};

		var response = await operations.WithRetryAsync(
			() => transport.PutAsync<StringResponse>(_lookupIndex, PostData.String(mapping.ToJsonString()), ct),
			$"PUT {_lookupIndex}",
			ct
		);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to create content date lookup index {_lookupIndex}: {response.ApiCallDetails.DebugInformation}");

		logger.LogInformation("Created content date lookup index {Index}", _lookupIndex);
	}

	private async Task PutEnrichPolicyAsync(Cancel ct)
	{
		var policy = new JsonObject
		{
			["match"] = new JsonObject
			{
				["indices"] = _lookupIndex,
				["match_field"] = "url",
				["enrich_fields"] = new JsonArray("content_hash", "content_last_updated")
			}
		};

		var response = await operations.WithRetryAsync(
			() => transport.PutAsync<StringResponse>($"/_enrich/policy/{PolicyName}", PostData.String(policy.ToJsonString()), ct),
			$"PUT _enrich/policy/{PolicyName}",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to create enrich policy {PolicyName}: {response.ApiCallDetails.DebugInformation}");

		logger.LogInformation("Created enrich policy {Policy}", PolicyName);
	}

	private async Task ExecutePolicyAsync(Cancel ct)
	{
		var response = await operations.WithRetryAsync(
			() => transport.PostAsync<StringResponse>($"/_enrich/policy/{PolicyName}/_execute", PostData.Empty, ct),
			$"POST _enrich/policy/{PolicyName}/_execute",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to execute enrich policy {PolicyName}: {response.ApiCallDetails.DebugInformation}");

		logger.LogInformation("Executed enrich policy {Policy}", PolicyName);
	}

	private async Task PutPipelineAsync(Cancel ct)
	{
		var pipeline = new JsonObject
		{
			["description"] = "Resolves content_last_updated via enrich policy lookup on content_hash",
			["processors"] = new JsonArray(
				new JsonObject
				{
					["enrich"] = new JsonObject
					{
						["policy_name"] = PolicyName,
						["field"] = "url",
						["target_field"] = "_content_date_lookup",
						["max_matches"] = 1,
						["ignore_missing"] = true
					}
				},
				new JsonObject
				{
					["script"] = new JsonObject
					{
						["lang"] = "painless",
						["source"] = """
							def lookup = ctx._content_date_lookup;
							if (lookup != null && lookup.content_hash != null && lookup.content_hash == ctx.content_hash) {
								ctx.content_last_updated = lookup.content_last_updated;
							} else {
								ctx.content_last_updated = ctx._ingest.timestamp;
							}
							ctx.remove('_content_date_lookup');
							"""
					}
				}
			)
		};

		var response = await operations.WithRetryAsync(
			() => transport.PutAsync<StringResponse>($"/_ingest/pipeline/{PipelineName}", PostData.String(pipeline.ToJsonString()), ct),
			$"PUT _ingest/pipeline/{PipelineName}",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to create ingest pipeline {PipelineName}: {response.ApiCallDetails.DebugInformation}");

		logger.LogInformation("Created ingest pipeline {Pipeline}", PipelineName);
	}

	private async Task RefreshLookupIndexAsync(Cancel ct)
	{
		var response = await operations.WithRetryAsync(
			() => transport.PostAsync<StringResponse>($"/{_lookupIndex}/_refresh", PostData.Empty, ct),
			$"POST {_lookupIndex}/_refresh",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogWarning("Failed to refresh lookup index {Index}: {Info}", _lookupIndex, response.ApiCallDetails.DebugInformation);
		else
			logger.LogInformation("Refreshed lookup index {Index}", _lookupIndex);
	}

	private async Task DeleteLookupContentsAsync(Cancel ct)
	{
		var body = new JsonObject
		{
			["query"] = new JsonObject { ["match_all"] = new JsonObject() }
		};
		await operations.DeleteByQueryAsync(_lookupIndex, PostData.String(body.ToJsonString()), ct);
	}

	private async Task ReindexToLookupAsync(string sourceAlias, Cancel ct)
	{
		var reindexBody = new JsonObject
		{
			["source"] = new JsonObject
			{
				["index"] = sourceAlias,
				["_source"] = new JsonArray("url", "content_hash", "content_last_updated")
			},
			["dest"] = new JsonObject
			{
				["index"] = _lookupIndex
			},
			["script"] = new JsonObject
			{
				["lang"] = "painless",
				["source"] = "ctx._id = ctx._source.url.sha256().substring(0, 16)"
			}
		};

		await operations.ReindexAsync(sourceAlias, PostData.String(reindexBody.ToJsonString()), _lookupIndex, ct);
	}
}
