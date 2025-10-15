// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Ingest.Elasticsearch;
using Elastic.Markdown.Helpers;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters.Elasticsearch;

public class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly IDiagnosticsCollector _collector;
	private readonly ILogger _logger;
	private readonly ElasticsearchLexicalExporter _lexicalChannel;
	private readonly ElasticsearchSemanticExporter _semanticChannel;

	private readonly ElasticsearchEndpoint _endpoint;

	private readonly DateTimeOffset _batchIndexDate = DateTimeOffset.UtcNow;
	private readonly DistributedTransport _transport;

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

		var es = endpoints.Elasticsearch;

		var configuration = new ElasticsearchConfiguration(es.Uri)
		{
			Authentication = es.ApiKey is { } apiKey
				? new ApiKey(apiKey)
				: es is { Username: { } username, Password: { } password }
					? new BasicAuthentication(username, password)
					: null,
			EnableHttpCompression = true,
			DebugMode = _endpoint.DebugMode,
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

		_lexicalChannel = new ElasticsearchLexicalExporter(logFactory, collector, es, indexNamespace, _transport, _batchIndexDate);
		_semanticChannel = new ElasticsearchSemanticExporter(logFactory, collector, es, indexNamespace, _transport);

	}

	/// <inheritdoc />
	public async ValueTask StartAsync(Cancel ctx = default) =>
		await _lexicalChannel.Channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);

	/// <inheritdoc />
	public async ValueTask StopAsync(Cancel ctx = default)
	{
		var semanticWriteAlias = string.Format(_semanticChannel.Channel.Options.IndexFormat, "latest");
		var lexicalWriteAlias = string.Format(_lexicalChannel.Channel.Options.IndexFormat, "latest");

		var semanticIndex = _semanticChannel.Channel.IndexName;
		var semanticIndexHead = await _transport.HeadAsync(semanticWriteAlias, ctx);

		if (_endpoint.NoSemantic)
		{
			_logger.LogInformation("--no-semantic was specified so exiting early before syncing to {Index}", semanticIndex);
			return;
		}

		var stopped = await _lexicalChannel.StopAsync(ctx);
		if (!stopped)
			throw new Exception($"Failed to stop {_lexicalChannel.GetType().Name}");

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
		var request = PostData.Serializable(new
		{
			dest = new { index = semanticWriteAlias },
			source = new
			{
				index = lexicalWriteAlias,
				size = 100,
				query = new
				{
					range = new
					{
						last_updated = new { gte = _batchIndexDate.ToString("o") }
					}
				}
			}
		});
		await DoReindex(request, lexicalWriteAlias, semanticWriteAlias, "updates", ctx);

		_logger.LogInformation("_reindex deletions: '{SourceIndex}' => '{DestinationIndex}'", lexicalWriteAlias, semanticWriteAlias);
		request = PostData.Serializable(new
		{
			dest = new { index = semanticWriteAlias },
			script = new { source = "ctx.op = \"delete\"" },
			source = new
			{
				index = lexicalWriteAlias,
				size = 100,
				query = new
				{
					range = new
					{
						batch_index_date = new { lt = _batchIndexDate.ToString("o") }
					}
				}
			}
		});
		await DoReindex(request, lexicalWriteAlias, semanticWriteAlias, "deletions", ctx);

		await DoDeleteByQuery(lexicalWriteAlias, ctx);
	}

	private async ValueTask DoDeleteByQuery(string lexicalWriteAlias, Cancel ctx)
	{
		// delete all documents with batch_index_date < _batchIndexDate
		// they weren't part of the current export
		_logger.LogInformation("Delete data in '{SourceIndex}' not part of batch date: {Date}", lexicalWriteAlias, _batchIndexDate.ToString("o"));
		var request = PostData.Serializable(new
		{
			query = new
			{
				range = new
				{
					batch_index_date = new { lt = _batchIndexDate.ToString("o") }
				}
			}
		});
		var reindexUrl = $"/{lexicalWriteAlias}/_delete_by_query?wait_for_completion=false";
		var reindexNewChanges = await _transport.PostAsync<DynamicResponse>(reindexUrl, request, ctx);
		var taskId = reindexNewChanges.Body.Get<string>("task");
		if (string.IsNullOrWhiteSpace(taskId))
		{
			_collector.EmitGlobalError($"Failed to delete data in '{lexicalWriteAlias}' not part of batch date: {_batchIndexDate:o}");
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
