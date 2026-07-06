// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// One downloaded changelog entry: the registry file name and its raw YAML content.
/// </summary>
public readonly record struct CdnChangelogEntry(string FileName, string Content);

/// <summary>
/// Fetches the individual (scrubbed) changelog entries for a single authoring org/repo/branch pool from
/// the public CDN, for the <c>changelog bundle</c> command when sourcing entries from S3 rather than a
/// local folder. It reads <c>{base}/changelog/{org}/{repo}/{branch}/registry.json</c> to enumerate entries
/// and downloads each <c>{base}/changelog/{org}/{repo}/{branch}/{file}</c> as raw YAML; the bundle command
/// then applies its usual filter (products / prs / issues) to the downloaded set.
/// </summary>
/// <remarks>
/// <para>
/// A registry that cannot be fetched or parsed is a hard error (the caller gets an empty list and an
/// emitted error). An individual entry that the registry lists but the CDN does not yet serve is
/// retried a few times with short backoff (and cache-busting, to defeat any CloudFront negative-cache)
/// to ride out the brief upload→scrub→propagate window. If it still cannot be fetched after the retry
/// budget it is escalated to an error, not skipped: the registry asserts the entry exists (uploads
/// never prune) and scrubbing is sub-second, so a persistent miss is a real pipeline problem and
/// silently shipping an incomplete release bundle is worse than failing the run.
/// </para>
/// </remarks>
public sealed class CdnChangelogEntryFetcher : IDisposable
{
	private const int SupportedSchemaVersion = 1;

	/// <summary>Total GET attempts per entry (1 initial + retries). ~3.5s budget at the default backoff.</summary>
	private const int DefaultMaxAttempts = 4;
	private const int BaseRetryDelayMs = 500;
	private const int MaxRetryDelayMs = 2000;

	/// <summary>
	/// Bounds an individual registry/entry HTTP request so a stalled CDN connection cannot hang a bundle run.
	/// </summary>
	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Process-wide client shared by every fetcher built for the production (no injected handler) path.
	/// <see cref="HttpClient"/> is thread-safe and intended to be long-lived; a single static instance avoids
	/// leaking a socket handle per fetch, and <see cref="SocketsHttpHandler.PooledConnectionLifetime"/>
	/// bounds DNS staleness. It is intentionally never disposed — it lives for the lifetime of the process.
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
	private readonly int _maxAttempts;
	private readonly Func<TimeSpan, Cancel, Task> _sleep;

	/// <summary>
	/// Non-null only when a caller injects its own <see cref="HttpMessageHandler"/> (tests): in that case we
	/// own a per-instance client and must dispose it. On the production path <see cref="_httpClient"/> points
	/// at <see cref="SharedHttpClient"/>, which is never disposed.
	/// </summary>
	private readonly HttpClient? _ownedHttpClient;

	public CdnChangelogEntryFetcher(
		ILoggerFactory logFactory,
		HttpMessageHandler? handler = null,
		int maxAttempts = DefaultMaxAttempts,
		Func<TimeSpan, Cancel, Task>? sleep = null)
	{
		_logger = logFactory.CreateLogger<CdnChangelogEntryFetcher>();
		_maxAttempts = maxAttempts < 1 ? DefaultMaxAttempts : maxAttempts;
		_sleep = sleep ?? DefaultSleepAsync;

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
	/// Downloads the changelog entries for the authoring <paramref name="org"/>/<paramref name="repo"/>/<paramref name="branch"/>
	/// pool from the CDN at <paramref name="baseUri"/>. Returns an empty list after emitting an error when
	/// the registry cannot be read or when a registry-listed entry cannot be fetched within the retry budget.
	/// Entries are returned in registry order; the caller owns filtering and de-duplication.
	/// </summary>
	public async Task<IReadOnlyList<CdnChangelogEntry>> FetchAsync(
		Uri baseUri,
		string org,
		string repo,
		string branch,
		Action<string> emitError,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var poolLabel = $"{org}/{repo}/{branch}";

		// Defense-in-depth: org/repo come from config and branch from config or CLI on the consumer side.
		// Reject empty or traversal segments before building the URI so it cannot normalize a "../" into a
		// different changelog pool than intended.
		if (!IsValidPool(org, repo, branch))
		{
			emitError(
				$"Invalid changelog pool '{poolLabel}': the org, repo, and each branch segment must be non-empty, contain no path separators, and not be '.' or '..'.");
			return [];
		}

		var poolSegments = PoolSegments(org, repo, branch).ToArray();
		var registryUri = CombineSegments(baseUri, [.. poolSegments, "registry.json"]);

		ChangelogRegistry? registry;
		try
		{
			registry = await FetchRegistryAsync(registryUri, ctx).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			emitError($"Could not fetch changelog entry registry for '{poolLabel}' from {registryUri}: {ex.Message}");
			return [];
		}

		if (registry is null)
		{
			emitError($"Changelog entry registry for '{poolLabel}' at {registryUri} was empty or unparseable.");
			return [];
		}

		if (registry.SchemaVersion > SupportedSchemaVersion)
		{
			emitError(
				$"Changelog entry registry for '{poolLabel}' uses schema version {registry.SchemaVersion}, but this build only understands version {SupportedSchemaVersion}. Update docs-builder.");
			return [];
		}

		var entries = new List<CdnChangelogEntry>(registry.Bundles.Count);
		foreach (var entry in registry.Bundles)
		{
			ctx.ThrowIfCancellationRequested();

			var fileName = entry.File;
			if (string.IsNullOrWhiteSpace(fileName) || !IsSafeFileName(fileName))
			{
				emitWarning($"Changelog entry registry for '{poolLabel}' lists an invalid file name '{fileName}'; skipping.");
				continue;
			}

			var entryUri = CombineSegments(baseUri, [.. poolSegments, fileName]);
			var (fetched, content, lastError) = await TryFetchEntryAsync(entryUri, fileName, poolLabel, ctx).ConfigureAwait(false);
			if (fetched)
			{
				entries.Add(new CdnChangelogEntry(fileName, content));
				continue;
			}

			// The registry lists this entry, so it exists in the private bucket and should have been
			// scrubbed to the public one within milliseconds. Still missing after the retry budget means
			// a genuine propagation/scrub failure — fail rather than ship a bundle missing this entry.
			emitError(
				$"Changelog entry '{fileName}' for '{poolLabel}' is listed in the registry but could not be fetched from {entryUri} after {_maxAttempts} attempt(s): {lastError}. " +
				"The scrubbed copy may not have propagated to the CDN yet; retry shortly, and if it persists check the changelog scrubber pipeline.");
			return [];
		}

		_logger.LogInformation("Fetched {Count} changelog entry(ies) for {Pool} from {BaseUri}", entries.Count, poolLabel, baseUri);
		return entries;
	}

	/// <summary>
	/// Fetches a single entry, retrying transient failures (most importantly a not-yet-propagated 404)
	/// up to <see cref="_maxAttempts"/> times with exponential backoff. Retry requests are cache-busted
	/// so a CloudFront-cached 404 cannot pin the result for the whole window.
	/// </summary>
	private async Task<(bool Fetched, string Content, string? LastError)> TryFetchEntryAsync(Uri uri, string fileName, string poolLabel, Cancel ctx)
	{
		string? lastError = null;

		for (var attempt = 1; attempt <= _maxAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				var content = await FetchTextAsync(uri, attempt, ctx).ConfigureAwait(false);
				if (attempt > 1)
					_logger.LogInformation("Fetched changelog entry '{File}' for {Pool} on attempt {Attempt}/{Max}", fileName, poolLabel, attempt, _maxAttempts);
				return (true, content, null);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				lastError = ex.Message;
				if (attempt >= _maxAttempts)
					break;

				var delay = RetryDelay(attempt);
				_logger.LogDebug(
					"Changelog entry '{File}' for {Pool} not yet available (attempt {Attempt}/{Max}: {Error}); retrying in {Delay}",
					fileName, poolLabel, attempt, _maxAttempts, ex.Message, delay);
				await _sleep(delay, ctx).ConfigureAwait(false);
			}
		}

		return (false, string.Empty, lastError);
	}

	private async Task<ChangelogRegistry?> FetchRegistryAsync(Uri registryUri, Cancel ctx)
	{
		_logger.LogInformation("Fetching changelog entry registry {RegistryUri}", registryUri);
		using var request = new HttpRequestMessage(HttpMethod.Get, registryUri);
		using var response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
		_ = response.EnsureSuccessStatusCode();
		await using var stream = await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
		return await JsonSerializer.DeserializeAsync(stream, ChangelogRegistryJsonContext.Default.ChangelogRegistry, ctx).ConfigureAwait(false);
	}

	private async Task<string> FetchTextAsync(Uri uri, int attempt, Cancel ctx)
	{
		// Only bust the cache on retries: the first hit should use the CDN cache normally (the common,
		// already-propagated case); retries explicitly want to bypass any cached 404.
		var requestUri = attempt > 1 ? WithCacheBuster(uri) : uri;
		using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
		if (attempt > 1)
			_ = request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
		using var response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
		_ = response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync(ctx).ConfigureAwait(false);
	}

	private static TimeSpan RetryDelay(int attempt)
	{
		// attempt is 1-based; first retry waits BaseRetryDelayMs, doubling up to the cap.
		var ms = Math.Min(BaseRetryDelayMs * (1L << (attempt - 1)), MaxRetryDelayMs);
		return TimeSpan.FromMilliseconds(ms);
	}

	private static async Task DefaultSleepAsync(TimeSpan delay, Cancel ctx)
	{
		if (delay > TimeSpan.Zero)
			await Task.Delay(delay, ctx).ConfigureAwait(false);
	}

	private static Uri WithCacheBuster(Uri uri)
	{
		var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
		return new Uri($"{uri.AbsoluteUri}{separator}_={DateTimeOffset.UtcNow.Ticks:x}");
	}

	private static Uri CombineSegments(Uri baseUri, IReadOnlyList<string> segments)
	{
		var basePath = baseUri.AbsoluteUri.TrimEnd('/');
		var suffix = string.Join('/', segments.Select(Uri.EscapeDataString));
		return new Uri($"{basePath}/{suffix}");
	}

	/// <summary>
	/// Expands the pool into individually-escapable path segments — <c>changelog</c>, org, repo, then each
	/// <c>/</c>-delimited part of the branch — so a branch's slashes stay real CDN key separators rather
	/// than being percent-encoded into a single segment.
	/// </summary>
	private static IEnumerable<string> PoolSegments(string org, string repo, string branch)
	{
		yield return "changelog";
		yield return org;
		yield return repo;
		foreach (var part in branch.Split('/'))
			yield return part;
	}

	/// <summary>
	/// Validates the consumer-supplied pool coordinates (org, repo, and each <c>/</c>-delimited branch
	/// segment) so a malformed branch cannot redirect the fetch to a different pool via URI normalization.
	/// </summary>
	private static bool IsValidPool(string org, string repo, string branch)
	{
		if (!IsSafePoolSegment(org) || !IsSafePoolSegment(repo))
			return false;

		foreach (var part in branch.Split('/'))
		{
			if (!IsSafePoolSegment(part))
				return false;
		}

		return true;
	}

	private static bool IsSafePoolSegment(string segment) =>
		!string.IsNullOrEmpty(segment)
		&& segment is not ("." or "..")
		&& !segment.Contains('/', StringComparison.Ordinal)
		&& !segment.Contains('\\', StringComparison.Ordinal);

	/// <summary>
	/// Guards against path traversal or nested keys sneaking in via the registry: an entry file name
	/// must be a single path segment (the producer always writes <c>changelog/{org}/{repo}/{branch}/{file}</c>).
	/// </summary>
	private static bool IsSafeFileName(string fileName) =>
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
