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
	ITranslationDiscovery translationDiscovery,
	CrawlerSettings crawlerSettings
)
{
	private const string DefaultSitemapUrl = "https://www.elastic.co/sitemap.xml";

	// Additional sitemaps for labs content (not included in main sitemap)
	private static readonly string[] AdditionalSitemaps =
	[
		"https://www.elastic.co/search-labs/sitemap.xml",
		"https://www.elastic.co/security-labs/sitemap.xml",
		"https://www.elastic.co/observability-labs/sitemap.xml"
	];

	// Paths to exclude from site crawl
	private static readonly string[] DefaultExcludePaths =
	[
		"/guide/",                    // Handled by guide crawler
		"/downloads/past-releases/"   // Low search value, ~15k URLs
	];

	// Path patterns and their relevance levels
	// Note: Order matters - more specific patterns should come before general ones
	private static readonly Dictionary<string, (string Relevance, string Emoji, Color Color)> PathInfo = new(StringComparer.OrdinalIgnoreCase)
	{
		// Labs content - highly technical, top priority
		{ "/search-labs/", ("high", "🔬", Color.Cyan1) },
		{ "/security-labs/", ("high", "🛡", Color.Cyan1) },
		{ "/observability-labs/", ("high", "🔭", Color.Cyan1) },

		// General blog and educational content
		{ "/blog/", ("high", "📝", Color.Green) },
		{ "/what-is/", ("high", "❓", Color.Green) },

		// Product pages
		{ "/elasticsearch", ("high", "🔍", Color.Green) },
		{ "/kibana", ("high", "📊", Color.Green) },
		{ "/observability", ("high", "👁", Color.Green) },
		{ "/security", ("high", "🔒", Color.Green) },
		{ "/enterprise-search", ("high", "🏢", Color.Green) },
		{ "/explore/", ("high", "🧭", Color.Green) },

		// Medium priority content
		{ "/downloads/", ("medium", "📥", Color.Yellow) },
		{ "/resources/", ("medium", "📚", Color.Yellow) },
		{ "/training/", ("medium", "🎓", Color.Yellow) },
		{ "/industries/", ("medium", "🏭", Color.Yellow) },
		{ "/webinars/", ("medium", "🎥", Color.Yellow) },
		{ "/virtual-events/", ("medium", "🎪", Color.Yellow) },
		{ "/elasticon/", ("medium", "🎉", Color.Yellow) },
		{ "/events/", ("medium", "📅", Color.Yellow) },
		{ "/demo-gallery/", ("medium", "🖼", Color.Yellow) },

		// Lower priority content
		{ "/campaigns/", ("low", "📣", Color.Grey) },
		{ "/customers/", ("low", "👥", Color.Grey) },
		{ "/partners/", ("low", "🤝", Color.Grey) },
		{ "/about/", ("low", "ℹ", Color.Grey) },
		{ "/agreements/", ("low", "📜", Color.Grey) }
	};

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
	/// <param name="noTranslations">Skip translation discovery</param>
	/// <param name="noAi">Disable AI enrichment</param>
	/// <param name="noSemantic">Skip semantic index</param>
	/// <param name="failFast">Stop immediately when an indexing error occurs</param>
	/// <param name="missingTranslationReport">Directory to write missing translation report (optional)</param>
	/// <param name="rps">Rate limit in requests per second (0 or omit for unlimited)</param>
	/// <param name="fair">Distribute --max-pages evenly across categories (for validation)</param>
	/// <param name="translateRevalidate">Re-probe translations that were previously not found</param>
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
		bool noSemantic = false,
		bool failFast = false,
		string? missingTranslationReport = null,
		int? rps = null,
		bool fair = false,
		bool translateRevalidate = false,
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

		_ = noSemantic; // TODO: Pass to orchestrator when single-channel mode is supported

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

			var allSitemaps = new[] { sitemapUrl }.Concat(AdditionalSitemaps).ToList();
			var allUrlsList = new List<SitemapEntry>();

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

					foreach (var currentSitemap in allSitemaps)
					{
						try
						{
							var shortUrl = currentSitemap.Length > 50 ? "..." + currentSitemap[^47..] : currentSitemap;
							mainTask.Description = $"[aqua]🌍[/] [dim]{Markup.Escape(shortUrl)}[/]";

							var urls = await sitemapParser.ParseAsync(
								new Uri(currentSitemap),
								null, // No per-child progress for additional sitemaps
								ctx
							);

							allUrlsList.AddRange(urls);
						}
						catch (HttpRequestException ex)
						{
							// Some labs sitemaps may not exist yet
							logger.LogDebug("Sitemap {Url} not found: {Message}", currentSitemap, ex.Message);
						}

						mainTask.Increment(1);
					}

					mainTask.Description = "[green]✓[/] Sitemap discovery complete";
				});

			// Deduplicate URLs (in case of overlap)
			var allUrls = allUrlsList
				.GroupBy(u => u.Location)
				.Select(g => g.First())
				.ToList();

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

			// Parse language filter (used later for translation discovery and final filtering)
			var languageFilter = ParseLanguageFilter(languages);

			// Filter URLs by path exclusions only (language filter applied later)
			// This ensures we have English URLs for translation discovery
			// Note: maxPages limit is applied later to urlsToCrawl, not here
			var filteredUrls = FilterUrls(allUrls, exclusions, []);

			// Create transport once and reuse for cache and exporter
			var endpoints = configurationContext.Endpoints;
			var buildType = endpoints.BuildType;
			var environment = endpoints.Environment;
			var transport = ElasticsearchTransportFactory.Create(endpoints.Elasticsearch);

			// Load cache from Elasticsearch (if index exists)
			SpectreConsoleTheme.WriteSection("Cache Analysis");

			var cache = new Dictionary<string, CachedDocInfo>(StringComparer.OrdinalIgnoreCase);
			var crawlCache = new ElasticsearchCrawlCache(
				loggerFactory.CreateLogger<ElasticsearchCrawlCache>(),
				transport
			);

			var indexAlias = SiteIndexerExporter.ResolveLexicalReadAlias(buildType, environment);
			if (await crawlCache.IndexExistsAsync(indexAlias, ctx))
			{
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

						cache = await crawlCache.LoadCacheAsync(
							indexAlias,
							new Progress<(int loaded, string? url)>(p =>
							{
								task.Description = $"[aqua]📦 Loaded {p.loaded:N0} docs[/]";
								// Use indeterminate progress since we don't know total
								task.IsIndeterminate = true;
							}),
							ctx
						);

						task.IsIndeterminate = false;
						task.Value = task.MaxValue = 100;
						task.Description = "[green]✓[/] Cache loaded";
					});

				SpectreConsoleTheme.WriteSuccess($"Loaded [yellow]{cache.Count:N0}[/] documents from cache");
			}
			else
			{
				SpectreConsoleTheme.WriteInfo("No existing index - performing full crawl");
			}

			// Extract English URLs from sitemap for crawl decisions
			var englishUrls = filteredUrls
				.Where(u => GetLanguageFromUrl(u.Location) == "en")
				.ToList();

			// Make crawl decisions for English pages
			var decisionMaker = new CrawlDecisionMaker(loggerFactory.CreateLogger<CrawlDecisionMaker>());
			var englishDecisions = decisionMaker.MakeDecisions(englishUrls, cache).ToList();

			// Apply --fair limit early so translation discovery only probes the limited set
			// This also ensures dry-run shows accurate counts
			if (fair && maxPages > 0)
			{
				var needsCrawling = englishDecisions.Where(d => d.Reason != CrawlReason.Unchanged).ToList();
				if (needsCrawling.Count > maxPages)
				{
					var fairSample = ApplyCategoryFairness(needsCrawling, maxPages);
					var cachedDecisions = englishDecisions.Where(d => d.Reason == CrawlReason.Unchanged).ToList();
					englishDecisions = cachedDecisions.Concat(fairSample).ToList();
					SpectreConsoleTheme.WriteInfo($"Fair sampling: [yellow]{fairSample.Count:N0}[/] pages to crawl across categories");
				}
			}

			var englishStats = CrawlDecisionMaker.GetStats(englishDecisions);

			SpectreConsoleTheme.WriteInfo(
				$"English pages: [green]{englishStats.NewUrls:N0}[/] new, " +
				$"[grey]{englishStats.UnchangedUrls:N0}[/] unchanged, " +
				$"[yellow]{englishStats.PossiblyChangedUrls:N0}[/] to verify"
			);

			// Translation discovery phase
			var discoveredTranslations = new List<SitemapEntry>();
			var translationsByLanguage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var translationsFromCache = 0;

			if (!noTranslations && (englishStats.NewUrls > 0 || englishStats.PossiblyChangedUrls > 0))
			{
				SpectreConsoleTheme.WriteSection("Translation Discovery");

				// Calculate total probes: pages × languages
				var pagesToProbe = englishDecisions.Count(d => d.Reason != CrawlReason.Unchanged);
				var languagesToProbe = languageFilter.Count > 0
					? languageFilter.Count(l => l is "de" or "fr" or "es" or "jp" or "kr" or "cn" or "pt")
					: 7; // All 7 translation languages
				var totalProbes = pagesToProbe * languagesToProbe;

				// Build RPS suffix for progress display
				var rpsSuffix = crawlerSettings.RateLimitingEnabled
					? $" [dim]@ {crawlerSettings.Rps} RPS[/]"
					: "";

				TranslationDiscoveryStats? translationStats = null;

				try
				{
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
							var task = progressCtx.AddTask($"[aqua]🌐 Probing 0/{totalProbes:N0}{rpsSuffix}[/]", maxValue: totalProbes);

							translationStats = await translationDiscovery.DiscoverAsync(
								englishDecisions,
								cache,
								languageFilter,
								translation =>
								{
									// Create sitemap entry for the translation, inheriting lastmod from English page
									var englishDecision = englishDecisions.First(d => d.Entry.Location == translation.EnglishUrl);
									discoveredTranslations.Add(new SitemapEntry(
										translation.TranslatedUrl,
										englishDecision.Entry.LastModified
									));
								},
								translateRevalidate,
								new Progress<(int probed, int found, string? url)>(p =>
								{
									task.Value = p.probed;
									task.Description = $"[aqua]🌐 Probing {p.probed:N0}/{totalProbes:N0} — found {p.found:N0}{rpsSuffix}[/]";
								}),
								ctx
							);

							task.Value = task.MaxValue;
							task.Description = "[green]✓[/] Translation discovery complete";
						});
				}
				catch (TranslationDiscoveryException ex)
				{
					SpectreConsoleTheme.WriteError($"Translation discovery failed: {ex.Message}");
					return;
				}

				if (translationStats is not null)
				{
					translationsByLanguage = new Dictionary<string, int>(translationStats.ByLanguage, StringComparer.OrdinalIgnoreCase);
					translationsFromCache = translationStats.FromCache;
				}

				IndexingDisplay.DisplayTranslationDiscoverySummary(discoveredTranslations.Count, translationsFromCache, translationsByLanguage);
			}

			// Display Site Analysis AFTER translation discovery (shows combined English + translations)
			SpectreConsoleTheme.WriteSection("Site Analysis");
			var excludedUrls = allUrls.Where(u => !filteredUrls.Contains(u)).ToList();
			DisplaySiteAnalysis(filteredUrls, excludedUrls, exclusions, discoveredTranslations);

			// Make decisions for discovered translations
			var translationDecisions = discoveredTranslations.Count > 0
				? decisionMaker.MakeDecisions(discoveredTranslations, cache).ToList()
				: [];

			// Combine all decisions
			var allDecisions = englishDecisions.Concat(translationDecisions).ToList();

			// Find stale URLs (in cache but not in sitemap or discovered translations)
			var allKnownUrls = englishUrls
				.Select(u => u.Location)
				.Concat(discoveredTranslations.Select(t => t.Location))
				.ToHashSet(StringComparer.OrdinalIgnoreCase);
			var staleUrls = decisionMaker.FindStaleUrls(cache, allKnownUrls).ToList();

			if (staleUrls.Count > 0)
				SpectreConsoleTheme.WriteWarning($"Found [red]{staleUrls.Count:N0}[/] stale URLs to delete");

			if (dryRun)
			{
				// Calculate stats accounting for language filter
				var filteredEnglishStats = languageFilter.Count == 0 || languageFilter.Contains("en")
					? englishStats
					: new CrawlDecisionStats(0, 0, 0);

				IndexingDisplay.DisplayDryRunWithCacheStats(
					filteredEnglishStats,
					staleUrls.Count,
					discoveredTranslations.Count,
					translationsByLanguage,
					languageFilter
				);
				return;
			}

			// Get URLs to crawl, filtered by language if specified
			// With --unchanged, include cached URLs so their batch_index_date is refreshed via hash noop
			var urlsToCrawl = allDecisions
				.Where(d => unchanged || d.Reason != CrawlReason.Unchanged)
				.Where(d => languageFilter.Count == 0 || languageFilter.Contains(GetLanguageFromUrl(d.Entry.Location)))
				.ToList();

			// Apply max-pages limit (only for non-fair mode; fair mode was applied earlier)
			if (!fair && maxPages > 0 && urlsToCrawl.Count > maxPages)
			{
				urlsToCrawl = urlsToCrawl.Take(maxPages).ToList();
				SpectreConsoleTheme.WriteWarning($"Limited to [yellow]{maxPages:N0}[/] pages to crawl");
			}

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
			var missingTranslations = new List<(string Url, string Language)>();
			var startTime = DateTime.UtcNow;

			await progress.RunWithLiveAsync("Crawling site pages", async (progressCtx, _) =>
			{
				await foreach (var result in crawler.CrawlAsync(urlsToCrawl, effectiveToken))
				{
					// Check for fail-fast cancellation
					if (effectiveToken.IsCancellationRequested)
						break;

					// Handle HTTP 304 Not Modified
					if (result.NotModified)
					{
						skippedNotModified++;
						progressCtx.ReportUrlSkipped(result.Url, "Not modified (304)");
						continue;
					}

					if (!result.Success)
					{
						// Fatal errors (e.g., HTTP 406) stop crawling immediately
						if (result.FatalError)
						{
							progressCtx.ReportUrlFailed(result.Url, result.Error ?? "Fatal error");
							diagnostics.EmitError(result.Url, $"Fatal error: {result.Error}");
							SpectreConsoleTheme.WriteError($"Fatal error: {result.Error} - stopping crawl");
							break;
						}

						var urlLanguage = GetLanguageFromUrl(result.Url);
						var isTranslation = urlLanguage != "en";

						if (result.StatusCode == 404)
						{
							if (isTranslation)
							{
								// Track missing translation silently (no warning, no error)
								missingTranslations.Add((result.Url, urlLanguage));
								progressCtx.ReportUrlUnavailable(result.Url);
							}
							else
							{
								// English page 404 - track as unavailable with warning
								progressCtx.ReportUrlUnavailable(result.Url);
								diagnostics.EmitWarning(result.Url, $"Page not found: {result.Error}");
							}
							continue;
						}

						// Non-404 failures are errors
						errorCount++;
						progressCtx.ReportUrlFailed(result.Url, result.Error ?? "Unknown error");
						diagnostics.EmitError(result.Url, $"Failed to crawl: {result.Error}");
						continue;
					}

					progressCtx.ReportUrlCrawled(result.Url, result.Content?.Length ?? 0);

					try
					{
						var language = GetLanguageFromUrl(result.Url);
						var relevance = GetRelevance(result.Url);
						var pageType = GetPageType(result.Url);

						var document = await htmlExtractor.ExtractAsync(
							result.Url,
							result.Content!,
							result.LastModified,
							language,
							relevance,
							pageType,
							effectiveToken
						);

						if (document is null)
						{
							progressCtx.ReportUrlSkipped(result.Url, "Failed to extract");
							diagnostics.EmitWarning(result.Url, "Failed to extract document from HTML");
							continue;
						}

						// Set HTTP caching headers for future conditional requests
						document.HttpEtag = result.HttpEtag;
						document.HttpLastModified = result.HttpLastModified;

						await exporter.ExportAsync(document, effectiveToken);
						processedCount++;
						progressCtx.ReportUrlIndexed();
					}
					catch (OperationCanceledException) when (effectiveToken.IsCancellationRequested)
					{
						break;
					}
					catch (Exception ex)
					{
						errorCount++;
						progressCtx.ReportIndexingError(result.Url, ex.Message);
						diagnostics.EmitError(result.Url, $"Failed to index: {ex.Message}", ex);
					}
				}

			});

			// Finalization (reindex to semantic, cleanup) with its own progress display
			await IndexingDisplay.RunFinalizationWithProgressAsync(exporter, ctx);

			if (skippedNotModified > 0)
				SpectreConsoleTheme.WriteInfo($"Skipped [grey]{skippedNotModified:N0}[/] unchanged pages (HTTP 304)");

			if (missingTranslations.Count > 0)
			{
				var byLang = missingTranslations
					.GroupBy(t => t.Language)
					.OrderByDescending(g => g.Count())
					.Select(g => $"{g.Key}: {g.Count():N0}")
					.ToList();
				SpectreConsoleTheme.WriteInfo($"Missing translations: [grey]{missingTranslations.Count:N0}[/] ({string.Join(", ", byLang)})");

				// Write report if directory specified
				if (!string.IsNullOrEmpty(missingTranslationReport))
					await WriteMissingTranslationReportAsync(missingTranslationReport, missingTranslations, ctx);
			}

			// AI enrichment phase (separate from crawling for visible progress)
			var aiResult = await IndexingDisplay.RunAiEnrichmentWithProgressAsync(exporter, ctx);

			var crawlTime = DateTime.UtcNow - startTime;

			// Display final summary
			var stats = progress.GetStats();
			var finalStats = CrawlDecisionMaker.GetStats(allDecisions);
			IndexingDisplay.DisplayFinalSummary(stats, finalStats, aiResult);

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
		List<string> exclusionPatterns,
		List<SitemapEntry> discoveredTranslations
	)
	{
		// Combine sitemap URLs with discovered translations for analysis
		var allUrls = urls.Concat(discoveredTranslations).ToList();

		// Group by relevance (all URLs)
		var byRelevance = allUrls
			.GroupBy(u => GetRelevance(u.Location))
			.ToDictionary(g => g.Key, g => g.Count());

		// Group by category (separate English and translations for unique/total)
		var englishByCategory = urls
			.Where(u => GetLanguageFromUrl(u.Location) == "en")
			.GroupBy(u => GetCategory(u.Location))
			.ToDictionary(g => g.Key, g => g.Count());

		var translationsByCategory = discoveredTranslations
			.GroupBy(u => GetCategory(u.Location))
			.ToDictionary(g => g.Key, g => g.Count());

		var allCategories = englishByCategory.Keys
			.Concat(translationsByCategory.Keys)
			.Distinct()
			.OrderByDescending(c => englishByCategory.GetValueOrDefault(c, 0) + translationsByCategory.GetValueOrDefault(c, 0))
			.Take(12)
			.ToList();

		// Group by language (all URLs)
		var byLanguage = allUrls
			.GroupBy(u => GetLanguageFromUrl(u.Location))
			.OrderByDescending(g => g.Count())
			.ToList();

		// Relevance breakdown with breakdown chart (pie-style)
		var relevanceBreakdown = new BreakdownChart()
			.Width(60);

		if (byRelevance.TryGetValue("high", out var high))
			_ = relevanceBreakdown.AddItem("High", high, Color.Green);
		if (byRelevance.TryGetValue("medium", out var medium))
			_ = relevanceBreakdown.AddItem("Medium", medium, Color.Yellow);
		if (byRelevance.TryGetValue("low", out var low))
			_ = relevanceBreakdown.AddItem("Low", low, Color.Grey);

		AnsiConsole.MarkupLine("[aqua]📊 Content Distribution by Relevance[/]");
		AnsiConsole.Write(relevanceBreakdown);
		AnsiConsole.WriteLine();

		// Category table with Unique | Total columns
		var categoryTable = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.AddColumn("[aqua]Category[/]")
			.AddColumn(new TableColumn("[aqua]Unique[/]").RightAligned())
			.AddColumn(new TableColumn("[aqua]Total[/]").RightAligned())
			.AddColumn("[aqua]Relevance[/]");

		foreach (var category in allCategories)
		{
			var (relevance, emoji, color) = PathInfo.GetValueOrDefault(
				category,
				("medium", "📄", Color.White)
			);
			var displayName = category.Trim('/');
			if (string.IsNullOrEmpty(displayName))
				displayName = "Root";

			var unique = englishByCategory.GetValueOrDefault(category, 0);
			var translations = translationsByCategory.GetValueOrDefault(category, 0);
			var total = unique + translations;

			// Show translations indicator if there are any
			var totalDisplay = translations > 0
				? $"[white]{total:N0}[/]"
				: $"[dim]{total:N0}[/]";

			_ = categoryTable.AddRow(
				$"{emoji} [{color}]{Markup.Escape(displayName)}[/]",
				$"[white]{unique:N0}[/]",
				totalDisplay,
				$"[{color}]{relevance}[/]"
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

			// Count URLs that didn't match any exclusion pattern (e.g., non-elastic.co domains)
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
		var summaryPanel = new Panel(
			new Rows(
				new Markup($"[green]✓ English URLs:[/] [white]{englishCount:N0}[/]"),
				new Markup($"[cyan]🌐 Translations:[/] [white]{discoveredTranslations.Count:N0}[/]"),
				new Markup($"[aqua]📊 Total:[/] [white]{allUrls.Count:N0}[/] URLs"),
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
		var langPrefixes = new[] { "/de/", "/fr/", "/jp/", "/kr/", "/cn/", "/es/", "/pt/" };
		foreach (var prefix in langPrefixes)
		{
			if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				path = $"/{path[prefix.Length..]}";
				break;
			}
		}

		foreach (var pattern in PathInfo.Keys)
		{
			if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
				return pattern;
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

	private static string GetRelevance(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		foreach (var (pattern, info) in PathInfo)
		{
			if (path.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
				path.Contains(pattern, StringComparison.OrdinalIgnoreCase))
				return info.Relevance;
		}

		return "medium";
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

	private async Task WriteMissingTranslationReportAsync(
		string directory,
		List<(string Url, string Language)> missingTranslations,
		CancellationToken ct
	)
	{
		try
		{
			_ = Directory.CreateDirectory(directory);

			var reportFileName = $"missing-translations-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
			var reportPath = Path.Combine(directory, reportFileName);

			var lines = new List<string> { "url,language,english_url,category" };
			foreach (var (url, language) in missingTranslations.OrderBy(t => t.Language).ThenBy(t => t.Url))
			{
				// Derive English URL by removing language prefix
				var uri = new Uri(url);
				var englishPath = uri.AbsolutePath[$"/{language}/".Length..];
				var englishUrl = $"{uri.Scheme}://{uri.Host}/{englishPath}";
				var category = GetCategory(url);

				lines.Add($"\"{url}\",\"{language}\",\"{englishUrl}\",\"{category.Trim('/')}\"");
			}

			await File.WriteAllLinesAsync(reportPath, lines, ct);

			SpectreConsoleTheme.WriteSuccess($"Missing translation report written to [cyan]{reportPath}[/]");

			// Also write summary by language
			var summaryFileName = $"missing-translations-summary-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
			var summaryPath = Path.Combine(directory, summaryFileName);
			var summary = new List<string>
			{
				$"Missing Translation Report - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
				$"Total missing: {missingTranslations.Count:N0}",
				"",
				"By Language:"
			};

			foreach (var group in missingTranslations.GroupBy(t => t.Language).OrderByDescending(g => g.Count()))
			{
				var langName = LanguageNames.GetValueOrDefault(group.Key, group.Key);
				summary.Add($"  {langName}: {group.Count():N0}");
			}

			summary.Add("");
			summary.Add("By Category:");

			foreach (var group in missingTranslations.GroupBy(t => GetCategory(t.Url)).OrderByDescending(g => g.Count()))
			{
				summary.Add($"  {group.Key.Trim('/')}: {group.Count():N0}");
			}

			await File.WriteAllLinesAsync(summaryPath, summary, ct);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to write missing translation report to {Directory}", directory);
		}
	}
}
