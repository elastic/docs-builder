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
/// Rebuilds product-scoped notes indexes for one repository by listing all <c>note-*.yml</c>
/// objects under <c>changelog/{org}/{repo}/</c>, reading each to extract <c>(product, version)</c>
/// pairs from <c>products[]</c>, and writing <c>notes-{product}-{version}.json</c> with
/// conditional S3 writes. Leftover version-only <c>notes-{version}.json</c> keys are deleted.
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
	/// Rebuilds product-scoped notes indexes for the given repository scope.
	/// All currently published <c>note-*.yml</c> files across every branch are listed and
	/// read to derive the grouping; every affected index is then (re)written.
	/// Leftover version-only <c>notes-{version}.json</c> objects are deleted as stale.
	/// </summary>
	/// <returns>
	/// Notes grouped by product, then version. Consumed by <see cref="NoteAmendReconciler"/>.
	/// A product×version whose product-scoped index is stale is included with an empty note list
	/// so amend can drop that product's sidecar. Those keys stay in S3 until amend succeeds;
	/// leftover version-only indexes are deleted in this pass.
	/// </returns>
	public async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>>> ReconcileRepoAsync(
		ChangelogScope notesScope,
		Cancel ctx
	)
	{
		if (notesScope.Kind != ChangelogScopeKind.Notes)
			throw new ArgumentException($"Notes reconcile requires a Notes scope; got '{notesScope}'.", nameof(notesScope));

		_logger.LogInformation("Reconciling notes indexes for repo {Repo}", notesScope.Group);

		var noteObjects = await ListNoteFiles(notesScope, ctx);
		_logger.LogDebug("Found {Count} note file(s) for {Repo}", noteObjects.Count, notesScope.Group);

		var byProductVersion = new Dictionary<string, List<NoteIndexEntry>>(StringComparer.Ordinal);
		foreach (var obj in noteObjects)
		{
			ctx.ThrowIfCancellationRequested();
			var poolRelativePath = obj.Key[notesScope.Prefix.Length..];

			var pairs = await ExtractProductVersionsAsync(obj.Key, ctx);
			foreach (var (product, version) in pairs)
				AddIndexEntry(byProductVersion, ProductVersionGroupKey(product, version), poolRelativePath);
		}

		var groupParts = notesScope.Group.Split('/');
		var (org, repo) = (groupParts[0], groupParts[1]);

		var existingIndexKeys = await ListExistingNotesIndexes(notesScope, ctx);
		var intendedKeys = IntendedIndexKeys(org, repo, byProductVersion.Keys);

		var writtenProducts = new Dictionary<string, Dictionary<string, IReadOnlyList<NoteIndexEntry>>>(StringComparer.Ordinal);
		IReadOnlyList<(string Product, string Version)> vanished = [];
		try
		{
			if (byProductVersion.Count == 0)
			{
				_logger.LogDebug("No versions found for repo {Repo}; removing any stale indexes", notesScope.Group);
			}
			else
			{
				var writes = BuildIndexWrites(org, repo, byProductVersion);
				await Parallel.ForEachAsync(writes, new ParallelOptions
				{
					MaxDegreeOfParallelism = MaxParallelReads,
					CancellationToken = ctx
				}, async (write, ct) =>
				{
					await WriteIndexAsync(write.Key, write.Entries, ct, new NotesIndexMetadata(write.Product, write.Version));
					lock (writtenProducts)
					{
						if (!writtenProducts.TryGetValue(write.Product, out var byVer))
							writtenProducts[write.Product] = byVer = [with(StringComparer.Ordinal)];
						byVer[write.Version] = write.Entries;
					}
				});
			}
		}
		finally
		{
			vanished = await DeleteStaleIndexes(existingIndexKeys, intendedKeys, ctx);
		}

		MergeVanishedProductVersions(writtenProducts, vanished);
		return ToProductMap(writtenProducts);
	}

	private static void MergeVanishedProductVersions(
		Dictionary<string, Dictionary<string, IReadOnlyList<NoteIndexEntry>>> map,
		IReadOnlyList<(string Product, string Version)> vanished
	)
	{
		foreach (var (product, version) in vanished)
		{
			if (!map.TryGetValue(product, out var byVer))
				map[product] = byVer = [with(StringComparer.Ordinal)];
			if (!byVer.ContainsKey(version))
				byVer[version] = [];
		}
	}

	private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>> ToProductMap(
		Dictionary<string, Dictionary<string, IReadOnlyList<NoteIndexEntry>>> map
	) => map.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>)kv.Value, StringComparer.Ordinal);

	private static void AddIndexEntry(Dictionary<string, List<NoteIndexEntry>> map, string groupKey, string poolRelativePath)
	{
		if (!map.TryGetValue(groupKey, out var entries))
			map[groupKey] = entries = [];

		if (!entries.Any(e => e.Path == poolRelativePath))
			entries.Add(new NoteIndexEntry { Path = poolRelativePath, BundleSeq = 0 });
	}

	private static string ProductVersionGroupKey(string product, string version) => $"{product}/{version}";

	private static HashSet<string> IntendedIndexKeys(string org, string repo, IEnumerable<string> productVersionKeys)
	{
		var intended = new HashSet<string>(StringComparer.Ordinal);
		foreach (var groupKey in productVersionKeys)
		{
			var slash = groupKey.IndexOf('/', StringComparison.Ordinal);
			_ = intended.Add(ChangelogKeys.NotesIndexKey(org, repo, groupKey[..slash], groupKey[(slash + 1)..]));
		}
		return intended;
	}

	private static List<NotesIndexWrite> BuildIndexWrites(
		string org,
		string repo,
		Dictionary<string, List<NoteIndexEntry>> byProductVersion
	)
	{
		var writes = new List<NotesIndexWrite>(byProductVersion.Count);
		foreach (var (groupKey, entries) in byProductVersion)
		{
			var slash = groupKey.IndexOf('/', StringComparison.Ordinal);
			var product = groupKey[..slash];
			var version = groupKey[(slash + 1)..];
			writes.Add(
				new NotesIndexWrite(ChangelogKeys.NotesIndexKey(org, repo, product, version), SortEntries(entries), product, version)
			);
		}
		return writes;
	}

	private static List<NoteIndexEntry> SortEntries(IReadOnlyList<NoteIndexEntry> entries) =>
		entries.DistinctBy(e => e.Path, StringComparer.Ordinal).OrderBy(e => e.Path, StringComparer.Ordinal).ToList();

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

	private async Task<IReadOnlyList<(string Product, string Version)>> DeleteStaleIndexes(
		IReadOnlyList<string> existingKeys,
		HashSet<string> intendedKeys,
		Cancel ctx
	)
	{
		var vanished = new List<(string Product, string Version)>();
		foreach (var key in existingKeys)
		{
			if (intendedKeys.Contains(key))
				continue;

			var read = await ReadStaleIndexIdentity(key, ctx);
			if (read.Kind is StaleIndexReadKind.ReadFailed or StaleIndexReadKind.AlreadyGone)
				continue;

			if (read.Kind == StaleIndexReadKind.ProductScoped)
			{
				vanished.Add((read.Product!, read.Version!));
				continue;
			}

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

		return vanished;
	}

	/// <summary>
	/// Reads identity from a stale notes-index body. Does not parse the filename slug.
	/// A failed GET/deserialize must not delete the object; a legacy body (no product/version)
	/// may be deleted without a vanished product.
	/// </summary>
	private async Task<StaleIndexRead> ReadStaleIndexIdentity(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest { BucketName = publicBucketName, Key = key }, ctx);
			await using var stream = response.ResponseStream;
			var index = await JsonSerializer.DeserializeAsync(stream, NotesIndexJsonContext.Default.NotesIndex, ctx);
			if (index?.Product is not { Length: > 0 } product || index.Version is not { Length: > 0 } version)
				return new StaleIndexRead(StaleIndexReadKind.Legacy);
			if (!ChangelogKeys.IsValidProduct(product) || !ChangelogKeys.IsValidRepo(version))
				return new StaleIndexRead(StaleIndexReadKind.Legacy);
			return new StaleIndexRead(StaleIndexReadKind.ProductScoped, product, version);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new StaleIndexRead(StaleIndexReadKind.AlreadyGone);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read identity from stale notes index {Key}; leaving it for the next reconcile", key);
			return new StaleIndexRead(StaleIndexReadKind.ReadFailed);
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
	/// Reads a note file and returns every valid <c>(product, version)</c> pair it should be
	/// indexed under. Prefers <c>products[].versions</c>; falls back to the legacy
	/// <c>products[].target</c> for already-published notes that pre-date the <c>versions:</c> field.
	/// </summary>
	private async Task<IReadOnlyList<(string Product, string Version)>> ExtractProductVersionsAsync(string key, Cancel ctx)
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

			var valid = new List<(string Product, string Version)>();
			foreach (var productInfo in dto.Products)
			{
				var productId = productInfo.Product?.Trim();
				if (
					string.IsNullOrEmpty(productId)
					|| productId.Contains('/', StringComparison.Ordinal)
					|| !ChangelogKeys.IsValidProduct(productId)
				)
				{
					if (!string.IsNullOrEmpty(productId))
					{
						_logger.LogWarning(
							"Note {Key} has product '{Product}' that is not a valid product segment; skipping",
							key,
							productId
						);
					}
					continue;
				}

				// Prefer the new `versions` list; fall back to the legacy `target` field for compat.
#pragma warning disable CS0618 // reading obsolete Target for backward compat
				IEnumerable<string?> rawVersions = productInfo.Versions is { Count: > 0 }
					? productInfo.Versions
					: productInfo.Target is not null ? [productInfo.Target] : [];
#pragma warning restore CS0618

				foreach (var raw in rawVersions.Where(v => !string.IsNullOrWhiteSpace(v)))
				{
					var v = raw!.Trim();
					if (v.Contains('/', StringComparison.Ordinal) || !ChangelogKeys.IsValidRepo(v))
					{
						_logger.LogWarning("Note {Key} has version '{Version}' that is not a valid single path segment; skipping", key, v);
						continue;
					}
					if (!valid.Any(p => p.Product == productId && p.Version == v))
						valid.Add((productId, v));
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
	/// Deletes a notes-index object. A missing key is success so amend can drop a vanished
	/// product's index after the sidecar work without racing a prior delete.
	/// </summary>
	public async Task DeleteIndexAsync(string key, Cancel ctx)
	{
		try
		{
			_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = publicBucketName, Key = key }, ctx);
			_logger.LogInformation("Removed notes index {Key}", key);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			_logger.LogDebug("Notes index {Key} already absent", key);
		}
	}

	/// <summary>
	/// Writes the notes index with conditional S3 writes (If-Match / If-None-Match) to guard against
	/// concurrent reconcile races, mirroring the pattern used by <see cref="BundleRegistryReconciler"/>.
	/// </summary>
	/// <remarks>
	/// <paramref name="entries"/> have their <c>bundle_seq</c> already set by the caller
	/// (0 from this reconciler; updated values from <see cref="NoteAmendReconciler"/>).
	/// Pass <paramref name="metadata"/> only for product-scoped keys; omit it for the legacy
	/// version-union body so older deserializers keep seeing <c>notes</c> only.
	/// </remarks>
	public async Task WriteIndexAsync(string key, IReadOnlyList<NoteIndexEntry> entries, Cancel ctx, NotesIndexMetadata? metadata = null)
	{
		var index = new NotesIndex
		{
			SchemaVersion = NotesIndex.CurrentSchemaVersion,
			Product = metadata?.Product,
			Version = metadata?.Version,
			Notes = entries
		};
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

	private readonly record struct NotesIndexWrite(string Key, IReadOnlyList<NoteIndexEntry> Entries, string Product, string Version);

	private enum StaleIndexReadKind
	{
		ReadFailed,
		AlreadyGone,
		Legacy,
		ProductScoped
	}

	private readonly record struct StaleIndexRead(StaleIndexReadKind Kind, string? Product = null, string? Version = null);
}

/// <summary>Optional product and version written onto a product-scoped notes index body.</summary>
public sealed record NotesIndexMetadata(string Product, string Version);
