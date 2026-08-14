// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Elastic.Codex;
using Elastic.Codex.Indexing;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands.Codex;

/// <summary>Index codex documentation into Elasticsearch.</summary>
internal sealed class CodexIndexCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>Index the built portal documentation into Elasticsearch.</summary>
	/// <remarks>
	/// <para>Run after <c>codex build</c>. Streams documents from all included documentation sets to the cluster.</para>
	/// </remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>

	[RequiresAuth]
	public async Task<int> Index(
		GlobalCliOptions _,
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		[AsParameters] ElasticsearchIndexOptions es,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new CodexFileSystem(config.FullName);
		if (!CodexConfigurationLoader.TryLoad(fs.ConfigurationFile, config.FullName, collector, out var codexConfig, out var environment))
			return 1;

		var codexContext = new CodexContext(codexConfig, fs.ConfigurationFile, collector, fs);

		var cloneResult = await CodexCloneService.DiscoverCheckouts(codexContext, logFactory, ct);

		if (cloneResult == null || cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder codex clone' first.");
			return 1;
		}

		var service = new CodexIndexService(logFactory, configurationContext);
		serviceInvoker.AddCommand(service, (codexContext, cloneResult, fs, es),
			static async (s, col, state, c) =>
				await s.Index(state.codexContext, state.cloneResult, state.fs, state.es, c)
		);

		return await serviceInvoker.InvokeAsync(ct);
	}
}
