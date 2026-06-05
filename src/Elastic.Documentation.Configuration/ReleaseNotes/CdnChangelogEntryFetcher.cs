// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Configuration.ReleaseNotes;

/// <summary>
/// One downloaded changelog entry: the registry file name and its raw YAML content.
/// </summary>
public readonly record struct CdnChangelogEntry(string FileName, string Content);

/// <summary>
/// Fetches the individual (scrubbed) changelog entries for a single product from the public CDN, for
/// the <c>changelog bundle</c> command when sourcing entries from S3 rather than a local folder. It
/// reads <c>{base}/{product}/changelog/registry.json</c> to enumerate entries and downloads each
/// <c>{base}/{product}/changelog/{file}</c> as raw YAML; the bundle command then applies its usual
/// filter (products / prs / issues) to the downloaded set.
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
public sealed class CdnChangelogEntryFetcher(
	ILoggerFactory logFactory,
	HttpMessageHandler? handler = null,
	int maxAttempts = CdnChangelogEntryFetcher.DefaultMaxAttempts,
	Action<TimeSpan, Cancel>? sleep = null)
{
	private const int SupportedSchemaVersion = 1;

	/// <summary>Total GET attempts per entry (1 initial + retries). ~3.5s budget at the default backoff.</summary>
	private const int DefaultMaxAttempts = 4;
	private const int BaseRetryDelayMs = 500;
	private const int MaxRetryDelayMs = 2000;

	private readonly ILogger _logger = logFactory.CreateLogger<CdnChangelogEntryFetcher>();
	private readonly HttpClient _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
	private readonly int _maxAttempts = maxAttempts < 1 ? DefaultMaxAttempts : maxAttempts;
	private readonly Action<TimeSpan, Cancel> _sleep = sleep ?? DefaultSleep;

	/// <summary>
	/// Downloads the changelog entries for <paramref name="product"/> from the CDN at
	/// <paramref name="baseUri"/>. Returns an empty list after emitting an error when the registry cannot
	/// be read or when a registry-listed entry cannot be fetched within the retry budget. Entries are
	/// returned in registry order; the caller owns filtering and de-duplication.
	/// </summary>
	public IReadOnlyList<CdnChangelogEntry> Fetch(
		Uri baseUri,
		string product,
		Action<string> emitError,
		Action<string> emitWarning,
		Cancel ctx)
	{
		var registryUri = Combine(baseUri, product, "changelog", "registry.json");

		ChangelogRegistry? registry;
		try
		{
			registry = FetchRegistry(registryUri, ctx);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			emitError($"Could not fetch changelog entry registry for product '{product}' from {registryUri}: {ex.Message}");
			return [];
		}

		if (registry is null)
		{
			emitError($"Changelog entry registry for product '{product}' at {registryUri} was empty or unparseable.");
			return [];
		}

		if (registry.SchemaVersion > SupportedSchemaVersion)
		{
			emitError(
				$"Changelog entry registry for product '{product}' uses schema version {registry.SchemaVersion}, but this build only understands version {SupportedSchemaVersion}. Update docs-builder.");
			return [];
		}

		var entries = new List<CdnChangelogEntry>(registry.Bundles.Count);
		foreach (var entry in registry.Bundles)
		{
			ctx.ThrowIfCancellationRequested();

			var fileName = entry.File;
			if (string.IsNullOrWhiteSpace(fileName) || !IsSafeFileName(fileName))
			{
				emitWarning($"Changelog entry registry for '{product}' lists an invalid file name '{fileName}'; skipping.");
				continue;
			}

			var entryUri = Combine(baseUri, product, "changelog", fileName);
			if (TryFetchEntry(entryUri, fileName, product, ctx, out var content, out var lastError))
			{
				entries.Add(new CdnChangelogEntry(fileName, content));
				continue;
			}

			// The registry lists this entry, so it exists in the private bucket and should have been
			// scrubbed to the public one within milliseconds. Still missing after the retry budget means
			// a genuine propagation/scrub failure — fail rather than ship a bundle missing this entry.
			emitError(
				$"Changelog entry '{fileName}' for product '{product}' is listed in the registry but could not be fetched from {entryUri} after {_maxAttempts} attempt(s): {lastError}. " +
				"The scrubbed copy may not have propagated to the CDN yet; retry shortly, and if it persists check the changelog scrubber pipeline.");
			return [];
		}

		_logger.LogInformation("Fetched {Count} changelog entry(ies) for {Product} from {BaseUri}", entries.Count, product, baseUri);
		return entries;
	}

	/// <summary>
	/// Fetches a single entry, retrying transient failures (most importantly a not-yet-propagated 404)
	/// up to <see cref="_maxAttempts"/> times with exponential backoff. Retry requests are cache-busted
	/// so a CloudFront-cached 404 cannot pin the result for the whole window.
	/// </summary>
	private bool TryFetchEntry(Uri uri, string fileName, string product, Cancel ctx, out string content, out string? lastError)
	{
		content = string.Empty;
		lastError = null;

		for (var attempt = 1; attempt <= _maxAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				content = FetchText(uri, attempt, ctx);
				if (attempt > 1)
					_logger.LogInformation("Fetched changelog entry '{File}' for {Product} on attempt {Attempt}/{Max}", fileName, product, attempt, _maxAttempts);
				return true;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				lastError = ex.Message;
				if (attempt >= _maxAttempts)
					break;

				var delay = RetryDelay(attempt);
				_logger.LogDebug(
					"Changelog entry '{File}' for {Product} not yet available (attempt {Attempt}/{Max}: {Error}); retrying in {Delay}",
					fileName, product, attempt, _maxAttempts, ex.Message, delay);
				_sleep(delay, ctx);
			}
		}

		return false;
	}

	private ChangelogRegistry? FetchRegistry(Uri registryUri, Cancel ctx)
	{
		_logger.LogInformation("Fetching changelog entry registry {RegistryUri}", registryUri);
		using var request = new HttpRequestMessage(HttpMethod.Get, registryUri);
		using var response = _httpClient.Send(request, ctx);
		_ = response.EnsureSuccessStatusCode();
		using var stream = response.Content.ReadAsStream(ctx);
		return JsonSerializer.Deserialize(stream, ChangelogRegistryJsonContext.Default.ChangelogRegistry);
	}

	private string FetchText(Uri uri, int attempt, Cancel ctx)
	{
		// Only bust the cache on retries: the first hit should use the CDN cache normally (the common,
		// already-propagated case); retries explicitly want to bypass any cached 404.
		var requestUri = attempt > 1 ? WithCacheBuster(uri) : uri;
		using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
		if (attempt > 1)
			_ = request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
		using var response = _httpClient.Send(request, ctx);
		_ = response.EnsureSuccessStatusCode();
		using var stream = response.Content.ReadAsStream(ctx);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private static TimeSpan RetryDelay(int attempt)
	{
		// attempt is 1-based; first retry waits BaseRetryDelayMs, doubling up to the cap.
		var ms = Math.Min(BaseRetryDelayMs * (1L << (attempt - 1)), MaxRetryDelayMs);
		return TimeSpan.FromMilliseconds(ms);
	}

	private static void DefaultSleep(TimeSpan delay, Cancel ctx)
	{
		if (delay > TimeSpan.Zero)
			_ = ctx.WaitHandle.WaitOne(delay);
	}

	private static Uri WithCacheBuster(Uri uri)
	{
		var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
		return new Uri($"{uri.AbsoluteUri}{separator}_={DateTimeOffset.UtcNow.Ticks:x}");
	}

	private static Uri Combine(Uri baseUri, params string[] segments)
	{
		var basePath = baseUri.AbsoluteUri.TrimEnd('/');
		var suffix = string.Join('/', segments.Select(Uri.EscapeDataString));
		return new Uri($"{basePath}/{suffix}");
	}

	/// <summary>
	/// Guards against path traversal or nested keys sneaking in via the registry: an entry file name
	/// must be a single path segment (the producer always writes <c>{product}/changelog/{file}</c>).
	/// </summary>
	private static bool IsSafeFileName(string fileName) =>
		!fileName.Contains('/', StringComparison.Ordinal)
		&& !fileName.Contains('\\', StringComparison.Ordinal)
		&& fileName is not ("." or "..");
}
