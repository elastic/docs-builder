// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;
using static Elastic.Documentation.Exporter;

namespace Elastic.Documentation.Isolated;

public class IsolatedIndexService(
	ILoggerFactory logFactory,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
) : IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables)
{
	private readonly IConfigurationContext _configurationContext = configurationContext;

	/// <summary>Index a single documentation set to Elasticsearch.</summary>
	public async Task<bool> Index(
		IDiagnosticsCollector collector,
		ScopedFileSystem fileSystem,
		ElasticsearchIndexOptions es,
		string? path = null,
		Cancel ctx = default
	)
	{
		var cfg = _configurationContext.Endpoints.Elasticsearch;
		await ElasticsearchEndpointConfigurator.ApplyAsync(cfg, es, collector, fileSystem, ctx);

		return await Build(collector, fileSystem, new IsolatedBuildOptions
		{
			Path = path,
			MetadataOnly = true,
			Strict = false,
			Force = true,
			SkipApi = true,
			Exporters = new HashSet<Exporter> { Elasticsearch }
		}, ctx: ctx);
	}
}
