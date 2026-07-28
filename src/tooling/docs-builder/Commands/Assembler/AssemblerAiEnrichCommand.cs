// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Indexing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssemblerAiEnrichCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>Run generative AI enrichment against an already-indexed semantic index, without a full assembler build.</summary>
	/// <remarks>
	/// <para>
	/// Bootstraps and resolves the existing Elasticsearch aliases for <paramref name="environment"/>, then runs a
	/// bounded generative AI enrichment pass on the semantic index, budgeted by <c>--max-ai-docs</c>/<c>--max-ai-time</c>
	/// (the same flags used by <c>index</c> and <c>assembler index</c>). Does not crawl, build, or index documents —
	/// run <c>assemble</c> or <c>assembler index</c> first.
	/// </para>
	/// </remarks>
	/// <param name="environment">Named deployment target whose existing indices should be enriched.</param>
	/// <param name="bootstrapOnly">Bootstrap the enrich policy, pipeline, and lookup index, then exit without enriching any documents.</param>
	[CommandName("ai-enrich")]
	public async Task<int> AiEnrich(
		GlobalCliOptions _,
		[AsParameters] ElasticsearchIndexOptions es,
		string? environment = null,
		bool bootstrapOnly = false,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		var service = new AssemblerAiEnrichService(logFactory, configuration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(service,
			async (s, col, ctx) => await s.AiEnrich(col, readFs, writeFs, es, environment, bootstrapOnly, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
