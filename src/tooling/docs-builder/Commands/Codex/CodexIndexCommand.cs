// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Actions.Core.Services;

using Elastic.Codex;
using Elastic.Codex.Indexing;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Codex;

/// <summary>Index codex documentation into Elasticsearch.</summary>
internal sealed class CodexIndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>Index the built portal documentation into Elasticsearch.</summary>
	/// <remarks>
	/// <para>Run after <c>codex build</c>. Streams documents from all included documentation sets to the cluster.</para>
	/// </remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>
	public async Task<int> Index(
		GlobalCliOptions _,
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		[AsParameters] ElasticsearchIndexOptions es,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealRead;
		var configFile = fs.FileInfo.New(config.FullName);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {config.FullName}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);

		if (string.IsNullOrWhiteSpace(codexConfig.Environment))
		{
			collector.EmitGlobalError("Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
			return 1;
		}

		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, null);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment);
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		var cloneResult = await cloneService.CloneAll(codexContext, fetchLatest: false, assumeCloned: true, ct);

		if (cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder codex clone' first.");
			return 1;
		}

		var isolatedBuildService = new IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		var service = new CodexIndexService(logFactory, configurationContext, isolatedBuildService);
		serviceInvoker.AddCommand(service, (codexContext, cloneResult, fs, es),
			static async (s, col, state, c) =>
				await s.Index(state.codexContext, state.cloneResult, state.fs, state.es, c)
		);

		return await serviceInvoker.InvokeAsync(ct);
	}
}
