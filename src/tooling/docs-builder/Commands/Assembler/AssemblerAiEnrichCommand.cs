// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
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
	/// bounded generative AI enrichment pass on the semantic index. Does not crawl, build, or index documents —
	/// run <c>assemble</c> or <c>assembler index</c> first.
	/// </para>
	/// </remarks>
	/// <param name="environment">Named deployment target whose existing indices should be enriched.</param>
	/// <param name="maxRunDocs">Maximum documents to enrich; <c>0</c> means no cap.</param>
	/// <param name="maxRunTime">Optional wall-clock limit for the run (e.g. <c>10m</c>, <c>1h</c>); minimum 1 minute when set.</param>
	[CommandName("ai-enrich")]
	public async Task<int> AiEnrich(
		GlobalCliOptions _,
		[AsParameters] ElasticsearchIndexOptions es,
		string? environment = null,
		[Range(0, int.MaxValue)] int maxRunDocs = 0,
		[TimeSpanRange("1m", "24h")] TimeSpan? maxRunTime = null,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		var service = new AssemblerAiEnrichService(logFactory, configuration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(service,
			async (s, col, ctx) => await s.AiEnrich(col, readFs, writeFs, es, environment, maxRunDocs, maxRunTime, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
