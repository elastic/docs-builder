// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

internal sealed record AiEnrichmentResult(int Enriched, int Failed, int TotalCandidates, TimeSpan Duration);

internal static class AiEnrichmentConsole
{
	internal static async Task<AiEnrichmentResult?> RunInteractiveAsync(
		bool aiEnrichmentEnabled,
		Func<int, CancellationToken, IAsyncEnumerable<AiEnrichmentProgress>> runEnrichment,
		int maxAiDocs,
		CancellationToken ct
	)
	{
		if (!aiEnrichmentEnabled)
		{
			AnsiConsole.MarkupLine("[grey]AI enrichment is not enabled.[/]");
			return null;
		}

		var sw = System.Diagnostics.Stopwatch.StartNew();
		AiEnrichmentProgress? last = null;

		await AnsiConsole.Progress()
			.AutoRefresh(true)
			.AutoClear(false)
			.HideCompleted(false)
			.Columns(
				new SpinnerColumn(),
				new TaskDescriptionColumn { Alignment = Justify.Left },
				new ProgressBarColumn(),
				new PercentageColumn()
			)
			.StartAsync(async progressCtx =>
			{
				var task = progressCtx.AddTask("[purple]Discovering candidates...[/]", maxValue: 100);
				task.IsIndeterminate = true;

				await foreach (var p in runEnrichment(maxAiDocs, ct).ConfigureAwait(false))
				{
					last = p;
					switch (p.Phase)
					{
						case AiEnrichmentPhase.Querying when p.TotalCandidates > 0:
							var effectiveMax = maxAiDocs > 0
								? Math.Min(p.TotalCandidates, maxAiDocs)
								: p.TotalCandidates;
							task.IsIndeterminate = false;
							task.MaxValue = effectiveMax;
							task.Value = 0;
							task.Description = $"[purple]Found {p.TotalCandidates:N0} candidates[/]"
								+ (maxAiDocs > 0 ? $" [dim](limit: {maxAiDocs:N0})[/]" : "");
							break;
						case AiEnrichmentPhase.Enriching:
							task.Value = p.Enriched + p.Failed;
							var failSuffix = p.Failed > 0 ? $" [red]({p.Failed} failed)[/]" : "";
							task.Description = $"[purple]Enriching[/] [dim]{p.Enriched:N0}/{p.TotalCandidates:N0}[/]{failSuffix}";
							break;
						case AiEnrichmentPhase.Refreshing:
							task.Value = task.MaxValue;
							task.Description = "[purple]Refreshing lookup index...[/]";
							break;
						case AiEnrichmentPhase.ExecutingPolicy:
							task.Description = "[purple]Executing enrich policy...[/]";
							break;
						case AiEnrichmentPhase.Backfilling:
							task.Description = $"[purple]Backfilling {p.Enriched:N0} docs...[/]";
							break;
						case AiEnrichmentPhase.Complete:
							task.Value = task.MaxValue;
							var completeFail = p.Failed > 0 ? $" [red]({p.Failed} failed)[/]" : "";
							task.Description = p.TotalCandidates > 0
								? $"[green]✓ AI enrichment complete[/] [dim]({p.Enriched:N0} enriched)[/]{completeFail}"
								: "[green]✓ AI enrichment complete[/] [dim](no new candidates)[/]";
							break;
					}
				}

				if (last is null)
				{
					task.IsIndeterminate = false;
					task.Value = task.MaxValue;
					task.Description = "[green]✓ AI enrichment complete[/] [dim](no new candidates)[/]";
				}
			})
			.ConfigureAwait(false);

		sw.Stop();

		return new AiEnrichmentResult(
			last?.Enriched ?? 0,
			last?.Failed ?? 0,
			last?.TotalCandidates ?? 0,
			sw.Elapsed
		);
	}

	internal static void DisplaySummary(AiEnrichmentResult? result, TimeSpan? maxAiTime, int maxAiDocs, string panelTitle = "[aqua]AI Enrichment Complete[/]")
	{
		if (result is null)
			return;

		AnsiConsole.WriteLine();

		var rows = new List<Spectre.Console.Rendering.IRenderable>();

		var aiGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = aiGrid.AddRow(
			new Markup("[purple]Candidates[/]"),
			new Markup($"[white]{result.TotalCandidates:N0}[/]")
		);

		if (result.Enriched > 0)
		{
			_ = aiGrid.AddRow(
				new Markup("[green]   Enriched[/]"),
				new Markup($"[white]{result.Enriched:N0}[/]")
			);
		}

		if (result.Failed > 0)
		{
			_ = aiGrid.AddRow(
				new Markup("[red]   Failed[/]"),
				new Markup($"[white]{result.Failed:N0}[/]")
			);
		}

		_ = aiGrid.AddRow(
			new Markup("[dim]   Duration[/]"),
			new Markup($"[white]{result.Duration:hh\\:mm\\:ss}[/]")
		);

		rows.Add(aiGrid);

		if (maxAiTime is { } || maxAiDocs > 0)
		{
			rows.Add(new Rule { Style = Style.Parse("grey") });
			if (maxAiTime is { } wall)
				rows.Add(new Markup($"[dim]Time limit: {wall}[/]"));
			if (maxAiDocs > 0)
				rows.Add(new Markup($"[dim]Document limit: {maxAiDocs:N0}[/]"));
		}

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader(panelTitle),
			Border = BoxBorder.Double,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};
		AnsiConsole.Write(panel);
	}
}
