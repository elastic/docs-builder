// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Maintains the shallow per-tree registries on the public bucket —
/// <c>bundle/registry.json</c> and <c>changelog/registry.json</c> — mapping each folder (a
/// product, or an <c>{org}/{repo}/{branch}</c> pool) to an opaque change token. CDN consumers
/// that cache a folder's content can compare one small object to decide whether anything under
/// that folder changed, before diving into the folder itself.
/// </summary>
/// <remarks>
/// <para>
/// The token is a digest over the folder's full listing (sorted file/ETag pairs), not the ETag of
/// any single object: a "last-touched object's ETag" goes stale when an <em>older</em> object is
/// deleted, since the newest object — and therefore the value — would not change. Consumers must
/// treat the value as opaque.
/// </para>
/// <para>
/// Like the group reconciler, this is <c>f(state)</c>: touched folders are re-listed and the map
/// is patched with optimistic concurrency. An absent or unparseable map is rebuilt from a full
/// tree listing, which is also how the map is seeded on first deploy.
/// </para>
/// </remarks>
public sealed class ShallowRegistryReconciler(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string publicBucketName,
	TimeSpan? retryBaseDelay = null,
	ReconcileMetrics? metrics = null
)
{
	// Bounds the optimistic-concurrency retry loop; each attempt re-lists and re-reads before retrying.
	private const int MaxWriteAttempts = 5;

	private readonly ILogger _logger = logFactory.CreateLogger<ShallowRegistryReconciler>();
	private readonly TimeSpan _retryBaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(200);
	private readonly ReconcileMetrics _metrics = metrics ?? new ReconcileMetrics();

	/// <summary>
	/// Converges the tree's shallow map for <paramref name="touched"/> folders (all of
	/// <paramref name="kind"/>). Throws <see cref="ReconcileConflictException"/> when concurrent
	/// conditional writers win every bounded retry — the caller fails the SQS message so
	/// redelivery retries later.
	/// </summary>
	public async Task ReconcileAsync(ChangelogScopeKind kind, IReadOnlyCollection<ChangelogScope> touched, Cancel ctx)
	{
		if (touched.Count == 0)
			return;
		if (touched.Any(scope => scope.Kind != kind))
			throw new ArgumentException($"Every touched scope must be of kind {kind}.", nameof(touched));

		var mapKey = TreeRegistryKey(kind);

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var existing = await FetchMap(mapKey, ctx);

			SortedDictionary<string, string> map;
			if (existing.Map is { } parsed)
			{
				map = parsed;
				foreach (var scope in touched)
				{
					var token = await ComputeFolderToken(scope, ctx);
					if (token is null)
						_ = map.Remove(scope.Group);
					else
						map[scope.Group] = token;
				}
			}
			else
			{
				// Absent or unparseable: rebuild the whole tree's map from one full listing. This
				// is the first-deploy seed path too, so a single event heals every folder at once.
				map = await RebuildTreeMap(kind, ctx);
			}

			if (existing.Map is not null && MapsEqual(existing.Map, existing.Original!, map))
			{
				_metrics.IncrementShallowRegistryUnchanged();
				_logger.LogDebug("Shallow map {Key} already matches state; skipping write", mapKey);
				return;
			}

			var converged = map.Count == 0
				? await TryDeleteMap(mapKey, existing.ETag, attempt, ctx)
				: await TryPutMap(mapKey, map, existing.ETag, attempt, ctx);
			if (converged)
				return;
			await BackOff(attempt, ctx);
		}

		throw new ReconcileConflictException(
			$"Shallow map {mapKey} kept changing concurrently after {MaxWriteAttempts} attempts; failing the message for redelivery.");
	}

	/// <summary>The S3 key of a tree's shallow map: <c>bundle/registry.json</c> or <c>changelog/registry.json</c>.</summary>
	public static string TreeRegistryKey(ChangelogScopeKind kind) => kind == ChangelogScopeKind.Bundle
		? $"{ChangelogKeys.BundlePrefix}{ChangelogKeys.RegistryFileName}"
		: $"{ChangelogKeys.ChangelogPrefix}{ChangelogKeys.RegistryFileName}";

	/// <summary>
	/// The folder's change token from its current public listing, or null when the folder holds no
	/// content. Group manifests are excluded: they are derived from the same content this token
	/// already covers, and the reconciler rewriting one must not invalidate consumer caches.
	/// </summary>
	private async Task<string?> ComputeFolderToken(ChangelogScope scope, Cancel ctx)
	{
		var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
		var request = new ListObjectsV2Request
		{
			BucketName = publicBucketName,
			Prefix = scope.Prefix,
			Delimiter = "/"
		};

		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var obj in response.S3Objects ?? [])
			{
				var file = obj.Key[scope.Prefix.Length..];
				if (IsYamlFileName(file))
					files[file] = NormalizeETag(obj.ETag);
			}
			request.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated == true);

		return files.Count == 0 ? null : TokenOf(files);
	}

	/// <summary>
	/// Rebuilds every folder's token of <paramref name="kind"/> from one full (undelimited) tree
	/// listing. Keys that do not parse into a valid scope, nested keys, and manifests are skipped —
	/// the same content rules the per-folder listing applies.
	/// </summary>
	private async Task<SortedDictionary<string, string>> RebuildTreeMap(ChangelogScopeKind kind, Cancel ctx)
	{
		var folders = new Dictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
		var request = new ListObjectsV2Request
		{
			BucketName = publicBucketName,
			Prefix = kind == ChangelogScopeKind.Bundle ? ChangelogKeys.BundlePrefix : ChangelogKeys.ChangelogPrefix
		};

		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var obj in response.S3Objects ?? [])
			{
				if (!ChangelogScope.TryFromKey(obj.Key, out var scope) || scope.Kind != kind)
					continue;

				var file = obj.Key[scope.Prefix.Length..];
				if (!IsYamlFileName(file) || file.Contains('/', StringComparison.Ordinal))
					continue;

				if (!folders.TryGetValue(scope.Group, out var files))
				{
					files = [with(StringComparer.Ordinal)];
					folders[scope.Group] = files;
				}
				files[file] = NormalizeETag(obj.ETag);
			}
			request.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated == true);

		var map = new SortedDictionary<string, string>(StringComparer.Ordinal);
		foreach (var (group, files) in folders)
			map[group] = TokenOf(files);
		return map;
	}

	private static bool IsYamlFileName(string file) =>
		file.Length > 0
		&& (file.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));

	private static string TokenOf(SortedDictionary<string, string> files)
	{
		var builder = new StringBuilder();
		foreach (var (file, etag) in files)
			_ = builder.Append(file).Append('\n').Append(etag).Append('\n');
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))[..32];
	}

	private sealed record MapState(SortedDictionary<string, string>? Map, string? Original, string? ETag);

	/// <summary>Reads the map, distinguishing absent (no ETag) from unparseable (live ETag, null map).</summary>
	private async Task<MapState> FetchMap(string key, Cancel ctx)
	{
		string? etag = null;
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = publicBucketName,
				Key = key
			}, ctx);

			etag = response.ETag;
			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var original = await reader.ReadToEndAsync(ctx);
			var map = JsonSerializer.Deserialize(original, ShallowRegistryJsonContext.Default.SortedDictionaryStringString);
			return new MapState(map, original, etag);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new MapState(null, null, null);
		}
		catch (JsonException ex)
		{
			// An unparseable map is rebuilt from the tree listing; its live ETag lets the
			// conditional write replace it safely.
			_logger.LogWarning(ex, "Shallow map {Key} could not be parsed; rebuilding from the tree listing", key);
			return new MapState(null, null, etag);
		}
	}

	/// <summary>
	/// Serialized-form comparison: a map that parses to the same pairs but was not written by this
	/// serializer (different ordering/whitespace) is rewritten once so the stored bytes converge.
	/// </summary>
	private static bool MapsEqual(SortedDictionary<string, string> before, string original, SortedDictionary<string, string> after)
	{
		if (before.Count != after.Count)
			return false;

		foreach (var (group, token) in after)
		{
			if (!before.TryGetValue(group, out var existing) || !string.Equals(existing, token, StringComparison.Ordinal))
				return false;
		}

		return string.Equals(original, Serialize(after), StringComparison.Ordinal);
	}

	private static string Serialize(SortedDictionary<string, string> map) =>
		JsonSerializer.Serialize(map, ShallowRegistryJsonContext.Default.SortedDictionaryStringString);

	private async Task<bool> TryPutMap(string key, SortedDictionary<string, string> map, string? etag, int attempt, Cancel ctx)
	{
		var request = new PutObjectRequest
		{
			BucketName = publicBucketName,
			Key = key,
			ContentBody = Serialize(map),
			ContentType = "application/json"
		};

		// Optimistic concurrency: update only if unchanged, create only if still absent.
		if (etag is null)
			request.IfNoneMatch = "*";
		else
			request.IfMatch = etag;

		try
		{
			_ = await s3Client.PutObjectAsync(request, ctx);
			_metrics.IncrementShallowRegistryWrites();
			_logger.LogInformation("Wrote shallow map {Key} with {Count} folder(s)", key, map.Count);
			return true;
		}
		catch (AmazonS3Exception ex) when (IsConditionalWriteConflict(ex))
		{
			_metrics.IncrementWriteConflicts();
			_logger.LogInformation(
				"Shallow map {Key} changed concurrently (attempt {Attempt}/{Max}); re-listing and retrying",
				key, attempt, MaxWriteAttempts);
			return false;
		}
	}

	private async Task<bool> TryDeleteMap(string key, string? etag, int attempt, Cancel ctx)
	{
		if (etag is null)
			return true;

		try
		{
			_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest
			{
				BucketName = publicBucketName,
				Key = key,
				IfMatch = etag
			}, ctx);
			_metrics.IncrementShallowRegistryWrites();
			_logger.LogInformation("Deleted shallow map {Key}: the tree is empty", key);
			return true;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			// Another reconciler removed it first; converged all the same.
			return true;
		}
		catch (AmazonS3Exception ex) when (IsConditionalWriteConflict(ex))
		{
			_metrics.IncrementWriteConflicts();
			_logger.LogInformation(
				"Shallow map {Key} changed concurrently during delete (attempt {Attempt}/{Max}); re-listing and retrying",
				key, attempt, MaxWriteAttempts);
			return false;
		}
	}

	// 412 = a plain conditional-request loss; 409 = ConditionalRequestConflict, S3's signal for
	// concurrent conditional writers on the same key. Both mean: re-read state and retry.
	private static bool IsConditionalWriteConflict(AmazonS3Exception ex) =>
		ex.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict;

	private async Task BackOff(int attempt, Cancel ctx)
	{
		if (_retryBaseDelay <= TimeSpan.Zero)
			return;
		var jitter = TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * _retryBaseDelay.TotalMilliseconds);
		await Task.Delay((_retryBaseDelay * attempt) + jitter, ctx);
	}

	private static string NormalizeETag(string? etag) => etag?.Trim('"') ?? string.Empty;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SortedDictionary<string, string>))]
public sealed partial class ShallowRegistryJsonContext : JsonSerializerContext;
