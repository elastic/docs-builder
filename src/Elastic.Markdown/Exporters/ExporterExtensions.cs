// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Exporters;

public static class ExporterExtensions
{
	public static IReadOnlyCollection<IMarkdownExporter> CreateMarkdownExporters(
		this IReadOnlySet<Exporter> exportOptions,
		ILoggerFactory logFactory,
		IDocumentationConfigurationContext context,
		string indexNamespace
	)
	{
		var markdownExporters = new List<IMarkdownExporter>(3);
		if (exportOptions.Contains(Exporter.LLMText))
			markdownExporters.Add(new LlmMarkdownExporter());
		if (exportOptions.Contains(Exporter.Configuration))
			markdownExporters.Add(new ConfigurationExporter(logFactory, context.ConfigurationFileProvider, context));
		if (exportOptions.Contains(Exporter.Elasticsearch))
			markdownExporters.Add(new ElasticsearchMarkdownSemanticExporter(logFactory, context.Collector, indexNamespace, context.Endpoints));
		if (exportOptions.Contains(Exporter.ElasticsearchNoSemantic))
			markdownExporters.Add(new ElasticsearchMarkdownExporter(logFactory, context.Collector, indexNamespace, context.Endpoints));
		return markdownExporters;
	}
}

