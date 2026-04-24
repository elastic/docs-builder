// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation;
using Nullean.Argh.Parsing;
using static Elastic.Documentation.Exporter;

namespace Documentation.Builder.Arguments;

/// <summary>
/// Parses a comma-separated exporter list into <see cref="IReadOnlySet{T}"/>.
/// Use with <c>[ArgumentParser(typeof(ExporterParser))]</c> on command parameters.
/// </summary>
/// <remarks>
/// Accepted values: <c>html</c>, <c>es</c> / <c>elasticsearch</c>, <c>config</c>, <c>links</c>,
/// <c>state</c>, <c>llm</c> / <c>llmtext</c>, <c>redirect</c> / <c>redirects</c>, <c>metadata</c>,
/// <c>none</c>, <c>default</c>.
/// </remarks>
public class ExporterParser : IArgumentParser<IReadOnlySet<Exporter>>
{
	public bool TryParse(string raw, out IReadOnlySet<Exporter> result)
	{
		result = ExportOptions.Default;
		var set = new HashSet<Exporter>();
		var span = raw.AsSpan();
		var options = span.Split(',');
		foreach (var option in options)
		{
			var export = span[option].Trim().ToString().ToLowerInvariant() switch
			{
				"llm" => LLMText,
				"llmtext" => LLMText,
				"es" => Elasticsearch,
				"elasticsearch" => Elasticsearch,
				"html" => Html,
				"config" => Configuration,
				"links" => LinkMetadata,
				"state" => DocumentationState,
				"redirects" => Redirects,
				"redirect" => Redirects,
				"none" => null,
				"default" => AddDefaultReturnNull(set, ExportOptions.Default),
				"metadata" => AddDefaultReturnNull(set, ExportOptions.MetadataOnly),
				_ => throw new Exception($"Unknown exporter {span[option].Trim().ToString().ToLowerInvariant()}")
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
