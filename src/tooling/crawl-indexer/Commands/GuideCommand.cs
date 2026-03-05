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
/// Command for crawling and indexing legacy /guide documentation.
/// </summary>
public class GuideCommand(
	ILogger<GuideCommand> logger,
	ILoggerFactory loggerFactory,
	IDiagnosticsCollector diagnostics,
	IndexingErrorTracker errorTracker,
	IConfigurationContext configurationContext,
	ISitemapParser sitemapParser,
	IVersionDiscovery versionDiscovery,
	IAdaptiveCrawler crawler,
	CrawlerSettings crawlerSettings
)
{
	private const string DefaultSitemapUrl = "https://www.elastic.co/guide/sitemap.xml";
	private const string GuideBaseUrl = "https://www.elastic.co/guide/";

	/// <summary>
	/// Crawl and index legacy /guide documentation.
	/// </summary>
	/// <param name="versions">Comma-separated versions to crawl (default: auto-discover all 8.x + latest 7.x)</param>
	/// <param name="products">Comma-separated products to include (default: all)</param>
	/// <param name="sitemapUrl">Override sitemap location</param>
	/// <param name="maxPages">Limit pages to crawl (0 = unlimited)</param>
	/// <param name="dryRun">Discover URLs without crawling</param>
	/// <param name="enableAiEnrichment">Enable AI enrichment (default: true)</param>
	/// <param name="noSemantic">Skip semantic index</param>
	/// <param name="failFast">Stop immediately when an indexing error occurs</param>
	/// <param name="rps">Rate limit in requests per second (0 or omit for unlimited)</param>
	/// <param name="ctx">Cancellation token</param>
	[Command("")]
	public async Task RunAsync(
		string? versions = null,
		string? products = null,
		string sitemapUrl = DefaultSitemapUrl,
		int maxPages = 0,
		bool dryRun = false,
		bool enableAiEnrichment = true,
		bool noSemantic = false,
		bool failFast = false,
		int? rps = null,
		Cancel ctx = default
	)
	{
		_ = noSemantic; // TODO: Pass to orchestrator when single-channel mode is supported

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
			SpectreConsoleTheme.WriteHeader("guide");

			// Display crawler configuration
			CrawlerConfigDisplay.DisplayConfiguration(crawlerSettings);

			if (dryRun)
				SpectreConsoleTheme.WriteDryRunBanner();

			// Parse sitemap with progress spinner
			SpectreConsoleTheme.WriteSection("Sitemap Discovery");

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
					var task = progressCtx.AddTask("[aqua]🌍 Fetching sitemaps[/]", maxValue: 1);

					allUrls = await sitemapParser.ParseAsync(
						new Uri(sitemapUrl),
						(current, total, url) =>
						{
							task.MaxValue = total;
							task.Value = current;
							var shortUrl = url.Length > 60 ? "..." + url[^57..] : url;
							task.Description = $"[aqua]📄[/] [dim]{Markup.Escape(shortUrl)}[/]";
						},
						ctx
					);

					task.Value = task.MaxValue;
					task.Description = "[green]✓[/] Sitemap discovery complete";
				});

			AnsiConsole.MarkupLine("[dim]Filtering guide URLs...[/]");
			var guideUrls = allUrls
				.Where(u => u.Location.StartsWith(GuideBaseUrl, StringComparison.OrdinalIgnoreCase))
				.ToList();

			SpectreConsoleTheme.WriteSuccess($"Found [yellow]{guideUrls.Count:N0}[/] guide URLs in sitemap");

			if (allUrls.Count == 0)
			{
				diagnostics.EmitError("sitemap", "Sitemap returned 0 URLs - nothing to crawl");
				SpectreConsoleTheme.WriteError("Sitemap returned 0 URLs - nothing to crawl");
				return;
			}

			if (guideUrls.Count == 0)
			{
				diagnostics.EmitError("sitemap", "Sitemap contained no /guide/ URLs - nothing to crawl");
				SpectreConsoleTheme.WriteError("Sitemap contained no /guide/ URLs - nothing to crawl");
				return;
			}

			// Discover versions
			SpectreConsoleTheme.WriteSection("Version Discovery");

			var versionFilter = ParseVersionFilter(versions);
			var productFilter = ParseProductFilter(products);

			IReadOnlyList<DiscoveredVersion> discoveredVersions;
			if (versionFilter.Count > 0)
			{
				discoveredVersions = versionFilter
					.Select(v => new DiscoveredVersion(v, v.StartsWith("8.") ? 8 : 7))
					.ToList();

				SpectreConsoleTheme.WriteInfo($"Using specified versions: [cyan]{string.Join(", ", versionFilter)}[/]");
			}
			else
			{
				discoveredVersions = versionDiscovery.DiscoverVersions(guideUrls);
				DisplayDiscoveredVersions(discoveredVersions);
			}

			// Filter URLs
			var filteredUrls = FilterUrls(guideUrls, discoveredVersions, productFilter);

			if (maxPages > 0 && filteredUrls.Count > maxPages)
			{
				filteredUrls = filteredUrls.Take(maxPages).ToList();
				SpectreConsoleTheme.WriteWarning($"Limited to [yellow]{maxPages:N0}[/] pages");
			}

			SpectreConsoleTheme.WriteSuccess($"[yellow]{filteredUrls.Count:N0}[/] URLs ready for processing");

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

			var indexAlias = GuideIndexerExporter.ResolveLexicalReadAlias(buildType, environment);
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
			using var exporter = new GuideIndexerExporter(
				loggerFactory,
				diagnostics,
				errorTracker,
				endpoints.Elasticsearch,
				transport,
				buildType,
				environment,
				configurationContext.SearchConfiguration,
				enableAiEnrichment
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

			var htmlExtractor = new GuideHtmlExtractor(loggerFactory.CreateLogger<GuideHtmlExtractor>());
			var processedCount = 0;
			var skippedNotModified = 0;
			var errorCount = 0;
			var startTime = DateTime.UtcNow;

			await progress.RunWithLiveAsync("Crawling guide documentation", async (progressCtx, _) =>
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

						if (result.StatusCode == 404)
						{
							// 404 is unavailable, not a failure
							progressCtx.ReportUrlUnavailable(result.Url);
							diagnostics.EmitWarning(result.Url, $"Page not found: {result.Error}");
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
						var document = await htmlExtractor.ExtractAsync(
							result.Url,
							result.Content!,
							result.LastModified,
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

				// Finalize inside progress context so it completes before showing "complete"
				await exporter.FinalizeAsync(ctx);
			});

			if (skippedNotModified > 0)
				SpectreConsoleTheme.WriteInfo($"Skipped [grey]{skippedNotModified:N0}[/] unchanged pages (HTTP 304)");

			var crawlTime = DateTime.UtcNow - startTime;

			// Display final summary
			var stats = progress.GetStats();
			IndexingDisplay.DisplayFinalSummary(stats, decisionStats);

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

	private static void DisplayDiscoveredVersions(IReadOnlyList<DiscoveredVersion> versions)
	{
		var v8x = versions.Where(v => v.MajorVersion == 8).OrderByDescending(v => v.Version).ToList();
		var v7x = versions.Where(v => v.MajorVersion == 7).OrderByDescending(v => v.Version).ToList();

		var tree = new Tree("[aqua]📦 Discovered Versions[/]");

		if (v8x.Count > 0)
		{
			var node8 = tree.AddNode("[green]8.x[/]");
			var versionList = string.Join(", ", v8x.Take(10).Select(v => $"[cyan]{v.Version}[/]"));
			if (v8x.Count > 10)
				versionList += $" [dim](+{v8x.Count - 10} more)[/]";
			_ = node8.AddNode(versionList);
		}

		if (v7x.Count > 0)
		{
			var node7 = tree.AddNode("[yellow]7.x[/]");
			var latest = v7x.First();
			_ = node7.AddNode($"[cyan]{latest.Version}[/] [dim](latest only)[/]");
		}

		AnsiConsole.Write(tree);
		AnsiConsole.WriteLine();
	}

	private static HashSet<string> ParseVersionFilter(string? versions) =>
		string.IsNullOrWhiteSpace(versions)
			? []
			: versions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

	private static HashSet<string> ParseProductFilter(string? products) =>
		string.IsNullOrWhiteSpace(products)
			? []
			: products.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

	private static List<SitemapEntry> FilterUrls(
		List<SitemapEntry> urls,
		IReadOnlyList<DiscoveredVersion> versions,
		HashSet<string> productFilter
	)
	{
		var versionSet = versions.Select(v => v.Version).ToHashSet(StringComparer.OrdinalIgnoreCase);

		return urls
			.Where(u =>
			{
				// Parse URL to extract version
				var match = GuideUrlParser.Parse(u.Location);
				if (match is null)
					return false;

				// Filter by version
				if (!versionSet.Contains(match.Value.Version))
					return false;

				// Filter by product if specified
				if (productFilter.Count > 0 && !productFilter.Contains(match.Value.Product))
					return false;

				return true;
			})
			.ToList();
	}
}

internal static class GuideUrlParser
{
	// Pattern: /guide/en/{product}/{subpath}/{version}/{page}
	// Example: /guide/en/elasticsearch/reference/8.15/index.html

	public static (string Product, string Version)? Parse(string url)
	{
		if (!url.Contains("/guide/", StringComparison.OrdinalIgnoreCase))
			return null;

		var uri = new Uri(url);
		var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

		// Minimum: guide, en, product, subpath, version, page
		if (segments.Length < 6)
			return null;

		if (!segments[0].Equals("guide", StringComparison.OrdinalIgnoreCase))
			return null;

		// segments[1] = language (en, etc.)
		// segments[2] = product (elasticsearch, kibana, etc.)
		// segments[3] = subpath (reference, guide, etc.)
		// segments[4] = version (8.15, 7.17, current, master, etc.)

		var product = segments[2];
		var version = segments[4];

		// Accept numeric versions (8.15, 7.17) and version aliases (current, master)
		if (!IsVersionString(version))
			return null;

		return (product, version);
	}

	private static bool IsVersionString(string s)
	{
		// Accept numeric versions (8.15, 7.17)
		if (s.Length >= 1 && char.IsDigit(s[0]) && s.Contains('.'))
			return true;

		// Accept version aliases
		return s.Equals("current", StringComparison.OrdinalIgnoreCase) ||
			   s.Equals("master", StringComparison.OrdinalIgnoreCase);
	}
}
