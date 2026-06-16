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
/// Fetches changelog bundles for a single product from the public CDN at build time, for the
/// <c>changelog</c> directive in <c>cdn:</c> mode. It reads <c>{base}/{product}/registry.json</c>
/// to enumerate bundles, downloads each <c>{base}/{product}/bundle/{file}</c>, and parses them via
/// <see cref="BundleLoader.LoadBundlesFromContent"/>.
/// </summary>
/// <remarks>
/// <para>
/// Fetching is synchronous on purpose: directive finalization runs inside the (synchronous) Markdig
/// block parser, exactly like the local-folder loader's file reads. Results are memoized per process
/// keyed by base URL + product so repeated directives across pages don't refetch. In long-running
/// <c>serve</c> mode this means CDN content is effectively pinned for the process lifetime, which is
/// acceptable: release-note bundles change rarely and serve targets local markdown authoring.
/// </para>
/// <para>
/// Resilience follows the manifest's consistency model: a registry that cannot be fetched or parsed
/// is a hard error (the block renders empty), while an individual bundle that 404s or fails to parse
/// is a warning and is skipped — the index can legitimately list a bundle whose scrubbed copy is not
/// yet on the public bucket.
/// </para>
/// </remarks>
public sealed class CdnChangelogFetcher : IDisposable
{
	private const int SupportedSchemaVersion = 1;

	/// <summary>
	/// Bounds an individual registry/bundle HTTP request. The fetch runs synchronously inside the Markdig
	/// finalize pass (see <see cref="Fetch"/>), so without a timeout a stalled CDN connection could hang a
	/// build — or a long-lived <c>serve</c> process — indefinitely.
	/// </summary>
	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Process-wide client shared by every fetcher built for the production (no injected handler) path.
	/// <see cref="HttpClient"/> is thread-safe and intended to be long-lived; a single static instance avoids
	/// leaking a socket handle per directive, and <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>
	/// bounds DNS staleness in long-lived <c>serve</c>/watch runs. It is intentionally never disposed — it
	/// lives for the lifetime of the process. (Most fetcher instances never touch it: the static
	/// <see cref="Cache"/> short-circuits repeated fetches.)
	/// </summary>
	private static readonly HttpClient SharedHttpClient = new(
		new SocketsHttpHandler
		{
			AutomaticDecompression = DecompressionMethods.All,
			PooledConnectionLifetime = TimeSpan.FromMinutes(5)
		})
	{ Timeout = FetchTimeout };

	private static readonly ConcurrentDictionary<string, IReadOnlyList<LoadedBundle>> Cache = new(StringComparer.Ordinal);

	private readonly ILogger _logger;
	private readonly HttpClient _httpClient;
	private readonly BundleLoader _bundleLoader;

	/// <summary>
	/// Non-null only when a caller injects its own <see cref="HttpMessageHandler"/> (tests): in that case we
	/// own a per-instance client and must dispose it. On the production path <see cref="_httpClient"/> points
	/// at <see cref="SharedHttpClient"/>, which is never disposed.
	/// </summary>
	private readonly HttpClient? _ownedHttpClient;

	public CdnChangelogFetcher(ILoggerFactory logFactory, IFileSystem fileSystem, HttpMessageHandler? handler = null)
	{
		_logger = logFactory.CreateLogger<CdnChangelogFetcher>();
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
	/// When <paramref name="version"/> is set, only the matching registry entry is downloaded.
	/// Returns an empty list on a registry-level failure.
	/// </summary>
	public IReadOnlyList<LoadedBundle> Fetch(
		Uri baseUri,
		string product,
		string? version,
		Action<string> emitError,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var cacheKey = CacheKey(baseUri, product, version);
		if (Cache.TryGetValue(cacheKey, out var cached))
		{
			_logger.LogTrace("Using cached CDN changelog bundles for {Product} from {BaseUri}", product, baseUri);
			return cached;
		}

		// Track whether the fetch was clean: a registry/bundle failure (or any skipped bundle) may be a
		// transient CDN hiccup or replication lag, and memoizing it would pin an empty/partial result for
		// the process lifetime (notably under `serve`). Only a clean, non-empty fetch is cached.
		var clean = true;
		var bundles = FetchUncached(
			baseUri,
			product,
			version,
			msg => { clean = false; emitError(msg); },
			msg => { clean = false; emitWarning(msg); },
			ctx);

		if (clean && bundles.Count > 0)
			_ = Cache.TryAdd(cacheKey, bundles);

		return bundles;
	}

	private static string CacheKey(Uri baseUri, string product, string? version) =>
		$"{baseUri.AbsoluteUri}\n{product}\n{version}";

	/// <summary>
	/// Test-only: pre-populate the process cache so the directive's render path can be exercised
	/// offline (the directive constructs its own fetcher, so there is no handler seam to inject).
	/// </summary>
	internal static void PrimeCacheForTesting(Uri baseUri, string product, string? version, IReadOnlyList<LoadedBundle> bundles) =>
		Cache[CacheKey(baseUri, product, version)] = bundles;

	private IReadOnlyList<LoadedBundle> FetchUncached(
		Uri baseUri,
		string product,
		string? version,
		Action<string> emitError,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var registryUri = Combine(baseUri, product, "registry.json");

		ChangelogRegistry? registry;
		try
		{
			registry = FetchRegistry(registryUri, ctx);
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

		var contents = DownloadBundles(baseUri, product, version, registry, emitWarning, ctx);
		if (contents.Count == 0)
		{
			_logger.LogInformation("No usable changelog bundles fetched for {Product} from {BaseUri}", product, baseUri);
			return [];
		}

		return _bundleLoader.LoadBundlesFromContent(contents, emitWarning);
	}

	private ChangelogRegistry? FetchRegistry(Uri registryUri, Cancel ctx)
	{
		_logger.LogInformation("Fetching changelog registry {RegistryUri}", registryUri);
		using var request = new HttpRequestMessage(HttpMethod.Get, registryUri);
		using var response = _httpClient.Send(request, ctx);
		_ = response.EnsureSuccessStatusCode();
		using var stream = response.Content.ReadAsStream(ctx);
		return JsonSerializer.Deserialize(stream, ChangelogRegistryJsonContext.Default.ChangelogRegistry);
	}

	private List<(string FileName, string Content)> DownloadBundles(
		Uri baseUri,
		string product,
		string? version,
		ChangelogRegistry registry,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var contents = new List<(string FileName, string Content)>(registry.Bundles.Count);
		foreach (var bundle in registry.Bundles)
		{
			ctx.ThrowIfCancellationRequested();

			// When a single version is requested, only download the matching entry; the directive
			// re-applies the same match after load, so this is purely a fetch optimization.
			if (!string.IsNullOrWhiteSpace(version) && !ChangelogVersionMatch.Matches(version, bundle.Target, bundle.File))
				continue;

			var fileName = bundle.File;
			if (string.IsNullOrWhiteSpace(fileName) || !IsSafeBundleFileName(fileName))
			{
				emitWarning($"Changelog registry for '{product}' lists an invalid bundle file name '{fileName}'; skipping.");
				continue;
			}

			var bundleUri = Combine(baseUri, product, "bundle", fileName);
			try
			{
				contents.Add((fileName, FetchText(bundleUri, ctx)));
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// The registry can reference a bundle whose scrubbed copy is not yet public; skip + warn.
				emitWarning($"Could not fetch changelog bundle '{fileName}' for '{product}' from {bundleUri}: {ex.Message}");
			}
		}

		return contents;
	}

	private string FetchText(Uri uri, Cancel ctx)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		using var response = _httpClient.Send(request, ctx);
		_ = response.EnsureSuccessStatusCode();
		using var stream = response.Content.ReadAsStream(ctx);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private static Uri Combine(Uri baseUri, params string[] segments)
	{
		var basePath = baseUri.AbsoluteUri.TrimEnd('/');
		var suffix = string.Join('/', segments.Select(Uri.EscapeDataString));
		return new Uri($"{basePath}/{suffix}");
	}

	/// <summary>
	/// Guards against path traversal or nested keys sneaking in via the registry: a bundle file name
	/// must be a single path segment (the producer always writes <c>{product}/bundle/{file}</c>).
	/// </summary>
	private static bool IsSafeBundleFileName(string fileName) =>
		!fileName.Contains('/', StringComparison.Ordinal)
		&& !fileName.Contains('\\', StringComparison.Ordinal)
		&& fileName is not ("." or "..");

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
