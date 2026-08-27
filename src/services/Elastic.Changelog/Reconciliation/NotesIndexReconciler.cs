// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Rebuilds the per-version <c>notes-{version}.json</c> indexes for one repository by listing
/// all <c>note-*.yml</c> objects under <c>changelog/{org}/{repo}/</c>, reading each to extract
/// its <c>versions:</c> values (falling back to the legacy <c>target:</c> field for backward
/// compatibility), and writing the affected indexes atomically with conditional S3 writes.
/// </summary>
/// <remarks>
/// A note may declare multiple versions, so one note can appear in several indexes. The index
/// stores pool-relative paths (<c>{branch}/note-{name}.yml</c>) so the same filename on two
/// branches yields two distinct entries in the same index. The <c>bundle_seq</c> field on each
/// entry is filled by <see cref="NoteAmendReconciler"/> in a subsequent pass; this reconciler
/// sets it to 0 for all entries (no bundle awareness here, keeping concerns separated).
/// </remarks>
public sealed class NotesIndexReconciler(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string publicBucketName,
	string? sourceBucketName = null,
	TimeSpan? retryBaseDelay = null,
	ReconcileMetrics? metrics = null
)
{
	private const int MaxWriteAttempts = 5;
	private const int MaxParallelReads = 8;

	private readonly ILogger _logger = logFactory.CreateLogger<NotesIndexReconciler>();
	private readonly TimeSpan _retryBaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(200);
	private readonly ReconcileMetrics _metrics = metrics ?? new ReconcileMetrics();
	private readonly string _sourceBucketName = sourceBucketName ?? publicBucketName;

	/// <summary>
	/// Rebuilds all <c>notes-{version}.json</c> indexes for the given repository scope.
	/// All currently published <c>note-*.yml</c> files across every branch are listed and
	/// read to derive the version grouping; every affected index is then (re)written.
	/// </summary>
	/// <returns>
	/// A map of <c>version → list of NoteIndexEntry</c> for all versions found.
	/// Returns an empty dictionary when no notes exist. Consumed by <see cref="NoteAmendReconciler"/>
	/// in the same SQS-batch pass to compute <c>bundle_seq</c> and publish amend sidecars.
	/// </returns>
	public async Task<IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>> ReconcileRepoAsync(ChangelogScope notesScope, Cancel ctx)
	{
		if (notesScope.Kind != ChangelogScopeKind.Notes)
			throw new ArgumentException($"Notes reconcile requires a Notes scope; got '{notesScope}'.", nameof(notesScope));

		_logger.LogInformation("Reconciling notes indexes for repo {Repo}", notesScope.Group);

		// List every note-*.yml under changelog/{org}/{repo}/ (all branches).
		var noteObjects = await ListNoteFiles(notesScope, ctx);
		_logger.LogDebug("Found {Count} note file(s) for {Repo}", noteObjects.Count, notesScope.Group);

		// Read each note to extract its versions.
		// byVersion: version slug → list of NoteIndexEntry (bundle_seq defaulted to 0; filled by NoteAmendReconciler)
		var byVersion = new Dictionary<string, List<NoteIndexEntry>>(StringComparer.Ordinal);
		foreach (var obj in noteObjects)
		{
			ctx.ThrowIfCancellationRequested();
			var poolRelativePath = obj.Key[notesScope.Prefix.Length..];

			var versions = await ExtractVersionsAsync(obj.Key, ctx);
			foreach (var version in versions)
			{
				if (!byVersion.TryGetValue(version, out var entries))
					byVersion[version] = entries = [];

				// Deduplicate by path within the same version.
				if (!entries.Any(e => e.Path == poolRelativePath))
					entries.Add(new NoteIndexEntry { Path = poolRelativePath, BundleSeq = 0 });
			}
		}

		var groupParts = notesScope.Group.Split('/');
		var (org, repo) = (groupParts[0], groupParts[1]);

		// List existing notes-*.json indexes so we can remove obsolete ones.
		var existingIndexKeys = await ListExistingNotesIndexes(notesScope, ctx);

		if (byVersion.Count == 0)
		{
			_logger.LogDebug("No versions found for repo {Repo}; removing any stale indexes", notesScope.Group);
			await DeleteStaleIndexes(existingIndexKeys, [], org, repo, ctx);
			return new Dictionary<string, IReadOnlyList<NoteIndexEntry>>();
		}

		// Write one index per version. bundle_seq values default to 0 here; NoteAmendReconciler updates them.
		// DeleteStaleIndexes runs even if some writes fail — stale deletion is safe because we only
		// remove versions absent from byVersion.Keys, which is independent of write success.
		var written = new Dictionary<string, IReadOnlyList<NoteIndexEntry>>(StringComparer.Ordinal);
		try
		{
			await Parallel.ForEachAsync(byVersion, new ParallelOptions
			{
				MaxDegreeOfParallelism = MaxParallelReads,
				CancellationToken = ctx
			}, async (kvp, ct) =>
			{
				var (version, entries) = kvp;
				var indexKey = ChangelogKeys.NotesIndexKey(org, repo, version);
				var sortedEntries = entries
					.DistinctBy(e => e.Path, StringComparer.Ordinal)
					.OrderBy(e => e.Path, StringComparer.Ordinal)
					.ToList();
				await WriteIndexAsync(indexKey, sortedEntries, ct);
				lock (written)
					written[version] = sortedEntries;
			});
		}
		finally
		{
			// Remove indexes whose versions are no longer present.
			await DeleteStaleIndexes(existingIndexKeys, byVersion.Keys.ToHashSet(StringComparer.Ordinal), org, repo, ctx);
		}

		return written;
	}

	private async Task<IReadOnlyList<string>> ListExistingNotesIndexes(ChangelogScope notesScope, Cancel ctx)
	{
		var request = new ListObjectsV2Request { BucketName = publicBucketName, Prefix = notesScope.Prefix };

		var keys = new List<string>();
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var obj in response.S3Objects ?? [])
			{
				if (ChangelogKeys.IsNotesIndex(obj.Key))
					keys.Add(obj.Key);
			}
			request.ContinuationToken = response.NextContinuationToken;
		}
		while (response.IsTruncated == true);

		return keys;
	}

	private async Task DeleteStaleIndexes(
		IReadOnlyList<string> existingKeys,
		HashSet<string> currentVersions,
		string org,
		string repo,
		Cancel ctx
	)
	{
		// "changelog/{org}/{repo}/notes-" — the stable prefix shared by all notes-*.json keys for this repo.
		var notesKeyPrefix = $"{ChangelogKeys.ChangelogPrefix}{org}/{repo}/notes-";

		foreach (var key in existingKeys)
		{
			// Extract the version slug from the key to check if it's still needed.
			if (!key.StartsWith(notesKeyPrefix, StringComparison.Ordinal))
				continue;
			var versionSlug = key[notesKeyPrefix.Length..^".json".Length];
			if (currentVersions.Contains(versionSlug))
				continue;

			try
			{
				_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = publicBucketName, Key = key }, ctx);
				_logger.LogInformation("Removed stale notes index {Key}", key);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogWarning(ex, "Failed to delete stale notes index {Key}", key);
			}
		}
	}

	private async Task<IReadOnlyList<S3Object>> ListNoteFiles(ChangelogScope notesScope, Cancel ctx)
	{
		var request = new ListObjectsV2Request
		{
			BucketName = publicBucketName,
			Prefix = notesScope.Prefix
			// No delimiter: list all branches recursively.
		};

		var files = new List<S3Object>();
		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var obj in response.S3Objects ?? [])
			{
				var relativePath = obj.Key[notesScope.Prefix.Length..];
				// Use LastIndexOf so branch names containing '/' (e.g. feature/foo) are handled correctly.
				var slash = relativePath.LastIndexOf('/');
				if (slash <= 0)
					continue;
				var fileName = relativePath[(slash + 1)..];
				if (!IsNoteFileName(fileName))
					continue;
				files.Add(obj);
				_metrics.IncrementObjectsListed();
			}
			request.ContinuationToken = response.NextContinuationToken;
		}
		while (response.IsTruncated == true);

		return files;
	}

	private static bool IsNoteFileName(string fileName) =>
		fileName.StartsWith("note-", StringComparison.OrdinalIgnoreCase)
			&& (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
				|| fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
			&& !fileName.Contains('/', StringComparison.Ordinal);

	/// <summary>
	/// Reads a note file and returns all version slugs it should be indexed under.
	/// Prefers <c>products[].versions</c>; falls back to the legacy <c>products[].target</c>
	/// for already-published notes that pre-date the <c>versions:</c> field.
	/// </summary>
	private async Task<IReadOnlyList<string>> ExtractVersionsAsync(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest { BucketName = _sourceBucketName, Key = key }, ctx);

			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var yaml = await reader.ReadToEndAsync(ctx);

			var normalized = ReleaseNotesSerialization.NormalizeYaml(yaml);
			var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized);

			if (dto.Products is not { Count: > 0 })
				return [];

			var valid = new List<string>();
			foreach (var productInfo in dto.Products)
			{
				// Prefer the new `versions` list; fall back to the legacy `target` field for compat.
#pragma warning disable CS0618 // reading obsolete Target for backward compat
				IEnumerable<string?> rawVersions = productInfo.Versions is { Count: > 0 }
					? productInfo.Versions
					: productInfo.Target is not null ? [productInfo.Target] : [];
#pragma warning restore CS0618

				foreach (var raw in rawVersions.Where(v => !string.IsNullOrWhiteSpace(v)))
				{
					var v = raw!.Trim();
					if (v.Contains('/', StringComparison.Ordinal))
					{
						_logger.LogWarning(
							"Note {Key} has version '{Version}' containing '/'; skipping — versions must be single path segments",
							key,
							v
						);
						continue;
					}
					if (!valid.Contains(v, StringComparer.Ordinal))
						valid.Add(v);
				}
			}
			return valid;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			// Note was deleted between the list and the read; skip it.
			return [];
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read versions from note {Key}; skipping", key);
			return [];
		}
	}

	/// <summary>
	/// Writes the notes index with conditional S3 writes (If-Match / If-None-Match) to guard against
	/// concurrent reconcile races, mirroring the pattern used by <see cref="BundleRegistryReconciler"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="entries"/> have their <c>bundle_seq</c> already set by the caller
	/// (0 from this reconciler; updated values from <see cref="NoteAmendReconciler"/>).
	/// </remarks>
	public async Task WriteIndexAsync(string key, IReadOnlyList<NoteIndexEntry> entries, Cancel ctx)
	{
		var index = new NotesIndex { SchemaVersion = NotesIndex.CurrentSchemaVersion, Notes = entries };
		var newJson = JsonSerializer.Serialize(index, NotesIndexJsonContext.Default.NotesIndex);

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				// Read current ETag so we can do a conditional PUT.
				string? currentETag = null;
				try
				{
					var head = await s3Client.GetObjectMetadataAsync(
						new GetObjectMetadataRequest { BucketName = publicBucketName, Key = key },
						ctx
					);
					currentETag = head.ETag;
				}
				catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
				{
					// Key does not exist yet — conditional create.
				}

				// Skip write when content is unchanged (content equality, not ETag).
				if (currentETag != null)
				{
					try
					{
						var existing = await s3Client.GetObjectAsync(
							new GetObjectRequest { BucketName = publicBucketName, Key = key },
							ctx
						);
						await using var existStream = existing.ResponseStream;
						using var existReader = new StreamReader(existStream);
						var existingJson = await existReader.ReadToEndAsync(ctx);
						if (existingJson == newJson)
						{
							_logger.LogDebug("Notes index {Key} is unchanged; skipping write", key);
							return;
						}
					}
					catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
					{
						currentETag = null; // lost a race — treat as not-found
					}
				}

				var putRequest = new PutObjectRequest
				{
					BucketName = publicBucketName,
					Key = key,
					ContentBody = newJson,
					ContentType = "application/json"
				};

				// Conditional write: update matches ETag; create uses If-None-Match.
				if (currentETag != null)
					putRequest.Headers["If-Match"] = currentETag;
				else
					putRequest.Headers["If-None-Match"] = "*";

				_ = await s3Client.PutObjectAsync(putRequest, ctx);

				_metrics.IncrementRegistryWrites();
				_logger.LogInformation("Wrote notes index {Key} with {Count} entry(ies)", key, entries.Count);
				return;
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.PreconditionFailed || (int)ex.StatusCode == 409)
			{
				// Conditional write lost — another reconciler won the race. Retry after jittered delay.
				if (attempt >= MaxWriteAttempts)
				{
					_logger.LogError("Notes index write {Key} failed after {Max} conditional-write conflicts", key, MaxWriteAttempts);
					throw;
				}
				var jitter = TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * 100);
				var delay = (_retryBaseDelay * attempt) + jitter;
				_logger.LogDebug(
					"Notes index {Key} conditional write conflict (attempt {A}/{Max}); retrying in {Delay}",
					key,
					attempt,
					MaxWriteAttempts,
					delay
				);
				await Task.Delay(delay, ctx);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				if (attempt >= MaxWriteAttempts)
					throw;

				var delay = _retryBaseDelay * attempt;
				_logger.LogDebug(
					ex,
					"Notes index write {Key} failed (attempt {A}/{Max}); retrying in {Delay}",
					key,
					attempt,
					MaxWriteAttempts,
					delay
				);
				await Task.Delay(delay, ctx);
			}
		}
	}
}
