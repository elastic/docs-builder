// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Changelog.Bundling;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Compares each note in the product-scoped notes indexes against that product's published
/// bundles for the same version, and either creates or deletes a reconciler-owned amend sidecar
/// (<c>{parent}.amend-notes.yaml</c>) that carries notes that arrived after the release shipped.
/// Also updates each <c>bundle_seq</c> on the product-scoped notes index: 0 = no bundle yet,
/// 1 = shipped in the original bundle or a human amend, 2 = carried by the reconciler amend sidecar.
/// The dual-written legacy <c>notes-{version}.json</c> path list is refreshed in the same pass.
/// </summary>
/// <remarks>
/// <para>
/// This reconciler is <b>idempotent</b>: the sidecar is rebuilt from current state on every pass,
/// so a redelivered or out-of-order S3 event cannot produce a duplicate amend.
/// </para>
/// <para>
/// Matching notes against bundle entries uses the <b>leaf file name</b> (case-insensitive), not
/// the checksum. The checksum is unreliable for identity because the scrubber re-serializes content
/// when it strips private references, so a public pool object's hash differs from the one a
/// locally-bundled entry recorded. A missing <c>file:</c> block on an entry means the shipped
/// status is unknown (hand-authored bundles); those versions are skipped to avoid false positives.
/// </para>
/// </remarks>
public sealed class NoteAmendReconciler(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string publicBucketName,
	NotesIndexReconciler notesIndexReconciler,
	TimeSpan? retryBaseDelay = null,
	ReconcileMetrics? metrics = null
)
{
	private const int MaxParallelWrites = 4;

	private readonly ILogger _logger = logFactory.CreateLogger<NoteAmendReconciler>();
	private readonly ReconcileMetrics _metrics = metrics ?? new ReconcileMetrics();
	private readonly TimeSpan _retryBaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(200);

	/// <summary>
	/// For the given repository scope, scans each product that appears in
	/// <paramref name="notesByProduct"/> (not every <c>bundle/{product}/</c> prefix), writes or
	/// deletes that product's reconciler-owned amend sidecars, and re-writes product-scoped and
	/// legacy notes indexes with correct <c>bundle_seq</c> values. Empty version lists mean that
	/// product×version just vanished from the notes index; amend still runs so sidecars can drop.
	/// </summary>
	/// <returns>
	/// Product ids that had an amend-notes write, skip-unchanged, or delete (including when the
	/// sidecar was already absent). Callers rebuild those products' <c>registry.json</c> and the
	/// bundle shallow map. Products that were not in the notes map are not returned and are not swept.
	/// </returns>
	public async Task<IReadOnlyList<string>> ReconcileAsync(
		ChangelogScope notesScope,
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>> notesByProduct,
		Cancel ctx
	)
	{
		if (notesByProduct.Count == 0)
			return [];

		var groupParts = notesScope.Group.Split('/');
		var (org, repo) = (groupParts[0], groupParts[1]);

		var seqMap = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
		foreach (var (product, byVersion) in notesByProduct)
		{
			foreach (var (version, notes) in byVersion)
				seqMap[ProductVersionKey(product, version)] = notes.ToDictionary(n => n.Path, _ => 0, StringComparer.Ordinal);
		}

		_logger.LogDebug(
			"NoteAmendReconciler: scanning {Count} product(s) from the notes index for repo {Org}/{Repo}",
			notesByProduct.Count,
			org,
			repo
		);

		var touched = new HashSet<string>(StringComparer.Ordinal);
		foreach (var (product, byVersion) in notesByProduct)
		{
			ctx.ThrowIfCancellationRequested();
			if (await ProcessProductAsync(org, repo, product, byVersion, seqMap, ctx))
				_ = touched.Add(product);
		}

		await RewriteNotesIndexesAsync(org, repo, notesByProduct, seqMap, ctx);
		return [.. touched];
	}

	private async Task RewriteNotesIndexesAsync(
		string org,
		string repo,
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>> notesByProduct,
		IReadOnlyDictionary<string, Dictionary<string, int>> seqMap,
		Cancel ctx
	)
	{
		var productWrites = notesByProduct.SelectMany(p => p.Value.Select(v => (Product: p.Key, Version: v.Key, Notes: v.Value))).ToList();

		await Parallel.ForEachAsync(productWrites, new ParallelOptions
		{
			MaxDegreeOfParallelism = MaxParallelWrites,
			CancellationToken = ctx
		}, async (write, ct) =>
		{
			if (write.Notes.Count == 0)
				return;

			var seqs = seqMap[ProductVersionKey(write.Product, write.Version)];
			var updatedEntries = WithSeqs(write.Notes, seqs);
			var indexKey = ChangelogKeys.NotesIndexKey(org, repo, write.Product, write.Version);
			await notesIndexReconciler.WriteIndexAsync(indexKey, updatedEntries, ct, new NotesIndexMetadata(write.Product, write.Version));
		});

		foreach (var (version, unionNotes) in UnionByVersion(notesByProduct))
		{
			ctx.ThrowIfCancellationRequested();
			if (unionNotes.Count == 0)
				continue;

			var seqs = MaxSeqsForVersion(notesByProduct.Keys, version, seqMap);
			var updatedEntries = WithSeqs(unionNotes, seqs);
			var indexKey = ChangelogKeys.NotesIndexKey(org, repo, version);
			await notesIndexReconciler.WriteIndexAsync(indexKey, updatedEntries, ctx);
		}
	}

	private static List<NoteIndexEntry> WithSeqs(IReadOnlyList<NoteIndexEntry> notes, IReadOnlyDictionary<string, int> seqs) =>
		notes
			.Select(n => n with { BundleSeq = seqs.TryGetValue(n.Path, out var s) ? s : 0 })
			.OrderBy(n => n.Path, StringComparer.Ordinal)
			.ToList();

	private static Dictionary<string, List<NoteIndexEntry>> UnionByVersion(
		IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>>> notesByProduct
	)
	{
		var byVersion = new Dictionary<string, List<NoteIndexEntry>>(StringComparer.Ordinal);
		foreach (var byProductVersion in notesByProduct.Values)
		{
			foreach (var (version, notes) in byProductVersion)
			{
				if (!byVersion.TryGetValue(version, out var union))
					byVersion[version] = union = [];
				foreach (var note in notes)
				{
					if (!union.Any(e => e.Path == note.Path))
						union.Add(note);
				}
			}
		}
		return byVersion;
	}

	private static Dictionary<string, int> MaxSeqsForVersion(
		IEnumerable<string> products,
		string version,
		IReadOnlyDictionary<string, Dictionary<string, int>> seqMap
	)
	{
		var max = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var product in products)
		{
			if (!seqMap.TryGetValue(ProductVersionKey(product, version), out var seqs))
				continue;
			foreach (var (path, seq) in seqs)
			{
				if (!max.TryGetValue(path, out var current) || seq > current)
					max[path] = seq;
			}
		}
		return max;
	}

	private static string ProductVersionKey(string product, string version) => $"{product}/{version}";

	private async Task<bool> ProcessProductAsync(
		string org,
		string repo,
		string product,
		IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>> notesByVersion,
		Dictionary<string, Dictionary<string, int>> seqMap,
		Cancel ctx
	)
	{
		// Read this product's bundle registry.
		var registryKey = ChangelogKeys.BundleRegistryKey(product);
		ChangelogRegistry? registry;
		try
		{
			using var response = await s3Client.GetObjectAsync(
				new GetObjectRequest { BucketName = publicBucketName, Key = registryKey },
				ctx
			);
			await using var stream = response.ResponseStream;
			registry = await JsonSerializer.DeserializeAsync(stream, ChangelogRegistryJsonContext.Default.ChangelogRegistry, ctx);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return false;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read bundle registry for product {Product}; skipping", product);
			return false;
		}

		if (registry is null || registry.Bundles.Count == 0)
			return false;

		var touched = false;
		foreach (var (version, notes) in notesByVersion)
		{
			ctx.ThrowIfCancellationRequested();

			var parentBundle = registry.Bundles.FirstOrDefault(
				b => !string.IsNullOrEmpty(b.File) && !BundleAmendMerger.IsAmendFile(b.File) && ChangelogVersionMatch.Matches(
					version,
					b.Target,
					b.File
				)
			);

			if (parentBundle is null)
				continue;

			if (
				await ProcessVersionBundleAsync(
					org,
					repo,
					product,
					parentBundle,
					registry,
					version,
					notes,
					seqMap[ProductVersionKey(product, version)],
					ctx
				)
			)
				touched = true;
		}
		return touched;
	}

	private async Task<bool> ProcessVersionBundleAsync(
		string org,
		string repo,
		string product,
		ChangelogRegistryBundle parentRegistryBundle,
		ChangelogRegistry registry,
		string version,
		IReadOnlyList<NoteIndexEntry> notes,
		Dictionary<string, int> seqByPath,
		Cancel ctx
	)
	{
		var parentFile = parentRegistryBundle.File!;
		var parentKey = $"{ChangelogKeys.BundlePrefix}{product}/{parentFile}";

		var parent = await TryReadBundleAsync(parentKey, ctx);
		if (parent is null)
			return false;

		// A bundle with no file-annotated entries (hand-authored / legacy) has no reliable
		// shipped set — skip to avoid false positives.
		var parentHasFileAnnotations = parent.Entries.Any(e => !string.IsNullOrEmpty(e.File?.Name));
		if (parent.Entries.Count > 0 && !parentHasFileAnnotations)
		{
			_logger.LogDebug(
				"Parent bundle {Key} has no file annotations; skipping amend-notes reconcile for version {Version}",
				parentKey,
				version
			);
			return false;
		}

		// Read existing numeric amend bundles (in order) to compute the full merged set.
		var numericAmends = registry
			.Bundles
			.Where(
				b => !string.IsNullOrEmpty(b.File) && BundleAmendMerger.IsAmendFile(b.File) && BundleAmendMerger.GetAmendFileNumber(
					b.File
				) > 0 && string.Equals(BundleAmendMerger.GetParentBundlePath(b.File), parentFile, StringComparison.OrdinalIgnoreCase)
			)
			.OrderBy(b => BundleAmendMerger.GetAmendFileNumber(b.File!))
			.ToList();

		var amendBundles = new List<Bundle>(numericAmends.Count);
		foreach (var amend in numericAmends)
		{
			var bundle = await TryReadBundleAsync($"{ChangelogKeys.BundlePrefix}{product}/{amend.File}", ctx);
			if (bundle is not null)
				amendBundles.Add(bundle);
		}

		// Shipped set = parent entries merged with all numeric amends.
		var mergedEntries = BundleAmendMerger.MergeEntries(parent.Entries, amendBundles);
		var shippedLeaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var entry in mergedEntries)
		{
			var leaf = LeafName(entry.File?.Name);
			if (!string.IsNullOrEmpty(leaf))
				_ = shippedLeaves.Add(leaf);
		}

		// Classify each note.
		var lateNotes = new List<NoteIndexEntry>();
		foreach (var note in notes)
		{
			var leaf = LeafName(note.Path);
			if (leaf is not null && shippedLeaves.Contains(leaf))
				seqByPath[note.Path] = 1; // shipped in original bundle or a human amend

			else
				lateNotes.Add(note);
		}

		// Amend-notes sidecar key.
		var parentStem = Path.GetFileNameWithoutExtension(parentFile);
		var parentExt = Path.GetExtension(parentFile);
		var amendNotesFile = $"{parentStem}.amend-notes{parentExt}";
		var amendNotesKey = $"{ChangelogKeys.BundlePrefix}{product}/{amendNotesFile}";

		if (lateNotes.Count > 0)
		{
			// Fetch each late note's content from the pool to build BundledEntry records.
			var lateEntries = await FetchLateNoteEntriesAsync(org, repo, lateNotes, ctx);
			if (lateEntries.Count > 0)
			{
				var amendBundle = AmendDocumentBuilder.Build(parent.Products, lateEntries, []);
				var newJson = ReleaseNotesSerialization.SerializeBundle(amendBundle);
				_ = await WriteAmendNotesAsync(amendNotesKey, newJson, ctx);

				foreach (var note in lateNotes)
					seqByPath[note.Path] = 2;
				return true;
			}
			return false;
		}

		return await DeleteAmendNotesIfExistsAsync(amendNotesKey, ctx);
	}

	// -----------------------------------------------------------------------------------------
	// Note content fetching
	// -----------------------------------------------------------------------------------------

	private async Task<List<BundledEntry>> FetchLateNoteEntriesAsync(
		string org,
		string repo,
		IReadOnlyList<NoteIndexEntry> lateNotes,
		Cancel ctx
	)
	{
		var entries = new List<BundledEntry>(lateNotes.Count);
		foreach (var note in lateNotes)
		{
			ctx.ThrowIfCancellationRequested();
			var key = $"changelog/{org}/{repo}/{note.Path}";
			try
			{
				using var response = await s3Client.GetObjectAsync(new GetObjectRequest { BucketName = publicBucketName, Key = key }, ctx);

				await using var stream = response.ResponseStream;
				using var reader = new StreamReader(stream);
				var yaml = await reader.ReadToEndAsync(ctx);

				var normalized = ReleaseNotesSerialization.NormalizeYaml(yaml);
				var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized);
				var entry = ReleaseNotesSerialization.ConvertEntry(dto);
				var checksum = ChangelogBundlingService.ComputeSha1(yaml);

				entries.Add(entry.ToBundledEntry() with { File = new BundledFile { Name = note.Path, Checksum = checksum } });
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
				_logger.LogWarning("Late note {Key} not found in pool; skipping", key);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogWarning(ex, "Could not read late note {Key}; skipping", key);
			}
		}
		return entries;
	}

	// -----------------------------------------------------------------------------------------
	// Conditional S3 write / delete
	// -----------------------------------------------------------------------------------------

	private async Task<bool> WriteAmendNotesAsync(string key, string newJson, Cancel ctx)
	{
		const int maxAttempts = 5;
		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				// Read current ETag for conditional PUT.
				string? currentETag = null;
				try
				{
					var head = await s3Client.GetObjectMetadataAsync(
						new GetObjectMetadataRequest { BucketName = publicBucketName, Key = key },
						ctx
					);
					currentETag = head.ETag;
				}
				catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound) { }

				// Skip when content is unchanged.
				if (currentETag is not null)
				{
					try
					{
						using var existing = await s3Client.GetObjectAsync(
							new GetObjectRequest { BucketName = publicBucketName, Key = key },
							ctx
						);
						await using var existStream = existing.ResponseStream;
						using var existReader = new StreamReader(existStream);
						var existingJson = await existReader.ReadToEndAsync(ctx);
						if (existingJson == newJson)
						{
							_logger.LogDebug("Amend-notes {Key} is unchanged; skipping write", key);
							return true;
						}
					}
					catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
					{
						currentETag = null;
					}
				}

				var putRequest = new PutObjectRequest
				{
					BucketName = publicBucketName,
					Key = key,
					ContentBody = newJson,
					ContentType = "application/yaml"
				};
				if (currentETag is not null)
					putRequest.IfMatch = currentETag.Trim('"');
				else
					putRequest.IfNoneMatch = "*";

				_ = await s3Client.PutObjectAsync(putRequest, ctx);
				_metrics.IncrementRegistryWrites();
				_logger.LogInformation("Wrote amend-notes sidecar {Key}", key);
				return true;
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode is HttpStatusCode.PreconditionFailed || (int)ex.StatusCode == 409)
			{
				if (attempt >= maxAttempts)
				{
					_logger.LogError("Amend-notes write {Key} failed after {Max} conditional conflicts", key, maxAttempts);
					throw;
				}
				var jitter = TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * 100);
				var delay = (_retryBaseDelay * attempt) + jitter;
				await Task.Delay(delay, ctx);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				if (attempt >= maxAttempts)
					throw;
				await Task.Delay(_retryBaseDelay * attempt, ctx);
				_logger.LogDebug(ex, "Amend-notes write {Key} failed (attempt {A}/{Max}); retrying", key, attempt, maxAttempts);
			}
		}
		return false;
	}

	private async Task<bool> DeleteAmendNotesIfExistsAsync(string key, Cancel ctx)
	{
		try
		{
			var head = await s3Client.GetObjectMetadataAsync(
				new GetObjectMetadataRequest { BucketName = publicBucketName, Key = key },
				ctx
			);
			var etag = head.ETag;

			_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = publicBucketName, Key = key, IfMatch = etag }, ctx);
			_logger.LogInformation("Deleted stale amend-notes sidecar {Key}", key);
			return true;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			_logger.LogDebug("Amend-notes {Key} already absent; treating delete as done so registry rebuild can retry", key);
			return true;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
		{
			_logger.LogDebug("Amend-notes {Key} was updated concurrently; delete skipped", key);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not delete stale amend-notes sidecar {Key}; will retry on next reconcile", key);
			return false;
		}
	}

	// -----------------------------------------------------------------------------------------
	// S3 bundle reading
	// -----------------------------------------------------------------------------------------

	private async Task<Bundle?> TryReadBundleAsync(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest { BucketName = publicBucketName, Key = key }, ctx);
			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var yaml = await reader.ReadToEndAsync(ctx);
			return ReleaseNotesSerialization.DeserializeBundle(yaml);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			_logger.LogDebug("Bundle {Key} not found; skipping", key);
			return null;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read bundle {Key}; skipping", key);
			return null;
		}
	}

	// -----------------------------------------------------------------------------------------
	// Helpers
	// -----------------------------------------------------------------------------------------

	private static string? LeafName(string? path)
	{
		if (string.IsNullOrEmpty(path))
			return null;
		var normalized = path.Replace('\\', '/');
		var slash = normalized.LastIndexOf('/');
		return slash >= 0 ? normalized[(slash + 1)..] : normalized;
	}
}
