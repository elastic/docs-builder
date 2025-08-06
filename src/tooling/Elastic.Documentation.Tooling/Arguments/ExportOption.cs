// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Tooling.Exporters;
using Elastic.Markdown.Exporters;
using Microsoft.Extensions.Logging;
using static Elastic.Documentation.Exporter;

namespace Elastic.Documentation.Tooling.Arguments;

[AttributeUsage(AttributeTargets.Parameter)]
public class ExporterParserAttribute : Attribute, IArgumentParser<IReadOnlySet<Exporter>>
{
	public static bool TryParse(ReadOnlySpan<char> s, out IReadOnlySet<Exporter> result)
	{
		result = ExportOptions.Default;
		var set = new HashSet<Exporter>();
		var options = s.Split(',');
		foreach (var option in options)
		{
			var export = s[option].Trim().ToString().ToLowerInvariant() switch
			{
				"llm" => LLMText,
				"llmtext" => LLMText,
				"es" => Elasticsearch,
				"elasticsearch" => Elasticsearch,
				"html" => Html,
				"config" => Exporter.Configuration,
				"links" => LinkMetadata,
				"state" => DocumentationState,
				"redirects" => Redirects,
				"redirect" => Redirects,
				"none" => null,
				"default" => AddDefaultReturnNull(set, ExportOptions.Default),
				"metadata" => AddDefaultReturnNull(set, ExportOptions.MetadataOnly),
				_ => throw new Exception($"Unknown exporter {s[option].Trim().ToString().ToLowerInvariant()}")
			};
			if (export.HasValue)
				_ = set.Add(export.Value);
		}
		result = set;
		return true;
	}

	private static Exporter? AddDefaultReturnNull(HashSet<Exporter> set, HashSet<Exporter> defaultSet)
	{
		foreach (var option in defaultSet)
			_ = set.Add(option);
		return null;
	}
}

public static class ExporterExtensions
{

	public static IReadOnlyCollection<IMarkdownExporter> CreateMarkdownExporters(
		this IReadOnlySet<Exporter> exportOptions,
		ILoggerFactory logFactory,
		IDocumentationConfigurationContext context
	)
	{
		var esExporter = new ElasticsearchMarkdownExporter(logFactory, context.Collector, context.Endpoints);

		var markdownExporters = new List<IMarkdownExporter>(3);
		if (exportOptions.Contains(LLMText))
			markdownExporters.Add(new LlmMarkdownExporter());
		if (exportOptions.Contains(Exporter.Configuration))
			markdownExporters.Add(new ConfigurationExporter(logFactory, context.ConfigurationFileProvider, context));
		if (exportOptions.Contains(Elasticsearch))
			markdownExporters.Add(esExporter);
		return markdownExporters;
	}
}
