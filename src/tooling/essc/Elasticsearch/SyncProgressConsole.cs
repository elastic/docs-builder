// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Ingest.Elasticsearch.Enrichment;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Elasticsearch;

/// <summary>Markup lines for <see cref="SyncProgressInfo"/> during <c>FinalizeAsync</c> (flush, rollover, reindex, delete-by-query).</summary>
internal static class SyncProgressConsole
{
	/// <summary>
	/// Opening finalize status before <c>CompleteAsync</c>. Even with zero crawl-time indexing, the orchestrator
	/// still drains channels and may run rollover, reindex, delete-by-query, and alias updates — so this can sit
	/// here until the next phase emits progress.
	/// </summary>
	public static string FinalizeStartingLabel(int bulkDocumentsAcknowledged) =>
		bulkDocumentsAcknowledged > 0
			? "Completing bulk ingest"
			: "No writes to Elasticsearch this run — finalizing (drain, rollover, reindex, aliases may still run)";

	/// <summary>Single-line status markup for Spectre <see cref="Spectre.Console.AnsiConsole.Status"/>.</summary>
	public static string FormatStatusMarkup(SyncProgressInfo info)
	{
		var label = Markup.Escape(info.Label);
		if (info.Total > 0)
		{
			var pct = (int)(info.Processed * 100 / Math.Max(1, info.Total));
			var prefix = info.IsComplete ? "[green]✓ [/]" : "";
			return
				$"{prefix}[aqua]{label}[/] [dim]—[/] [dim]{pct}%[/] [white]{info.Processed:N0}[/]/[dim]{info.Total:N0}[/]";
		}

		if (info.Processed > 0)
			return $"[aqua]{label}[/] [dim]—[/] [white]{info.Processed:N0}[/] [dim]docs acknowledged[/]";

		return $"[aqua]{label}[/] [dim]…[/]";
	}

	/// <summary>Maps generative AI enrichment streaming progress to <see cref="SyncProgressInfo"/> for the same status line as finalize.</summary>
	public static SyncProgressInfo FromAiProgress(AiEnrichmentProgress p)
	{
		var msg = p.Message is { } m ? $" ({m})" : "";
		var totalCandidates = p.TotalCandidates;
		var done = p.Enriched + p.Failed;

		return p.Phase switch
		{
			AiEnrichmentPhase.Querying => new SyncProgressInfo(
				$"AI enrichment — querying{msg}",
				totalCandidates,
				0,
				false),
			AiEnrichmentPhase.Enriching => new SyncProgressInfo(
				$"AI enrichment — enriching{msg}",
				Math.Max(1, totalCandidates),
				done,
				false),
			AiEnrichmentPhase.Complete => totalCandidates > 0
				? new SyncProgressInfo(
					$"AI enrichment — complete{msg}",
					totalCandidates,
					Math.Min(done, totalCandidates),
					true)
				: new SyncProgressInfo($"AI enrichment — complete{msg}", 1, 1, true),
			_ => new SyncProgressInfo(
				$"AI enrichment — {p.Phase}{msg}",
				0,
				p.Enriched,
				false)
		};
	}
}
