// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CrawlIndexer.Caching;

namespace CrawlIndexer.Crawling;

/// <summary>
/// Discovered translation of an English page.
/// </summary>
public record DiscoveredTranslation(string EnglishUrl, string TranslatedUrl, string Language);

/// <summary>
/// Summary of translation discovery results.
/// </summary>
public record TranslationDiscoveryStats(
	int TotalProbed,
	int TranslationsFound,
	int FromCache,
	IReadOnlyDictionary<string, int> ByLanguage
);

/// <summary>
/// Discovers translated versions of English pages by probing URLs with HEAD requests.
/// Uses a persistent cache to avoid re-probing known translations.
/// </summary>
public interface ITranslationDiscovery
{
	/// <summary>
	/// Discovers translations for English pages that are new or possibly changed.
	/// Uses cached translations when available, probes only when necessary.
	/// </summary>
	/// <param name="englishDecisions">Crawl decisions for English pages.</param>
	/// <param name="cache">Existing document cache from Elasticsearch.</param>
	/// <param name="languageFilter">Languages to probe (empty = all).</param>
	/// <param name="onTranslationFound">Callback for each discovered translation.</param>
	/// <param name="revalidateNotFound">If true, re-probe URLs that were previously not found.</param>
	/// <param name="progress">Progress reporter.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>Discovered translations and statistics.</returns>
	Task<TranslationDiscoveryStats> DiscoverAsync(
		IReadOnlyList<CrawlDecision> englishDecisions,
		IReadOnlyDictionary<string, CachedDocInfo> cache,
		HashSet<string> languageFilter,
		Action<DiscoveredTranslation> onTranslationFound,
		bool revalidateNotFound = false,
		IProgress<(int probed, int found, string? currentUrl)>? progress = null,
		CancellationToken ct = default
	);
}
