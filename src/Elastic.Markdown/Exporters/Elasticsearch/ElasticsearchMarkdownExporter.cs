// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;
using NetEscapades.EnumGenerators;

namespace Elastic.Markdown.Exporters.Elasticsearch;

[EnumExtensions]
public enum IngestStrategy { Reindex, Multiplex }

public class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly IDiagnosticsCollector _collector;
	private readonly ILogger _logger;
	private readonly ElasticsearchLexicalExporter _lexicalChannel;
	private readonly ElasticsearchSemanticExporter _semanticChannel;

	private readonly ElasticsearchEndpoint _endpoint;

	private readonly DateTimeOffset _batchIndexDate = DateTimeOffset.UtcNow;
	private readonly DistributedTransport _transport;
	private IngestStrategy _indexStrategy;

	public ElasticsearchMarkdownExporter(
		ILoggerFactory logFactory,
		IDiagnosticsCollector collector,
		DocumentationEndpoints endpoints,
		string indexNamespace
	)
	{
		_collector = collector;
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		_endpoint = endpoints.Elasticsearch;
		_indexStrategy = IngestStrategy.Reindex;

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

		_lexicalChannel = new ElasticsearchLexicalExporter(logFactory, collector, es, indexNamespace, _transport);
		_semanticChannel = new ElasticsearchSemanticExporter(logFactory, collector, es, indexNamespace, _transport);
	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default)
	{
		_ = await _lexicalChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);

		var semanticIndex = _semanticChannel.Channel.IndexName;
		var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
		var semanticIndexHead = await _transport.HeadAsync(semanticWriteAlias, ctx);
		if (!semanticIndexHead.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogInformation("No semantic index exists yet, creating index {Index} for semantic search", semanticIndex);
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
			var semanticIndexPut = await _transport.PutAsync<StringResponse>(semanticIndex, PostData.String("{}"), ctx);
			if (!semanticIndexPut.ApiCallDetails.HasSuccessfulStatusCode)
				throw new Exception($"Failed to create index {semanticIndex}: {semanticIndexPut}");
			_ = await _semanticChannel.Channel.ApplyAliasesAsync(ctx);
			if (!_endpoint.ForceReindex)
			{
				_indexStrategy = IngestStrategy.Multiplex;
				_logger.LogInformation("Index strategy set to multiplex because {SemanticIndex} does not exist, pass --force-reindex to always use reindex", semanticIndex);
			}
		}
		_logger.LogInformation("Using {IndexStrategy} to sync lexical index to semantic index", _indexStrategy.ToStringFast(true));
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
		var semanticIndex = _semanticChannel.Channel.IndexName;

		var stopped = await _lexicalChannel.StopAsync(ctx);
		if (!stopped)
			throw new Exception($"Failed to stop {_lexicalChannel.GetType().Name}");

		await QueryIngestStatistics(lexicalWriteAlias, ctx);

		if (_indexStrategy == IngestStrategy.Multiplex)
		{
			if (!_endpoint.NoSemantic)
				_ = await _semanticChannel.StopAsync(ctx);
			else
				_logger.LogInformation("--no-semantic was specified when doing multiplex writes, not rolling over {SemanticIndex}", semanticIndex);

			// cleanup lexical index of old data
			await DoDeleteByQuery(lexicalWriteAlias, ctx);
			_ = await _lexicalChannel.RefreshAsync(ctx);
			_logger.LogInformation("Finish sync to semantic index using {IndexStrategy} strategy", _indexStrategy.ToStringFast(true));
			await QueryDocumentCounts(ctx);
			return;
		}

		if (_endpoint.NoSemantic)
		{
			_logger.LogInformation("--no-semantic was specified so exiting early before reindexing to {Index}", semanticIndex);
			return;
		}

		var semanticIndexHead = await _transport.HeadAsync(semanticWriteAlias, ctx);
		if (!semanticIndexHead.ApiCallDetails.HasSuccessfulStatusCode)
		{
			_logger.LogInformation("No semantic index exists yet, creating index {Index} for semantic search", semanticIndex);
			_ = await _semanticChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
			var semanticIndexPut = await _transport.PutAsync<StringResponse>(semanticIndex, PostData.String("{}"), ctx);
			if (!semanticIndexPut.ApiCallDetails.HasSuccessfulStatusCode)
				throw new Exception($"Failed to create index {semanticIndex}: {semanticIndexPut}");
			_ = await _semanticChannel.Channel.ApplyAliasesAsync(ctx);
		}

		_logger.LogInformation("_reindex updates: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, semanticWriteAlias);
		var request = PostData.String(@"
		{
			""dest"": {
				""index"": """ + semanticWriteAlias + @"""
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
		await DoReindex(request, lexicalWriteAlias, semanticWriteAlias, "updates", ctx);

		_logger.LogInformation("_reindex deletions: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, semanticWriteAlias);
		request = PostData.String(@"
		{
			""dest"": {
				""index"": """ + semanticWriteAlias + @"""
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
		await DoReindex(request, lexicalWriteAlias, semanticWriteAlias, "deletions", ctx);

		await DoDeleteByQuery(lexicalWriteAlias, ctx);

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
		var reindexUrl = "/_reindex?wait_for_completion=false&require_alias=true&scroll=10m";
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

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var file = fileContext.SourceFile;
		var url = file.Url;

		if (url is "/docs" or "/docs/404")
		{
			// Skip the root and 404 pages
			_logger.LogInformation("Skipping export for {Url}", url);
			return true;
		}

		IPositionalNavigation navigation = fileContext.DocumentationSet;

		// Remove the first h1 because we already have the title
		// and we don't want it to appear in the body
		var h1 = fileContext.Document.Descendants<HeadingBlock>().FirstOrDefault(h => h.Level == 1);
		if (h1 is not null)
			_ = fileContext.Document.Remove(h1);

		var body = LlmMarkdownExporter.ConvertToLlmMarkdown(fileContext.Document, fileContext.BuildContext);

		var headings = fileContext.Document.Descendants<HeadingBlock>()
			.Select(h => h.GetData("header") as string ?? string.Empty) // TODO: Confirm that 'header' data is correctly set for all HeadingBlock instances and that this extraction is reliable.
			.Where(text => !string.IsNullOrEmpty(text))
			.ToArray();

		var @abstract = !string.IsNullOrEmpty(body)
			? body[..Math.Min(body.Length, 400)] + " " + string.Join(" \n- ", headings)
			: string.Empty;

		var doc = new DocumentationDocument
		{
			Url = url,
			Title = file.Title,
			Body = body,
			StrippedBody = body.StripMarkdown(),
			Description = fileContext.SourceFile.YamlFrontMatter?.Description,
			Abstract = @abstract,
			Applies = fileContext.SourceFile.YamlFrontMatter?.AppliesTo,
			UrlSegmentCount = url.Split('/', StringSplitOptions.RemoveEmptyEntries).Length,
			Parents = navigation.GetParentsOfMarkdownFile(file).Select(i => new ParentDocument
			{
				Title = i.NavigationTitle,
				Url = i.Url
			}).Reverse().ToArray(),
			Headings = headings
		};

		var semanticHash = _semanticChannel.Channel.Options.ChannelHash;
		var lexicalHash = _lexicalChannel.Channel.Options.ChannelHash;
		var hash = HashedBulkUpdate.CreateHash(semanticHash, lexicalHash,
			doc.Url, doc.Body ?? string.Empty, string.Join(",", doc.Headings.OrderBy(h => h))
		);
		doc.Hash = hash;
		doc.LastUpdated = _batchIndexDate;
		doc.BatchIndexDate = _batchIndexDate;

		if (_indexStrategy == IngestStrategy.Multiplex)
			return await _lexicalChannel.TryWrite(doc, ctx) && await _semanticChannel.TryWrite(doc, ctx);
		return await _lexicalChannel.TryWrite(doc, ctx);
	}

	/// <inheritdoc />
	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx) => ValueTask.FromResult(true);

	/// <inheritdoc />
	public void Dispose()
	{
		_lexicalChannel.Dispose();
		_semanticChannel.Dispose();
		GC.SuppressFinalize(this);
	}
}
