// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Indexing;
using Elastic.Documentation.Services;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Elastic.Markdown.Exporters.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

namespace Elastic.Documentation.Assembler.Indexing;

/// <summary>
/// Runs generative AI enrichment against an already-indexed semantic alias, without a full assembler build.
/// </summary>
public class AssemblerAiEnrichService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<AssemblerAiEnrichService>();

	/// <summary>Enrich the existing semantic index for <paramref name="environment"/>; does not crawl, build, or index documents.</summary>
	public async Task<bool> AiEnrich(
		IDiagnosticsCollector collector,
		ScopedFileSystem readFs,
		ScopedFileSystem writeFs,
		ElasticsearchIndexOptions es,
		string? environment,
		Cancel ctx
	)
	{
		var cfg = configurationContext.Endpoints.Elasticsearch;
		await ElasticsearchEndpointConfigurator.ApplyAsync(cfg, es, collector, readFs, ctx);

		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, readFs, writeFs, null, null);

		using var exporter = new ElasticsearchMarkdownExporter(logFactory, collector, assembleContext.Endpoints, assembleContext);
		if (!exporter.AiEnrichmentEnabled)
		{
			_logger.LogWarning("AI enrichment is not enabled for this endpoint — pass --ai-enrichment or check configuration.");
			return true;
		}

		// Bootstraps and resolves the existing aliases only; never writes or deletes documents.
		await exporter.StartAsync(ctx);

		var budget = new AiEnrichmentBudget(cfg.MaxAiDocs, cfg.MaxAiTime);
		using var deadline = AiEnrichmentDeadline.Create(budget.MaxTime, ctx);
		AiEnrichmentProgress? last = null;
		try
		{
			await foreach (var p in exporter.RunAiEnrichmentAsync(budget.EffectiveMaxDocs, deadline.Token))
			{
				_logger.LogInformation(
					"[AI enrichment] {Phase}: enriched={Enriched} failed={Failed} candidates={Candidates}{Message}",
					p.Phase, p.Enriched, p.Failed, p.TotalCandidates, p.Message is not null ? $" — {p.Message}" : "");
				last = p;
			}
		}
		catch (OperationCanceledException) when (deadline.TimedOut)
		{
			_logger.LogWarning("AI enrichment stopped — time limit reached");
		}

		if (last is not null)
			_logger.LogInformation(
				"AI enrichment complete: {Enriched} enriched, {Failed} failed, {Candidates} candidates",
				last.Enriched, last.Failed, last.TotalCandidates);

		// Intentionally does not call exporter.StopAsync(): completing a zero-write incremental sync
		// would delete every document that wasn't re-written this run.
		return true;
	}
}
