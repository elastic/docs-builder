// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Search;
using Elastic.Markdown.Exporters;
using Elastic.Markdown.IO;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Indexing;

public class ElasticsearchMarkdownExporter(ILoggerFactory logFactory, string url, string apiKey) : IMarkdownExporter, IDisposable
{
	private readonly IngestCollector _ingestCollector = new(logFactory, url, apiKey);

	public void Dispose()
	{
		_ingestCollector.Dispose();
		GC.SuppressFinalize(this);
	}

	public async ValueTask<bool> Export(MarkdownFile file)
	{
		var doc = new DocumentationDocument
		{
			Title = file.Title,
		};
		return await _ingestCollector.TryWrite(doc);
	}
}
