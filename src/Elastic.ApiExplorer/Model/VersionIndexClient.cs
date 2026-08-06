// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using System.Text.Json;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;

namespace Elastic.ApiExplorer.Model;

/// <summary>
/// One version-index moniker (<c>main</c>, <c>9</c>, <c>8</c>, ...), resolved to either the docset's
/// local override file or a remote object key fetched through CloudFront.
/// </summary>
public sealed record ResolvedApiVersion
{
	public required string Moniker { get; init; }

	/// <summary>The branch segment from the index entry, used to build the remote object key.</summary>
	public required string Version { get; init; }

	/// <summary>True when this version renders from <see cref="LocalFile"/> rather than <see cref="ObjectKey"/>.</summary>
	public required bool IsLocal { get; init; }

	/// <summary>Set only when <see cref="IsLocal"/> is true.</summary>
	public IFileInfo? LocalFile { get; init; }

	/// <summary>
	/// Set only when <see cref="IsLocal"/> is false; the object key relative to the CloudFront base,
	/// e.g. <c>elastic/elasticsearch-specification/9.5/elasticsearch.json</c>.
	/// </summary>
	public string? ObjectKey { get; init; }
}

/// <summary>
/// Fetches the single root <c>index.json</c> version-index manifest and versioned OpenAPI spec objects
/// from the public CloudFront distribution in front of the <c>elastic-docs-openapi-specs</c> bucket,
/// without an AWS SDK dependency.
/// </summary>
public sealed class VersionIndexClient : IDisposable
{
	private const int DefaultMaxAttempts = 4;
	private const int BaseRetryDelayMs = 500;
	private const int MaxRetryDelayMs = 2000;

	private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

	/// <summary>The public CloudFront distribution in front of the <c>elastic-docs-openapi-specs</c> bucket.</summary>
	public static readonly Uri DefaultBaseUri = new("https://d29hkgsdo66d1n.cloudfront.net/");

	private static readonly HttpClient SharedHttpClient = new(
		new SocketsHttpHandler
		{
			AutomaticDecompression = DecompressionMethods.All,
			PooledConnectionLifetime = TimeSpan.FromMinutes(5)
		})
	{ Timeout = FetchTimeout };

	private readonly HttpClient _httpClient;
	private readonly HttpClient? _ownedHttpClient;
	private readonly Uri _baseUri;
	private readonly Uri _indexUri;
	private readonly int _maxAttempts;
	private readonly Func<TimeSpan, Cancel, Task> _sleep;

	private readonly SemaphoreSlim _rootIndexLock = new(1, 1);
	private bool _rootIndexFetched;
	private RootVersionIndex? _rootIndex;
	private string? _rootIndexFetchError;

	public VersionIndexClient(
		Uri? baseUri = null,
		HttpMessageHandler? handler = null,
		int maxAttempts = DefaultMaxAttempts,
		Func<TimeSpan, Cancel, Task>? sleep = null)
	{
		_baseUri = baseUri ?? DefaultBaseUri;
		_indexUri = new Uri(_baseUri, "index.json");
		_maxAttempts = maxAttempts < 1 ? DefaultMaxAttempts : maxAttempts;
		_sleep = sleep ?? DefaultSleepAsync;

		if (handler is null)
			_httpClient = SharedHttpClient;
		else
		{
			_ownedHttpClient = new HttpClient(handler, disposeHandler: false) { Timeout = FetchTimeout };
			_httpClient = _ownedHttpClient;
		}
	}

	public async Task<IReadOnlyList<ResolvedApiVersion>> ResolveVersionsAsync(
		GitCheckoutInformation git,
		string apiKey,
		ResolvedApiConfiguration apiConfig,
		IDiagnosticsCollector collector,
		Cancel ctx = default)
	{
		var repository = apiConfig.Repository ?? git.GitHubRepository;
		if (repository is null)
			return NoRepositoryFallback(apiKey, apiConfig, collector);

		var (index, fetchError) = await GetRootIndexAsync(ctx).ConfigureAwait(false);
		if (fetchError is not null)
			return MissingEntryFallback(apiKey, apiConfig, $"Version index at {_indexUri} could not be fetched ({fetchError})", collector);

		if (index is null || index.Count == 0)
			return MissingEntryFallback(apiKey, apiConfig, $"Version index at {_indexUri} declares no repositories", collector);

		if (!index.TryGetValue(repository, out var specsForRepo))
			return MissingEntryFallback(apiKey, apiConfig, $"Version index at {_indexUri} has no entry for repository '{repository}'", collector);

		if (!specsForRepo.TryGetValue(apiConfig.SpecFileName, out var versions) || versions.Count == 0)
		{
			return MissingEntryFallback(apiKey, apiConfig,
				$"Version index at {_indexUri} has no entry for spec '{apiConfig.SpecFileName}' under repository '{repository}'", collector);
		}

		var resolved = new List<ResolvedApiVersion>(versions.Count);
		foreach (var (moniker, entry) in versions)
		{
			if (moniker == "main" && apiConfig.LocalSpecFile is { } localFile)
			{
				resolved.Add(new ResolvedApiVersion
				{
					Moniker = moniker,
					Version = entry.Version,
					IsLocal = true,
					LocalFile = localFile
				});
				continue;
			}

			resolved.Add(new ResolvedApiVersion
			{
				Moniker = moniker,
				Version = entry.Version,
				IsLocal = false,
				ObjectKey = $"{repository}/{entry.Version}/{apiConfig.SpecFileName}"
			});
		}

		return resolved;
	}

	public async Task<Stream?> FetchSpecStreamAsync(string apiKey, ResolvedApiVersion version, IDiagnosticsCollector collector, Cancel ctx = default)
	{
		if (version.ObjectKey is not { } objectKey)
			throw new InvalidOperationException($"Version '{version.Moniker}' of API '{apiKey}' is local; read {nameof(ResolvedApiVersion.LocalFile)} instead.");

		var uri = new Uri(_baseUri, objectKey);
		string? lastError = null;
		for (var attempt = 1; attempt <= _maxAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				return await FetchStreamAsync(uri, attempt, ctx).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				lastError = ex.Message;
				if (attempt >= _maxAttempts)
					break;
				await _sleep(RetryDelay(attempt), ctx).ConfigureAwait(false);
			}
		}

		collector.EmitGlobalWarning(
			$"Could not fetch spec '{objectKey}' for version '{version.Moniker}' of API '{apiKey}' from {uri} after {_maxAttempts} attempt(s): {lastError}. Skipping this version.");
		return null;
	}

	private static ResolvedApiVersion LocalMain(IFileInfo localFile) =>
		new() { Moniker = "main", Version = "main", IsLocal = true, LocalFile = localFile };

	private static IReadOnlyList<ResolvedApiVersion> NoRepositoryFallback(string apiKey, ResolvedApiConfiguration apiConfig, IDiagnosticsCollector collector)
	{
		if (apiConfig.LocalSpecFile is { } localFile)
			return [LocalMain(localFile)];

		collector.EmitGlobalError(
			$"API '{apiKey}' has no local spec file, and its repository could not be determined: there is no " +
			"'repository:' override on the api entry and the current checkout has no resolvable GitHub remote. " +
			"Add a local spec, set 'repository:' on the api entry, or build from a checkout with a GitHub remote.");
		return [];
	}

	private static IReadOnlyList<ResolvedApiVersion> MissingEntryFallback(
		string apiKey, ResolvedApiConfiguration apiConfig, string reason, IDiagnosticsCollector collector)
	{
		if (apiConfig.LocalSpecFile is { } localFile)
		{
			collector.EmitGlobalWarning($"{reason}; only the local spec will be rendered for API '{apiKey}'.");
			return [LocalMain(localFile)];
		}

		collector.EmitGlobalError($"{reason}, and no local spec file is configured for API '{apiKey}'; this API cannot be rendered.");
		return [];
	}

	private async Task<(RootVersionIndex? Index, string? Error)> GetRootIndexAsync(Cancel ctx)
	{
		if (_rootIndexFetched)
			return (_rootIndex, _rootIndexFetchError);

		await _rootIndexLock.WaitAsync(ctx).ConfigureAwait(false);
		try
		{
			if (_rootIndexFetched)
				return (_rootIndex, _rootIndexFetchError);

			try
			{
				_rootIndex = await FetchIndexAsync(ctx).ConfigureAwait(false);
				_rootIndexFetchError = _rootIndex is null ? "the response was empty or could not be parsed" : null;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_rootIndexFetchError = ex.Message;
			}
			finally
			{
				_rootIndexFetched = true;
			}

			return (_rootIndex, _rootIndexFetchError);
		}
		finally
		{
			_ = _rootIndexLock.Release();
		}
	}

	private async Task<RootVersionIndex?> FetchIndexAsync(Cancel ctx)
	{
		Exception? lastError = null;
		for (var attempt = 1; attempt <= _maxAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				await using var stream = await FetchStreamAsync(_indexUri, attempt, ctx).ConfigureAwait(false);
				return await JsonSerializer.DeserializeAsync(
					stream,
					VersionIndexJsonContext.Default.DictionaryStringDictionaryStringDictionaryStringVersionIndexEntry,
					ctx).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				lastError = ex;
				if (attempt >= _maxAttempts)
					break;
				await _sleep(RetryDelay(attempt), ctx).ConfigureAwait(false);
			}
		}

		if (lastError is not null)
			throw lastError;
		return null;
	}

	private async Task<Stream> FetchStreamAsync(Uri uri, int attempt, Cancel ctx)
	{
		var requestUri = attempt > 1 ? WithCacheBuster(uri) : uri;
		using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
		var response = await _httpClient.SendAsync(request, ctx).ConfigureAwait(false);
		_ = response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStreamAsync(ctx).ConfigureAwait(false);
	}

	private static TimeSpan RetryDelay(int attempt)
	{
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

	public void Dispose()
	{
		_ownedHttpClient?.Dispose();
		_rootIndexLock.Dispose();
		GC.SuppressFinalize(this);
	}
}
