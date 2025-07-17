// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Semantic;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Exporters;

public class ConfigurationExporter(ILoggerFactory logFactory, AssembleContext context) : IMarkdownExporter
{
	private readonly ILogger<ConfigurationExporter> _logger = logFactory.CreateLogger<ConfigurationExporter>();

	/// <inheritdoc />
	public ValueTask StartAsync(CancellationToken ctx = default) => default;

	/// <inheritdoc />
	public ValueTask StopAsync(CancellationToken ctx = default) => default;

	/// <inheritdoc />
	public ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, CancellationToken ctx) => default;

	/// <inheritdoc />
	public ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, CancellationToken ctx)
	{
		var fs = context.WriteFileSystem;
		var configFolder = fs.DirectoryInfo.New(Path.Combine(context.OutputDirectory.FullName, "config"));
		if (!configFolder.Exists)
			configFolder.Create();

		_logger.LogInformation("Exporting configuration");

		var assemblerConfig = context.ConfigurationFileProvider.AssemblerFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", assemblerConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(assemblerConfig.FullName, Path.Combine(configFolder.FullName, assemblerConfig.Name), true);

		var navigationConfig = context.ConfigurationFileProvider.NavigationFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", navigationConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(navigationConfig.FullName, Path.Combine(configFolder.FullName, navigationConfig.Name), true);

		var legacyUrlMappingsConfig = context.ConfigurationFileProvider.LegacyUrlMappingsFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", legacyUrlMappingsConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(legacyUrlMappingsConfig.Name, Path.Combine(configFolder.FullName, legacyUrlMappingsConfig.Name), true);

		var versionsConfig = context.ConfigurationFileProvider.VersionFile;
		_logger.LogInformation("Exporting {Name} to {ConfigFolder}", versionsConfig.Name, configFolder.FullName);
		context.WriteFileSystem.File.Copy(versionsConfig.Name, Path.Combine(configFolder.FullName, versionsConfig.Name), true);

		return default;
	}
}

public class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly DiagnosticsCollector _collector;
	private readonly SemanticIndexChannel<DocumentationDocument> _channel;
	private readonly ILogger<ElasticsearchMarkdownExporter> _logger;

	public ElasticsearchMarkdownExporter(ILoggerFactory logFactory, DiagnosticsCollector collector, string url, string apiKey)
	{
		_collector = collector;
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		var configuration = new ElasticsearchConfiguration(new Uri(url), new ApiKey(apiKey))
		{
			//Uncomment to see the requests with Fiddler
			ProxyAddress = "http://localhost:8866"
		};
		var transport = new DistributedTransport(configuration);
		//The max num threads per allocated node, from testing its best to limit our max concurrency
		//producing to this number as well
		var indexNumThreads = 8;
		var options = new SemanticIndexChannelOptions<DocumentationDocument>(transport)
		{
			BufferOptions =
			{
				OutboundBufferMaxSize = 100,
				ExportMaxConcurrency = indexNumThreads,
				ExportMaxRetries = 3
			},
			SerializerContext = SourceGenerationContext.Default,
			IndexFormat = "documentation-{0:yyyy.MM.dd.HHmmss}",
			IndexNumThreads = indexNumThreads,
			ActiveSearchAlias = "documentation",
			ExportExceptionCallback = e => _logger.LogError(e, "Failed to export document"),
			ServerRejectionCallback = items => _logger.LogInformation("Server rejection: {Rejection}", items.First().Item2),
			GetMapping = (inferenceId, _) => // language=json
			$$"""
				{
				  "properties": {
				    "title": { "type": "text" },
				    "body": {
				      "type": "text"
				    },
				    "abstract": {
				       "type": "semantic_text",
				       "inference_id": "{{inferenceId}}"
				    }
				  }
				}
				"""
		};
		_channel = new SemanticIndexChannel<DocumentationDocument>(options);
	}

	public async ValueTask StartAsync(Cancel ctx = default)
	{
		_logger.LogInformation($"Bootstrapping {nameof(SemanticIndexChannel<DocumentationDocument>)} Elasticsearch target for indexing");
		_ = await _channel.BootstrapElasticsearchAsync(BootstrapMethod.Failure, null, ctx);
	}

	public async ValueTask StopAsync(Cancel ctx = default)
	{
		_logger.LogInformation("Waiting to drain all inflight exports to Elasticsearch");
		var drained = await _channel.WaitForDrainAsync(null, ctx);
		if (!drained)
			_collector.EmitGlobalError("Elasticsearch export: failed to complete indexing in a timely fashion while shutting down");

		_logger.LogInformation("Refreshing target index {Index}", _channel.IndexName);
		var refreshed = await _channel.RefreshAsync(ctx);
		if (!refreshed)
			_logger.LogError("Refreshing target index {Index} did not complete successfully", _channel.IndexName);

		_logger.LogInformation("Applying aliases to {Index}", _channel.IndexName);
		var swapped = await _channel.ApplyAliasesAsync(ctx);
		if (!swapped)
			_collector.EmitGlobalError($"{nameof(ElasticsearchMarkdownExporter)} failed to apply aliases to index {_channel.IndexName}");
	}

	public void Dispose()
	{
		_channel.Complete();
		_channel.Dispose();
		GC.SuppressFinalize(this);
	}

	private async ValueTask<bool> TryWrite(DocumentationDocument document, Cancel ctx = default)
	{
		if (_channel.TryWrite(document))
			return true;

		if (await _channel.WaitToWriteAsync(ctx))
			return _channel.TryWrite(document);
		return false;
	}

	public async ValueTask<bool> ExportAsync(MarkdownExportFileContext fileContext, Cancel ctx)
	{
		var file = fileContext.SourceFile;
		var document = fileContext.Document;
		if (file.FileName.EndsWith(".toml", StringComparison.OrdinalIgnoreCase))
			return true;

		var url = file.Url;
		// integrations are too big, we need to sanitize the fieldsets and example docs out of these.
		if (url.Contains("/reference/integrations"))
			return true;

		// TODO!
		var body = fileContext.LLMText ??= "string.Empty";
		var doc = new DocumentationDocument
		{
			Title = file.Title,
			//Body = body,
			Abstract = !string.IsNullOrEmpty(body)
				? body[..Math.Min(body.Length, 400)]
				: string.Empty,
			Url = url
		};
		return await TryWrite(doc, ctx);
	}

	/// <inheritdoc />
	public async ValueTask<bool> FinishExportAsync(IDirectoryInfo outputFolder, Cancel ctx) => await _channel.RefreshAsync(ctx);
}
