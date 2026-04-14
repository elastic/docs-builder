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
/// Uses a stable alias over timestamped backing indices so that lookup data is atomically
/// swapped after a full reindex, avoiding any window where the lookup is empty.
/// </summary>
public class ContentDateEnrichment(
	DistributedTransport transport,
	ElasticsearchOperations operations,
	ILogger logger,
	string buildType,
	string environment)
{
	private readonly string _lookupAlias = $"docs-{buildType}-content-dates-{environment}";

	public string PipelineName => $"{_lookupAlias}-pipeline";

	private string PolicyName => $"{_lookupAlias}-policy";

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
	/// After indexing completes, reindexes into a fresh staging index and atomically swaps the
	/// alias to point at it. The old backing index is deleted only after the swap succeeds.
	/// This replaces all lookup entries with current data (implicitly removing orphans) and ensures
	/// the next run's pipeline sees up-to-date content hashes.
	/// </summary>
	public async Task SyncLookupIndexAsync(string lexicalAlias, Cancel ct)
	{
		logger.LogInformation("Syncing content date lookup from {Source} via staging index", lexicalAlias);

		var oldIndex = await ResolveBackingIndexAsync(ct);
		var stagingIndex = GenerateStagingName();

		await CreateLookupIndexAsync(stagingIndex, ct);
		await ReindexToLookupAsync(lexicalAlias, stagingIndex, ct);
		await RefreshIndexAsync(stagingIndex, ct);
		await SwapAliasAsync(oldIndex, stagingIndex, ct);

		if (oldIndex != null)
			await DeleteIndexAsync(oldIndex, ct);

		await ExecutePolicyAsync(ct);

		logger.LogInformation("Content date lookup sync complete");
	}

	private string GenerateStagingName() =>
		$"{_lookupAlias}-{DateTime.UtcNow:yyyyMMddHHmmss}";

	private async Task<string?> ResolveBackingIndexAsync(Cancel ct)
	{
		var response = await operations.WithRetryAsync(
			() => transport.GetAsync<StringResponse>($"/_alias/{_lookupAlias}", ct),
			$"GET /_alias/{_lookupAlias}",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			return null;

		var json = JsonNode.Parse(response.Body);
		return json?.AsObject().Select(kv => kv.Key).FirstOrDefault();
	}

	private async Task EnsureLookupIndexAsync(Cancel ct)
	{
		var existing = await ResolveBackingIndexAsync(ct);
		if (existing != null)
		{
			logger.LogInformation("Content date lookup alias {Alias} already exists, backed by {Index}", _lookupAlias, existing);
			return;
		}

		var indexName = GenerateStagingName();
		await CreateLookupIndexAsync(indexName, ct);
		await SwapAliasAsync(null, indexName, ct);

		logger.LogInformation("Created content date lookup index {Index} with alias {Alias}", indexName, _lookupAlias);
	}

	private async Task CreateLookupIndexAsync(string indexName, Cancel ct)
	{
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
			() => transport.PutAsync<StringResponse>(indexName, PostData.String(mapping.ToJsonString()), ct),
			$"PUT {indexName}",
			ct
		);
		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to create content date lookup index {indexName}: {response.ApiCallDetails.DebugInformation}");

		logger.LogInformation("Created content date lookup index {Index}", indexName);
	}

	private async Task SwapAliasAsync(string? oldIndex, string newIndex, Cancel ct)
	{
		var addAction = new JsonObject { ["add"] = new JsonObject { ["index"] = newIndex, ["alias"] = _lookupAlias } };

		var actions = oldIndex != null
			? new JsonArray(
				new JsonObject { ["remove"] = new JsonObject { ["index"] = oldIndex, ["alias"] = _lookupAlias } },
				addAction
			)
			: new JsonArray(addAction);

		var body = new JsonObject { ["actions"] = actions };

		var response = await operations.WithRetryAsync(
			() => transport.PostAsync<StringResponse>("/_aliases", PostData.String(body.ToJsonString()), ct),
			"POST /_aliases",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			throw new InvalidOperationException(
				$"Failed to swap alias {_lookupAlias} to {newIndex}: {response.ApiCallDetails.DebugInformation}");

		logger.LogInformation("Swapped alias {Alias} from {OldIndex} to {NewIndex}", _lookupAlias, oldIndex ?? "(none)", newIndex);
	}

	private async Task DeleteIndexAsync(string indexName, Cancel ct)
	{
		var response = await operations.WithRetryAsync(
			() => transport.DeleteAsync<StringResponse>(indexName, new DefaultRequestParameters(), PostData.Empty, ct),
			$"DELETE {indexName}",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogWarning("Failed to delete old lookup index {Index}: {Info}", indexName, response.ApiCallDetails.DebugInformation);
		else
			logger.LogInformation("Deleted old lookup index {Index}", indexName);
	}

	private async Task PutEnrichPolicyAsync(Cancel ct)
	{
		var policy = new JsonObject
		{
			["match"] = new JsonObject
			{
				["indices"] = _lookupAlias,
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
					["set"] = new JsonObject
					{
						["field"] = "content_last_updated",
						["value"] = "{{{_ingest.timestamp}}}"
					}
				},
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

	private async Task RefreshIndexAsync(string indexName, Cancel ct)
	{
		var response = await operations.WithRetryAsync(
			() => transport.PostAsync<StringResponse>($"/{indexName}/_refresh", PostData.Empty, ct),
			$"POST {indexName}/_refresh",
			ct
		);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			logger.LogWarning("Failed to refresh index {Index}: {Info}", indexName, response.ApiCallDetails.DebugInformation);
		else
			logger.LogInformation("Refreshed index {Index}", indexName);
	}

	private async Task ReindexToLookupAsync(string sourceAlias, string destIndex, Cancel ct)
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
				["index"] = destIndex
			},
			["script"] = new JsonObject
			{
				["lang"] = "painless",
				["source"] = "ctx._id = ctx._source.url.sha256().substring(0, 16)"
			}
		};

		await operations.ReindexAsync(sourceAlias, PostData.String(reindexBody.ToJsonString()), destIndex, ct);
	}
}
