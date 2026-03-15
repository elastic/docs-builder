// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using CrawlIndexer.Caching;
using CrawlIndexer.Crawling;
using CrawlIndexer.Display;
using CrawlIndexer.Html;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Markdown.Exporters.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CrawlIndexer.Commands;

/// <summary>
/// Command for crawling and indexing non-documentation site pages (marketing, blog, etc.)
/// </summary>
public class SiteCommand(
	ILogger<SiteCommand> logger,
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	IConfigurationContext configurationContext,
	ISitemapParser sitemapParser,
	IAdaptiveCrawler crawler,
	CrawlerSettings crawlerSettings
)
{
	private const string DefaultSitemapUrl = "https://www.elastic.co/sitemap.xml";

	// Additional sitemaps not included in the main sitemap
	private static readonly string[] AdditionalSitemaps =
	[
		"https://www.elastic.co/search-labs/sitemap.xml",
		"https://www.elastic.co/security-labs/sitemap.xml",
		"https://www.elastic.co/observability-labs/sitemap.xml"
	];

	// Language-specific sitemaps provide translated URLs directly.
	// NOTE: security-labs translations exist on the site but are NOT covered by these sitemaps.
	private static readonly string[] TranslationSitemaps =
	[
		"https://www.elastic.co/sitemap-fr.xml",
		"https://www.elastic.co/sitemap-de.xml",
		"https://www.elastic.co/sitemap-es.xml",
		"https://www.elastic.co/sitemap-pt.xml",
		"https://www.elastic.co/sitemap-kr.xml",
		"https://www.elastic.co/sitemap-jp.xml",
		"https://www.elastic.co/sitemap-cn.xml"
	];

	// Paths to exclude from site crawl
	private static readonly string[] DefaultExcludePaths =
	[
		"/guide/",                    // Handled by guide crawler
		"/downloads/past-releases/"   // Low search value, ~15k URLs
	];

	private record PathEntry(string Pattern, string Emoji, Color Color);

	// Path patterns with display metadata; order matters — more specific before general
	private static readonly IReadOnlyList<PathEntry> PathEntries =
	[
		// Labs content
		new("/search-labs/",        "🔬", Color.Cyan1),
		new("/security-labs/",      "🛡",  Color.Cyan1),
		new("/observability-labs/", "🔭", Color.Cyan1),

		// General blog and educational content
		new("/blog/",     "📝", Color.Green),
		new("/what-is/",  "❓", Color.Green),

		// Product pages
		new("/elasticsearch",   "🔍", Color.Green),
		new("/kibana",          "📊", Color.Green),
		new("/observability",   "👁",  Color.Green),
		new("/security",        "🔒", Color.Green),
		new("/enterprise-search","🏢", Color.Green),
		new("/explore/",        "🧭", Color.Green),

		// Medium priority content
		new("/downloads/",     "📥", Color.Yellow),
		new("/resources/",     "📚", Color.Yellow),
		new("/training/",      "🎓", Color.Yellow),
		new("/industries/",    "🏭", Color.Yellow),
		new("/webinars/",      "🎥", Color.Yellow),
		new("/virtual-events/","🎪", Color.Yellow),
		new("/elasticon/",     "🎉", Color.Yellow),
		new("/events/",        "📅", Color.Yellow),
		new("/demo-gallery/",  "🖼",  Color.Yellow),

		// Lower priority content
		new("/campaigns/",  "📣", Color.Grey),
		new("/customers/",  "👥", Color.Grey),
		new("/partners/",   "🤝", Color.Grey),
		new("/about/",      "ℹ",  Color.Grey),
		new("/agreements/", "📜", Color.Grey)
	];

	private static readonly string[] LangPrefixes = ["/de/", "/fr/", "/jp/", "/kr/", "/cn/", "/es/", "/pt/"];

	private static readonly Dictionary<string, string> LanguageNames = new()
	{
		["en"] = "English 🇬🇧",
		["de"] = "German 🇩🇪",
		["fr"] = "French 🇫🇷",
		["ja"] = "Japanese 🇯🇵",
		["ko"] = "Korean 🇰🇷",
		["zh"] = "Chinese 🇨🇳",
		["es"] = "Spanish 🇪🇸",
		["pt"] = "Portuguese 🇵🇹"
	};

	/// <summary>
	/// Crawl and index non-documentation site pages.
	/// </summary>
	/// <param name="languages">Comma-separated languages to include (default: all)</param>
	/// <param name="excludePaths">Additional paths to exclude (comma-separated)</param>
	/// <param name="sitemapUrl">Override sitemap location</param>
	/// <param name="maxPages">Limit pages to crawl (0 = unlimited)</param>
	/// <param name="dryRun">Discover URLs without crawling</param>
	/// <param name="noTranslations">Skip language sitemaps (English only)</param>
	/// <param name="noAi">Disable AI enrichment</param>
	/// <param name="failFast">Stop immediately when an indexing error occurs</param>
	/// <param name="rps">Rate limit in requests per second (0 or omit for unlimited)</param>
	/// <param name="fair">Distribute --max-pages evenly across categories (for validation)</param>
	/// <param name="unchanged">Include unchanged (cached) URLs in the crawl set to refresh their batch timestamp</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("index")]
	public async Task Index(
		string? languages = null,
		string? excludePaths = null,
		string sitemapUrl = DefaultSitemapUrl,
		int maxPages = 0,
		bool dryRun = false,
		bool noTranslations = false,
		bool noAi = false,
		bool failFast = false,
		int? rps = null,
		bool fair = false,
		bool unchanged = false,
		Cancel ctx = default
	)
	{
		// Validate and configure RPS
		try
		{
			crawlerSettings.Configure(rps);
		}
		catch (ArgumentException ex)
		{
			SpectreConsoleTheme.WriteError(ex.Message);
			return;
		}

		// Set up fail-fast cancellation
		using var failFastCts = failFast ? new CancellationTokenSource() : null;
		using var linkedCts = failFastCts is not null
			? CancellationTokenSource.CreateLinkedTokenSource(ctx, failFastCts.Token)
			: null;
		var effectiveToken = linkedCts?.Token ?? ctx;

		// Configure error tracker for fail-fast
		errorTracker.SetFailFastToken(failFastCts);

		_ = diagnostics.StartAsync(ctx);

		try
		{
			// Display header
			SpectreConsoleTheme.WriteHeader("site");

			// Display crawler configuration
			CrawlerConfigDisplay.DisplayConfiguration(crawlerSettings);

			if (dryRun)
				SpectreConsoleTheme.WriteDryRunBanner();

			// Parse sitemaps with progress spinner
			SpectreConsoleTheme.WriteSection("Sitemap Discovery");

			var allSitemaps = new[] { sitemapUrl }
				.Concat(AdditionalSitemaps)
				.Concat(noTranslations ? [] : TranslationSitemaps)
				.ToList();

			IReadOnlyList<SitemapEntry> allUrls = [];
			await AnsiConsole.Progress()
				.AutoRefresh(true)
				.AutoClear(false)
				.HideCompleted(false)
				.Columns(
					new SpinnerColumn(),
					new TaskDescriptionColumn(),
					new ProgressBarColumn(),
					new PercentageColumn(),
					new RemainingTimeColumn()
				)
				.StartAsync(async progressCtx =>
				{
					var mainTask = progressCtx.AddTask("[aqua]🌍 Fetching sitemaps[/]", maxValue: allSitemaps.Count);
					allUrls = await DiscoverUrlsAsync(
						allSitemaps,
						new Progress<(int completed, string currentSitemap)>(p =>
						{
							var shortUrl = p.currentSitemap.Length > 50 ? "..." + p.currentSitemap[^47..] : p.currentSitemap;
							mainTask.Description = $"[aqua]🌍[/] [dim]{Markup.Escape(shortUrl)}[/]";
							mainTask.Value = p.completed;
						}),
						ctx
					);
					mainTask.Description = "[green]✓[/] Sitemap discovery complete";
				});

			SpectreConsoleTheme.WriteSuccess($"Found [yellow]{allUrls.Count:N0}[/] total URLs from [cyan]{allSitemaps.Count}[/] sitemaps");

			if (allUrls.Count == 0)
			{
				diagnostics.EmitError("sitemap", "Sitemap returned 0 URLs - nothing to crawl");
				SpectreConsoleTheme.WriteError("Sitemap returned 0 URLs - nothing to crawl");
				return;
			}

			// Build exclusion list
			var exclusions = DefaultExcludePaths.ToList();
			if (!string.IsNullOrWhiteSpace(excludePaths))
				exclusions.AddRange(excludePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

			// Parse language filter
			var languageFilter = ParseLanguageFilter(languages);

			// Filter URLs by path exclusions and language
			var filteredUrls = FilterUrls(allUrls, exclusions, languageFilter);

			// Create transport once and reuse for cache and exporter
			var endpoints = configurationContext.Endpoints;
			var buildType = endpoints.BuildType;
			var environment = endpoints.Environment;
			var transport = ElasticsearchTransportFactory.Create(endpoints.Elasticsearch);

			// Load cache from Elasticsearch (if index exists)
			SpectreConsoleTheme.WriteSection("Cache Analysis");

			var indexAlias = SiteIndexerExporter.ResolveLexicalReadAlias(buildType, environment);
			var cache = new Dictionary<string, CachedDocInfo>(StringComparer.OrdinalIgnoreCase);
			await AnsiConsole.Progress()
				.AutoRefresh(true)
				.AutoClear(false)
				.HideCompleted(false)
				.Columns(
					new SpinnerColumn(),
					new TaskDescriptionColumn(),
					new ProgressBarColumn(),
					new PercentageColumn()
				)
				.StartAsync(async progressCtx =>
				{
					var task = progressCtx.AddTask("[aqua]📦 Loading cached documents[/]", maxValue: 100);
					cache = await LoadCacheAsync(
						indexAlias,
						transport,
						new Progress<(int loaded, string? url)>(p =>
						{
							task.Description = $"[aqua]📦 Loaded {p.loaded:N0} docs[/]";
							task.IsIndeterminate = true;
						}),
						ctx
					);
					task.IsIndeterminate = false;
					task.Value = task.MaxValue = 100;
					task.Description = "[green]✓[/] Cache loaded";
				});
			if (cache.Count > 0)
				SpectreConsoleTheme.WriteSuccess($"Loaded [yellow]{cache.Count:N0}[/] documents from cache");
			else
				SpectreConsoleTheme.WriteInfo("No existing index - performing full crawl");
			var plan = BuildCrawlPlan(filteredUrls, cache, unchanged, fair, maxPages);

			SpectreConsoleTheme.WriteInfo(
				$"Pages: [green]{plan.Stats.NewUrls:N0}[/] new, " +
				$"[grey]{plan.Stats.UnchangedUrls:N0}[/] unchanged, " +
				$"[yellow]{plan.Stats.PossiblyChangedUrls:N0}[/] to verify"
			);

			// Display Site Analysis
			SpectreConsoleTheme.WriteSection("Site Analysis");
			var excludedUrls = allUrls.Where(u => !filteredUrls.Contains(u)).ToList();
			DisplaySiteAnalysis(filteredUrls, excludedUrls, exclusions);

			if (plan.StaleUrls.Count > 0)
				SpectreConsoleTheme.WriteWarning($"Found [red]{plan.StaleUrls.Count:N0}[/] stale URLs to delete");

			if (dryRun)
			{
				IndexingDisplay.DisplayDryRunWithCacheStats(plan.Stats, plan.StaleUrls.Count);
				return;
			}

			var urlsToCrawl = plan.UrlsToCrawl.ToList();
			// Create exporter with shared transport
			using var exporter = new SiteIndexerExporter(
				loggerFactory,
				diagnostics,
				errorTracker,
				endpoints.Elasticsearch,
				transport,
				buildType,
				environment,
				configurationContext.SearchConfiguration,
				enableAiEnrichment: !noAi
			);

			// Bootstrap indices
			await AnsiConsole.Status()
				.AutoRefresh(true)
				.Spinner(Spinner.Known.Dots)
				.StartAsync("[aqua]Bootstrapping Elasticsearch indices...[/]", async _ =>
				{
					await exporter.StartAsync(ctx);
				});

			// Crawl and index with progress display
			SpectreConsoleTheme.WriteSection("Crawling & Indexing");

			using var progress = new CrawlProgressContext();
			progress.ReportUrlDiscovered(urlsToCrawl.Count);

			var htmlExtractor = new SiteHtmlExtractor(loggerFactory.CreateLogger<SiteHtmlExtractor>());
			var processedCount = 0;
			var skippedNotModified = 0;
			var errorCount = 0;
			var startTime = DateTime.UtcNow;

			var session = new CrawlSession<SiteDocument>(crawler, htmlExtractor, exporter, diagnostics);
			await progress.RunWithLiveAsync("Crawling site pages", async (progressCtx, _) =>
			{
				session.OnUrlCrawled = (url, bytes) => progressCtx.ReportUrlCrawled(url, bytes);
				session.OnUrlSkipped = (url, reason) =>
				{
					if (reason == "Not modified (304)") skippedNotModified++;
					progressCtx.ReportUrlSkipped(url, reason);
				};
				session.OnUrlFailed = (url, error) =>
				{
					errorCount++;
					progressCtx.ReportUrlFailed(url, error);
				};
				session.OnFatalError = (url, error) =>
				{
					progressCtx.ReportUrlFailed(url, error);
					SpectreConsoleTheme.WriteError($"Fatal error: {error} - stopping crawl");
				};
				session.OnUrlIndexed = () =>
				{
					processedCount++;
					progressCtx.ReportUrlIndexed();
				};
				session.OnUrlUnavailable = url =>
				{
					progressCtx.ReportUrlUnavailable(url);
					if (GetLanguageFromUrl(url) == "en")
						diagnostics.EmitWarning(url, "Page not found");
				};
				session.OnIndexingError = (url, error) =>
				{
					errorCount++;
					progressCtx.ReportIndexingError(url, error);
				};
				await session.RunAsync(urlsToCrawl, effectiveToken);
			}););

			// Finalization (reindex to semantic, cleanup) with its own progress display
			await IndexingDisplay.RunFinalizationWithProgressAsync(exporter, ctx);

			if (skippedNotModified > 0)
				SpectreConsoleTheme.WriteInfo($"Skipped [grey]{skippedNotModified:N0}[/] unchanged pages (HTTP 304)");

			// AI enrichment phase (separate from crawling for visible progress)
			var aiResult = await IndexingDisplay.RunAiEnrichmentWithProgressAsync(exporter, ctx);

			var crawlTime = DateTime.UtcNow - startTime;

			// Display final summary
			var crawlStats = progress.GetStats();
			IndexingDisplay.DisplayFinalSummary(crawlStats, aiResult);

			logger.LogInformation(
				"Crawling complete. Processed: {Processed}, Errors: {Errors}, Duration: {Duration}",
				processedCount,
				errorCount,
				crawlTime
			);
		}
		catch (OperationCanceledException) when (failFast && errorTracker.HasErrors)
		{
			SpectreConsoleTheme.WriteWarning("Crawl stopped due to --fail-fast");
		}
		finally
		{
			await diagnostics.StopAsync(ctx);
			// Display error summary using Errata-style formatting
			IndexingDisplay.DisplayErrorSummary(diagnostics);
		}
	}

	private static void DisplaySiteAnalysis(
		List<SitemapEntry> urls,
		List<SitemapEntry> excludedUrls,
		List<string> exclusionPatterns
	)
	{
		// Group by category
		var byCategory = urls
			.GroupBy(u => GetCategory(u.Location))
			.OrderByDescending(g => g.Count())
			.Take(12)
			.ToList();

		// Group by language
		var byLanguage = urls
			.GroupBy(u => GetLanguageFromUrl(u.Location))
			.OrderByDescending(g => g.Count())
			.ToList();

		// Category table
		var categoryTable = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.AddColumn("[aqua]Category[/]")
			.AddColumn(new TableColumn("[aqua]Count[/]").RightAligned());

		foreach (var group in byCategory)
		{
			var entry = PathEntries.FirstOrDefault(e => e.Pattern == group.Key);
			var emoji = entry?.Emoji ?? "📄";
			var color = entry?.Color ?? Color.White;
			var displayName = group.Key.Trim('/');
			if (string.IsNullOrEmpty(displayName))
				displayName = "Root";

			_ = categoryTable.AddRow(
				$"{emoji} [{color}]{Markup.Escape(displayName)}[/]",
				$"[white]{group.Count():N0}[/]"
			);
		}

		AnsiConsole.Write(categoryTable);
		AnsiConsole.WriteLine();

		// Language grid
		var langGrid = new Grid()
			.AddColumn()
			.AddColumn()
			.AddColumn()
			.AddColumn();

		var langRows = byLanguage
			.Select(g =>
			{
				var langName = LanguageNames.GetValueOrDefault(g.Key, g.Key);
				return $"[cyan]{langName}[/]: [white]{g.Count():N0}[/]";
			})
			.Chunk(4)
			.ToList();

		foreach (var row in langRows)
		{
			var cells = row.Select(x => new Markup(x)).ToArray();
			while (cells.Length < 4)
				cells = [.. cells, new Markup("")];
			_ = langGrid.AddRow(cells);
		}

		AnsiConsole.MarkupLine("[aqua]Languages:[/]");
		AnsiConsole.Write(langGrid);
		AnsiConsole.WriteLine();

		// Excluded URLs breakdown
		if (excludedUrls.Count > 0)
		{
			var excludedByPattern = exclusionPatterns
				.Select(p => new
				{
					Pattern = p,
					Count = excludedUrls.Count(u => new Uri(u.Location).AbsolutePath.StartsWith(p, StringComparison.OrdinalIgnoreCase))
				})
				.Where(x => x.Count > 0)
				.OrderByDescending(x => x.Count)
				.ToList();

			var otherExcluded = excludedUrls.Count - excludedByPattern.Sum(x => x.Count);

			var excludedChart = new BreakdownChart()
				.Width(60);

			var excludeColors = new[] { Color.Red, Color.Orange1, Color.Yellow, Color.Grey };
			var idx = 0;
			foreach (var group in excludedByPattern)
			{
				var displayName = group.Pattern.Trim('/');
				_ = excludedChart.AddItem(displayName, group.Count, excludeColors[idx++ % excludeColors.Length]);
			}
			if (otherExcluded > 0)
				_ = excludedChart.AddItem("other", otherExcluded, Color.Grey);

			AnsiConsole.MarkupLine($"[red]🚫 Excluded URLs[/] [dim]({excludedUrls.Count:N0} total)[/]");
			AnsiConsole.Write(excludedChart);
			AnsiConsole.WriteLine();
		}

		// Summary
		var englishCount = urls.Count(u => GetLanguageFromUrl(u.Location) == "en");
		var translationCount = urls.Count - englishCount;
		var summaryPanel = new Panel(
			new Rows(
				new Markup($"[green]✓ English URLs:[/] [white]{englishCount:N0}[/]"),
				new Markup($"[cyan]🌐 Translations:[/] [white]{translationCount:N0}[/]"),
				new Markup($"[aqua]📊 Total:[/] [white]{urls.Count:N0}[/] URLs"),
				new Markup($"[red]✗ Excluded:[/] [white]{excludedUrls.Count:N0}[/] URLs")
			)
		)
		{
			Header = new PanelHeader("[aqua]Summary[/]"),
			Border = BoxBorder.Rounded,
			BorderStyle = Style.Parse("aqua"),
			Padding = new Padding(2, 1)
		};

		AnsiConsole.Write(summaryPanel);
	}

	private static string GetCategory(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		// Remove language prefix if present
		foreach (var prefix in LangPrefixes)
		{
			if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				path = $"/{path[prefix.Length..]}";
				break;
			}
		}

		foreach (var entry in PathEntries)
		{
			if (path.StartsWith(entry.Pattern, StringComparison.OrdinalIgnoreCase))
				return entry.Pattern;
		}

		// Get first path segment
		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return segments.Length > 0 ? $"/{segments[0]}/" : "/";
	}

	private static HashSet<string> ParseLanguageFilter(string? languages) =>
		string.IsNullOrWhiteSpace(languages)
			? []
			: languages.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

	private static List<SitemapEntry> FilterUrls(
		IReadOnlyList<SitemapEntry> urls,
		List<string> exclusions,
		HashSet<string> languageFilter
	) => urls
			.Where(u =>
			{
				var uri = new Uri(u.Location);

				// Must be elastic.co
				if (!uri.Host.Equals("www.elastic.co", StringComparison.OrdinalIgnoreCase) &&
					!uri.Host.Equals("elastic.co", StringComparison.OrdinalIgnoreCase))
					return false;

				// Check exclusions
				foreach (var exclusion in exclusions)
				{
					if (uri.AbsolutePath.StartsWith(exclusion, StringComparison.OrdinalIgnoreCase))
						return false;
				}

				// Check language filter
				if (languageFilter.Count > 0)
				{
					var lang = GetLanguageFromUrl(u.Location);
					if (!languageFilter.Contains(lang))
						return false;
				}

				return true;
			})
			.ToList();

	/// <summary>
	/// Distributes maxPages evenly across categories for validation sampling.
	/// Ensures each category gets representation in the sample.
	/// Redistributes unused quota from small categories to larger ones.
	/// </summary>
	private static List<CrawlDecision> ApplyCategoryFairness(List<CrawlDecision> decisions, int maxPages)
	{
		// Group by category
		var byCategory = decisions
			.GroupBy(d => GetCategory(d.Entry.Location))
			.ToDictionary(g => g.Key, g => g.ToList());

		var categoryCount = byCategory.Count;
		if (categoryCount == 0)
			return [];

		var result = new List<CrawlDecision>();
		var remaining = maxPages;

		// Sort categories by size (largest first)
		var sortedCategories = byCategory
			.OrderByDescending(kvp => kvp.Value.Count)
			.ToList();

		// Track how many we've taken from each category
		var taken = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		// First pass: give each category its fair share (or all if smaller)
		var fairShare = maxPages / categoryCount;
		var extraSlots = maxPages % categoryCount;

		foreach (var (category, categoryDecisions) in sortedCategories)
		{
			var quota = fairShare + (extraSlots > 0 ? 1 : 0);
			if (extraSlots > 0)
				extraSlots--;

			var toTake = Math.Min(quota, categoryDecisions.Count);
			result.AddRange(categoryDecisions.Take(toTake));
			taken[category] = toTake;
			remaining -= toTake;
		}

		// Second pass: redistribute remaining quota to categories with more content
		if (remaining > 0)
		{
			foreach (var (category, categoryDecisions) in sortedCategories)
			{
				if (remaining <= 0)
					break;

				var alreadyTaken = taken[category];
				var available = categoryDecisions.Count - alreadyTaken;
				if (available > 0)
				{
					var toTake = Math.Min(remaining, available);
					result.AddRange(categoryDecisions.Skip(alreadyTaken).Take(toTake));
					remaining -= toTake;
				}
			}
		}

		return result;
	}

	private static string GetLanguageFromUrl(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		// Check for language prefixes
		if (path.StartsWith("/de/", StringComparison.OrdinalIgnoreCase))
			return "de";
		if (path.StartsWith("/fr/", StringComparison.OrdinalIgnoreCase))
			return "fr";
		if (path.StartsWith("/jp/", StringComparison.OrdinalIgnoreCase))
			return "ja";
		if (path.StartsWith("/kr/", StringComparison.OrdinalIgnoreCase))
			return "ko";
		if (path.StartsWith("/cn/", StringComparison.OrdinalIgnoreCase))
			return "zh";
		if (path.StartsWith("/es/", StringComparison.OrdinalIgnoreCase))
			return "es";
		if (path.StartsWith("/pt/", StringComparison.OrdinalIgnoreCase))
			return "pt";

		return "en";
	}


	private static string GetPageType(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		// Labs content - technical articles and tutorials
		if (path.Contains("/search-labs/", StringComparison.OrdinalIgnoreCase))
			return "search-labs";
		if (path.Contains("/security-labs/", StringComparison.OrdinalIgnoreCase))
			return "security-labs";
		if (path.Contains("/observability-labs/", StringComparison.OrdinalIgnoreCase))
			return "observability-labs";

		// General content types
		if (path.Contains("/blog/", StringComparison.OrdinalIgnoreCase))
			return "blog";
		if (path.Contains("/what-is/", StringComparison.OrdinalIgnoreCase))
			return "concept";
		if (path.Contains("/webinars/", StringComparison.OrdinalIgnoreCase))
			return "webinar";
		if (path.Contains("/virtual-events/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (path.Contains("/elasticon/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (path.Contains("/events/", StringComparison.OrdinalIgnoreCase))
			return "event";
		if (path.Contains("/training/", StringComparison.OrdinalIgnoreCase))
			return "training";
		if (path.Contains("/resources/", StringComparison.OrdinalIgnoreCase))
			return "resource";
		if (path.Contains("/customers/", StringComparison.OrdinalIgnoreCase))
			return "customer-story";
		if (path.Contains("/downloads/", StringComparison.OrdinalIgnoreCase))
			return "download";
		if (path.Contains("/demo-gallery/", StringComparison.OrdinalIgnoreCase))
			return "demo";
		if (path.Contains("/industries/", StringComparison.OrdinalIgnoreCase))
			return "industry";
		if (path.Contains("/partners/", StringComparison.OrdinalIgnoreCase))
			return "partner";
		if (path.Contains("/about/", StringComparison.OrdinalIgnoreCase))
			return "about";

		// Product pages
		if (path.Contains("/elasticsearch", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/kibana", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/observability", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/security", StringComparison.OrdinalIgnoreCase) ||
			path.Contains("/enterprise-search", StringComparison.OrdinalIgnoreCase))
			return "product";

		return "marketing";
	}

	private CrawlPlan BuildCrawlPlan(
		List<SitemapEntry> filteredUrls,
		Dictionary<string, CachedDocInfo> cache,
		bool unchanged,
		bool fair,
		int maxPages
	)
	{
		var decisionMaker = new CrawlDecisionMaker(loggerFactory.CreateLogger<CrawlDecisionMaker>());
		var allDecisions = decisionMaker.MakeDecisions(filteredUrls, cache).ToList();

		if (fair && maxPages > 0)
		{
			var needsCrawling = allDecisions.Where(d => d.Reason != CrawlReason.Unchanged).ToList();
			if (needsCrawling.Count > maxPages)
			{
				var fairSample = ApplyCategoryFairness(needsCrawling, maxPages);
				var cachedDecisions = allDecisions.Where(d => d.Reason == CrawlReason.Unchanged).ToList();
				allDecisions = [.. cachedDecisions, .. fairSample];
				SpectreConsoleTheme.WriteInfo($"Fair sampling: [yellow]{fairSample.Count:N0}[/] pages to crawl across categories");
			}
		}

		var stats = CrawlDecisionMaker.GetStats(allDecisions);

		var allKnownUrls = filteredUrls
			.Select(u => u.Location)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var staleUrls = decisionMaker.FindStaleUrls(cache, allKnownUrls).ToList();

		var urlsToCrawl = allDecisions
			.Where(d => unchanged || d.Reason != CrawlReason.Unchanged)
			.ToList();

		if (!fair && maxPages > 0 && urlsToCrawl.Count > maxPages)
		{
			urlsToCrawl = urlsToCrawl.Take(maxPages).ToList();
			SpectreConsoleTheme.WriteWarning($"Limited to [yellow]{maxPages:N0}[/] pages to crawl");
		}

		return new(urlsToCrawl, staleUrls, stats);
	}


	private record CrawlPlan(
		IReadOnlyList<CrawlDecision> UrlsToCrawl,
		IReadOnlyList<string> StaleUrls,
		CrawlDecisionStats Stats
	);

	private async Task<IReadOnlyList<SitemapEntry>> DiscoverUrlsAsync(
		IReadOnlyList<string> sitemaps,
		IProgress<(int completed, string currentSitemap)> progress,
		CancellationToken ct
	)
	{
		var allUrlsList = new List<SitemapEntry>();
		var completed = 0;
		foreach (var currentSitemap in sitemaps)
		{
			try
			{
				var urls = await sitemapParser.ParseAsync(new Uri(currentSitemap), null, ct);
				allUrlsList.AddRange(urls);
			}
			catch (HttpRequestException ex)
			{
				logger.LogDebug("Sitemap {Url} not found: {Message}", currentSitemap, ex.Message);
			}
			completed++;
			progress.Report((completed, currentSitemap));
		}
		return allUrlsList
			.GroupBy(u => u.Location)
			.Select(g => g.First())
			.ToList();
	}

	private async Task<Dictionary<string, CachedDocInfo>> LoadCacheAsync(
		string indexAlias,
		DistributedTransport transport,
		IProgress<(int loaded, string? url)>? progress,
		CancellationToken ct
	)
	{
		var crawlCache = new ElasticsearchCrawlCache(
			loggerFactory.CreateLogger<ElasticsearchCrawlCache>(),
			transport
		);
		if (!await crawlCache.IndexExistsAsync(indexAlias, ct))
			return new(StringComparer.OrdinalIgnoreCase);
		return await crawlCache.LoadCacheAsync(indexAlias, progress, ct);
	}


}
