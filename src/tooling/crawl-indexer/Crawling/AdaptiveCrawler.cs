// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using CrawlIndexer.Caching;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Adaptive HTTP crawler that adjusts concurrency based on server response times.
/// </summary>
public class AdaptiveCrawler(ILogger<AdaptiveCrawler> logger, HttpClient httpClient) : IAdaptiveCrawler
{
	private const int InitialConcurrency = 10;
	private const int MinConcurrency = 2;
	private const int MaxConcurrency = 50;
	private const int SlowResponseThresholdMs = 2000;
	private const int FastResponseThresholdMs = 500;
	private const int AdjustmentIntervalRequests = 50;

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

		logger.LogInformation("Starting adaptive crawl of {Count} URLs", decisionList.Count);

		var currentConcurrency = InitialConcurrency;
		var semaphore = new SemaphoreSlim(currentConcurrency);
		var responseTimes = new List<long>();
		var requestCount = 0;
		var lockObj = new object();

		var channel = Channel.CreateBounded<CrawlResult>(new BoundedChannelOptions(100)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = false
		});

		var crawlTasks = decisionList.Select(async decision =>
		{
			await semaphore.WaitAsync(ctx);
			try
			{
				var result = await CrawlUrlAsync(decision.Entry, decision.Cached, ctx);

				// Track response time for adaptive adjustment
				if (result.Success)
				{
					lock (lockObj)
					{
						requestCount++;

						// Adjust concurrency periodically
						if (requestCount % AdjustmentIntervalRequests == 0)
							AdjustConcurrency(semaphore, responseTimes, ref currentConcurrency);
					}
				}

				await channel.Writer.WriteAsync(result, ctx);
			}
			finally
			{
				_ = semaphore.Release();
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
		var stopwatch = Stopwatch.StartNew();

		try
		{
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

			using var response = await httpClient.SendAsync(request, ctx);
			stopwatch.Stop();

			// Handle 304 Not Modified
			if (response.StatusCode == HttpStatusCode.NotModified)
			{
				logger.LogDebug("Not modified (304): {Url} in {ElapsedMs}ms", entry.Location, stopwatch.ElapsedMilliseconds);
				return CrawlResult.NotModifiedResult(entry.Location, cached!.Hash);
			}

			logger.LogDebug("Crawled {Url} in {ElapsedMs}ms (status: {StatusCode})",
				entry.Location, stopwatch.ElapsedMilliseconds, (int)response.StatusCode);

			if (!response.IsSuccessStatusCode)
				return CrawlResult.Failed(entry.Location, $"HTTP {(int)response.StatusCode}", (int)response.StatusCode);

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
			stopwatch.Stop();
			logger.LogWarning("Failed to crawl {Url}: {Message}", entry.Location, ex.Message);
			return CrawlResult.Failed(entry.Location, ex.Message);
		}
		catch (TaskCanceledException) when (ctx.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			logger.LogWarning(ex, "Unexpected error crawling {Url}", entry.Location);
			return CrawlResult.Failed(entry.Location, ex.Message);
		}
	}

	private void AdjustConcurrency(SemaphoreSlim semaphore, List<long> responseTimes, ref int currentConcurrency)
	{
		if (responseTimes.Count == 0)
			return;

		var avgResponseTime = responseTimes.Average();
		responseTimes.Clear();

		var oldConcurrency = currentConcurrency;

		if (avgResponseTime > SlowResponseThresholdMs && currentConcurrency > MinConcurrency)
		{
			// Slow responses - reduce concurrency
			currentConcurrency = Math.Max(MinConcurrency, currentConcurrency - 2);
		}
		else if (avgResponseTime < FastResponseThresholdMs && currentConcurrency < MaxConcurrency)
		{
			// Fast responses - increase concurrency
			currentConcurrency = Math.Min(MaxConcurrency, currentConcurrency + 2);
		}

		if (currentConcurrency != oldConcurrency)
		{
			logger.LogInformation("Adjusting concurrency from {Old} to {New} (avg response time: {AvgMs}ms)",
				oldConcurrency, currentConcurrency, avgResponseTime);

			// Adjust semaphore
			var diff = currentConcurrency - oldConcurrency;
			if (diff > 0)
			{
				_ = semaphore.Release(diff);
			}
			// Note: We can't easily reduce semaphore count, but the reduced concurrency
			// will take effect as slots are released
		}
	}
}
