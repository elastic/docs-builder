// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Search;
using Elastic.Documentation.Configuration.Versions;
using Elastic.Documentation.Diagnostics;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;
using NetEscapades.EnumGenerators;

namespace Elastic.Markdown.Exporters.Elasticsearch;

[EnumExtensions]
public enum IngestStrategy { Reindex, Multiplex }

public partial class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly IDiagnosticsCollector _collector;
	private readonly IDocumentationConfigurationContext _context;
	private readonly ILogger _logger;
	private readonly ElasticsearchLexicalIngestChannel _lexicalChannel;
	private readonly ElasticsearchSemanticIngestChannel _semanticChannel;

	private readonly ElasticsearchEndpoint _endpoint;

	private readonly DateTimeOffset _batchIndexDate = DateTimeOffset.UtcNow;
	private readonly DistributedTransport _transport;
	private IngestStrategy _indexStrategy;
	private readonly string _indexNamespace;
	private string _currentLexicalHash = string.Empty;
	private string _currentSemanticHash = string.Empty;

	private readonly IReadOnlyDictionary<string, string[]> _synonyms;
	private readonly IReadOnlyCollection<QueryRule> _rules;
	private readonly VersionsConfiguration _versionsConfiguration;
	private readonly string _fixedSynonymsHash;

	public ElasticsearchMarkdownExporter(
		ILoggerFactory logFactory,
		IDiagnosticsCollector collector,
		DocumentationEndpoints endpoints,
		string indexNamespace,
		IDocumentationConfigurationContext context
	)
	{
		_collector = collector;
		_context = context;
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		_endpoint = endpoints.Elasticsearch;
		_indexStrategy = IngestStrategy.Reindex;
		_indexNamespace = indexNamespace;
		_versionsConfiguration = context.VersionsConfiguration;
		_synonyms = context.SearchConfiguration.Synonyms;
		_rules = context.SearchConfiguration.Rules;
		var es = endpoints.Elasticsearch;

		var configuration = new ElasticsearchConfiguration(es.Uri)
		{
			Authentication = es.ApiKey is { } apiKey
				? new ApiKey(apiKey)
				: es is { Username: { } username, Password: { } password }
					? new BasicAuthentication(username, password)
					: null,
			EnableHttpCompression = true,
			//DebugMode = _endpoint.DebugMode,
			DebugMode = true,
			CertificateFingerprint = _endpoint.CertificateFingerprint,
			ProxyAddress = _endpoint.ProxyAddress,
			ProxyPassword = _endpoint.ProxyPassword,
			ProxyUsername = _endpoint.ProxyUsername,
			ServerCertificateValidationCallback = _endpoint.DisableSslVerification
				? CertificateValidations.AllowAll
				: _endpoint.Certificate is { } cert
					? _endpoint.CertificateIsNotRoot
						? CertificateValidations.AuthorityPartOfChain(cert)
						: CertificateValidations.AuthorityIsRoot(cert)
					: null
		};

		_transport = new DistributedTransport(configuration);

		string[] fixedSynonyms = ["esql", "data-stream", "data-streams", "machine-learning"];
		var indexTimeSynonyms = _synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			var id = synonym.Key;
			acc.Add(new SynonymRule { Id = id, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		}).Where(r => fixedSynonyms.Contains(r.Id)).Select(r => r.Synonyms).ToArray();
		_fixedSynonymsHash = HashedBulkUpdate.CreateHash(string.Join(",", indexTimeSynonyms));

		_lexicalChannel = new ElasticsearchLexicalIngestChannel(logFactory, collector, es, indexNamespace, _transport, indexTimeSynonyms);
		_semanticChannel = new ElasticsearchSemanticIngestChannel(logFactory, collector, es, indexNamespace, _transport, indexTimeSynonyms);
	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default)
	{
		_currentLexicalHash = await _lexicalChannel.Channel.GetIndexTemplateHashAsync(ctx) ?? string.Empty;
		_currentSemanticHash = await _semanticChannel.Channel.GetIndexTemplateHashAsync(ctx) ?? string.Empty;

		await PublishSynonymsAsync(ctx);
		await PublishQueryRulesAsync(ctx);
		_ = await _lexicalChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);

		// if the previous hash does not match the current hash, we know already we want to multiplex to a new index
		if (_currentLexicalHash != _lexicalChannel.Channel.ChannelHash)
			_indexStrategy = IngestStrategy.Multiplex;

		if (!_endpoint.NoSemantic)
		{
			var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
			var semanticIndexAvailable = await _transport.HeadAsync(semanticWriteAlias, ctx);
			if (!semanticIndexAvailable.ApiCallDetails.HasSuccessfulStatusCode && _endpoint is { ForceReindex: false, NoSemantic: false })
			{
				_indexStrategy = IngestStrategy.Multiplex;
				_logger.LogInformation("Index strategy set to multiplex because {SemanticIndex} does not exist, pass --force-reindex to always use reindex", semanticWriteAlias);
			}

			//try re-use index if we are re-indexing. Multiplex should always go to a new index
			_semanticChannel.Channel.Options.TryReuseIndex = _indexStrategy == IngestStrategy.Reindex;
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
		}

		var lexicalIndexExists = await IndexExists(_lexicalChannel.Channel.IndexName) ? "existing" : "new";
		var semanticIndexExists = await IndexExists(_semanticChannel.Channel.IndexName) ? "existing" : "new";
		if (_currentLexicalHash != _lexicalChannel.Channel.ChannelHash)
		{
			_indexStrategy = IngestStrategy.Multiplex;
			_logger.LogInformation("Multiplexing lexical new index: '{Index}' since current hash on server '{HashCurrent}' does not match new '{HashNew}'",
				_lexicalChannel.Channel.IndexName, _currentLexicalHash, _lexicalChannel.Channel.ChannelHash);
		}
		else
			_logger.LogInformation("Targeting {State} lexical: '{Index}'", lexicalIndexExists, _lexicalChannel.Channel.IndexName);

		if (!_endpoint.NoSemantic && _currentSemanticHash != _semanticChannel.Channel.ChannelHash)
		{
			_indexStrategy = IngestStrategy.Multiplex;
			_logger.LogInformation("Multiplexing new index '{Index}' since current hash on server '{HashCurrent}' does not match new '{HashNew}'",
				_semanticChannel.Channel.IndexName, _currentSemanticHash, _semanticChannel.Channel.ChannelHash);
		}
		else if (!_endpoint.NoSemantic)
			_logger.LogInformation("Targeting {State} semantical: '{Index}'", semanticIndexExists, _semanticChannel.Channel.IndexName);

		_logger.LogInformation("Using {IndexStrategy} to sync lexical index to semantic index", _indexStrategy.ToStringFast(true));

		async ValueTask<bool> IndexExists(string name) => (await _transport.HeadAsync(name, ctx)).ApiCallDetails.HasSuccessfulStatusCode;
	}

	private async Task PublishSynonymsAsync(Cancel ctx)
	{
		var setName = $"docs-{_indexNamespace}";
		_logger.LogInformation("Publishing synonym set '{SetName}' to Elasticsearch", setName);

		var synonymRules = _synonyms.Aggregate(new List<SynonymRule>(), (acc, synonym) =>
		{
			var id = synonym.Key;
			acc.Add(new SynonymRule { Id = id, Synonyms = string.Join(", ", synonym.Value) });
			return acc;
		});

		var synonymsSet = new SynonymsSet { Synonyms = synonymRules };
		await PutSynonyms(synonymsSet, setName, ctx);
	}

	private async Task PutSynonyms(SynonymsSet synonymsSet, string setName, Cancel ctx)
	{
		var json = JsonSerializer.Serialize(synonymsSet, SynonymSerializerContext.Default.SynonymsSet);

		var response = await _transport.PutAsync<StringResponse>($"_synonyms/{setName}", PostData.String(json), ctx);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			_collector.EmitGlobalError($"Failed to publish synonym set '{setName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			_logger.LogInformation("Successfully published synonym set '{SetName}'.", setName);
	}

	private async Task PublishQueryRulesAsync(Cancel ctx)
	{
		if (_rules.Count == 0)
		{
			_logger.LogInformation("No query rules to publish");
			return;
		}

		var rulesetName = $"docs-ruleset-{_indexNamespace}";
		_logger.LogInformation("Publishing query ruleset '{RulesetName}' with {Count} rules to Elasticsearch", rulesetName, _rules.Count);

		var rulesetRules = _rules.Select(r => new QueryRulesetRule
		{
			RuleId = r.RuleId,
			Type = r.Type.ToString().ToLowerInvariant(),
			Criteria = r.Criteria.Select(c => new QueryRulesetCriteria
			{
				Type = c.Type.ToString().ToLowerInvariant(),
				Metadata = c.Metadata,
				Values = c.Values.ToList()
			}).ToList(),
			Actions = new QueryRulesetActions { Ids = r.Actions.Ids.ToList() }
		}).ToList();

		var ruleset = new QueryRuleset { Rules = rulesetRules };
		await PutQueryRuleset(ruleset, rulesetName, ctx);
	}

	private async Task PutQueryRuleset(QueryRuleset ruleset, string rulesetName, Cancel ctx)
	{
		var json = JsonSerializer.Serialize(ruleset, QueryRulesetSerializerContext.Default.QueryRuleset);

		var response = await _transport.PutAsync<StringResponse>($"_query_rules/{rulesetName}", PostData.String(json), ctx);

		if (!response.ApiCallDetails.HasSuccessfulStatusCode)
			_collector.EmitGlobalError($"Failed to publish query ruleset '{rulesetName}'. Reason: {response.ApiCallDetails.OriginalException?.Message ?? response.ToString()}");
		else
			_logger.LogInformation("Successfully published query ruleset '{RulesetName}'.", rulesetName);
	}

	private async ValueTask<long> CountAsync(string index, string body, Cancel ctx = default)
	{
		var countResponse = await _transport.PostAsync<DynamicResponse>($"/{index}/_count", PostData.String(body), ctx);
		return countResponse.Body.Get<long>("count");
	}

	/// <inheritdoc />
	public async ValueTask StopAsync(Cancel ctx = default)
	{
		var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
		var lexicalWriteAlias = string.Format(_lexicalChannel.Channel.Options.IndexFormat, "latest");

		var stopped = await _lexicalChannel.StopAsync(ctx);
		if (!stopped)
			throw new Exception($"Failed to stop {_lexicalChannel.GetType().Name}");

		await QueryIngestStatistics(lexicalWriteAlias, ctx);

		if (_indexStrategy == IngestStrategy.Multiplex)
		{
			if (!_endpoint.NoSemantic)
				_ = await _semanticChannel.StopAsync(ctx);

			// cleanup lexical index of old data
			await DoDeleteByQuery(lexicalWriteAlias, ctx);
			// need to refresh the lexical index to ensure that the delete by query is available
			_ = await _lexicalChannel.RefreshAsync(ctx);
			await QueryDocumentCounts(ctx);
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (_endpoint.NoSemantic)
				_logger.LogInformation("Finish indexing {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
			else
				_logger.LogInformation("Finish syncing to semantic in {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
			return;
		}

		if (_endpoint.NoSemantic)
		{
			_logger.LogInformation("--no-semantic was specified so exiting early before reindexing to {Index}", lexicalWriteAlias);
			return;
		}

		var semanticIndex = _semanticChannel.Channel.IndexName;
		// check if the alias exists
		var semanticIndexHead = await _transport.HeadAsync(semanticWriteAlias, ctx);
		if (!semanticIndexHead.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogInformation("No semantic index exists yet, creating index {Index} for semantic search", semanticIndex);
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
			var semanticIndexPut = await _transport.PutAsync<StringResponse>(semanticIndex, PostData.String("{}"), ctx);
			if (!semanticIndexPut.ApiCallDetails.HasSuccessfulStatusCode)
				throw new Exception($"Failed to create index {semanticIndex}: {semanticIndexPut}");
		}
		var destinationIndex = _semanticChannel.Channel.IndexName;

		_logger.LogInformation("_reindex updates: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, destinationIndex);
		var request = PostData.String(@"
		{
			""dest"": {
				""index"": """ + destinationIndex + @"""
			},
			""source"": {
				""index"": """ + lexicalWriteAlias + @""",
				""size"": 100,
				""query"": {
					""range"": {
						""last_updated"": {
							""gte"": """ + _batchIndexDate.ToString("o") + @"""
						}
					}
				}
			}
		}");
		await DoReindex(request, lexicalWriteAlias, destinationIndex, "updates", ctx);

		_logger.LogInformation("_reindex deletions: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, destinationIndex);
		request = PostData.String(@"
		{
			""dest"": {
				""index"": """ + destinationIndex + @"""
			},
			""script"": {
				""source"": ""ctx.op = \""delete\""""
			},
			""source"": {
				""index"": """ + lexicalWriteAlias + @""",
				""size"": 100,
				""query"": {
					""range"": {
						""batch_index_date"": {
							""lt"": """ + _batchIndexDate.ToString("o") + @"""
						}
					}
				}
			}
		}");
		await DoReindex(request, lexicalWriteAlias, destinationIndex, "deletions", ctx);

		await DoDeleteByQuery(lexicalWriteAlias, ctx);

		_ = await _lexicalChannel.Channel.ApplyLatestAliasAsync(ctx);
		_ = await _semanticChannel.Channel.ApplyAliasesAsync(ctx);

		_ = await _lexicalChannel.RefreshAsync(ctx);
		_ = await _semanticChannel.RefreshAsync(ctx);

		_logger.LogInformation("Finish sync to semantic index using {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
		await QueryDocumentCounts(ctx);
	}

	private async ValueTask QueryIngestStatistics(string lexicalWriteAlias, Cancel ctx)
	{
		var lexicalSearchAlias = _lexicalChannel.Channel.Options.ActiveSearchAlias;
		var updated = await CountAsync(lexicalSearchAlias, $$""" { "query": { "range": { "last_updated": { "gte": "{{_batchIndexDate:o}}" } } } }""", ctx);
		var total = await CountAsync(lexicalSearchAlias, $$""" { "query": { "range": { "batch_index_date": { "gte": "{{_batchIndexDate:o}}" } } } }""", ctx);
		var deleted = await CountAsync(lexicalSearchAlias, $$""" { "query": { "range": { "batch_index_date": { "lt": "{{_batchIndexDate:o}}" } } } }""", ctx);

		// TODO emit these as metrics
		_logger.LogInformation("Exported {Total}, Updated {Updated}, Deleted, {Deleted} documents to {LexicalIndex}", total, updated, deleted, lexicalWriteAlias);
		_logger.LogInformation("Syncing to semantic index using {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
	}

	private async ValueTask QueryDocumentCounts(Cancel ctx)
	{
		var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
		var lexicalWriteAlias = string.Format(_lexicalChannel.Channel.Options.IndexFormat, "latest");
		var totalLexical = await CountAsync(lexicalWriteAlias, "{}", ctx);
		var totalSemantic = await CountAsync(semanticWriteAlias, "{}", ctx);

		// TODO emit these as metrics
		_logger.LogInformation("Document counts -> Semantic Index: {TotalSemantic}, Lexical Index: {TotalLexical}", totalSemantic, totalLexical);
	}

	private async ValueTask DoDeleteByQuery(string lexicalWriteAlias, Cancel ctx)
	{
		// delete all documents with batch_index_date < _batchIndexDate
		// they weren't part of the current export
		_logger.LogInformation("Delete data in '{SourceIndex}' not part of batch date: {Date}", lexicalWriteAlias, _batchIndexDate.ToString("o"));
		var request = PostData.String(@"
		{
			""query"": {
				""range"": {
					""batch_index_date"": {
						""lt"": """ + _batchIndexDate.ToString("o") + @"""
					}
				}
			}
		}");
		var reindexUrl = $"/{lexicalWriteAlias}/_delete_by_query?wait_for_completion=false";
		var deleteOldLexicalDocs = await _transport.PostAsync<DynamicResponse>(reindexUrl, request, ctx);
		var taskId = deleteOldLexicalDocs.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_collector.EmitGlobalError($"Failed to delete data in '{lexicalWriteAlias}' not part of batch date: {_batchIndexDate:o}");
			_logger.LogError("Failed to delete data to '{LexicalWriteAlias}' {Response}", lexicalWriteAlias, deleteOldLexicalDocs);
			return;
		}
		_logger.LogInformation("_delete_by_query task id: {TaskId}", taskId);
		bool completed;
		do
		{
			var reindexTask = await _transport.GetAsync<DynamicResponse>($"/_tasks/{taskId}", ctx);
			completed = reindexTask.Body.Get<bool>("completed");
			var total = reindexTask.Body.Get<int>("task.status.total");
			var updated = reindexTask.Body.Get<int>("task.status.updated");
			var created = reindexTask.Body.Get<int>("task.status.created");
			var deleted = reindexTask.Body.Get<int>("task.status.deleted");
			var batches = reindexTask.Body.Get<int>("task.status.batches");
			var runningTimeInNanos = reindexTask.Body.Get<long>("task.running_time_in_nanos");
			var time = TimeSpan.FromMicroseconds(runningTimeInNanos / 1000);
			_logger.LogInformation("_delete_by_query '{SourceIndex}': {RunningTimeInNanos} Documents {Total}: {Updated} updated, {Created} created, {Deleted} deleted, {Batches} batches",
				lexicalWriteAlias, time.ToString(@"hh\:mm\:ss"), total, updated, created, deleted, batches);
			if (!completed)
				await Task.Delay(TimeSpan.FromSeconds(5), ctx);

		} while (!completed);
	}

	private async ValueTask DoReindex(PostData request, string lexicalWriteAlias, string semanticWriteAlias, string typeOfSync, Cancel ctx)
	{
		var reindexUrl = "/_reindex?wait_for_completion=false&scroll=10m";
		var reindexNewChanges = await _transport.PostAsync<DynamicResponse>(reindexUrl, request, ctx);
		var taskId = reindexNewChanges.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_logger.LogError("Failed to reindex {Type} data to '{SemanticWriteAlias}' {Response}", typeOfSync, semanticWriteAlias, reindexNewChanges);
			_collector.EmitGlobalError($"Failed to reindex {typeOfSync} data to '{semanticWriteAlias}'");
			return;
		}
		_logger.LogInformation("_reindex {Type} task id: {TaskId}", typeOfSync, taskId);
		bool completed;
		do
		{
			var reindexTask = await _transport.GetAsync<DynamicResponse>($"/_tasks/{taskId}", ctx);
			completed = reindexTask.Body.Get<bool>("completed");
			var total = reindexTask.Body.Get<int>("task.status.total");
			var updated = reindexTask.Body.Get<int>("task.status.updated");
			var created = reindexTask.Body.Get<int>("task.status.created");
			var deleted = reindexTask.Body.Get<int>("task.status.deleted");
			var batches = reindexTask.Body.Get<int>("task.status.batches");
			var runningTimeInNanos = reindexTask.Body.Get<long>("task.running_time_in_nanos");
			var time = TimeSpan.FromMicroseconds(runningTimeInNanos / 1000);
			_logger.LogInformation("_reindex {Type}: {RunningTimeInNanos} '{SourceIndex}' => '{DestinationIndex}'. Documents {Total}: {Updated} updated, {Created} created, {Deleted} deleted, {Batches} batches",
				typeOfSync, time.ToString(@"hh\:mm\:ss"), lexicalWriteAlias, semanticWriteAlias, total, updated, created, deleted, batches);
			if (!completed)
				await Task.Delay(TimeSpan.FromSeconds(5), ctx);

		} while (!completed);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_lexicalChannel.Dispose();
		_semanticChannel.Dispose();
		GC.SuppressFinalize(this);
	}
}

internal sealed record SynonymsSet
{
	[JsonPropertyName("synonyms_set")]
	public required List<SynonymRule> Synonyms { get; init; } = [];
}

internal sealed record SynonymRule
{
	public required string Id { get; init; }
	public required string Synonyms { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(SynonymsSet))]
[JsonSerializable(typeof(SynonymRule))]
internal sealed partial class SynonymSerializerContext : JsonSerializerContext;

internal sealed record QueryRuleset
{
	[JsonPropertyName("rules")]
	public required List<QueryRulesetRule> Rules { get; init; } = [];
}

internal sealed record QueryRulesetRule
{
	[JsonPropertyName("rule_id")]
	public required string RuleId { get; init; }

	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("criteria")]
	public required List<QueryRulesetCriteria> Criteria { get; init; } = [];

	[JsonPropertyName("actions")]
	public required QueryRulesetActions Actions { get; init; }
}

internal sealed record QueryRulesetCriteria
{
	[JsonPropertyName("type")]
	public required string Type { get; init; }

	[JsonPropertyName("metadata")]
	public required string Metadata { get; init; }

	[JsonPropertyName("values")]
	public required List<string> Values { get; init; } = [];
}

internal sealed record QueryRulesetActions
{
	[JsonPropertyName("ids")]
	public required List<string> Ids { get; init; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(QueryRuleset))]
[JsonSerializable(typeof(QueryRulesetRule))]
[JsonSerializable(typeof(QueryRulesetCriteria))]
[JsonSerializable(typeof(QueryRulesetActions))]
internal sealed partial class QueryRulesetSerializerContext : JsonSerializerContext;
