// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Buffers;
using System.Text.Json;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Indices;
using Elastic.Transport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Search;

public record DocumentationDocument
{
	public string? Title { get; set; }
}

public class IngestCollector : IDisposable
{
	private readonly IndexChannel<DocumentationDocument> _channel;
	private readonly ILogger<IngestCollector> _logger;

	public IngestCollector(ILoggerFactory logFactory, string url, string apiKey)
	{
		_logger = logFactory.CreateLogger<IngestCollector>();
		var uri = new Uri(url);
		var moniker = $"{uri.Host}${Guid.NewGuid():N}";
		var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(moniker));
		var cloudId = $"name:{base64}";

		var pool = new CloudNodePool(cloudId, new ApiKey(apiKey));
		var configuration = new TransportConfiguration(pool);
		var transport = new DistributedTransport(configuration);
		var options = new IndexChannelOptions<DocumentationDocument>(transport)
		{
			IndexFormat = "documentation",
			ExportExceptionCallback = e => _logger.LogError(e, "Failed to export document"),
			ServerRejectionCallback = items => _logger.LogInformation("Server rejection: {Rejection}", items.First().Item2)
		};
		_channel = new IndexChannel<DocumentationDocument>(options);
	}

	public async ValueTask<bool> TryWrite(DocumentationDocument document, CancellationToken ctx = default)
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
}
