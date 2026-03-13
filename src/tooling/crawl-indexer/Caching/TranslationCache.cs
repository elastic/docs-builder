// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Elastic.Documentation.Configuration;
using Microsoft.Extensions.Logging;

namespace CrawlIndexer.Caching;

/// <summary>
/// Cache of discovered translations, stored in AppLocalData.
/// </summary>
public record TranslationCache
{
	/// <summary>Maps URL path to languages that have translations.</summary>
	public Dictionary<string, HashSet<string>> Entries { get; init; } = [];

	/// <summary>Set of "path:lang" keys that have been probed and found to not exist.</summary>
	public HashSet<string> NotFound { get; init; } = [];

	/// <summary>When this cache was last updated.</summary>
	public DateTimeOffset LastUpdated { get; init; }

	/// <summary>Creates a cache key for not-found entries.</summary>
	public static string NotFoundKey(string pathKey, string lang) => $"{pathKey}:{lang}";
}

/// <summary>
/// JSON source generation context for translation cache serialization.
/// </summary>
[JsonSerializable(typeof(TranslationCache))]
[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase
)]
internal sealed partial class TranslationCacheContext : JsonSerializerContext;

/// <summary>
/// Service for loading and saving translation cache from AppLocalData.
/// </summary>
public class TranslationCacheService(ILogger<TranslationCacheService> logger)
{
	private static readonly string CachePath =
		Path.Combine(Paths.ApplicationData.FullName, "translations", "site-translations.json");

	/// <summary>
	/// Loads the translation cache from disk.
	/// </summary>
	public async Task<TranslationCache> LoadAsync(CancellationToken ct = default)
	{
		if (!File.Exists(CachePath))
		{
			logger.LogDebug("Translation cache not found at {Path}", CachePath);
			return new TranslationCache { LastUpdated = DateTimeOffset.MinValue };
		}

		try
		{
			var json = await File.ReadAllTextAsync(CachePath, ct);
			var cache = JsonSerializer.Deserialize(json, TranslationCacheContext.Default.TranslationCache);
			if (cache is null)
			{
				logger.LogWarning("Failed to deserialize translation cache, returning empty");
				return new TranslationCache { LastUpdated = DateTimeOffset.MinValue };
			}

			// Ensure NotFound is never null (old cache files won't have this field)
			if (cache.NotFound is null)
			{
				cache = cache with { NotFound = [] };
			}

			logger.LogInformation(
				"Loaded translation cache with {Count} entries (last updated: {LastUpdated})",
				cache.Entries.Count,
				cache.LastUpdated
			);
			return cache;
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to load translation cache from {Path}", CachePath);
			return new TranslationCache { LastUpdated = DateTimeOffset.MinValue };
		}
	}

	/// <summary>
	/// Saves the translation cache to disk.
	/// </summary>
	public async Task SaveAsync(TranslationCache cache, CancellationToken ct = default)
	{
		try
		{
			var directory = Path.GetDirectoryName(CachePath);
			if (!string.IsNullOrEmpty(directory))
				_ = Directory.CreateDirectory(directory);

			var json = JsonSerializer.Serialize(cache, TranslationCacheContext.Default.TranslationCache);
			await File.WriteAllTextAsync(CachePath, json, ct);

			logger.LogInformation(
				"Saved translation cache with {Count} entries to {Path}",
				cache.Entries.Count,
				CachePath
			);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to save translation cache to {Path}", CachePath);
		}
	}

	/// <summary>
	/// Gets the path of a URL without the language prefix.
	/// Used as the cache key.
	/// </summary>
	public static string GetPathKey(string url)
	{
		var uri = new Uri(url);
		var path = uri.AbsolutePath;

		// Remove language prefix if present
		var langPrefixes = new[] { "/de/", "/fr/", "/es/", "/jp/", "/kr/", "/cn/", "/pt/" };
		var matchingPrefix = langPrefixes.FirstOrDefault(
			prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
		);

		if (matchingPrefix is not null)
			return "/" + path[matchingPrefix.Length..];

		return path;
	}

	/// <summary>
	/// Checks if translations for a given English URL path are cached.
	/// </summary>
	public static bool TryGetCachedTranslations(
		TranslationCache cache,
		string englishUrl,
		out HashSet<string>? languages
	)
	{
		var pathKey = GetPathKey(englishUrl);
		return cache.Entries.TryGetValue(pathKey, out languages);
	}
}
