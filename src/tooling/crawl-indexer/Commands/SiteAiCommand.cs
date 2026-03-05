// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using CrawlIndexer.Display;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Exporters.Elasticsearch;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CrawlIndexer.Commands;

/// <summary>
/// Run AI enrichment on existing site indices without re-crawling.
/// </summary>
public class SiteAiCommand(
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Run AI enrichment against the current site semantic index.
	/// </summary>
	/// <param name="maxRunTime">Maximum run time in minutes (0 = unlimited)</param>
	/// <param name="maxRunDocs">Maximum documents to enrich (0 = unlimited)</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("")]
	public async Task Enrich(int maxRunTime = 0, int maxRunDocs = 0, Cancel ctx = default)
	{
		SpectreConsoleTheme.WriteHeader("site ai");

		var endpoints = configurationContext.Endpoints;
		var transport = ElasticsearchTransportFactory.Create(endpoints.Elasticsearch);
		var buildType = endpoints.BuildType;
		var environment = endpoints.Environment;

		using var timeoutCts = maxRunTime > 0 ? new CancellationTokenSource(TimeSpan.FromMinutes(maxRunTime)) : null;
		using var linkedCts = timeoutCts is not null
			? CancellationTokenSource.CreateLinkedTokenSource(ctx, timeoutCts.Token)
			: null;
		var effectiveToken = linkedCts?.Token ?? ctx;

		if (maxRunTime > 0)
			SpectreConsoleTheme.WriteInfo($"Time limit: [yellow]{maxRunTime}[/] minutes");
		if (maxRunDocs > 0)
			SpectreConsoleTheme.WriteInfo($"Document limit: [yellow]{maxRunDocs:N0}[/] documents");

		_ = diagnostics.StartAsync(ctx);

		try
		{
			using var exporter = new SiteIndexerExporter(
				loggerFactory,
				diagnostics,
				errorTracker,
				endpoints.Elasticsearch,
				transport,
				buildType,
				environment,
				configurationContext.SearchConfiguration,
				enableAiEnrichment: true
			);

			await AnsiConsole.Status()
				.AutoRefresh(true)
				.Spinner(Spinner.Known.Dots)
				.StartAsync("[aqua]Bootstrapping Elasticsearch indices...[/]", async _ =>
				{
					await exporter.StartAsync(effectiveToken);
				});

			var aiResult = await RunAiEnrichmentWithProgressAsync(exporter, maxRunDocs, effectiveToken);

			DisplayAiSummary(aiResult, maxRunTime, maxRunDocs);
		}
		catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
		{
			SpectreConsoleTheme.WriteWarning("AI enrichment stopped — time limit reached");
		}
		finally
		{
			await diagnostics.StopAsync(ctx);
			IndexingDisplay.DisplayErrorSummary(diagnostics);
		}
	}

	private static async Task<AiEnrichmentResult> RunAiEnrichmentWithProgressAsync(
		SiteIndexerExporter exporter,
		int maxRunDocs,
		CancellationToken ct
	)
	{
		SpectreConsoleTheme.WriteSection("AI Enrichment");

		var sw = System.Diagnostics.Stopwatch.StartNew();
		Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentProgress? last = null;
		var enrichedCount = 0;

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
				var task = progressCtx.AddTask("[purple]🧠 Discovering candidates...[/]", maxValue: 100);
				task.IsIndeterminate = true;

				await foreach (var p in exporter.RunAiEnrichmentAsync(maxRunDocs, ct))
				{
					last = p;

					switch (p.Phase)
					{
						case Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentPhase.Querying when p.TotalCandidates > 0:
							var effectiveMax = maxRunDocs > 0
								? Math.Min(p.TotalCandidates, maxRunDocs)
								: p.TotalCandidates;
							task.IsIndeterminate = false;
							task.MaxValue = effectiveMax;
							task.Value = 0;
							task.Description = $"[purple]🧠 Found {p.TotalCandidates:N0} candidates[/]"
								+ (maxRunDocs > 0 ? $" [dim](limit: {maxRunDocs:N0})[/]" : "");
							break;
						case Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentPhase.Enriching:
							enrichedCount = p.Enriched;
							task.Value = p.Enriched + p.Failed;
							var failSuffix = p.Failed > 0 ? $" [red]({p.Failed} failed)[/]" : "";
							task.Description = $"[purple]🧠 Enriching[/] [dim]{p.Enriched:N0}/{p.TotalCandidates:N0}[/]{failSuffix}";
							break;
						case Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentPhase.Refreshing:
							task.Value = task.MaxValue;
							task.Description = "[purple]🧠 Refreshing lookup index...[/]";
							break;
						case Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentPhase.ExecutingPolicy:
							task.Description = "[purple]🧠 Executing enrich policy...[/]";
							break;
						case Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentPhase.Backfilling:
							task.Description = $"[purple]🧠 Backfilling {p.Enriched:N0} docs...[/]";
							break;
						case Elastic.Ingest.Elasticsearch.Enrichment.AiEnrichmentPhase.Complete:
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
			});

		sw.Stop();

		return new AiEnrichmentResult(
			last?.Enriched ?? 0,
			last?.Failed ?? 0,
			last?.TotalCandidates ?? 0,
			sw.Elapsed
		);
	}

	private static void DisplayAiSummary(AiEnrichmentResult result, int maxRunTime, int maxRunDocs)
	{
		AnsiConsole.WriteLine();

		var rows = new List<Spectre.Console.Rendering.IRenderable>();

		var aiGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = aiGrid.AddRow(
			new Markup("[purple]🧠 Candidates[/]"),
			new Markup($"[white]{result.TotalCandidates:N0}[/]")
		);

		if (result.Enriched > 0)
		{
			_ = aiGrid.AddRow(
				new Markup("[green]   ✓ Enriched[/]"),
				new Markup($"[white]{result.Enriched:N0}[/]")
			);
		}

		if (result.Failed > 0)
		{
			_ = aiGrid.AddRow(
				new Markup("[red]   ✗ Failed[/]"),
				new Markup($"[white]{result.Failed:N0}[/]")
			);
		}

		_ = aiGrid.AddRow(
			new Markup("[dim]   ⏱ Duration[/]"),
			new Markup($"[white]{result.Duration:hh\\:mm\\:ss}[/]")
		);

		rows.Add(aiGrid);

		if (maxRunTime > 0 || maxRunDocs > 0)
		{
			rows.Add(new Rule { Style = Style.Parse("grey") });
			if (maxRunTime > 0)
				rows.Add(new Markup($"[dim]Time limit: {maxRunTime} min[/]"));
			if (maxRunDocs > 0)
				rows.Add(new Markup($"[dim]Document limit: {maxRunDocs:N0}[/]"));
		}

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]✨ AI Enrichment Complete ✨[/]"),
			Border = BoxBorder.Double,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};
		AnsiConsole.Write(panel);
	}
}
