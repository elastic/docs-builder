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
/// Compares each note in the per-version notes indexes against published bundles for the same
/// version, and either creates or deletes a reconciler-owned amend sidecar
/// (<c>{parent}.amend-notes.yaml</c>) that carries notes that arrived after the release shipped.
/// Also updates each <c>bundle_seq</c> in the notes index: 0 = no bundle yet, 1 = shipped in the
/// original bundle or a human amend, 2 = carried by the reconciler amend sidecar.
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
	/// For the given repository scope, scans every product's bundle registry to determine which
	/// notes have shipped and which are late, writes or deletes the reconciler-owned amend sidecars,
	/// and re-writes the notes indexes with correct <c>bundle_seq</c> values.
	/// </summary>
	/// <param name="notesScope">The notes scope for this repo.</param>
	/// <param name="notesByVersion">Output of <see cref="NotesIndexReconciler.ReconcileRepoAsync"/>.</param>
	/// <param name="ctx">Cancellation token.</param>
	public async Task ReconcileAsync(
		ChangelogScope notesScope,
		IReadOnlyDictionary<string, IReadOnlyList<NoteIndexEntry>> notesByVersion,
		Cancel ctx
	)
	{
		if (notesByVersion.Count == 0)
			return;

		var groupParts = notesScope.Group.Split('/');
		var (org, repo) = (groupParts[0], groupParts[1]);

		// Track bundle_seq for each (version → path → seq).  Default 0 = unreleased.
		var seqMap = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
		foreach (var (version, notes) in notesByVersion)
			seqMap[version] = notes.ToDictionary(n => n.Path, _ => 0, StringComparer.Ordinal);

		// List all product names from the bundle tree.
		var products = await ListBundleProductsAsync(ctx);
		_logger.LogDebug("NoteAmendReconciler: scanning {Count} bundle product(s) for repo {Org}/{Repo}", products.Count, org, repo);

		foreach (var product in products)
		{
			ctx.ThrowIfCancellationRequested();
			await ProcessProductAsync(org, repo, product, notesByVersion, seqMap, ctx);
		}

		// Re-write notes indexes with the updated bundle_seq values.
		await Parallel.ForEachAsync(notesByVersion, new ParallelOptions
		{
			MaxDegreeOfParallelism = MaxParallelWrites,
			CancellationToken = ctx
		}, async (kvp, ct) =>
		{
			var (version, notes) = kvp;
			var seqs = seqMap[version];
			var updatedEntries = notes
				.Select(n => n with { BundleSeq = seqs.TryGetValue(n.Path, out var s) ? s : 0 })
				.OrderBy(n => n.Path, StringComparer.Ordinal)
				.ToList<NoteIndexEntry>();
			var indexKey = ChangelogKeys.NotesIndexKey(org, repo, version);
			await notesIndexReconciler.WriteIndexAsync(indexKey, updatedEntries, ct);
		});
	}

	// -----------------------------------------------------------------------------------------
	// Product scanning
	// -----------------------------------------------------------------------------------------

	private async Task<IReadOnlyList<string>> ListBundleProductsAsync(Cancel ctx)
	{
		var products = new List<string>();
		var request = new ListObjectsV2Request { BucketName = publicBucketName, Prefix = ChangelogKeys.BundlePrefix, Delimiter = "/" };

		ListObjectsV2Response response;
		do
		{
			response = await s3Client.ListObjectsV2Async(request, ctx);
			foreach (var prefix in response.CommonPrefixes ?? [])
			{
				// CommonPrefix is like "bundle/elasticsearch/" — strip the outer segments.
				var inner = prefix[ChangelogKeys.BundlePrefix.Length..];
				var product = inner.TrimEnd('/');
				if (!string.IsNullOrEmpty(product))
					products.Add(product);
			}
			request.ContinuationToken = response.NextContinuationToken;
		}
		while (response.IsTruncated == true);

		return products;
	}

	private async Task ProcessProductAsync(
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
			return;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read bundle registry for product {Product}; skipping", product);
			return;
		}

		if (registry is null || registry.Bundles.Count == 0)
			return;

		// For each version that has notes, look for a matching parent bundle.
		foreach (var (version, notes) in notesByVersion)
		{
			ctx.ThrowIfCancellationRequested();

			// Parent bundle: not an amend file, target matches the version.
			var parentBundle = registry.Bundles.FirstOrDefault(
				b => !string.IsNullOrEmpty(b.File) && !BundleAmendMerger.IsAmendFile(b.File) && ChangelogVersionMatch.Matches(
					version,
					b.Target,
					b.File
				)
			);

			if (parentBundle is null)
				continue; // No bundle yet → every note stays at bundle_seq 0.

			await ProcessVersionBundleAsync(org, repo, product, parentBundle, registry, version, notes, seqMap[version], ctx);
		}
	}

	private async Task ProcessVersionBundleAsync(
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
			return;

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
			return;
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
				await WriteAmendNotesAsync(amendNotesKey, newJson, ctx);

				foreach (var note in lateNotes)
					seqByPath[note.Path] = 2; // carried by the reconciler amend
			}
		}
		else
		{
			// All notes are shipped — delete the sidecar if it exists.
			await DeleteAmendNotesIfExistsAsync(amendNotesKey, ctx);
		}
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

	private async Task WriteAmendNotesAsync(string key, string newJson, Cancel ctx)
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
							return;
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
				return;
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
	}

	private async Task DeleteAmendNotesIfExistsAsync(string key, Cancel ctx)
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
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			// Nothing to delete — this is the expected steady state when all notes are shipped.
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
		{
			// Another reconciler deleted or replaced it concurrently — safe to ignore.
			_logger.LogDebug("Amend-notes {Key} was updated concurrently; delete skipped", key);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not delete stale amend-notes sidecar {Key}; will retry on next reconcile", key);
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
