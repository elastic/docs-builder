// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using ConsoleAppFramework;
using CrawlIndexer.Caching;
using CrawlIndexer.Display;
using CrawlIndexer.Crawling;
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
	IAdaptiveCrawler crawler
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
	/// <param name="enableAiEnrichment">Enable AI enrichment (default: true)</param>
	/// <param name="noSemantic">Skip semantic index</param>
	/// <param name="failFast">Stop immediately when an indexing error occurs</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("")]
	public async Task RunAsync(
		string? languages = null,
		string? excludePaths = null,
		string sitemapUrl = DefaultSitemapUrl,
		int maxPages = 0,
		bool dryRun = false,
		bool enableAiEnrichment = true,
		bool noSemantic = false,
		bool failFast = false,
		Cancel ctx = default
	)
	{
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

			// Build exclusion list
			var exclusions = DefaultExcludePaths.ToList();
			if (!string.IsNullOrWhiteSpace(excludePaths))
				exclusions.AddRange(excludePaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

			// Parse language filter
			var languageFilter = ParseLanguageFilter(languages);

			// Filter URLs
			var filteredUrls = FilterUrls(allUrls, exclusions, languageFilter);

			// Display analysis
			SpectreConsoleTheme.WriteSection("Site Analysis");
			var excludedUrls = allUrls.Where(u => !filteredUrls.Contains(u)).ToList();
			DisplaySiteAnalysis(filteredUrls, excludedUrls, exclusions);

			if (maxPages > 0 && filteredUrls.Count > maxPages)
			{
				filteredUrls = filteredUrls.Take(maxPages).ToList();
				SpectreConsoleTheme.WriteWarning($"Limited to [yellow]{maxPages:N0}[/] pages");
			}

			// Create transport once and reuse for cache and exporter
			var endpoints = configurationContext.Endpoints;
			var transport = ElasticsearchTransportFactory.Create(endpoints.Elasticsearch);

			// Load cache from Elasticsearch (if index exists)
			SpectreConsoleTheme.WriteSection("Cache Analysis");

			var cache = new Dictionary<string, CachedDocInfo>(StringComparer.OrdinalIgnoreCase);
			var crawlCache = new ElasticsearchCrawlCache(
				loggerFactory.CreateLogger<ElasticsearchCrawlCache>(),
				transport
			);

			const string indexAlias = "site-lexical";
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

			// Make crawl decisions
			var decisionMaker = new CrawlDecisionMaker(loggerFactory.CreateLogger<CrawlDecisionMaker>());
			var decisions = decisionMaker.MakeDecisions(filteredUrls, cache).ToList();
			var decisionStats = CrawlDecisionMaker.GetStats(decisions);

			// Find stale URLs (in cache but not in sitemap)
			var sitemapUrlSet = filteredUrls.Select(u => u.Location).ToHashSet(StringComparer.OrdinalIgnoreCase);
			var staleUrls = decisionMaker.FindStaleUrls(cache, sitemapUrlSet).ToList();

			SpectreConsoleTheme.WriteInfo(
				$"Crawl analysis: [green]{decisionStats.NewUrls:N0}[/] new, " +
				$"[grey]{decisionStats.UnchangedUrls:N0}[/] unchanged, " +
				$"[yellow]{decisionStats.PossiblyChangedUrls:N0}[/] to verify"
			);

			if (staleUrls.Count > 0)
				SpectreConsoleTheme.WriteWarning($"Found [red]{staleUrls.Count:N0}[/] stale URLs to delete");

			if (dryRun)
			{
				IndexingDisplay.DisplayDryRunWithCacheStats(decisionStats, staleUrls.Count);
				return;
			}

			// Get URLs to crawl (new + possibly changed)
			var urlsToCrawl = decisions
				.Where(d => d.Reason != CrawlReason.Unchanged)
				.ToList();

			// Create exporter with shared transport
			_ = enableAiEnrichment; // TODO: Integrate AI enrichment
			var exporter = new SiteIndexerExporter(
				loggerFactory,
				diagnostics,
				errorTracker,
				endpoints.Elasticsearch,
				transport,
				noSemantic
			);

			// Bootstrap indices (detect hash match for index reuse)
			await AnsiConsole.Status()
				.AutoRefresh(true)
				.Spinner(Spinner.Known.Dots)
				.StartAsync("[aqua]Bootstrapping Elasticsearch indices...[/]", async _ =>
				{
					await exporter.StartAsync(ctx);
				});

			// Display bootstrap status with hash details
			IndexingDisplay.DisplayBootstrapStatus(exporter.GetChannelInfo());

			// Crawl and index with progress display
			SpectreConsoleTheme.WriteSection("Crawling & Indexing");

			using var progress = new CrawlProgressContext();
			progress.ReportUrlDiscovered(urlsToCrawl.Count);

			var htmlExtractor = new SiteHtmlExtractor(loggerFactory.CreateLogger<SiteHtmlExtractor>());
			var processedCount = 0;
			var skippedNotModified = 0;
			var errorCount = 0;
			var startTime = DateTime.UtcNow;

			IReadOnlyList<IndexChannelInfo> channelInfo = [];

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
						errorCount++;
						progressCtx.ReportUrlFailed(result.Url, result.Error ?? "Unknown error");
						// Treat 404s as warnings (pages may have been removed)
						if (result.StatusCode == 404)
							diagnostics.EmitWarning(result.Url, $"Page not found: {result.Error}");
						else
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
					}
					catch (OperationCanceledException) when (effectiveToken.IsCancellationRequested)
					{
						break;
					}
					catch (Exception ex)
					{
						errorCount++;
						progressCtx.ReportUrlFailed(result.Url, ex.Message);
						diagnostics.EmitError(result.Url, $"Failed to process: {ex.Message}", ex);
					}
				}

				// Finalize and dispose inside progress context so it completes before showing "complete"
				await exporter.FinalizeAsync(ctx);
				channelInfo = exporter.GetChannelInfo().ToList();
				await exporter.DisposeAsync();
			});

			if (skippedNotModified > 0)
				SpectreConsoleTheme.WriteInfo($"Skipped [grey]{skippedNotModified:N0}[/] unchanged pages (HTTP 304)");

			var crawlTime = DateTime.UtcNow - startTime;

			// Display final summary
			var stats = progress.GetStats();
			IndexingDisplay.DisplayFinalSummary(stats, decisionStats, channelInfo.Count > 0 ? channelInfo : null);

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

	private static void DisplaySiteAnalysis(List<SitemapEntry> urls, List<SitemapEntry> excludedUrls, List<string> exclusionPatterns)
	{
		// Group by relevance
		var byRelevance = urls
			.GroupBy(u => GetRelevance(u.Location))
			.ToDictionary(g => g.Key, g => g.Count());

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

		// Category table
		var categoryTable = new Table()
			.Border(TableBorder.Rounded)
			.BorderColor(Color.Grey)
			.AddColumn("[aqua]Category[/]")
			.AddColumn(new TableColumn("[aqua]URLs[/]").RightAligned())
			.AddColumn("[aqua]Relevance[/]");

		foreach (var group in byCategory)
		{
			var (relevance, emoji, color) = PathInfo.GetValueOrDefault(
				group.Key,
				("medium", "📄", Color.White)
			);
			var displayName = group.Key.Trim('/');
			if (string.IsNullOrEmpty(displayName))
				displayName = "Root";

			_ = categoryTable.AddRow(
				$"{emoji} [{color}]{Markup.Escape(displayName)}[/]",
				$"[white]{group.Count():N0}[/]",
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
		var summaryPanel = new Panel(
			new Rows(
				new Markup($"[green]✓ Included:[/] [white]{urls.Count:N0}[/] URLs"),
				new Markup($"[red]✗ Excluded:[/] [white]{excludedUrls.Count:N0}[/] URLs"),
				new Markup($"[yellow]🌐 Languages:[/] [white]{byLanguage.Count}[/]"),
				new Markup($"[aqua]📂 Categories:[/] [white]{byCategory.Count}[/]")
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
				path = "/" + path[prefix.Length..];
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
	)
	{
		return urls
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
}
