// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.CompilerServices;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Indexing;

/// <summary>
/// Runs bounded generative AI enrichment against an <see cref="AiEnrichmentOrchestrator"/>, shared by
/// docs-builder's post-index step and essc's post-sync batch / standalone <c>ai</c> commands.
/// </summary>
public static class AiEnrichmentRunner
{
	/// <summary>
	/// Runs enrichment for the secondary (semantic) alias of an <see cref="OrchestratorContext{TDoc}"/>, typically wired
	/// as an <c>IncrementalSyncOrchestrator&lt;TDoc&gt;.OnPostComplete</c> callback after indexing finishes.
	/// </summary>
	/// <param name="aiEnrichment">The orchestrator to run enrichment through.</param>
	/// <param name="context">The completed sync's orchestrator context, providing the secondary write alias.</param>
	/// <param name="budget">Document/time budget for this run.</param>
	/// <param name="logger">Logger for phase progress and skip/timeout diagnostics.</param>
	/// <param name="ct">Ambient cancellation token.</param>
	/// <param name="onProgress">Optional callback invoked for every reported <see cref="AiEnrichmentProgress"/>.</param>
	public static async Task RunPostSyncAsync<TDoc>(
		AiEnrichmentOrchestrator aiEnrichment,
		OrchestratorContext<TDoc> context,
		AiEnrichmentBudget budget,
		ILogger logger,
		CancellationToken ct,
		Action<AiEnrichmentProgress>? onProgress = null
	)
		where TDoc : class
	{
		var alias = context.SecondaryWriteAlias;
		if (string.IsNullOrEmpty(alias))
		{
			logger.LogWarning("Post-sync AI enrichment skipped: no secondary write alias on orchestrator context.");
			return;
		}

		using var deadline = AiEnrichmentDeadline.Create(budget.MaxTime, ct);

		logger.LogInformation(
			"Starting post-sync AI enrichment for {Alias} (max {MaxDocs} documents per run)...",
			alias,
			budget.EffectiveMaxDocs);

		var options = new AiEnrichmentOptions
		{
			CompletionTimeout = AiEnrichmentDefaults.CompletionTimeout,
			CompletionMaxRetries = AiEnrichmentDefaults.CompletionMaxRetries,
			MaxEnrichmentsPerRun = budget.EffectiveMaxDocs,
		};

		await foreach (var p in aiEnrichment.EnrichAsync(alias, options, deadline.Token).ConfigureAwait(false))
		{
			logger.LogInformation(
				"[AI enrichment] {Phase}: enriched={Enriched} failed={Failed} candidates={Candidates}{Message}",
				p.Phase,
				p.Enriched,
				p.Failed,
				p.TotalCandidates,
				p.Message is not null ? $" — {p.Message}" : "");
			onProgress?.Invoke(p);
		}
	}

	/// <summary>
	/// Runs enrichment against an explicit alias with an optional document cap and no wall-clock deadline of its own —
	/// used by standalone <c>ai</c> commands that build their own deadline via <see cref="AiEnrichmentDeadline"/> up front.
	/// </summary>
	/// <param name="aiEnrichment">The orchestrator to run enrichment through.</param>
	/// <param name="alias">The semantic index alias to enrich.</param>
	/// <param name="maxDocs">Maximum documents to enrich; <c>0</c> or less means no cap.</param>
	/// <param name="ct">Cancellation token — pass an <see cref="AiEnrichmentDeadline"/>'s <c>Token</c> to bound by wall clock.</param>
	public static async IAsyncEnumerable<AiEnrichmentProgress> EnrichAsync(
		AiEnrichmentOrchestrator aiEnrichment,
		string alias,
		int maxDocs,
		[EnumeratorCancellation] CancellationToken ct = default
	)
	{
		var options = new AiEnrichmentOptions
		{
			CompletionTimeout = AiEnrichmentDefaults.CompletionTimeout,
			CompletionMaxRetries = AiEnrichmentDefaults.CompletionMaxRetries,
		};
		if (maxDocs > 0)
			options.MaxEnrichmentsPerRun = maxDocs;

		await foreach (var p in aiEnrichment.EnrichAsync(alias, options, ct))
			yield return p;
	}
}
