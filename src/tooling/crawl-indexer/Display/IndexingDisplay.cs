// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Caching;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Diagnostics;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.Enrichment;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;

namespace CrawlIndexer.Display;

/// <summary>
/// Displays indexing progress and results in a rich format.
/// </summary>
public static class IndexingDisplay
{
	public static void DisplayDryRunResults(IReadOnlyList<string> urls)
	{
		SpectreConsoleTheme.WriteSection("Dry Run Results");

		if (urls.Count == 0)
		{
			AnsiConsole.MarkupLine("[yellow]No URLs matched the specified filters.[/]");
			return;
		}

		// Show breakdown chart by path
		var pathGroups = urls
			.Select(u => new Uri(u))
			.GroupBy(u =>
			{
				var segments = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
				return segments.Length > 0 ? segments[0] : "root";
			})
			.OrderByDescending(g => g.Count())
			.Take(10)
			.ToList();

		var breakdown = new BreakdownChart()
			.Width(60);

		var colors = new[] { Color.Green, Color.Yellow, Color.Aqua, Color.Blue, Color.Magenta1, Color.Orange1, Color.Cyan1, Color.Purple, Color.Teal, Color.Grey };
		var colorIndex = 0;

		foreach (var group in pathGroups)
			_ = breakdown.AddItem(group.Key, group.Count(), colors[colorIndex++ % colors.Length]);

		AnsiConsole.MarkupLine($"[aqua]📊 URL Distribution[/] [dim]({urls.Count:N0} total URLs)[/]");
		AnsiConsole.Write(breakdown);
		AnsiConsole.WriteLine();

		var panel = new Panel(
			new Markup($"[green bold]{urls.Count:N0}[/] [dim]URLs ready for crawling[/]")
		)
		{
			Header = new PanelHeader("[aqua]✓ Dry Run Complete[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("green"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
	}

	public static void DisplayFinalSummary(
		CrawlStats crawlStats,
		CrawlDecisionStats? decisionStats = null,
		AiEnrichmentResult? aiResult = null
	)
	{
		_ = decisionStats;

		AnsiConsole.WriteLine();

		var rows = new List<IRenderable>();

		// Crawling section grid
		var expectedCrawl = crawlStats.UrlsDiscovered;
		var actualCrawled = crawlStats.UrlsCrawled;
		var crawlDiff = expectedCrawl - actualCrawled;

		var crawlGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = crawlGrid.AddRow(
			new Markup("[aqua]🔍 Expected Crawl[/]"),
			new Markup($"[white]{expectedCrawl:N0}[/]")
		);
		_ = crawlGrid.AddRow(
			new Markup("[green]   Actually Crawled[/]"),
			new Markup($"[white]{actualCrawled:N0}[/]")
		);

		if (crawlDiff > 0)
		{
			if (crawlStats.UrlsSkipped > 0)
			{
				_ = crawlGrid.AddRow(
					new Markup("[grey]      ⊘ Skipped[/]"),
					new Markup($"[white]{crawlStats.UrlsSkipped:N0}[/]")
				);
			}
			if (crawlStats.UrlsUnavailable > 0)
			{
				_ = crawlGrid.AddRow(
					new Markup("[yellow]      ⚠ Unavailable[/]"),
					new Markup($"[white]{crawlStats.UrlsUnavailable:N0}[/]")
				);
			}
			if (crawlStats.UrlsFailed > 0)
			{
				_ = crawlGrid.AddRow(
					new Markup("[red]      ✗ Failed[/]"),
					new Markup($"[white]{crawlStats.UrlsFailed:N0}[/]")
				);
			}
		}

		rows.Add(crawlGrid);
		rows.Add(new Text(""));

		// Indexing section grid
		var expectedIndex = actualCrawled;
		var actualIndexed = crawlStats.UrlsIndexed;

		var indexGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = indexGrid.AddRow(
			new Markup("[yellow]📦 Expected Index[/]"),
			new Markup($"[white]{expectedIndex:N0}[/]")
		);
		_ = indexGrid.AddRow(
			new Markup("[green]   Actually Indexed[/]"),
			new Markup($"[white]{actualIndexed:N0}[/]")
		);

		if (crawlStats.IndexingErrors > 0)
		{
			_ = indexGrid.AddRow(
				new Markup("[red]      ✗ Failures[/]"),
				new Markup($"[white]{crawlStats.IndexingErrors:N0}[/]")
			);
		}

		rows.Add(indexGrid);

		// AI enrichment section
		if (aiResult is not null)
		{
			rows.Add(new Text(""));
			var aiGrid = new Grid()
				.AddColumn(new GridColumn().NoWrap().PadRight(2))
				.AddColumn(new GridColumn().NoWrap());

			_ = aiGrid.AddRow(
				new Markup("[purple]🧠 AI Candidates[/]"),
				new Markup($"[white]{aiResult.TotalCandidates:N0}[/]")
			);

			if (aiResult.Enriched > 0)
			{
				_ = aiGrid.AddRow(
					new Markup("[green]   ✓ Enriched[/]"),
					new Markup($"[white]{aiResult.Enriched:N0}[/]")
				);
			}

			if (aiResult.Failed > 0)
			{
				_ = aiGrid.AddRow(
					new Markup("[red]   ✗ Failed[/]"),
					new Markup($"[white]{aiResult.Failed:N0}[/]")
				);
			}

			_ = aiGrid.AddRow(
				new Markup("[dim]   ⏱ Duration[/]"),
				new Markup($"[white]{aiResult.Duration:hh\\:mm\\:ss}[/]")
			);

			rows.Add(aiGrid);
		}

		rows.Add(new Rule { Style = Style.Parse("grey") });

		// Metadata grid
		var metaGrid = new Grid()
			.AddColumn(new GridColumn().NoWrap().PadRight(2))
			.AddColumn(new GridColumn().NoWrap());

		_ = metaGrid.AddRow(
			new Markup("[cyan]📦 Downloaded[/]"),
			new Markup($"[white]{FormatBytes(crawlStats.BytesDownloaded)}[/]")
		);
		_ = metaGrid.AddRow(
			new Markup("[magenta]⏱  Duration[/]"),
			new Markup($"[white]{crawlStats.Elapsed:hh\\:mm\\:ss}[/]")
		);

		rows.Add(metaGrid);

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]✨ Crawl Complete ✨[/]"),
			Border = BoxBorder.Double,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);

		var hasErrors = crawlStats.UrlsFailed > 0 || crawlStats.IndexingErrors > 0;
		if (!hasErrors && crawlStats.UrlsCrawled > 0)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.Write(
				new FigletText("SUCCESS!")
					.Color(Color.Green)
					.Centered()
			);
		}
	}

	/// <summary>Runs finalization (reindex, cleanup) with Spectre.Console progress display.</summary>
	public static async Task RunFinalizationWithProgressAsync(
		SiteIndexerExporter exporter,
		Cancel ctx
	)
	{
		SpectreConsoleTheme.WriteSection(exporter.Strategy == IngestSyncStrategy.Reindex
			? "Syncing to Semantic Index"
			: "Finalizing Indices");

		var currentLabel = "";
		long currentTotal = 0;
		long currentProcessed = 0;
		var currentComplete = false;
		var syncLock = new Lock();

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
				var task = progressCtx.AddTask("[yellow]🔄 Draining buffers...[/]", maxValue: 100);
				task.IsIndeterminate = true;

				exporter.OnSyncProgress = info =>
				{
					lock (syncLock)
					{
						currentLabel = info.Label;
						currentTotal = info.Total;
						currentProcessed = info.Processed;
						currentComplete = info.IsComplete;
					}
				};

				using var cts = new CancellationTokenSource();
				var refreshTask = Task.Run(async () =>
				{
					while (!cts.Token.IsCancellationRequested)
					{
						try
						{
							await Task.Delay(200, cts.Token);
							lock (syncLock)
							{
								if (currentTotal <= 0)
									continue;

								task.IsIndeterminate = false;
								task.MaxValue = currentTotal;
								task.Value = currentProcessed;

								var desc = currentLabel switch
								{
									"reindex-updates" =>
										$"[yellow]🔄 Reindexing to semantic[/] [dim]{currentProcessed:N0}/{currentTotal:N0}[/]",
									"reindex-deletes" =>
										$"[yellow]🗑 Removing stale semantic docs[/] [dim]{currentProcessed:N0}/{currentTotal:N0}[/]",
									"primary-cleanup" =>
										$"[dim]🧹 Cleaning up primary[/] [dim]{currentProcessed:N0}/{currentTotal:N0}[/]",
									_ => $"[yellow]🔄 {Markup.Escape(currentLabel)}[/] [dim]{currentProcessed:N0}/{currentTotal:N0}[/]"
								};
								task.Description = desc;
							}
						}
						catch (OperationCanceledException)
						{
							break;
						}
					}
				}, cts.Token);

				try
				{
					await exporter.FinalizeAsync(ctx);
				}
				finally
				{
					await cts.CancelAsync();
					try
					{ await refreshTask; }
					catch (OperationCanceledException)
					{
						// Expected when the refresh task is canceled during finalization
					}
					exporter.OnSyncProgress = null;
				}

				task.IsIndeterminate = false;
				task.Value = task.MaxValue;
				task.Description = exporter.Strategy == IngestSyncStrategy.Reindex
					? "[green]✓ Semantic index synced[/]"
					: "[green]✓ Indices finalized[/]";
			});
	}

	/// <summary>Runs AI enrichment with Spectre.Console progress display.</summary>
	public static async Task<AiEnrichmentResult?> RunAiEnrichmentWithProgressAsync(
		SiteIndexerExporter exporter,
		Cancel ctx
	)
	{
		if (!exporter.AiEnrichmentEnabled)
			return null;

		SpectreConsoleTheme.WriteSection("AI Enrichment");

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
				var task = progressCtx.AddTask("[purple]🧠 Discovering candidates...[/]", maxValue: 100);
				task.IsIndeterminate = true;

				await foreach (var p in exporter.RunAiEnrichmentAsync(ctx: ctx))
				{
					last = p;
					switch (p.Phase)
					{
						case AiEnrichmentPhase.Querying when p.TotalCandidates > 0:
							task.IsIndeterminate = false;
							task.MaxValue = p.TotalCandidates;
							task.Value = 0;
							task.Description = $"[purple]🧠 Found {p.TotalCandidates:N0} candidates[/]";
							break;
						case AiEnrichmentPhase.Enriching:
							task.Value = p.Enriched + p.Failed;
							var failSuffix = p.Failed > 0 ? $" [red]({p.Failed} failed)[/]" : "";
							task.Description = $"[purple]🧠 Enriching[/] [dim]{p.Enriched:N0}/{p.TotalCandidates:N0}[/]{failSuffix}";
							break;
						case AiEnrichmentPhase.Refreshing:
							task.Value = p.Enriched;
							task.Description = "[purple]🧠 Refreshing lookup index...[/]";
							break;
						case AiEnrichmentPhase.ExecutingPolicy:
							task.Description = "[purple]🧠 Executing enrich policy...[/]";
							break;
						case AiEnrichmentPhase.Backfilling:
							task.Description = $"[purple]🧠 Backfilling {p.Enriched:N0} docs...[/]";
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
			});

		sw.Stop();

		return new AiEnrichmentResult(
			last?.Enriched ?? 0,
			last?.Failed ?? 0,
			last?.TotalCandidates ?? 0,
			sw.Elapsed
		);
	}

	public static void DisplayDryRunWithCacheStats(CrawlDecisionStats stats, int staleUrls)
	{
		SpectreConsoleTheme.WriteSection("Dry Run Results");

		var savingsPercent = stats.TotalUrls > 0
			? 100.0 * stats.UnchangedUrls / stats.TotalUrls
			: 0;

		List<IRenderable> rows =
		[
			new Markup($"[green]🆕 New URLs:[/] [white]{stats.NewUrls:N0}[/]"),
			new Markup($"[grey]✓ Unchanged (cached):[/] [white]{stats.UnchangedUrls:N0}[/]"),
			new Markup($"[yellow]🔄 To verify (HTTP):[/] [white]{stats.PossiblyChangedUrls:N0}[/]"),
			new Rule { Style = Style.Parse("grey") },
			new Markup($"[aqua]Total URLs:[/] [white]{stats.TotalUrls:N0}[/]"),
			new Markup($"[cyan]URLs to crawl:[/] [white]{stats.UrlsToCrawl:N0}[/]"),
			staleUrls > 0
				? new Markup($"[red]🗑 Stale (to delete):[/] [white]{staleUrls:N0}[/]")
				: new Markup("[dim]No stale URLs[/]"),
			new Rule { Style = Style.Parse("grey") },
			new Markup("[dim]Estimated HTTP savings:[/]"),
			new Markup($"[dim]  • Skipped requests: {stats.UnchangedUrls:N0} ({savingsPercent:F0}%)[/]")
		];

		var panel = new Panel(new Rows(rows))
		{
			Header = new PanelHeader("[aqua bold]📊 Crawl Analysis[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(panel);
	}

	public static void DisplayCacheLoadProgress(int loaded, string? currentUrl)
	{
		var shortUrl = currentUrl?.Length > 60 ? "..." + currentUrl[^57..] : currentUrl;
		AnsiConsole.MarkupLine($"[dim]Loaded {loaded:N0} docs[/] [grey]{Markup.Escape(shortUrl ?? "")}[/]");
	}

	/// <summary>
	/// Displays error summary using Errata-style formatting.
	/// </summary>
	public static void DisplayErrorSummary(IDiagnosticsCollector diagnostics)
	{
		if (diagnostics is CrawlIndexerDiagnosticsCollector crawlDiagnostics)
			crawlDiagnostics.WriteErrorsToConsole();
	}

	private static string FormatBytes(long bytes)
	{
		string[] sizes = ["B", "KB", "MB", "GB"];
		var order = 0;
		var size = (double)bytes;
		while (size >= 1024 && order < sizes.Length - 1)
		{
			order++;
			size /= 1024;
		}
		return $"{size:0.##} {sizes[order]}";
	}
}
