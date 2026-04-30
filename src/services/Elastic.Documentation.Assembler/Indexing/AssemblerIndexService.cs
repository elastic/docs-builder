// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;
using static Elastic.Documentation.Exporter;

namespace Elastic.Documentation.Assembler.Indexing;

public class AssemblerIndexService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
) : AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, environmentVariables)
{
	private readonly IConfigurationContext _configurationContext = configurationContext;

	/// <summary>Index assembled documentation to Elasticsearch.</summary>
	public async Task<bool> Index(
		IDiagnosticsCollector collector,
		ScopedFileSystem readFs,
		ScopedFileSystem writeFs,
		ElasticsearchIndexOptions es,
		string? environment = null,
		Cancel ctx = default
	)
	{
		var cfg = _configurationContext.Endpoints.Elasticsearch;
		await ElasticsearchEndpointConfigurator.ApplyAsync(cfg, es, collector, readFs, ctx);

		return await BuildAll(collector, new AssemblerBuildOptions
		{
			Strict = false,
			Environment = environment,
			MetadataOnly = true,
			ShowHints = false,
			Exporters = new HashSet<Exporter> { Elasticsearch },
			AssumeBuild = false
		}, readFs, writeFs, ctx);
	}
}
