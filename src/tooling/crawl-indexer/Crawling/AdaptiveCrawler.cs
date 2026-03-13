// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CrawlIndexer.Caching;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Crawling;

/// <summary>
/// HTTP crawler that uses standard resilience handler for throttling and retries.
/// Uses a semaphore to limit concurrent requests and shared rate limiter for RPS control.
/// </summary>
public class AdaptiveCrawler(
	ILogger<AdaptiveCrawler> logger,
	HttpClient httpClient,
	CrawlerRateLimiter rateLimiter,
	CrawlerSettings settings
) : IAdaptiveCrawler, IDisposable
{
	private SemaphoreSlim? _semaphore;

	private SemaphoreSlim Semaphore => _semaphore ??= new(settings.Concurrency);

	public void Dispose()
	{
		_semaphore?.Dispose();
		GC.SuppressFinalize(this);
	}

	public IAsyncEnumerable<CrawlResult> CrawlAsync(
		IEnumerable<SitemapEntry> urls,
		Cancel ctx = default
	) =>
		CrawlAsync(urls.Select(u => new CrawlDecision(u, CrawlReason.New)), ctx);

	public async IAsyncEnumerable<CrawlResult> CrawlAsync(
		IEnumerable<CrawlDecision> decisions,
		[EnumeratorCancellation] Cancel ctx = default
	)
	{
		var decisionList = decisions.ToList();
		if (decisionList.Count == 0)
			yield break;

		logger.LogInformation("Starting crawl of {Count} URLs", decisionList.Count);

		var channel = Channel.CreateBounded<CrawlResult>(new BoundedChannelOptions(100)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = false
		});

		var crawlTasks = decisionList.Select(async decision =>
		{
			await Semaphore.WaitAsync(ctx);
			try
			{
				var result = await CrawlUrlAsync(decision.Entry, decision.Cached, ctx);
				await channel.Writer.WriteAsync(result, ctx);
			}
			finally
			{
				_ = Semaphore.Release();
			}
		}).ToList();

		// Complete channel when all tasks are done
		_ = Task.WhenAll(crawlTasks).ContinueWith(_ => channel.Writer.Complete(), ctx);

		// Yield results as they arrive
		await foreach (var result in channel.Reader.ReadAllAsync(ctx))
		{
			yield return result;
		}
	}

	private async Task<CrawlResult> CrawlUrlAsync(SitemapEntry entry, CachedDocInfo? cached, Cancel ctx)
	{
		try
		{
			// Acquire rate limiter permit (may be null if rate limiting disabled)
			using var lease = await rateLimiter.AcquireAsync(ctx);

			using var request = new HttpRequestMessage(HttpMethod.Get, entry.Location);
			request.Headers.Add("User-Agent", "Elastic-Crawl-Indexer/1.0 (+https://elastic.co)");
			request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

			// Add conditional headers if we have cached values
			if (cached?.HttpEtag is not null)
			{
				try
				{
					// ETag may or may not have quotes - normalize
					var conditionalEtag = cached.HttpEtag;
					if (!conditionalEtag.StartsWith('"'))
						conditionalEtag = $"\"{conditionalEtag}\"";
					request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(conditionalEtag));
				}
				catch (FormatException)
				{
					// Invalid ETag format - skip conditional request
					logger.LogDebug("Invalid ETag format for {Url}: {ETag}", entry.Location, cached.HttpEtag);
				}
			}

			if (cached?.HttpLastModified is not null)
				request.Headers.IfModifiedSince = cached.HttpLastModified;

			// Use ResponseHeadersRead to check status before downloading body
			using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ctx);

			// Handle 304 Not Modified - no body to read
			if (response.StatusCode == HttpStatusCode.NotModified)
			{
				logger.LogDebug("Not modified (304): {Url}", entry.Location);
				return CrawlResult.NotModifiedResult(entry.Location, cached!.Hash);
			}

			logger.LogDebug("Crawled {Url} (status: {StatusCode})",
				entry.Location, (int)response.StatusCode);

			// HTTP 406 Not Acceptable - fatal error, stop crawling to avoid amplifying the problem
			if (response.StatusCode == HttpStatusCode.NotAcceptable)
			{
				logger.LogError("Fatal: HTTP 406 Not Acceptable for {Url} - stopping crawl", entry.Location);
				return CrawlResult.Fatal(entry.Location, "HTTP 406 Not Acceptable - server rejecting requests", 406);
			}

			// Check status before reading body - avoids downloading content for errors
			if (!response.IsSuccessStatusCode)
				return CrawlResult.Failed(entry.Location, $"HTTP {(int)response.StatusCode}", (int)response.StatusCode);

			// Only read body for successful responses
			var content = await response.Content.ReadAsStringAsync(ctx);

			// Extract caching headers
			var etag = response.Headers.ETag?.Tag?.Trim('"');
			var httpLastModified = response.Content.Headers.LastModified;

			// Prefer Last-Modified header, fall back to sitemap lastmod
			var lastModified = httpLastModified ?? entry.LastModified;

			return CrawlResult.Succeeded(entry.Location, content, lastModified, etag, httpLastModified);
		}
		catch (HttpRequestException ex)
		{
			logger.LogWarning("Failed to crawl {Url}: {Message}", entry.Location, ex.Message);
			return CrawlResult.Failed(entry.Location, ex.Message);
		}
		catch (TaskCanceledException) when (ctx.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Unexpected error crawling {Url}", entry.Location);
			return CrawlResult.Failed(entry.Location, ex.Message);
		}
	}
}
