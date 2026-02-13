// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Elastic.Codex.Building;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Codex.Indexing;

/// <summary>
/// Service for indexing codex documentation into Elasticsearch.
/// Configures ES endpoint options using the shared <see cref="ElasticsearchEndpointConfigurator"/>
/// and delegates to <see cref="CodexBuildService.BuildAll"/> with the Elasticsearch exporter.
/// </summary>
public class CodexIndexService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	IsolatedBuildService isolatedBuildService
) : IService
{
	/// <summary>
	/// Index codex documentation to Elasticsearch.
	/// </summary>
	public async Task<bool> Index(
		CodexContext codexContext,
		CodexCloneResult cloneResult,
		FileSystem fileSystem,
		ElasticsearchIndexOptions esOptions,
		Cancel ctx = default)
	{
		var cfg = configurationContext.Endpoints.Elasticsearch;
		await ElasticsearchEndpointConfigurator.ApplyAsync(cfg, esOptions, codexContext.Collector, fileSystem, ctx);

		var exporters = new HashSet<Exporter> { Exporter.Elasticsearch };
		var buildService = new CodexBuildService(logFactory, configurationContext, isolatedBuildService);
		var result = await buildService.BuildAll(codexContext, cloneResult, fileSystem, ctx, exporters);
		return result.DocumentationSets.Count > 0;
	}
}
