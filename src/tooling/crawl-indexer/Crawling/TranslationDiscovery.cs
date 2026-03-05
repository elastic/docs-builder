// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.Net;
using CrawlIndexer.Caching;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Discovers translated versions of English pages by probing URLs with GET requests.
/// Uses a persistent cache for optimal performance, including caching negatives.
/// </summary>
public class TranslationDiscovery(
	HttpClient httpClient,
	TranslationCacheService cacheService,
	CrawlerRateLimiter rateLimiter,
	CrawlerSettings settings,
	ILogger<TranslationDiscovery> logger
) : ITranslationDiscovery, IDisposable
{
	private SemaphoreSlim? _semaphore;

	private SemaphoreSlim Semaphore => _semaphore ??= new(settings.Concurrency);

	public void Dispose()
	{
		_semaphore?.Dispose();
		GC.SuppressFinalize(this);
	}

	// Language prefixes used on elastic.co for translations
	private static readonly string[] TranslationPrefixes = ["de", "fr", "es", "jp", "kr", "cn", "pt"];

	// Categories that DON'T have translations (skip probing to save requests)
	private static readonly string[] NoTranslationPaths = ["/webinars/", "/search-labs/", "/observability-labs/"];

	public async Task<TranslationDiscoveryStats> DiscoverAsync(
		IReadOnlyList<CrawlDecision> englishDecisions,
		IReadOnlyDictionary<string, CachedDocInfo> cache,
		HashSet<string> languageFilter,
		Action<DiscoveredTranslation> onTranslationFound,
		bool revalidateNotFound = false,
		IProgress<(int probed, int found, string? currentUrl)>? progress = null,
		CancellationToken ct = default
	)
	{
		// Load translation cache from disk
		var translationCache = await cacheService.LoadAsync(ct);

		// Filter to pages that need probing (skip unchanged)
		var toProbe = englishDecisions
			.Where(d => d.Reason != CrawlReason.Unchanged)
			.Where(d => !NoTranslationPaths.Any(p =>
				d.Entry.Location.Contains(p, StringComparison.OrdinalIgnoreCase)))
			.ToList();

		// Determine which languages to probe
		var languages = languageFilter.Count > 0
			? TranslationPrefixes.Where(l => languageFilter.Contains(l)).ToArray()
			: TranslationPrefixes;

		if (toProbe.Count == 0 || languages.Length == 0)
		{
			logger.LogDebug("No pages or languages to probe for translations");
			return new TranslationDiscoveryStats(0, 0, 0, new Dictionary<string, int>());
		}

		// Pre-flight check: pick a random blog URL to validate for 406 before firing many requests
		var blogUrl = toProbe
			.FirstOrDefault(d => d.Entry.Location.Contains("/blog/", StringComparison.OrdinalIgnoreCase));
		if (blogUrl is not null)
		{
			var testUrl = BuildTranslatedUrl(blogUrl.Entry.Location, languages[0]);
			logger.LogDebug("Pre-flight 406 check: {Url}", testUrl);

			var preflightResult = await CheckUrlAsync(testUrl, ct);
			if (preflightResult == ProbeResult.Fatal406)
			{
				logger.LogError("Pre-flight check failed with HTTP 406 - aborting translation discovery");
				throw new TranslationDiscoveryException("HTTP 406 Not Acceptable - server rejecting translation requests");
			}
		}

		logger.LogInformation(
			"Probing {PageCount} English pages for translations in {LangCount} languages (revalidate: {Revalidate})",
			toProbe.Count, languages.Length, revalidateNotFound
		);

		// Build probe requests, separating cached from new
		var cachedTranslations = new List<DiscoveredTranslation>();
		var skippedNotFound = 0;
		var probeRequests = new List<(CrawlDecision Decision, string Language, string TranslatedUrl, bool InEsCache)>();

		foreach (var decision in toProbe)
		{
			var pathKey = TranslationCacheService.GetPathKey(decision.Entry.Location);

			// Check if we have cached translation info for this path
			if (TranslationCacheService.TryGetCachedTranslations(translationCache, decision.Entry.Location, out var cachedLanguages) &&
				cachedLanguages is not null)
			{
				// Use cached translations
				foreach (var lang in languages)
				{
					if (cachedLanguages.Contains(lang))
					{
						var translatedUrl = BuildTranslatedUrl(decision.Entry.Location, lang);
						cachedTranslations.Add(new DiscoveredTranslation(decision.Entry.Location, translatedUrl, lang));
					}
					else if (!revalidateNotFound)
					{
						// Check if this was previously probed and not found
						var notFoundKey = TranslationCache.NotFoundKey(pathKey, lang);
						if (translationCache.NotFound.Contains(notFoundKey))
						{
							skippedNotFound++;
							continue; // Skip - we know it doesn't exist
						}

						// Not in cache at all - need to probe
						var translatedUrl = BuildTranslatedUrl(decision.Entry.Location, lang);
						var inEsCache = cache.ContainsKey(translatedUrl);
						probeRequests.Add((decision, lang, translatedUrl, inEsCache));
					}
					else
					{
						// Revalidating - probe even if previously not found
						var translatedUrl = BuildTranslatedUrl(decision.Entry.Location, lang);
						var inEsCache = cache.ContainsKey(translatedUrl);
						probeRequests.Add((decision, lang, translatedUrl, inEsCache));
					}
				}
			}
			else
			{
				// No cache entry for this path at all
				foreach (var lang in languages)
				{
					// Check not-found cache unless revalidating
					if (!revalidateNotFound)
					{
						var notFoundKey = TranslationCache.NotFoundKey(pathKey, lang);
						if (translationCache.NotFound.Contains(notFoundKey))
						{
							skippedNotFound++;
							continue; // Skip - we know it doesn't exist
						}
					}

					var translatedUrl = BuildTranslatedUrl(decision.Entry.Location, lang);
					var inEsCache = cache.ContainsKey(translatedUrl);
					probeRequests.Add((decision, lang, translatedUrl, inEsCache));
				}
			}
		}

		// Report cached translations immediately
		var translationsByLanguage = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (var translation in cachedTranslations)
		{
			onTranslationFound(translation);
			_ = translationsByLanguage.AddOrUpdate(translation.Language, 1, (_, count) => count + 1);
		}

		var fromCache = cachedTranslations.Count;
		var probeCount = 0;
		var foundCount = cachedTranslations.Count;

		if (skippedNotFound > 0)
			logger.LogInformation("Skipped {Count} URLs from not-found cache", skippedNotFound);

		if (probeRequests.Count == 0)
		{
			logger.LogInformation("All translations resolved from cache ({Count} found, {NotFound} not-found)", fromCache, skippedNotFound);
			return new TranslationDiscoveryStats(0, foundCount, fromCache, new Dictionary<string, int>(translationsByLanguage));
		}

		logger.LogInformation(
			"Found {Cached} translations in cache, probing {Remaining} URLs",
			fromCache, probeRequests.Count
		);

		// Track new discoveries and not-found for cache updates (lock-free)
		var newDiscoveries = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.OrdinalIgnoreCase);
		var newNotFound = new ConcurrentBag<string>();
		var fatal406 = false;

		// Probe in parallel with concurrency limiting
		var tasks = probeRequests.Select(async probe =>
		{
			// Check if we should abort due to 406
			if (Volatile.Read(ref fatal406))
				return;

			DiscoveredTranslation? result = null;

			// If already in Elasticsearch cache, it exists - no need to probe
			if (probe.InEsCache)
			{
				result = new DiscoveredTranslation(probe.Decision.Entry.Location, probe.TranslatedUrl, probe.Language);
			}
			else
			{
				await Semaphore.WaitAsync(ct);
				try
				{
					// Check again after acquiring semaphore
					if (Volatile.Read(ref fatal406))
						return;

					var probeResult = await CheckUrlAsync(probe.TranslatedUrl, ct);

					if (probeResult == ProbeResult.Fatal406)
					{
						Volatile.Write(ref fatal406, true);
						logger.LogError("HTTP 406 received for {Url} - stopping translation discovery", probe.TranslatedUrl);
						return;
					}

					if (probeResult == ProbeResult.Exists)
					{
						result = new DiscoveredTranslation(probe.Decision.Entry.Location, probe.TranslatedUrl, probe.Language);
						logger.LogDebug("Translation discovered: {Url}", probe.TranslatedUrl);
					}
					else
					{
						// Not found - record in cache
						var pathKey = TranslationCacheService.GetPathKey(probe.Decision.Entry.Location);
						var notFoundKey = TranslationCache.NotFoundKey(pathKey, probe.Language);
						newNotFound.Add(notFoundKey);
					}
				}
				finally
				{
					_ = Semaphore.Release();
				}
			}

			// Update progress and tracking (lock-free)
			var currentProbeCount = Interlocked.Increment(ref probeCount);
			if (result is not null)
			{
				var currentFoundCount = Interlocked.Increment(ref foundCount);
				onTranslationFound(result);

				// Track for cache update
				var pathKey = TranslationCacheService.GetPathKey(result.EnglishUrl);
				var langs = newDiscoveries.GetOrAdd(pathKey, _ => []);
				langs.Add(result.Language);

				// Update by-language count
				_ = translationsByLanguage.AddOrUpdate(result.Language, 1, (_, count) => count + 1);

				progress?.Report((fromCache + currentProbeCount, currentFoundCount, probe.TranslatedUrl));
			}
			else
			{
				progress?.Report((fromCache + currentProbeCount, Volatile.Read(ref foundCount), probe.TranslatedUrl));
			}
		}).ToList();

		await Task.WhenAll(tasks);

		// Check if we aborted due to 406
		if (fatal406)
			throw new TranslationDiscoveryException("HTTP 406 Not Acceptable - server rejecting translation requests");

		// Update cache with new discoveries and not-found entries
		if (!newDiscoveries.IsEmpty || !newNotFound.IsEmpty)
		{
			var updatedEntries = new Dictionary<string, HashSet<string>>(translationCache.Entries);
			foreach (var (pathKey, langs) in newDiscoveries)
			{
				if (updatedEntries.TryGetValue(pathKey, out var existing))
				{
					foreach (var lang in langs)
						_ = existing.Add(lang);
				}
				else
					updatedEntries[pathKey] = [.. langs];
			}

			// Merge not-found entries
			var updatedNotFound = new HashSet<string>(translationCache.NotFound);
			foreach (var key in newNotFound)
				_ = updatedNotFound.Add(key);

			// Remove from not-found if now found
			foreach (var (pathKey, langs) in newDiscoveries)
			{
				foreach (var lang in langs)
					_ = updatedNotFound.Remove(TranslationCache.NotFoundKey(pathKey, lang));
			}

			var updatedCache = new TranslationCache
			{
				Entries = updatedEntries,
				NotFound = updatedNotFound,
				LastUpdated = DateTimeOffset.UtcNow
			};

			await cacheService.SaveAsync(updatedCache, ct);
		}

		logger.LogInformation(
			"Translation discovery complete: {Found} translations ({FromCache} cached, {Probed} probed, {NewNotFound} new not-found)",
			foundCount, fromCache, probeCount, newNotFound.Count
		);

		return new TranslationDiscoveryStats(probeCount, foundCount, fromCache, new Dictionary<string, int>(translationsByLanguage));
	}

	private static string BuildTranslatedUrl(string englishUrl, string lang)
	{
		// https://www.elastic.co/blog/x → https://www.elastic.co/de/blog/x
		var uri = new Uri(englishUrl);
		return $"{uri.Scheme}://{uri.Host}/{lang}{uri.AbsolutePath}";
	}

	private enum ProbeResult { Exists, NotFound, Fatal406 }

	private async Task<ProbeResult> CheckUrlAsync(string url, CancellationToken ct)
	{
		try
		{
			// Acquire rate limiter permit (may be null if rate limiting disabled)
			using var lease = await rateLimiter.AcquireAsync(ct);

			// Use GET with ResponseHeadersRead - more reliable than HEAD for 404 detection
			using var request = new HttpRequestMessage(HttpMethod.Get, url);
			request.Headers.Add("User-Agent", "Elastic-Crawl-Indexer/1.0 (+https://elastic.co)");

			using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

			// HTTP 406 Not Acceptable - fatal error
			if (response.StatusCode == HttpStatusCode.NotAcceptable)
				return ProbeResult.Fatal406;

			// 200 or 301/302 redirect = exists
			if (response.IsSuccessStatusCode ||
				response.StatusCode == HttpStatusCode.MovedPermanently ||
				response.StatusCode == HttpStatusCode.Found)
				return ProbeResult.Exists;

			return ProbeResult.NotFound;
		}
		catch (HttpRequestException ex)
		{
			logger.LogDebug("GET request failed for {Url}: {Error}", url, ex.Message);
			return ProbeResult.NotFound;
		}
		catch (TaskCanceledException) when (!ct.IsCancellationRequested)
		{
			logger.LogDebug("GET request timed out for {Url}", url);
			return ProbeResult.NotFound;
		}
	}
}

/// <summary>
/// Exception thrown when translation discovery encounters a fatal error (e.g., HTTP 406).
/// </summary>
public class TranslationDiscoveryException(string message) : Exception(message);
