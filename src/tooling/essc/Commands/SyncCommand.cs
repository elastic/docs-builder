// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Threading.Channels;
using Elastic.Documentation.Indexing;
using Elastic.SiteSearch.Cli.ContentStack;
using Elastic.SiteSearch.Cli.Elasticsearch;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

internal sealed class SyncCommand(
	ContentStackClient client,
	SourcingConfiguration config,
	ILoggerFactory loggerFactory
)
{
	private const string StateFile = "sync-state.json";
	private const int LaneCount = 5;

	private static bool IsInteractive() =>
		string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) &&
		AnsiConsole.Profile.Capabilities.Interactive;

	/// <summary>
	/// Sync published entries from Contentstack and index to Elasticsearch.
	/// </summary>
	/// <remarks>
	/// Uses parallel per-content-type streams with automatic initial/delta detection.
	/// Safe to interrupt and resume — cursors are saved after every page.
	/// User-facing CLI documentation lives on <see cref="ContentStackCommands.Sync"/> (generated <c>contentstack sync</c>).
	/// Unless <paramref name="noAi"/> is set, after finalize a bounded generative AI enrichment pass runs on the semantic index
	/// (default <c>100</c> enrichment candidates per run; see <paramref name="maxAiDocs"/>).
	/// </remarks>
	/// <param name="cacheFolder">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="apiKey">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="endpoint">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="force">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="noAi">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="maxAiDocs">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="maxAiTime">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="noIndex">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="pagePer">See <see cref="ContentStackCommands.Sync"/>.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task Sync(
		string? cacheFolder = null,
		string? apiKey = null,
		Uri? endpoint = null,
		bool force = false,
		bool noAi = false,
		int? maxAiDocs = null,
		TimeSpan? maxAiTime = null,
		bool noIndex = false,
		int pagePer = 0,
		Cancel ct = default
	)
	{
		if (!AiEnrichmentBudget.TryValidateMaxTime(maxAiTime, out var maxAiTimeError))
		{
			await Console.Error.WriteLineAsync($"Error: --max-ai-time {maxAiTimeError}");
			await Console.Error.WriteLineAsync("Run 'essc contentstack sync --help' for usage.");
			Environment.Exit(2);
		}

		var store = new StateManager(cacheFolder);

		if (force)
			store.Delete(StateFile);

		var cursorMap = store.Load(StateFile, StateJsonContext.Default.SyncCursorMap)
			?? new SyncCursorMap();

		var cfg = ResolveEndpoint(endpoint, apiKey);

		AnsiConsole.MarkupLine("[aqua bold]Contentstack Parallel Sync[/]");
		AnsiConsole.MarkupLine($"[dim]Cache: {Markup.Escape(store.CacheFolder)}[/]");
		if (!noIndex)
			AnsiConsole.MarkupLine($"[dim]Elasticsearch: {Markup.Escape(cfg.Uri.ToString())}[/]");
		var pagesInfo = pagePer > 0 ? $" | Max pages/type: {pagePer}" : "";
		var indexInfo = noIndex ? " | [yellow]indexing disabled[/]" : "";
		AnsiConsole.MarkupLine($"[dim]Content types: {PageContentTypes.All.Length} | Lanes: {LaneCount}{pagesInfo}{indexInfo}[/]");

		var resumeCount = cursorMap.Cursors.Count(c => c.Value.PaginationToken != null);
		var deltaCount = cursorMap.Cursors.Count(c => c.Value.SyncToken != null && c.Value.PaginationToken == null);
		var freshCount = PageContentTypes.All.Length - cursorMap.Cursors.Count;

		if (resumeCount > 0 || deltaCount > 0)
		{
			AnsiConsole.MarkupLine(
				$"[dim]Fresh: [white]{freshCount}[/] | Delta: [white]{deltaCount}[/] | Resume: [white]{resumeCount}[/][/]");
		}

		AnsiConsole.WriteLine();

		SiteDocumentExporter? exporter = null;
		if (!noIndex)
		{
			var transport = ElasticsearchTransportFactory.Create(cfg);
			exporter = new SiteDocumentExporter(
				loggerFactory,
				cfg,
				transport,
				config.BuildType,
				config.ElasticsearchEnvironment,
				enableAiEnrichment: !noAi
			);

			if (!noAi)
				exporter.ConfigurePostSyncAiBatch(maxAiDocs, maxAiTime);

			if (IsInteractive())
			{
				await AnsiConsole.Status()
					.AutoRefresh(true)
					.Spinner(Spinner.Known.Dots)
					.StartAsync("[aqua]Bootstrapping Elasticsearch indices...[/]", async _ =>
					{
						await exporter.StartAsync(ct);
					});
			}
			else
			{
				AnsiConsole.MarkupLine("[aqua]Bootstrapping Elasticsearch indices...[/]");
				await exporter.StartAsync(ct);
			}

			AnsiConsole.MarkupLine($"[green]✓[/] Elasticsearch indices ready [dim]({exporter.Strategy})[/]");
			AnsiConsole.WriteLine();
		}

		try
		{
			var writeSemaphore = new SemaphoreSlim(1, 1);
			var channel = Channel.CreateUnbounded<string>();

			foreach (var contentType in PageContentTypes.All)
				_ = channel.Writer.TryWrite(contentType);
			channel.Writer.Complete();

			var runCounts = new Dictionary<string, int>();
			var indexedCounts = new Dictionary<string, int>();
			var skippedCounts = new Dictionary<string, int>();
			var duplicateCounts = new Dictionary<string, int>();
			// Concurrent: incremented from every lane as items are indexed, unlike the per-content-type
			// dictionaries above (each content type is only ever owned by one lane at a time).
			var localeCounts = new ConcurrentDictionary<string, int>();

			if (IsInteractive())
			{
				await AnsiConsole.Progress()
					.AutoRefresh(true)
					.AutoClear(false)
					.HideCompleted(true)
					.Columns(
						new SpinnerColumn(),
						new TaskDescriptionColumn { Alignment = Justify.Left },
						new ProgressBarColumn(),
						new PercentageColumn()
					)
					.StartAsync(async ctx =>
					{
						var overallTask = ctx.AddTask(
							$"[aqua]Overall[/] — [white]0[/]/{PageContentTypes.All.Length} types",
							maxValue: PageContentTypes.All.Length);

						var lanes = Enumerable.Range(0, LaneCount).Select(i =>
						{
							var laneTask = ctx.AddTask($"[dim]Lane {i + 1}: idle[/]", maxValue: 100);
							laneTask.IsIndeterminate = true;
							return RunLaneAsync(i + 1, laneTask, channel.Reader, cursorMap, runCounts, indexedCounts, skippedCounts,
								duplicateCounts, localeCounts, pagePer, exporter, store, writeSemaphore, overallTask, ct);
						}).ToArray();

						await Task.WhenAll(lanes);

						overallTask.Value = overallTask.MaxValue;
						overallTask.Description = $"[green]✓ All {PageContentTypes.All.Length} content types synced[/]";
					});
			}
			else
			{
				var lanes = Enumerable.Range(0, LaneCount).Select(i =>
					RunLaneAsync(i + 1, null, channel.Reader, cursorMap, runCounts, indexedCounts, skippedCounts,
						duplicateCounts, localeCounts, pagePer, exporter, store, writeSemaphore, null, ct)
				).ToArray();
				await Task.WhenAll(lanes);
				AnsiConsole.MarkupLine($"[green]✓[/] All {PageContentTypes.All.Length} content types synced");
			}

			if (exporter is not null)
			{
				if (IsInteractive())
				{
					await AnsiConsole.Status()
						.AutoRefresh(true)
						.Spinner(Spinner.Known.Dots)
						.StartAsync("[aqua]Finalizing…[/]", async ctx =>
						{
							exporter.OnSyncProgress = info => ctx.Status(SyncProgressConsole.FormatStatusMarkup(info));
							await exporter.FinalizeAsync(ct);
						});
				}
				else
				{
					AnsiConsole.MarkupLine("[aqua]Finalizing…[/] [dim](bulk ingest, rollover, reindex — progress below; flush details in logs)[/]");
					exporter.OnSyncProgress = info =>
					{
						if (info.Total == 0 && info.Label.StartsWith("Flush", StringComparison.Ordinal))
							return;
						AnsiConsole.MarkupLine(SyncProgressConsole.FormatStatusMarkup(info));
					};
					await exporter.FinalizeAsync(ct);
				}
				AnsiConsole.MarkupLine("[green]✓[/] Elasticsearch indices finalized");
			}

			AnsiConsole.WriteLine();
			DisplaySummary(cursorMap, runCounts, indexedCounts, skippedCounts, duplicateCounts, localeCounts, exporter, noIndex, store.CacheFolder);
		}
		finally
		{
			exporter?.Dispose();
		}
	}

	private ElasticsearchEndpoint ResolveEndpoint(Uri? endpoint, string? apiKey)
	{
		var cfg = config.Elasticsearch;
		if (endpoint is not null)
			cfg.Uri = endpoint;
		if (apiKey is not null)
		{
			cfg.ApiKey = apiKey;
			cfg.Username = null;
			cfg.Password = null;
		}
		return cfg;
	}

	private async Task RunLaneAsync(
		int laneId,
		ProgressTask? laneTask,
		ChannelReader<string> reader,
		SyncCursorMap cursorMap,
		Dictionary<string, int> runCounts,
		Dictionary<string, int> indexedCounts,
		Dictionary<string, int> skippedCounts,
		Dictionary<string, int> duplicateCounts,
		ConcurrentDictionary<string, int> localeCounts,
		int maxPages,
		SiteDocumentExporter? exporter,
		StateManager store,
		SemaphoreSlim writeSemaphore,
		ProgressTask? overallTask,
		Cancel ct
	)
	{
		await foreach (var contentType in reader.ReadAllAsync(ct))
		{
			if (laneTask is not null)
			{
				laneTask.IsIndeterminate = true;
				laneTask.Value = 0;
				laneTask.Description = $"[aqua]Lane {laneId}:[/] {Markup.Escape(contentType)}";
			}

			if (!cursorMap.Cursors.TryGetValue(contentType, out var cursor))
				cursor = new SyncCursorState();

			var isDelta = cursor.SyncToken != null && cursor.PaginationToken == null;
			var itemsThisType = 0;
			var indexedThisType = 0;
			var skippedThisType = 0;
			var duplicatesThisType = 0;
			var totalThisType = 0;

			// Contentstack's sync log can emit the exact same publish event twice within a single
			// pass (same uid, same eventAt, byte-identical payload) — exporting both races the same
			// document id across bulk batches and Elasticsearch rejects the loser with a 409. Track
			// paths already exported in this pass only (never persisted, cleared per content type)
			// so the second identical delivery is skipped instead of racing the first.
			var exportedPaths = new HashSet<string>(StringComparer.Ordinal);

			var progress = new Progress<SyncProgress>(p =>
			{
				if (p.TotalCount > 0)
					totalThisType = p.TotalCount;
				if (laneTask is null)
					return;
				if (p.TotalCount > 0)
				{
					laneTask.IsIndeterminate = false;
					laneTask.MaxValue = p.TotalCount;
					laneTask.Value = p.ItemsSoFar;
				}
				laneTask.Description =
					$"[aqua]Lane {laneId}:[/] {Markup.Escape(contentType)} [dim]({p.ItemsSoFar:N0})[/]";
			});

			async Task OnPage(SyncResponse response)
			{
				_ = Interlocked.Add(ref itemsThisType, response.Items.Count);

				if (exporter is not null)
				{
					foreach (var item in response.Items)
					{
						var doc = ContentStackMapper.ToSiteDocument(item);
						if (doc is null)
						{
							_ = Interlocked.Increment(ref skippedThisType);
							continue;
						}

						if (!exportedPaths.Add(doc.Path))
						{
							_ = Interlocked.Increment(ref duplicatesThisType);
							continue;
						}

						_ = localeCounts.AddOrUpdate(doc.Locale, 1, (_, c) => c + 1);
						await exporter.ExportAsync(doc, ct);
						_ = Interlocked.Increment(ref indexedThisType);
					}
				}

				await writeSemaphore.WaitAsync(ct);
				try
				{
					cursor.PaginationToken = response.PaginationToken;
					cursor.ItemsProcessed += response.Items.Count;
					cursorMap.Cursors[contentType] = cursor;
					store.Save(StateFile, cursorMap, StateJsonContext.Default.SyncCursorMap);
				}
				finally
				{
					_ = writeSemaphore.Release();
				}
			}

			var result = isDelta
				? await client.DeltaSyncAsync(
					cursor.SyncToken!,
					maxPages: maxPages,
					progress: progress,
					onPage: OnPage,
					ct: ct
				)
				: await client.InitialSyncAsync(
					resumePaginationToken: cursor.PaginationToken,
					contentTypeUid: cursor.PaginationToken == null ? contentType : null,
					maxPages: maxPages,
					progress: progress,
					onPage: OnPage,
					ct: ct
				);

			await writeSemaphore.WaitAsync(ct);
			try
			{
				cursor.SyncToken = result.SyncToken;
				// Only clear the pagination token when a terminal sync token was returned.
				// If --page-per stopped the run early, preserve it so the next run resumes
				// from the saved page rather than restarting from page 1.
				if (result.SyncToken is not null)
					cursor.PaginationToken = null;
				cursorMap.Cursors[contentType] = cursor;
				runCounts[contentType] = itemsThisType;
				indexedCounts[contentType] = indexedThisType;
				skippedCounts[contentType] = skippedThisType;
				duplicateCounts[contentType] = duplicatesThisType;
				store.Save(StateFile, cursorMap, StateJsonContext.Default.SyncCursorMap);
			}
			finally
			{
				_ = writeSemaphore.Release();
			}

			if (overallTask is not null)
			{
				overallTask.Increment(1);
				overallTask.Description =
					$"[aqua]Overall[/] — [white]{(int)overallTask.Value}[/]/{PageContentTypes.All.Length} types";
			}

			if (laneTask is not null)
				laneTask.Description = $"[green]✓ Lane {laneId}:[/] {Markup.Escape(contentType)} [dim]({itemsThisType:N0})[/]";
			else
			{
				var pct = totalThisType > 0 ? itemsThisType * 100 / totalThisType : 100;
				var skippedSuffix = skippedThisType > 0 ? $" [yellow]({skippedThisType:N0} skipped)[/]" : "";
				AnsiConsole.MarkupLine(
					$"[dim][[Lane {laneId}]][/]\t[dim]{pct,3}%[/]\t{Markup.Escape(contentType)} — {itemsThisType:N0} fetched, {indexedThisType:N0} indexed{skippedSuffix}");
			}
		}

		laneTask?.StopTask();
	}

	private static void DisplaySummary(
		SyncCursorMap cursorMap,
		Dictionary<string, int> runCounts,
		Dictionary<string, int> indexedCounts,
		Dictionary<string, int> skippedCounts,
		Dictionary<string, int> duplicateCounts,
		ConcurrentDictionary<string, int> localeCounts,
		SiteDocumentExporter? exporter,
		bool noIndex,
		string cacheFolder
	)
	{
		var withToken = cursorMap.Cursors.Count(c => c.Value.SyncToken != null);
		var totalProcessed = cursorMap.Cursors.Values.Sum(c => c.ItemsProcessed);
		var totalThisRun = runCounts.Values.Sum();
		var totalIndexed = indexedCounts.Values.Sum();
		var totalSkipped = skippedCounts.Values.Sum();
		var totalDuplicates = duplicateCounts.Values.Sum();

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.Title("[aqua]Sync Results[/]")
			.AddColumn("[aqua]Content Type[/]")
			.AddColumn(new TableColumn("[aqua]Fetched[/]").RightAligned())
			.AddColumn(new TableColumn("[aqua]Indexed[/]").RightAligned())
			.AddColumn(new TableColumn("[aqua]Skipped[/]").RightAligned())
			.AddColumn(new TableColumn("[aqua]Status[/]").Centered());

		foreach (var contentType in PageContentTypes.All)
		{
			var cursor = cursorMap.Cursors.GetValueOrDefault(contentType);
			var fetched = runCounts.GetValueOrDefault(contentType);
			var indexed = indexedCounts.GetValueOrDefault(contentType);
			var skipped = skippedCounts.GetValueOrDefault(contentType);

			var status = cursor?.SyncToken != null
				? "[green]✓[/]"
				: cursor?.PaginationToken != null
					? "[yellow]partial[/]"
					: "[grey]—[/]";

			var fetchedDisplay = fetched > 0 ? $"[green]+{fetched:N0}[/]" : "[dim]0[/]";
			var indexedDisplay = noIndex
				? "[dim]—[/]"
				: indexed > 0 ? $"[green]{indexed:N0}[/]" : "[dim]0[/]";
			var skippedDisplay = skipped > 0 ? $"[yellow]{skipped:N0}[/]" : "[dim]0[/]";

			_ = table.AddRow(
				new Markup(Markup.Escape(contentType)),
				new Markup(fetchedDisplay),
				new Markup(indexedDisplay),
				new Markup(skippedDisplay),
				new Markup(status)
			);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();

		if (!localeCounts.IsEmpty)
		{
			var localeTable = new Table()
				.Border(TableBorder.Rounded)
				.BorderColor(Color.Grey)
				.Title("[aqua]Observed Locales[/]")
				.AddColumn("[aqua]Locale[/]")
				.AddColumn(new TableColumn("[aqua]Count[/]").RightAligned());

			foreach (var (locale, count) in localeCounts.OrderByDescending(kvp => kvp.Value))
				_ = localeTable.AddRow(new Markup(Markup.Escape(locale)), new Markup($"[white]{count:N0}[/]"));

			AnsiConsole.Write(localeTable);
			AnsiConsole.WriteLine();
		}

		var summaryRows = new List<Markup>
		{
			new($"[green]Fetched this run:[/] [white]{totalThisRun:N0}[/]"),
		};

		if (!noIndex)
		{
			summaryRows.Add(new Markup($"[green]Indexed this run:[/] [white]{totalIndexed:N0}[/]"));
			if (totalSkipped > 0)
				summaryRows.Add(new Markup($"[yellow]Skipped (no URL/title):[/] [white]{totalSkipped:N0}[/]"));
			if (totalDuplicates > 0)
				summaryRows.Add(new Markup($"[yellow]Duplicate deliveries skipped (this pass):[/] [white]{totalDuplicates:N0}[/]"));
			if (exporter is not null)
			{
				if (exporter.ReindexTotal > 0)
				{
					var reindexMissed = exporter.ReindexTotal - exporter.ReindexProcessed;
					var reindexColor = reindexMissed > 0 || exporter.ReindexError is not null ? "yellow" : "green";
					var reindexLine = $"[{reindexColor}]Reindexed to semantic:[/] [white]{exporter.ReindexProcessed:N0}/{exporter.ReindexTotal:N0}[/]";
					if (reindexMissed > 0)
						reindexLine += $" [yellow]({reindexMissed:N0} missed)[/]";
					if (exporter.ReindexVersionConflicts > 0)
						reindexLine += $" [yellow]{exporter.ReindexVersionConflicts:N0} version conflicts[/]";
					summaryRows.Add(new Markup(reindexLine));
					if (exporter.ReindexError is { } reindexErr)
						summaryRows.Add(new Markup($"[red]Reindex error:[/] [white]{Markup.Escape(reindexErr)}[/]"));
				}
				if (exporter.RejectedCount > 0)
					summaryRows.Add(new Markup($"[red]ES rejections (4xx):[/] [white]{exporter.RejectedCount:N0}[/]"));
				if (exporter.FailedCount > 0)
					summaryRows.Add(new Markup($"[red]ES failures (timeout/retry):[/] [white]{exporter.FailedCount:N0}[/]"));
				SyncProgressConsole.AddBootstrapRows(summaryRows, "primary", exporter.PrimaryBootstrap);
				SyncProgressConsole.AddBootstrapRows(summaryRows, "secondary", exporter.SecondaryBootstrap);
			}
		}

		summaryRows.Add(new Markup($"[green]Total items processed:[/] [white]{totalProcessed:N0}[/]"));
		summaryRows.Add(new Markup($"[green]Content types with cursors:[/] [white]{withToken}[/]/{PageContentTypes.All.Length}"));
		summaryRows.Add(new Markup($"[dim]Cache: {Markup.Escape(cacheFolder)}[/]"));

		var summary = new Panel(new Rows(summaryRows))
		{
			Header = new PanelHeader("[aqua]Summary[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(summary);
	}
}
