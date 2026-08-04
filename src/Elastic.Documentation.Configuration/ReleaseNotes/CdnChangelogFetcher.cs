// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Net;
using System.Text.Json;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// Fetches changelog bundles for a single product from the public CDN. It reads
/// <c>{base}/bundle/{product}/registry.json</c> to enumerate bundles, downloads each
/// <c>{base}/bundle/{product}/{file}</c>, and parses them via
/// <see cref="BundleLoader.LoadBundlesFromContent"/>.
/// </summary>
/// <remarks>
/// <para>
/// Individual bundle files are cached locally keyed by <c>{product}-{fileName}-{etag}</c> so that
/// repeated builds (and dev-server reloads) do not re-download unchanged content from the CDN.
/// The registry itself is always fetched (it's small and provides fresh ETags).
/// </para>
/// <para>
/// Resilience follows the manifest's consistency model: a registry that cannot be fetched or parsed
/// is a hard error (an empty list is returned and the caller's emit-error callback is invoked), while
/// an individual bundle that 404s or fails to parse is a warning and is skipped — the index can
/// legitimately list a bundle whose scrubbed copy is not yet on the public bucket.
/// </para>
/// </remarks>
public sealed class CdnChangelogFetcher : IDisposable
{
	private const int SupportedSchemaVersion = 1;

	/// <summary>
	/// Bounds an individual registry/bundle HTTP request so a stalled CDN connection cannot hang a build.
	/// </summary>
	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Process-wide client shared by every fetcher built for the production (no injected handler) path.
	/// <see cref="HttpClient"/> is thread-safe and intended to be long-lived; a single static instance avoids
	/// leaking a socket handle per fetch, and <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>
	/// bounds DNS staleness in long-lived <c>serve</c>/watch runs. It is intentionally never disposed — it
	/// lives for the lifetime of the process.
	/// </summary>
	private static readonly HttpClient SharedHttpClient = new(
		new SocketsHttpHandler
		{
			AutomaticDecompression = DecompressionMethods.All,
			PooledConnectionLifetime = TimeSpan.FromMinutes(5)
		})
	{ Timeout = FetchTimeout };

	private readonly ILogger _logger;
	private readonly HttpClient _httpClient;
	private readonly BundleLoader _bundleLoader;
	private readonly IFileSystem _fileSystem;
	private readonly ConcurrentDictionary<string, string> _memoryCache = new(StringComparer.Ordinal);

	/// <summary>
	/// Non-null only when a caller injects its own <see cref="HttpMessageHandler"/> (tests): in that case we
	/// own a per-instance client and must dispose it. On the production path <see cref="_httpClient"/> points
	/// at <see cref="SharedHttpClient"/>, which is never disposed.
	/// </summary>
	private readonly HttpClient? _ownedHttpClient;

	public CdnChangelogFetcher(ILoggerFactory logFactory, IFileSystem fileSystem, HttpMessageHandler? handler = null)
	{
		_logger = logFactory.CreateLogger<CdnChangelogFetcher>();
		_fileSystem = fileSystem;
		_bundleLoader = new BundleLoader(fileSystem);

		if (handler is null)
			_httpClient = SharedHttpClient;
		else
		{
			// disposeHandler: false — the injected handler is owned by the caller (tests), not by us.
			_ownedHttpClient = new HttpClient(handler, disposeHandler: false) { Timeout = FetchTimeout };
			_httpClient = _ownedHttpClient;
		}
	}

	/// <summary>
	/// Returns the loaded bundles for <paramref name="product"/> from the CDN at <paramref name="baseUri"/>.
	/// Bundles are merged-by-amend but not yet merged-by-target or sorted (the caller owns presentation).
	/// When <paramref name="version"/> is set, only matching registry entries (and their amend sidecars)
	/// are downloaded.
	/// Returns an empty list on a registry-level failure.
	/// </summary>
	public async Task<IReadOnlyList<LoadedBundle>> FetchAsync(
		Uri baseUri,
		string product,
		string? version,
		Action<string> emitError,
		Action<string> emitWarning,
		Cancel ctx)
	{
		// Defense-in-depth mirroring the entry fetcher's pool validation: reject anything the producer
		// would have refused to upload before building the URI, so normalization (e.g. a ".." product)
		// cannot redirect the fetch outside the bundle layout.
		if (!ChangelogKeys.IsValidProduct(product))
		{
			emitError($"Invalid changelog product '{product}': must be non-empty ASCII letters, digits, '_' or '-'.");
			return [];
		}

		var registryUri = Combine(baseUri, [.. ChangelogKeys.BundleSegments(product), ChangelogKeys.RegistryFileName]);

		ChangelogRegistry? registry;
		try
		{
			registry = await FetchRegistryAsync(registryUri, ctx).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			emitError($"Could not fetch changelog registry for product '{product}' from {registryUri}: {ex.Message}");
			return [];
		}

		if (registry is null)
		{
			emitError($"Changelog registry for product '{product}' at {registryUri} was empty or unparseable.");
			return [];
		}

		if (registry.SchemaVersion > SupportedSchemaVersion)
		{
			emitError(
				$"Changelog registry for product '{product}' uses schema version {registry.SchemaVersion}, but this build only understands version {SupportedSchemaVersion}. Update docs-builder.");
			return [];
		}

		var contents = await DownloadBundlesAsync(baseUri, product, version, registry, emitWarning, ctx).ConfigureAwait(false);
		if (contents.Count == 0)
		{
			_logger.LogInformation("No usable changelog bundles fetched for {Product} from {BaseUri}", product, baseUri);
			return [];
		}

		return _bundleLoader.LoadBundlesFromContent(contents, emitWarning);
	}

	private async Task<ChangelogRegistry?> FetchRegistryAsync(Uri registryUri, Cancel ctx)
	{
		_logger.LogInformation("Fetching changelog registry {RegistryUri}", registryUri);
		using var request = new HttpRequestMessage(HttpMethod.Get, registryUri);
		using var response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
		_ = response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
		return await JsonSerializer.DeserializeAsync(stream, ChangelogRegistryJsonContext.Default.ChangelogRegistry, ctx).ConfigureAwait(false);
	}

	private async Task<List<(string FileName, string Content)>> DownloadBundlesAsync(
		Uri baseUri,
		string product,
		string? version,
		ChangelogRegistry registry,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var selected = SelectBundles(version, registry.Bundles);
		var tasks = new List<Task<(string FileName, string Content)?>>(selected.Count);
		foreach (var bundle in selected)
		{
			var fileName = bundle.File;
			if (!ChangelogKeys.IsSafeFileName(fileName))
			{
				emitWarning($"Changelog registry for '{product}' lists an invalid bundle file name '{fileName}'; skipping.");
				continue;
			}

			tasks.Add(DownloadOrCacheBundleAsync(baseUri, product, fileName, bundle.ETag, emitWarning, ctx));
		}

		var results = await Task.WhenAll(tasks).ConfigureAwait(false);
		return results.Where(r => r is not null).Select(r => r!.Value).ToList();
	}

	private async Task<(string FileName, string Content)?> DownloadOrCacheBundleAsync(
		Uri baseUri,
		string product,
		string fileName,
		string? etag,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var cached = TryGetCachedBundle(product, fileName, etag);
		if (cached is not null)
		{
			_logger.LogInformation("Using locally cached bundle '{FileName}' for '{Product}'", fileName, product);
			return (fileName, cached);
		}

		var bundleUri = Combine(baseUri, [.. ChangelogKeys.BundleSegments(product), fileName]);
		try
		{
			var content = await FetchTextAsync(bundleUri, ctx).ConfigureAwait(false);
			WriteCachedBundle(product, fileName, etag, content);
			return (fileName, content);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			emitWarning($"Could not fetch changelog bundle '{fileName}' for '{product}' from {bundleUri}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Registry entries to download. When a single version is requested, only matching entries are
	/// selected; the directive re-applies the same match after load, so this is purely a fetch
	/// optimization. An amend sidecar is additionally selected when its parent bundle matches:
	/// amends published before products were copied from the parent carry a null registry
	/// <c>target</c> and a file name (<c>{name}.amend-{N}.yaml</c>) the version can never equal, and the
	/// post-load re-match cannot rescue an amend that was never fetched.
	/// </summary>
	private static List<ChangelogRegistryBundle> SelectBundles(string? version, IReadOnlyList<ChangelogRegistryBundle> bundles)
	{
		if (string.IsNullOrWhiteSpace(version))
			return [.. bundles];

		var selected = bundles
			.Where(b => ChangelogVersionMatch.Matches(version, b.Target, b.File))
			.ToList();

		var selectedFiles = new HashSet<string>(
			selected.Select(b => b.File).OfType<string>(),
			StringComparer.OrdinalIgnoreCase);

		foreach (var bundle in bundles)
		{
			if (bundle.File is null || selectedFiles.Contains(bundle.File) || !BundleAmendMerger.IsAmendFile(bundle.File))
				continue;

			var parentFile = BundleAmendMerger.GetParentBundlePath(bundle.File);
			if (parentFile is not null && selectedFiles.Contains(parentFile))
				selected.Add(bundle);
		}

		return selected;
	}

	private async Task<string> FetchTextAsync(Uri uri, Cancel ctx)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		using var response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
		_ = response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
	}

	private static Uri Combine(Uri baseUri, IReadOnlyList<string> segments)
	{
		var basePath = baseUri.AbsoluteUri.TrimEnd('/');
		var suffix = string.Join('/', segments.Select(Uri.EscapeDataString));
		return new Uri($"{basePath}/{suffix}");
	}

	private string? TryGetCachedBundle(string product, string fileName, string? etag)
	{
		if (string.IsNullOrWhiteSpace(etag))
			return null;

		var cacheKey = CacheKey(product, fileName, etag);
		if (_memoryCache.TryGetValue(cacheKey, out var cached))
			return cached;

		var cachePath = CachePath(cacheKey);
		if (!_fileSystem.File.Exists(cachePath))
			return null;

		try
		{
			var content = _fileSystem.File.ReadAllText(cachePath);
			_ = _memoryCache.TryAdd(cacheKey, content);
			return content;
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read cached changelog bundle {CachePath}", cachePath);
			return null;
		}
	}

	private void WriteCachedBundle(string product, string fileName, string? etag, string content)
	{
		if (string.IsNullOrWhiteSpace(etag))
			return;

		var cacheKey = CacheKey(product, fileName, etag);
		_ = _memoryCache.TryAdd(cacheKey, content);

		var cachePath = CachePath(cacheKey);
		if (_fileSystem.File.Exists(cachePath))
			return;

		try
		{
			_ = _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
			_fileSystem.File.WriteAllText(cachePath, content);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to write cached changelog bundle {CachePath}", cachePath);
		}
	}

	private static string CacheKey(string product, string fileName, string etag) =>
		$"changelog-{product}-{fileName}-{etag}";

	private static string CachePath(string cacheKey) =>
		Path.Join(Paths.ApplicationData.FullName, "changelog-bundles", cacheKey);

	/// <summary>
	/// Disposes the per-instance <see cref="HttpClient"/> created for an injected handler. The shared
	/// production client (<see cref="SharedHttpClient"/>) is process-lived and intentionally not disposed.
	/// </summary>
	public void Dispose()
	{
		_ownedHttpClient?.Dispose();
		GC.SuppressFinalize(this);
	}
}
