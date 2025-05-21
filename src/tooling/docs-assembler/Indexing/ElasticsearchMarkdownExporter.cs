// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;
using Elastic.Documentation.Serialization;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Elastic.Transport;
using Elastic.Transport.Products.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Indexing;

public class ElasticsearchMarkdownExporter : IMarkdownExporter, IDisposable
{
	private readonly IndexChannel<DocumentationDocument> _channel;
	private readonly ILogger<ElasticsearchMarkdownExporter> _logger;

	public ElasticsearchMarkdownExporter(ILoggerFactory logFactory, string url, string apiKey)
	{
		_logger = logFactory.CreateLogger<ElasticsearchMarkdownExporter>();
		var configuration = new ElasticsearchConfiguration(new Uri(url), new ApiKey(apiKey))
		{
			//Uncomment to see the requests with Fiddler
			//ProxyAddress = "http://localhost:8866"
		};
		var transport = new DistributedTransport(configuration);
		var options = new IndexChannelOptions<DocumentationDocument>(transport)
		{
			SerializerContext = SourceGenerationContext.Default,
			IndexFormat = "documentation",
			ExportExceptionCallback = e => _logger.LogError(e, "Failed to export document"),
			ServerRejectionCallback = items => _logger.LogInformation("Server rejection: {Rejection}", items.First().Item2)
		};
		_channel = new IndexChannel<DocumentationDocument>(options);
	}

	public async Task WaitForDrain()
	{
		_logger.LogInformation("Elasticsearch export: waiting for in flight exports");
		var drained = await _channel.WaitForDrainAsync();
		if (!drained)
			_logger.LogError("Elasticsearch export: failed to complete indexing in a timely fashion while shutting down");
	}

	private async ValueTask<bool> TryWrite(DocumentationDocument document, Cancel ctx = default)
	{
		if (_channel.TryWrite(document))
			return true;

		if (await _channel.WaitToWriteAsync(ctx))
			return _channel.TryWrite(document);
		return false;
	}

	public void Dispose()
	{
		_channel.Complete();
		_channel.Dispose();
		GC.SuppressFinalize(this);
	}

	public async ValueTask<bool> Export(MarkdownFile file)
	{
		var doc = new DocumentationDocument
		{
			Title = file.Title,
		};
		return await TryWrite(doc);
	}
}
