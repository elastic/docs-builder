// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Nullean.Argh.Parsing;

namespace Elastic.Documentation.Isolated;

/// <summary>
/// Parses a comma-separated list of exporter names into <see cref="IReadOnlySet{Exporter}"/>.
/// Supports short aliases and the shorthands <c>default</c> and <c>metadata</c>.
/// </summary>
/// <remarks>
/// Aliases: llm/llmtext, es/elasticsearch, html, config/configuration,
///          links/linkmetadata, state/documentationstate, redirect/redirects,
///          default (expands to <see cref="ExportOptions.Default"/>),
///          metadata (expands to <see cref="ExportOptions.MetadataOnly"/>),
///          none (empty set).
/// </remarks>
public class ExporterParser : IArgumentParser<IReadOnlySet<Exporter>>
{
	public bool TryParse(string raw, out IReadOnlySet<Exporter> result)
	{
		var set = new HashSet<Exporter>();
		foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			switch (token.ToLowerInvariant())
			{
				case "llm":
				case "llmtext":
					_ = set.Add(Exporter.LLMText);
					break;
				case "es":
				case "elasticsearch":
					_ = set.Add(Exporter.Elasticsearch);
					break;
				case "html":
					_ = set.Add(Exporter.Html);
					break;
				case "config":
				case "configuration":
					_ = set.Add(Exporter.Configuration);
					break;
				case "links":
				case "linkmetadata":
					_ = set.Add(Exporter.LinkMetadata);
					break;
				case "state":
				case "documentationstate":
					_ = set.Add(Exporter.DocumentationState);
					break;
				case "redirect":
				case "redirects":
					_ = set.Add(Exporter.Redirects);
					break;
				case "none":
					break;
				case "default":
					foreach (var e in ExportOptions.Default)
						_ = set.Add(e);
					break;
				case "metadata":
					foreach (var e in ExportOptions.MetadataOnly)
						_ = set.Add(e);
					break;
				default:
					throw new ArgumentException(
						$"Unknown exporter '{token}'. Valid values: html, llm, es, config, links, state, redirects, default, metadata, none.");
			}
		}
		result = set;
		return true;
	}
}
