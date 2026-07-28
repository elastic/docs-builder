// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Changelog.Uploading;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>How a group reconcile converged.</summary>
public enum GroupReconcileOutcome
{
	/// <summary>No public objects and no manifest — nothing to do.</summary>
	NoOp,

	/// <summary>The existing manifest already describes the listing exactly; no write issued.</summary>
	Unchanged,

	/// <summary>The manifest was (re)written from the public listing.</summary>
	Written,

	/// <summary>The group is empty; its manifest was conditionally deleted.</summary>
	Deleted,

	/// <summary>The existing manifest declares a newer schema than this producer understands; left untouched.</summary>
	RefusedNewerSchema
}

/// <summary>A conditional registry write kept losing races after every retry; the SQS message should be redelivered.</summary>
public sealed class ReconcileConflictException(string message) : Exception(message);

/// <summary>
/// Rebuilds one group's public <c>registry.json</c> from the <em>current public bucket state</em>
/// (<c>registry = f(state)</c>, never <c>f(event)</c> — see elastic/docs-eng-team#688). Lists the
/// group's prefix, reuses entries whose recorded ETag still matches, recomputes the rest from the
/// scrubbed public YAMLs, and writes the manifest back with optimistic concurrency. Any successful
/// reconcile therefore repairs <em>all</em> accumulated drift in the group, not just the change
/// that triggered it.
/// </summary>
public sealed class RegistryReconciler(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string publicBucketName,
	TimeProvider? timeProvider = null,
	TimeSpan? retryBaseDelay = null,
	ReconcileMetrics? metrics = null
)
{
	/// <summary>
	/// Identifies this reconciliation algorithm version in written manifests. Bump on any change to
	/// how entries are computed: a mismatch forces a full recompute (and a write even when the
	/// entries come out identical), which is how metadata-logic fixes roll out to every group.
	/// </summary>
	public const string Producer = "changelog-scrubber-reconcile/1";

	// Bounds the optimistic-concurrency retry loop; each attempt re-lists and re-reads before retrying.
	private const int MaxWriteAttempts = 5;

	// First-heal of a large group GETs every unlisted YAML; keep those reads bounded.
	private const int MaxParallelReads = 4;

	private readonly ILogger _logger = logFactory.CreateLogger<RegistryReconciler>();
	private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
	private readonly TimeSpan _retryBaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(200);
	private readonly ReconcileMetrics _metrics = metrics ?? new ReconcileMetrics();

	/// <summary>
	/// Converges the group's public manifest to its public listing. Throws
	/// <see cref="ReconcileConflictException"/> when concurrent conditional writers win every
	/// bounded retry — the caller fails the SQS message so redelivery retries later.
	/// </summary>
	public async Task<GroupReconcileOutcome> ReconcileGroupAsync(ChangelogScope scope, Cancel ctx)
	{
		_metrics.IncrementGroupReconciles();

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var listing = await ListGroupFiles(scope, ctx);
			var existing = await FetchManifest(scope.RegistryKey, ctx);

			// Empty group: conditionally delete before any equality check, so a stale empty
			// observation cannot destroy a concurrent reconciler's fresh manifest, and a manifest
			// whose bundles are already [] is still removed rather than short-circuited. Absent ≠
			// empty for consumers: deleting restores "unpublished" (404) semantics for the group.
			if (listing.Count == 0)
			{
				if (!existing.Exists)
					return GroupReconcileOutcome.NoOp;

				if (await TryDeleteManifest(scope, existing.ETag!, attempt, ctx))
					return GroupReconcileOutcome.Deleted;
				await BackOff(attempt, ctx);
				continue;
			}

			// Never rewrite (and implicitly downgrade) a manifest produced by a newer schema.
			if (existing.Manifest is { } newer && newer.SchemaVersion > Registry.CurrentSchemaVersion)
			{
				_logger.LogWarning(
					"Public manifest {Key} declares schema_version {Found} > supported {Supported}; leaving it untouched",
					scope.RegistryKey, newer.SchemaVersion, Registry.CurrentSchemaVersion);
				return GroupReconcileOutcome.RefusedNewerSchema;
			}

			// Entries are only reusable — and the write only skippable — when the whole manifest
			// is trustworthy: parsed cleanly and produced by this algorithm for this group. A
			// corrupt manifest or a producer/schema/product mismatch forces a full recompute and
			// a write even when the entries come out identical, otherwise the producer version
			// would never be adopted and every future reconcile would keep recomputing.
			var trusted = existing is { Manifest: not null, Corrupt: false }
				&& existing.Manifest.SchemaVersion == Registry.CurrentSchemaVersion
				&& string.Equals(existing.Manifest.Producer, Producer, StringComparison.Ordinal)
				&& string.Equals(existing.Manifest.Product, scope.Group, StringComparison.Ordinal);

			var (entries, reused) = await BuildEntries(scope, listing, trusted ? existing.Manifest!.Bundles : [], ctx);

			if (trusted && BundlesEqual(existing.Manifest!.Bundles, entries))
			{
				_metrics.IncrementRegistryUnchanged();
				_logger.LogDebug("Public manifest {Key} already matches the listing; skipping write", scope.RegistryKey);
				return GroupReconcileOutcome.Unchanged;
			}

			var manifest = new Registry
			{
				Product = scope.Group,
				Producer = Producer,
				GeneratedAt = _time.GetUtcNow(),
				Bundles = entries
			};
			var json = JsonSerializer.Serialize(manifest, RegistryJsonContext.Default.Registry);

			if (await TryPutManifest(scope, json, existing.ETag, attempt, ctx))
			{
				_metrics.IncrementRegistryWrites();
				_logger.LogInformation(
					"Wrote public manifest {Key} with {Count} entrie(s) ({Reused} reused, {Recomputed} recomputed)",
					scope.RegistryKey, entries.Count, reused, entries.Count - reused);
				return GroupReconcileOutcome.Written;
			}
			await BackOff(attempt, ctx);
		}

		throw new ReconcileConflictException(
			$"Public manifest {scope.RegistryKey} kept changing concurrently after {MaxWriteAttempts} attempts; failing the message for redelivery.");
	}

	/// <summary>
	/// Read-only diagnosis: compares the group's public manifest against what a reconcile of the
	/// current public listing would write — same listing spec, same entry rules, zero writes.
	/// An empty result means a reconcile would be a no-op for this group.
	/// </summary>
	public async Task<IReadOnlyList<RegistryDivergence>> VerifyGroupAsync(ChangelogScope scope, Cancel ctx)
	{
		var listing = await ListGroupFiles(scope, ctx);
		var existing = await FetchManifest(scope.RegistryKey, ctx);
		var divergences = new List<RegistryDivergence>();

		if (listing.Count == 0)
		{
			if (existing.Exists)
			{
				divergences.Add(new RegistryDivergence
				{
					Kind = RegistryDivergenceKind.Stale,
					File = ChangelogKeys.RegistryFileName,
					Detail = "The group holds no objects but its manifest still exists; a reconcile would delete it (absent ≠ empty for consumers)."
				});
			}
			return divergences;
		}

		if (!existing.Exists)
		{
			divergences.Add(new RegistryDivergence
			{
				Kind = RegistryDivergenceKind.Missing,
				File = ChangelogKeys.RegistryFileName,
				Detail = $"The group holds {listing.Count} object(s) but no manifest."
			});
			return divergences;
		}

		if (existing.Corrupt)
		{
			divergences.Add(new RegistryDivergence
			{
				Kind = RegistryDivergenceKind.Corrupt,
				File = ChangelogKeys.RegistryFileName,
				Detail = "The manifest cannot be parsed; a reconcile would rebuild it from the listing."
			});
			return divergences;
		}

		var manifest = existing.Manifest!;
		if (manifest.SchemaVersion > Registry.CurrentSchemaVersion)
		{
			divergences.Add(new RegistryDivergence
			{
				Kind = RegistryDivergenceKind.UnsupportedSchema,
				File = ChangelogKeys.RegistryFileName,
				Detail = $"The manifest declares schema_version {manifest.SchemaVersion} > supported {Registry.CurrentSchemaVersion}; this tool will not touch it."
			});
			return divergences;
		}

		var trusted = manifest.SchemaVersion == Registry.CurrentSchemaVersion
			&& string.Equals(manifest.Producer, Producer, StringComparison.Ordinal)
			&& string.Equals(manifest.Product, scope.Group, StringComparison.Ordinal);
		if (!trusted)
		{
			divergences.Add(new RegistryDivergence
			{
				Kind = RegistryDivergenceKind.Stale,
				File = ChangelogKeys.RegistryFileName,
				Detail = $"Manifest metadata is not this producer's (producer \"{manifest.Producer}\", product \"{manifest.Product}\"); a reconcile would rewrite it."
			});
		}

		var (desired, _) = await BuildEntries(scope, listing, trusted ? manifest.Bundles : [], ctx);
		var desiredByFile = desired.ToDictionary(b => b.File, b => b, StringComparer.Ordinal);
		var manifestByFile = manifest.Bundles.ToDictionary(b => b.File, b => b, StringComparer.Ordinal);

		foreach (var (file, entry) in desiredByFile)
		{
			if (!manifestByFile.TryGetValue(file, out var recorded))
			{
				divergences.Add(new RegistryDivergence
				{
					Kind = RegistryDivergenceKind.Missing,
					File = file,
					Detail = "The object exists in the public bucket but the manifest does not list it."
				});
			}
			else if (!string.Equals(recorded.ETag, entry.ETag, StringComparison.Ordinal)
				|| !string.Equals(recorded.Target, entry.Target, StringComparison.Ordinal))
			{
				divergences.Add(new RegistryDivergence
				{
					Kind = RegistryDivergenceKind.ObjectDivergent,
					File = file,
					Detail = $"The manifest records (target: {recorded.Target ?? "null"}, etag: {recorded.ETag}) but a reconcile would write (target: {entry.Target ?? "null"}, etag: {entry.ETag})."
				});
			}
		}

		foreach (var file in manifestByFile.Keys.Where(f => !desiredByFile.ContainsKey(f)))
		{
			divergences.Add(new RegistryDivergence
			{
				Kind = RegistryDivergenceKind.Stale,
				File = file,
				Detail = "The manifest lists an object that is no longer in the public bucket."
			});
		}

		return divergences;
	}

	private async Task<IReadOnlyList<S3Object>> ListGroupFiles(ChangelogScope scope, Cancel ctx)
	{
		var files = await S3GroupListing.ListImmediateYamlObjectsAsync(s3Client, publicBucketName, scope, ctx);
		for (var i = 0; i < files.Count; i++)
			_metrics.IncrementObjectsListed();
		return files;
	}

	private sealed record ManifestState(Registry? Manifest, string? ETag, bool Exists, bool Corrupt);

	/// <summary>Reads the manifest, distinguishing absent (no ETag) from corrupt (live ETag, no parse).</summary>
	private async Task<ManifestState> FetchManifest(string key, Cancel ctx)
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
			var manifest = await JsonSerializer.DeserializeAsync(stream, RegistryJsonContext.Default.Registry, ctx);
			return new ManifestState(manifest, etag, Exists: true, Corrupt: manifest is null);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return new ManifestState(null, null, Exists: false, Corrupt: false);
		}
		catch (JsonException ex)
		{
			// A corrupt manifest is rebuilt from the listing; its live ETag lets the conditional
			// write replace it safely. Transient S3/IO errors bubble up instead — they must not be
			// mistaken for corruption.
			_logger.LogWarning(ex, "Public manifest {Key} could not be parsed; rebuilding from the listing", key);
			return new ManifestState(null, etag, Exists: true, Corrupt: true);
		}
	}

	private async Task<(List<RegistryBundle> Entries, int Reused)> BuildEntries(
		ChangelogScope scope,
		IReadOnlyList<S3Object> listing,
		IReadOnlyList<RegistryBundle> reusable,
		Cancel ctx)
	{
		// The entry index is listing-only by design (target is null; consumers re-read each entry),
		// so the whole build is zero GETs.
		if (scope.Kind == ChangelogScopeKind.Changelog)
		{
			var entries = Sort(listing.Select(obj => new RegistryBundle
			{
				File = obj.Key[scope.Prefix.Length..],
				Target = null,
				ETag = NormalizeETag(obj.ETag)
			}));
			return (entries, entries.Count);
		}

		var byFile = reusable.ToDictionary(b => b.File, b => b, StringComparer.Ordinal);
		var built = new RegistryBundle?[listing.Count];
		var reused = 0;

		await Parallel.ForEachAsync(
			Enumerable.Range(0, listing.Count),
			new ParallelOptions { MaxDegreeOfParallelism = MaxParallelReads, CancellationToken = ctx },
			async (i, ct) =>
			{
				var obj = listing[i];
				var file = obj.Key[scope.Prefix.Length..];
				var etag = NormalizeETag(obj.ETag);

				// Amends are never ETag-skipped: their target depends on the parent bundle too,
				// and a parent appearing or changing does not touch the amend's own ETag.
				if (!BundleAmendMerger.IsAmendFile(file)
					&& byFile.TryGetValue(file, out var previous)
					&& string.Equals(previous.ETag, etag, StringComparison.Ordinal))
				{
					built[i] = previous;
					_ = Interlocked.Increment(ref reused);
					return;
				}

				var target = await ComputeTarget(scope, file, ct);
				_metrics.IncrementEntriesRecomputed();
				built[i] = new RegistryBundle { File = file, Target = target, ETag = etag };
			});

		// A null slot means the object vanished between the listing and the read; the delete's own
		// event (or the next reconcile) covers it.
		return (Sort(built.Where(b => b is not null)!), reused);
	}

	/// <summary>
	/// Reads the scrubbed public YAML and extracts the target for the group's product — matching
	/// the product id, never blindly the first product. Amends without products inherit the parent
	/// bundle's target; an absent parent yields a null target and a warning, self-correcting once
	/// the parent lands (never a permanent error, which would block the whole group).
	/// </summary>
	private async Task<string?> ComputeTarget(ChangelogScope scope, string file, Cancel ctx)
	{
		var bundle = await TryReadBundle(scope, file, ctx);
		if (bundle is null)
			return null;

		if (bundle.Products.Count > 0)
			return TargetForProduct(bundle, scope.Group);

		var parentFile = BundleAmendMerger.GetParentBundlePath(file);
		if (parentFile is null)
			return null;

		var parent = await TryReadBundle(scope, parentFile, ctx);
		if (parent is null || parent.Products.Count == 0)
		{
			_logger.LogWarning(
				"Amend {Prefix}{File} has no parent bundle {Parent} in the public bucket yet; recording a null target",
				scope.Prefix, file, parentFile);
			return null;
		}

		return TargetForProduct(parent, scope.Group);
	}

	private async Task<Bundle?> TryReadBundle(ChangelogScope scope, string file, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = publicBucketName,
				Key = scope.Prefix + file
			}, ctx);

			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var content = await reader.ReadToEndAsync(ctx);
			return ReleaseNotesSerialization.DeserializeBundle(content);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read bundle target from {Prefix}{File}; recording a null target", scope.Prefix, file);
			return null;
		}
	}

	private static string? TargetForProduct(Bundle bundle, string product)
	{
		var match = bundle.Products.FirstOrDefault(p => string.Equals(p.ProductId, product, StringComparison.Ordinal));
		return (match ?? bundle.Products[0]).Target;
	}

	private async Task<bool> TryDeleteManifest(ChangelogScope scope, string etag, int attempt, Cancel ctx)
	{
		try
		{
			_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest
			{
				BucketName = publicBucketName,
				Key = scope.RegistryKey,
				IfMatch = etag
			}, ctx);
			_metrics.IncrementRegistryDeletes();
			_logger.LogInformation("Deleted public manifest {Key}: the group is empty", scope.RegistryKey);
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
				"Public manifest {Key} changed concurrently during delete (attempt {Attempt}/{Max}); re-listing and retrying",
				scope.RegistryKey, attempt, MaxWriteAttempts);
			return false;
		}
	}

	private async Task<bool> TryPutManifest(ChangelogScope scope, string json, string? etag, int attempt, Cancel ctx)
	{
		var request = new PutObjectRequest
		{
			BucketName = publicBucketName,
			Key = scope.RegistryKey,
			ContentBody = json,
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
			return true;
		}
		catch (AmazonS3Exception ex) when (IsConditionalWriteConflict(ex))
		{
			_metrics.IncrementWriteConflicts();
			_logger.LogInformation(
				"Public manifest {Key} changed concurrently (attempt {Attempt}/{Max}); re-listing and retrying",
				scope.RegistryKey, attempt, MaxWriteAttempts);
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

	private static List<RegistryBundle> Sort(IEnumerable<RegistryBundle> entries) =>
		[.. entries
			.OrderByDescending(b => VersionOrDate.Parse(b.Target ?? string.Empty))
			.ThenBy(b => b.File, StringComparer.Ordinal)];

	private static string NormalizeETag(string? etag) => etag?.Trim('"') ?? string.Empty;

	private static bool BundlesEqual(IReadOnlyList<RegistryBundle> a, IReadOnlyList<RegistryBundle> b)
	{
		if (a.Count != b.Count)
			return false;

		for (var i = 0; i < a.Count; i++)
		{
			if (!string.Equals(a[i].File, b[i].File, StringComparison.Ordinal) ||
				!string.Equals(a[i].Target, b[i].Target, StringComparison.Ordinal) ||
				!string.Equals(a[i].ETag, b[i].ETag, StringComparison.Ordinal))
				return false;
		}

		return true;
	}
}
