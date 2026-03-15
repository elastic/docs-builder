// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using CrawlIndexer.Caching;
using CrawlIndexer.Crawling;
using CrawlIndexer.Display;
using CrawlIndexer.Indexing;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Tooling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer;

public static class CrawlIndexerTooling
{
	public static TBuilder AddCrawlIndexerToolingDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
	{
		// Determine if we should use interactive Spectre console
		var isInteractive = !Console.IsOutputRedirected
			&& string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
			&& string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

		// Configure logging: use SpectreLogger for interactive, keep default for CI/redirected
		if (isInteractive)
		{
			_ = builder.Logging.ClearProviders();
			_ = builder.Logging.AddProvider(new SpectreLoggerProvider());
		}

		// Register error tracker as singleton so it can be injected into commands
		_ = builder.Services.AddSingleton<IndexingErrorTracker>();

		// Wire up diagnostics with the error tracker as an output
		_ = builder.AddToolingDefaults(sp =>
		{
			var errorTracker = sp.GetRequiredService<IndexingErrorTracker>();
			return new CrawlIndexerDiagnosticsCollector(errorTracker);
		});

		// Add crawl-indexer specific services with automatic gzip/deflate decompression
		// and custom resilience settings (30s timeout for slow pages)
		_ = builder.Services
			.AddHttpClient<IAdaptiveCrawler, AdaptiveCrawler>()
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			})
			.AddStandardResilienceHandler(ConfigureCrawlerResilience)
			.Services
			.AddHttpClient<ISitemapParser, SitemapParser>()
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			})
			.Services
			.AddSingleton<IVersionDiscovery, VersionDiscovery>()
			.AddSingleton<CrawlDecisionMaker>()
			.AddSingleton<CrawlerSettings>()
			.AddSingleton<CrawlerRateLimiter>();

		// Set LiveDisplayState based on interactivity
		LiveDisplayState.SuppressInfoLogs = isInteractive;

		return builder;
	}

	private static void ConfigureCrawlerResilience(HttpStandardResilienceOptions options)
	{
		// Increase timeouts for slow pages
		options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
		options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);

		// Circuit breaker sampling duration must be at least 2x attempt timeout
		options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
	}
}
