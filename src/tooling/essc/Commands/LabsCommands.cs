// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Indexing;
using Elastic.SiteSearch.Cli.Elasticsearch;
using Elastic.SiteSearch.Cli.LabsCrawl;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Spectre.Console;

namespace Elastic.SiteSearch.Cli.Commands;

/// <summary>
/// Options for <c>labs sync</c>, expanded from the CLI via Argh <c>[AsParameters]</c>.
/// </summary>
internal sealed class LabsSyncOptions
{
	/// <summary>Print crawl plan without fetching or indexing.</summary>
	public bool DryRun { get; set; }

	/// <summary>Override Elasticsearch API key (otherwise from configuration).</summary>
	public string? ApiKey { get; set; }

	/// <summary>Override Elasticsearch base URL (absolute URI).</summary>
	[Url]
	public Uri? Endpoint { get; set; }

	/// <summary>
	/// Comma-separated extra path segments to exclude after default labs exclusions.
	/// </summary>
	[StringLength(8192)]
	public string? ExcludePaths { get; set; }

	/// <summary>Use fair-queue scheduling when ordering crawl work.</summary>
	public bool Fair { get; set; }

	/// <summary>Bypass persisted Elasticsearch crawl cache and plan a full URL set.</summary>
	public bool Force { get; set; }

	/// <summary>Comma-separated language codes to filter URLs (for example <c>en,de</c>).</summary>
	[StringLength(512)]
	public string? Languages { get; set; }

	/// <summary>
	/// Max pages to crawl as a non-negative integer string; empty or omitted means no cap.
	/// </summary>
	[RegularExpression(@"^\d*$")]
	public string? MaxPages { get; set; }

	/// <summary>When <see langword="true"/>, skip ingest-time AI wiring and the post-sync generative enrichment batch.</summary>
	public bool NoAi { get; set; }

	/// <summary>Maximum documents to enrich in the post-sync AI batch per run; omit for default <c>100</c>. Must be at least 1 when specified.</summary>
	[Range(1, int.MaxValue)]
	public int? MaxAiDocs { get; set; }

	/// <summary>Optional wall-clock limit for the post-sync AI phase (minimum 1 minute when set).</summary>
	public TimeSpan? MaxAiTime { get; set; }

	/// <summary>
	/// Target requests-per-second cap as a non-negative integer string; empty means default rate limiter behavior.
	/// </summary>
	[RegularExpression(@"^\d*$")]
	public string? Rps { get; set; }

	/// <summary>Skip URLs that look unchanged compared to the incremental cache.</summary>
	public bool Unchanged { get; set; }
}

/// <summary>
/// Crawl elastic.co labs properties (search, security, observability) into <c>labs-*</c> Elasticsearch indices.
/// </summary>
/// <remarks>
/// Independent of Contentstack <c>contentstack</c> commands targeting <c>site-*</c> indices.
/// Discovery starts from published labs sitemap URLs; use <c>--dry-run</c> to validate discovery only.
/// </remarks>
internal sealed class LabsCommands(
	SourcingConfiguration config,
	ILoggerFactory loggerFactory,
	ISitemapParser sitemapParser,
	IAdaptiveCrawler crawler,
	CrawlerSettings crawlerSettings
)
{
	private static bool IsInteractive() =>
		string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) &&
		AnsiConsole.Profile.Capabilities.Interactive;

	/// <summary>
	/// Discover labs URLs from sitemaps, crawl HTML, and bulk-ingest into Elasticsearch.
	/// </summary>
	/// <remarks>
	/// Unless <c>--no-ai</c> is passed, after finalize this command runs a bounded generative AI enrichment pass on the
	/// semantic <c>labs-*</c> index: by default up to <c>100</c> documents that are candidates for enrichment (still missing
	/// or due for enrichment). Use <c>--max-ai-docs</c> to change that cap, or <c>--no-ai</c> to skip the post-sync batch entirely.
	/// </remarks>
	/// <param name="options">Crawl, cache, and Elasticsearch options.</param>
	/// <param name="ct">Cancellation token.</param>
	public async Task Sync([AsParameters] LabsSyncOptions options, Cancel ct = default)
	{
		var dryRun = options.DryRun;
		var apiKey = options.ApiKey;
		var endpoint = options.Endpoint;
		var excludePaths = options.ExcludePaths;
		var fair = options.Fair;
		var force = options.Force;
		var languages = options.Languages;
		var maxPages = ParseOptionalNonNegativeInt(options.MaxPages);
		var noAi = options.NoAi;
		var rps = ParseOptionalPositiveInt(options.Rps);
		var unchanged = options.Unchanged;

		if (!AiEnrichmentBudget.TryValidateMaxTime(options.MaxAiTime, out var maxAiTimeError))
		{
			await Console.Error.WriteLineAsync($"Error: --max-ai-time {maxAiTimeError}");
			await Console.Error.WriteLineAsync("Run 'essc labs sync --help' for usage.");
			Environment.Exit(2);
		}

		try
		{
			crawlerSettings.Configure(rps);
		}
		catch (ArgumentException ex)
		{
			AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
			return;
		}

		var cfg = ResolveEndpoint(endpoint, apiKey);
		var buildType = config.BuildType;
		var env = config.ElasticsearchEnvironment;
		var indexAlias = LabsSiteCrawlPlanner.ResolveLexicalReadAlias(buildType, env);

		AnsiConsole.MarkupLine("[aqua bold]Labs crawl[/] — [dim]search-labs, security-labs, observability-labs[/]");
		AnsiConsole.MarkupLine($"[dim]Elasticsearch:{Markup.Escape(cfg.Uri.ToString())}[/]");
		AnsiConsole.MarkupLine($"[dim]Incremental cache alias:[/] [white]{Markup.Escape(indexAlias)}[/]");
		if (force)
			AnsiConsole.MarkupLine("[yellow]Force:[/] [dim]skipping Elasticsearch crawl cache (full crawl plan)[/]");
		if (dryRun)
			AnsiConsole.MarkupLine("[yellow]Dry run[/]");
		AnsiConsole.WriteLine();

		var transport = ElasticsearchTransportFactory.Create(cfg);

		LabsSiteCrawlPlanner.LabsSitemapDiscoveryResult discovery;
		if (IsInteractive())
			discovery = await DiscoverLabsSitemapsWithProgressAsync(ct);
		else
		{
			AnsiConsole.MarkupLine("[aqua]Fetching labs sitemaps...[/]");
			discovery = await LabsSiteCrawlPlanner.DiscoverUrlsAsync(
				sitemapParser,
				LabsSiteCrawlPlanner.LabsSitemapUrls,
				new Progress<(int completed, string currentSitemap)>(_ => { }),
				ct);
		}

		var allUrls = discovery.Urls;
		AnsiConsole.WriteLine();
		foreach (var (sitemapUrl, rawUrlCount) in discovery.PerSitemap)
		{
			var label = LabsSiteCrawlPlanner.SitemapDisplayLabel(sitemapUrl);
			AnsiConsole.MarkupLine(
				$"  [dim]{Markup.Escape(label)}[/]  [white]{rawUrlCount:N0}[/] [dim]URLs (from sitemap)[/]");
		}
		AnsiConsole.MarkupLine(
			$"[green]✓[/] [white]{allUrls.Count:N0}[/] [dim]unique URLs[/] [dim](cross-sitemap duplicates merged)[/]");
		if (allUrls.Count == 0)
			return;

		var exclusions = LabsSiteCrawlPlanner.DefaultExcludePaths.ToList();
		if (!string.IsNullOrWhiteSpace(excludePaths))
			exclusions.AddRange(excludePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

		var langFilter = ParseLanguageFilter(languages);
		var filtered = LabsSiteCrawlPlanner.FilterUrls(allUrls.ToList(), exclusions, langFilter.Count > 0 ? langFilter : null);

		var cacheLogger = loggerFactory.CreateLogger<ElasticsearchCrawlCache>();
		var crawlCache = new ElasticsearchCrawlCache(cacheLogger, transport);
		var cache = force
			? [with(StringComparer.OrdinalIgnoreCase)]
			: !await crawlCache.IndexExistsAsync(indexAlias, ct)
			? [with(StringComparer.OrdinalIgnoreCase)]
			: await crawlCache.LoadCacheAsync(indexAlias, progress: null, ct);

		var plan = LabsSiteCrawlPlanner.BuildCrawlPlan(filtered, cache, unchanged, fair, maxPages, loggerFactory);

		AnsiConsole.MarkupLine(
			$"[dim]new {plan.Stats.NewUrls:N0} | unchanged {plan.Stats.UnchangedUrls:N0} | verify {plan.Stats.PossiblyChangedUrls:N0} | crawl ops {plan.UrlsToCrawl.Count:N0}[/]");

		if (dryRun)
		{
			AnsiConsole.MarkupLine("[yellow]Dry run done[/]");
			return;
		}

		var decisions = plan.UrlsToCrawl;
		LabsDocumentExporter? exporter = null;
		try
		{
			exporter = new LabsDocumentExporter(
				loggerFactory,
				cfg,
				transport,
				buildType,
				env,
				enableAiEnrichment: !noAi);

			if (!noAi)
				exporter.ConfigurePostSyncAiBatch(options.MaxAiDocs, options.MaxAiTime);

			if (IsInteractive())
			{
				await AnsiConsole.Status()
					.AutoRefresh(true)
					.Spinner(Spinner.Known.Dots)
					.StartAsync("[aqua]Bootstrapping indices...[/]", async _ => await exporter.StartAsync(ct));
			}
			else
			{
				AnsiConsole.MarkupLine("[aqua]Bootstrapping indices...[/]");
				await exporter.StartAsync(ct);
			}

			AnsiConsole.MarkupLine($"[green]✓[/] Ready [dim]({exporter.Strategy})[/]");
			WriteBootstrapSummary(exporter);
			var loop = new LabsCrawlLoop(
				crawler,
				new LabsHtmlExtractor(loggerFactory.CreateLogger<LabsHtmlExtractor>()),
				exporter,
				loggerFactory.CreateLogger<LabsCrawlLoop>());

			var done = 0;
			var total = decisions.Count;
			var crawlOutcomes = new CrawlOutcomeCounters();

			void WireCrawlHandlers(Action bumpProgress)
			{
				loop.OnUrlIndexed = () =>
				{
					crawlOutcomes.Indexed++;
					bumpProgress();
				};
				loop.OnUrlSkipped = (_, reason) =>
				{
					if (reason.Contains("304", StringComparison.OrdinalIgnoreCase))
						crawlOutcomes.NotModified++;
					else
						crawlOutcomes.ExtractSkipped++;
					bumpProgress();
				};
				loop.OnUrlFailed = (_, _) =>
				{
					crawlOutcomes.CrawlFailed++;
					bumpProgress();
				};
				loop.OnUrlUnavailable = _ =>
				{
					crawlOutcomes.Unavailable++;
					bumpProgress();
				};
				loop.OnIndexingError = (_, _) =>
				{
					crawlOutcomes.IndexingFailed++;
					bumpProgress();
				};
				loop.OnFatalError = (_, _) =>
				{
					crawlOutcomes.FatalCrawlErrors++;
					bumpProgress();
				};
			}

			if (IsInteractive() && total > 0)
			{
				await AnsiConsole.Progress()
					.Columns(
						new SpinnerColumn(),
						new TaskDescriptionColumn(),
						new ProgressBarColumn(),
						new PercentageColumn())
					.StartAsync(async pc =>
					{
						var t = pc.AddTask("[aqua]Crawl[/]", maxValue: Math.Max(1, total));
						t.MaxValue = total;
						void ProgressBump()
						{
							done++;
							t.Value = Math.Min(done, total);
							t.Description = $"[aqua]Crawl[/] {done:N0}/{total:N0}";
						}

						WireCrawlHandlers(ProgressBump);
						await loop.RunAsync(decisions, ct);
					});
			}
			else
			{
				void SimpleBump() => done++;
				WireCrawlHandlers(SimpleBump);
				await loop.RunAsync(decisions, ct);
			}

			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine(
				$"[green]✓[/] Crawl phase [white]{done:N0}[/][dim]/[/][white]{total:N0}[/] [dim]URLs handled[/]");
			WriteCrawlOutcomeSummary(crawlOutcomes);
			if (done != total)
			{
				AnsiConsole.MarkupLine(
					$"[yellow]! [/][dim]Handled count ({done}) != crawl ops ({total}). Cancelled, or fewer HTTP results than tasks — check logs.[/]");
			}

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
				AnsiConsole.MarkupLine("[aqua]Finalizing…[/] [dim](bulk ingest — progress lines below; flush details in logs)[/]");
				exporter.OnSyncProgress = info =>
				{
					if (info.Total == 0 && info.Label.StartsWith("Flush", StringComparison.Ordinal))
						return;
					AnsiConsole.MarkupLine(SyncProgressConsole.FormatStatusMarkup(info));
				};
				await exporter.FinalizeAsync(ct);
			}
			AnsiConsole.MarkupLine("[green]✓[/] Done");
		}
		finally
		{
			exporter?.Dispose();
		}
	}

	/// <summary>
	/// Run generative AI enrichment on existing <c>labs-*</c> semantic indices (no labs crawl).
	/// </summary>
	/// <remarks>
	/// <paramref name="maxAiDocs"/> is an optional cap; <c>0</c> means no document limit.
	/// Omit <paramref name="maxAiTime"/> for no wall-clock limit, or set a duration of at least one minute (for example <c>1h</c>, <c>90m</c>).
	/// Use <paramref name="bootstrapOnly"/> to create the enrich policy, pipeline, and lookup index without enriching any documents.
	/// </remarks>
	[CommandName("ai-enrich")]
	public async Task AiEnrich(
		string? apiKey = null,
		[Url] Uri? endpoint = null,
		[Range(0, int.MaxValue)] int maxAiDocs = 0,
		TimeSpan? maxAiTime = null,
		bool bootstrapOnly = false,
		Cancel ct = default
	)
	{
		if (!AiEnrichmentBudget.TryValidateMaxTime(maxAiTime, out var maxAiTimeError))
		{
			await Console.Error.WriteLineAsync($"Error: --max-ai-time {maxAiTimeError}");
			await Console.Error.WriteLineAsync("Run 'essc labs ai-enrich --help' for usage.");
			Environment.Exit(2);
		}

		AnsiConsole.MarkupLine("[aqua bold]Labs AI enrichment[/] [dim](labs-* indices)[/]");
		AnsiConsole.WriteLine();

		var cfg = ResolveEndpoint(endpoint, apiKey);

		AnsiConsole.MarkupLine($"[dim]Elasticsearch: {Markup.Escape(cfg.Uri.ToString())}[/]");

		var transport = ElasticsearchTransportFactory.Create(cfg);

		using var deadline = AiEnrichmentDeadline.Create(maxAiTime, ct);
		var effectiveToken = deadline.Token;

		if (maxAiTime is { } limit)
			AnsiConsole.MarkupLine($"[dim]Time limit: [yellow]{Markup.Escape(limit.ToString())}[/][/]");
		if (maxAiDocs > 0)
			AnsiConsole.MarkupLine($"[dim]Document limit: [yellow]{maxAiDocs:N0}[/] documents[/]");

		AnsiConsole.WriteLine();

		try
		{
			using var exporter = new LabsDocumentExporter(
				loggerFactory,
				cfg,
				transport,
				config.BuildType,
				config.ElasticsearchEnvironment,
				enableAiEnrichment: true
			);

			await AnsiConsole.Status()
				.AutoRefresh(true)
				.Spinner(Spinner.Known.Dots)
				.StartAsync("[aqua]Bootstrapping Elasticsearch indices...[/]", async _ =>
				{
					await exporter.StartAsync(effectiveToken);
				});

			AnsiConsole.MarkupLine($"[green]✓[/] Elasticsearch indices ready [dim]({exporter.Strategy})[/]");
			WriteBootstrapSummary(exporter);
			AnsiConsole.WriteLine();

			if (bootstrapOnly)
			{
				AnsiConsole.MarkupLine("[dim]--bootstrap-only set — skipping AI enrichment[/]");
				return;
			}

			var aiResult = await AiEnrichmentConsole.RunInteractiveAsync(
				exporter.AiEnrichmentEnabled,
				(max, token) => exporter.RunAiEnrichmentAsync(max, token),
				maxAiDocs,
				effectiveToken);
			AiEnrichmentConsole.DisplaySummary(aiResult, maxAiTime, maxAiDocs);
		}
		catch (OperationCanceledException) when (deadline.TimedOut)
		{
			AnsiConsole.MarkupLine("[yellow]AI enrichment stopped — time limit reached[/]");
		}
	}

	/// <summary>Prints the resolved bootstrap decision (new/existing index) and target index for both write targets.</summary>
	private static void WriteBootstrapSummary(LabsDocumentExporter exporter)
	{
		var rows = new List<Markup>();
		SyncProgressConsole.AddBootstrapRows(rows, "primary", exporter.PrimaryBootstrap);
		SyncProgressConsole.AddBootstrapRows(rows, "secondary", exporter.SecondaryBootstrap);
		foreach (var row in rows)
			AnsiConsole.Write(new Rows(row));
	}

	private sealed class CrawlOutcomeCounters
	{
		public int Indexed;
		public int NotModified;
		public int ExtractSkipped;
		public int CrawlFailed;
		public int Unavailable;
		public int IndexingFailed;
		public int FatalCrawlErrors;
	}

	private static void WriteCrawlOutcomeSummary(CrawlOutcomeCounters c)
	{
		static string Dim0Or(string whenPositive, int n) => n == 0 ? "[dim]0[/]" : whenPositive;

		static string CountIndexed(int n) => Dim0Or($"[green]{n:N0}[/]", n);
		static string CountNotModified(int n) => Dim0Or($"[grey]{n:N0}[/]", n);
		static string CountExtractSkipped(int n) => Dim0Or($"[yellow]{n:N0}[/]", n);
		static string CountYellow(int n) => Dim0Or($"[yellow]{n:N0}[/]", n);
		static string CountRed(int n) => Dim0Or($"[red]{n:N0}[/]", n);

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.Title("[aqua]Crawl outcomes[/]")
			.AddColumn("Outcome")
			.AddColumn(new TableColumn("Count").RightAligned());

		_ = table.AddRow(new Markup("Indexed"), new Markup(CountIndexed(c.Indexed)));
		_ = table.AddRow(new Markup("Not modified (HTTP 304)"), new Markup(CountNotModified(c.NotModified)));
		_ = table.AddRow(new Markup("Extract skipped"), new Markup(CountExtractSkipped(c.ExtractSkipped)));
		_ = table.AddRow(new Markup("Crawl failed"), new Markup(CountYellow(c.CrawlFailed)));
		_ = table.AddRow(new Markup("Unavailable (404, etc.)"), new Markup(CountYellow(c.Unavailable)));
		_ = table.AddRow(new Markup("Index error"), new Markup(CountRed(c.IndexingFailed)));
		_ = table.AddRow(new Markup("Fatal"), new Markup(CountRed(c.FatalCrawlErrors)));

		var total = c.Indexed + c.NotModified + c.ExtractSkipped + c.CrawlFailed + c.Unavailable +
			c.IndexingFailed + c.FatalCrawlErrors;
		_ = table.AddRow(new Markup("[bold]Total[/]"), new Markup($"[bold]{total:N0}[/]"));

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	private async Task<LabsSiteCrawlPlanner.LabsSitemapDiscoveryResult> DiscoverLabsSitemapsWithProgressAsync(CancellationToken ct)
	{
		LabsSiteCrawlPlanner.LabsSitemapDiscoveryResult? result = null;
		await AnsiConsole.Progress()
			.AutoRefresh(true)
			.AutoClear(false)
			.Columns(
				new SpinnerColumn(),
				new TaskDescriptionColumn(),
				new ProgressBarColumn(),
				new PercentageColumn())
			.StartAsync(async pc =>
			{
				var task = pc.AddTask("[aqua]Labs sitemaps[/]", maxValue: LabsSiteCrawlPlanner.LabsSitemapUrls.Length);
				result = await LabsSiteCrawlPlanner.DiscoverUrlsAsync(
					sitemapParser,
					LabsSiteCrawlPlanner.LabsSitemapUrls,
					new Progress<(int completed, string currentSitemap)>(v =>
					{
						task.Description = $"[aqua]{Markup.Escape(v.currentSitemap)}[/]";
						task.Value = v.completed;
					}),
					ct);
			});
		return result ?? throw new InvalidOperationException("Labs sitemap discovery did not return a result.");
	}

	private static HashSet<string> ParseLanguageFilter(string? languages) =>
		string.IsNullOrWhiteSpace(languages)
			? []
			: languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

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

	private static int ParseOptionalNonNegativeInt(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return 0;
		if (!int.TryParse(value, out var n) || n < 0)
			throw new ArgumentException("Must be a non-negative integer.", nameof(value));
		return n;
	}

	private static int? ParseOptionalPositiveInt(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;
		if (!int.TryParse(value, out var n))
			throw new ArgumentException("Must be an integer.", nameof(value));
		return n == 0 ? null : n;
	}
}
