// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Microsoft.Extensions.Logging;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

internal static class AiPostSyncBatch
{
	/// <summary>
	/// Runs bounded generative AI enrichment after <see cref="IncrementalSyncOrchestrator{T}.CompleteAsync"/> via <c>OnPostComplete</c>.
	/// </summary>
	public static async Task RunAsync<TDoc>(
		AiEnrichmentOrchestrator aiEnrichment,
		OrchestratorContext<TDoc> context,
		int maxEnrichmentsPerRun,
		TimeSpan? maxWallClock,
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

		using var timeoutCts = maxWallClock is { } d ? new CancellationTokenSource(d) : null;
		using var linkedCts = timeoutCts is not null
			? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
			: null;
		var effective = linkedCts?.Token ?? ct;

		logger.LogInformation(
			"Starting post-sync AI enrichment for {Alias} (max {MaxDocs} documents per run)...",
			alias,
			maxEnrichmentsPerRun);

		var options = new AiEnrichmentOptions
		{
			CompletionTimeout = TimeSpan.FromMinutes(5),
			CompletionMaxRetries = 2,
			MaxEnrichmentsPerRun = maxEnrichmentsPerRun,
		};

		await foreach (var p in aiEnrichment.EnrichAsync(alias, options, effective).ConfigureAwait(false))
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
}
