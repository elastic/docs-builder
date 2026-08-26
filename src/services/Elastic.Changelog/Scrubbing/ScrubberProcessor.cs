// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Elastic.Changelog.Reconciliation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Scrubbing;

/// <summary>One SQS message as seen by the scrubber: its receipt identity and raw body.</summary>
public sealed record ScrubberQueueMessage(string MessageId, string Body);

/// <summary>
/// The scrubber Lambda's event processor (elastic/docs-eng-team#688), extracted from
/// <c>Program.cs</c> so it is testable. Events are triggers, state decides: the handler never
/// acts on an event's <em>type</em> — an event means only "this key may have changed, look at
/// it". Every distinct key gets one object-level reconcile against the private bucket; every
/// distinct <c>bundle/{product}/</c> group then gets one registry reconcile against the public
/// listing, and every touched tree gets one shallow-map reconcile. Out-of-order and
/// at-least-once S3 notifications are harmless and each batch heals accumulated drift.
/// </summary>
public sealed class ScrubberProcessor(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string publicBucketName,
	IChangelogContentScrubber scrubber,
	BundleRegistryReconciler reconciler,
	ShallowRegistryReconciler shallowReconciler,
	NotesIndexReconciler notesReconciler,
	ReconcileMetrics? metrics = null
)
{
	// Bounds the reread-and-redo loop of post-write source validation. Each redo only triggers
	// when the private object changed mid-flight, which itself queued another event; converging
	// here is an optimization, not a correctness requirement.
	private const int MaxObjectAttempts = 3;

	private readonly ILogger _logger = logFactory.CreateLogger<ScrubberProcessor>();
	private readonly ReconcileMetrics _metrics = metrics ?? new ReconcileMetrics();

	private sealed class ObjectWork(string sourceBucket, bool passThrough)
	{
		public string SourceBucket { get; set; } = sourceBucket;

		/// <summary>True for a pool manifest copied verbatim; false for YAML content that is scrubbed.</summary>
		public bool PassThrough { get; } = passThrough;

		public HashSet<string> MessageIds { get; } = [with(StringComparer.Ordinal)];
	}

	private sealed class GroupWork(ChangelogScope scope)
	{
		public ChangelogScope Scope { get; } = scope;
		public HashSet<string> MessageIds { get; } = [with(StringComparer.Ordinal)];
	}

	private sealed class ShallowWork(ChangelogScopeKind kind)
	{
		public ChangelogScopeKind Kind { get; } = kind;
		public Dictionary<string, ChangelogScope> Scopes { get; } = [with(StringComparer.Ordinal)];
		public HashSet<string> MessageIds { get; } = [with(StringComparer.Ordinal)];
	}

	/// <summary>
	/// Processes one SQS batch and returns the message ids that must be redelivered. Work is
	/// coalesced per distinct key and per distinct group; a failed object reconcile fails every
	/// message that referenced its key, a failed group reconcile fails every message that
	/// contributed to that group.
	/// </summary>
	public async Task<IReadOnlyList<string>> ProcessAsync(IReadOnlyList<ScrubberQueueMessage> messages, Cancel ctx)
	{
		var objectWork = new Dictionary<string, ObjectWork>(StringComparer.Ordinal);
		var groupWork = new Dictionary<string, GroupWork>(StringComparer.Ordinal);
		var notesWork = new Dictionary<string, GroupWork>(StringComparer.Ordinal);
		var shallowWork = new Dictionary<ChangelogScopeKind, ShallowWork>();
		var failedIds = new HashSet<string>(StringComparer.Ordinal);

		foreach (var message in messages)
		{
			try
			{
				var s3Event = S3EventNotification.ParseJson(message.Body);
				foreach (var record in s3Event.Records ?? [])
				{
					var key = Uri.UnescapeDataString(record.S3.Object.Key.Replace('+', ' '));
					_logger.LogInformation("Batch names key={Key} (event={EventName})", key, record.EventName?.Value);
					Classify(message.MessageId, record.S3.Bucket.Name, key, objectWork, groupWork, notesWork, shallowWork);
				}
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				_logger.LogWarning(e, "Failed to parse message {MessageId}", message.MessageId);
				_ = failedIds.Add(message.MessageId);
			}
		}

		foreach (var (key, work) in objectWork)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				await ReconcileObjectAsync(work.SourceBucket, key, work.PassThrough, ctx);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				_logger.LogError(e, "Object reconcile for {Key} failed; failing its {Count} message(s)", key, work.MessageIds.Count);
				failedIds.UnionWith(work.MessageIds);
			}
		}

		foreach (var work in groupWork.Values)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				_ = await reconciler.ReconcileGroupAsync(work.Scope, ctx);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				_logger.LogError(e, "Group reconcile for {Scope} failed; failing its {Count} contributing message(s)", work.Scope, work.MessageIds.Count);
				failedIds.UnionWith(work.MessageIds);
			}
		}

		foreach (var work in notesWork.Values)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				await notesReconciler.ReconcileRepoAsync(work.Scope, ctx);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				_logger.LogError(e, "Notes reconcile for {Scope} failed; failing its {Count} contributing message(s)", work.Scope, work.MessageIds.Count);
				failedIds.UnionWith(work.MessageIds);
			}
		}

		foreach (var work in shallowWork.Values)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				await shallowReconciler.ReconcileAsync(work.Kind, work.Scopes.Values, ctx);
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				_logger.LogError(e, "Shallow map reconcile for the {Kind} tree failed; failing its {Count} contributing message(s)", work.Kind, work.MessageIds.Count);
				failedIds.UnionWith(work.MessageIds);
			}
		}

		_metrics.AddFailedMessages(failedIds.Count);
		return [.. failedIds];
	}

	private void Classify(
		string messageId,
		string sourceBucket,
		string key,
		Dictionary<string, ObjectWork> objectWork,
		Dictionary<string, GroupWork> groupWork,
		Dictionary<string, GroupWork> notesWork,
		Dictionary<ChangelogScopeKind, ShallowWork> shallowWork)
	{
		var hasScope = ChangelogScope.TryFromKey(key, out var scope);

		if (ChangelogKeys.IsRegistry(key))
		{
			if (!hasScope)
				return;

			// Bundle manifests are reconciler-owned: the event schedules a group reconcile so
			// client-authored JSON never reaches the public bucket directly. Pool registry keys
			// (changelog/{org}/{repo}/{branch}/registry.json) are no longer written by any client
			// — the pool index was retired in #3760. Drop them with a debug log; a stale event
			// from an old client is harmless and does not need to copy anything.
			if (scope!.Kind == ChangelogScopeKind.Bundle)
				AddGroup(groupWork, scope, messageId);
			else
				_logger.LogDebug("Ignoring retired pool registry key: {Key}", key);
			return;
		}

		// Notes indexes (notes-{target}.json) are reconciler-owned; a client that uploads one is
		// rejected here — the reconciler writes directly to the public bucket, so no copy is needed.
		if (ChangelogKeys.IsNotesIndex(key))
		{
			_logger.LogWarning("Rejecting client-uploaded notes index {Key}; notes indexes are reconciler-owned", key);
			return;
		}

		if (key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogWarning("Skipping unapproved JSON key: {Key}", key);
			return;
		}

		if (!key.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) &&
			!key.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("Skipping non-YAML key: {Key}", key);
			return;
		}

		AddObject(objectWork, key, sourceBucket, messageId, passThrough: false);

		if (!hasScope)
			return;

		if (scope!.Kind == ChangelogScopeKind.Bundle)
			AddGroup(groupWork, scope, messageId);

		// A note-*.yml upload triggers a notes-index reconcile for the whole repo — all targets
		// whose index lists this note must be rebuilt. The Changelog scope's group is {org}/{repo}/{branch};
		// the notes scope is the two-segment {org}/{repo} prefix.
		if (scope.Kind == ChangelogScopeKind.Changelog)
		{
			var fileName = key[scope.Prefix.Length..];
			if (IsNoteFileName(fileName))
				AddNotesGroup(notesWork, scope.Group, messageId);
		}

		AddShallow(shallowWork, scope, messageId);
	}

	private static bool IsNoteFileName(string fileName) =>
		fileName.StartsWith("note-", StringComparison.OrdinalIgnoreCase)
		&& (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
		&& !fileName.Contains('/', StringComparison.Ordinal);

	private static void AddObject(
		Dictionary<string, ObjectWork> objectWork,
		string key,
		string sourceBucket,
		string messageId,
		bool passThrough)
	{
		if (!objectWork.TryGetValue(key, out var work))
		{
			work = new ObjectWork(sourceBucket, passThrough);
			objectWork[key] = work;
		}
		work.SourceBucket = sourceBucket;
		_ = work.MessageIds.Add(messageId);
	}

	private static void AddGroup(Dictionary<string, GroupWork> groupWork, ChangelogScope scope, string messageId)
	{
		if (!groupWork.TryGetValue(scope.Prefix, out var work))
		{
			work = new GroupWork(scope);
			groupWork[scope.Prefix] = work;
		}
		_ = work.MessageIds.Add(messageId);
	}

	private static void AddNotesGroup(Dictionary<string, GroupWork> notesWork, string changelogGroup, string messageId)
	{
		// changelogGroup is {org}/{repo}/{branch...}; extract {org}/{repo} by taking the first two segments.
		var parts = changelogGroup.Split('/');
		if (parts.Length < 2)
			return;
		var org = parts[0];
		var repo = parts[1];
		if (!ChangelogScope.TryCreateNotes(org, repo, out var notesScope))
			return;
		if (!notesWork.TryGetValue(notesScope.Prefix, out var work))
		{
			work = new GroupWork(notesScope);
			notesWork[notesScope.Prefix] = work;
		}
		_ = work.MessageIds.Add(messageId);
	}

	private static void AddShallow(Dictionary<ChangelogScopeKind, ShallowWork> shallowWork, ChangelogScope scope, string messageId)
	{
		if (!shallowWork.TryGetValue(scope.Kind, out var work))
		{
			work = new ShallowWork(scope.Kind);
			shallowWork[scope.Kind] = work;
		}
		work.Scopes[scope.Group] = scope;
		_ = work.MessageIds.Add(messageId);
	}

	/// <summary>
	/// Order-independent object reconcile: the event type is ignored; the private bucket's current
	/// state decides between copy and delete. A stale <c>ObjectRemoved</c> arriving after a
	/// recreate re-copies the live object instead of deleting it. YAML content is scrubbed on the
	/// way through; a pass-through pool manifest is copied verbatim.
	/// </summary>
	private async Task ReconcileObjectAsync(string sourceBucket, string key, bool passThrough, Cancel ctx)
	{
		_metrics.IncrementObjectReconciles();

		for (var attempt = 1; attempt <= MaxObjectAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var snapshot = await TryGetPrivateObject(sourceBucket, key, ctx);
			if (snapshot is { } source)
			{
				if (passThrough)
				{
					await PutPublicObject(key, source.Content, "application/json", ctx);
					_logger.LogInformation("Copied {Key} to public bucket (pass-through)", key);
				}
				else
				{
					var scrubResult = await scrubber.ScrubAsync(key, source.Content, ctx);
					var publicKey = scrubResult.CanonicalKey ?? key;

					// Read the current public entry before overwriting so we can derive which
					// marker objects it previously produced and delete any that are no longer needed.
					var oldPublicContent = await TryGetPublicObject(publicKey, ctx);

					// Issue 1 guard: a private marker derived from raw (pre-allowlist) PRs can race
					// with the scrubber writing canonical public content at the same key. If canonical
					// content already occupies the public key, skip the marker write so it cannot
					// overwrite the canonical entry that the primary object's scrub already produced.
					// A null return from TryDeserializeEntry (unparseable content) is treated as
					// canonical — the safest assumption when we cannot classify the existing object.
					var skipWrite = scrubResult.IsMarker
						&& oldPublicContent is not null
						&& TryDeserializeEntry(oldPublicContent)?.IsMarker != true;

					if (skipWrite)
					{
						_logger.LogInformation(
							"Skipped pass-through marker {Key}: canonical content in public bucket takes precedence", key);
					}
					else
					{
						await PutPublicObject(publicKey, scrubResult.Content, "application/yaml", ctx);
						if (scrubResult.CanonicalKey is not null)
						{
							_logger.LogInformation("Scrubbed {Key} → canonical public key {CanonicalKey}", key, scrubResult.CanonicalKey);
							// Issue 2: write a source pointer at the source key in the public bucket so
							// that a delete event for 'key' can trace back to the canonical key and clean
							// it up (see delete path below). The pointer uses the same link: format as
							// secondary-PR markers; the non-canonical filename makes it distinguishable.
							await WriteSourcePointerAsync(key, scrubResult.CanonicalKey, ctx);
						}
						else
							_logger.LogInformation("Scrubbed and wrote {Key} to public bucket", key);

						foreach (var (markerKey, markerContent) in scrubResult.Markers)
						{
							await PutPublicObject(markerKey, markerContent, "application/yaml", ctx);
							_logger.LogInformation("Wrote marker {MarkerKey} → {PrimaryKey}", markerKey, publicKey);
						}

						// Remove marker objects that existed in the previous scrub result but are no
						// longer produced (e.g. an entry shrank from 3 PRs to 1).
						await DeleteStaleMarkersAsync(publicKey, oldPublicContent, scrubResult.Markers, ctx);
					}
				}
			}
			else
			{
				// Issue 2: the private object is gone. Read the public bucket to discover what
				// was previously written for this source key so we can clean it up completely.
				//
				// Two sub-cases:
				// (a) Source key was non-canonical: the scrubber wrote canonical content to a
				//     different public key and left a source pointer (link: <pr>) at 'key'. Read
				//     the pointer, derive the canonical key, delete it and all its markers, then
				//     delete the pointer. Non-canonical source keys always have a non-integer
				//     filename (e.g. "12345-fix.yaml"), which differentiates them from PR markers.
				// (b) Source key was canonical or a secondary PR marker: delete its markers (if
				//     any), then delete the key itself.
				var publicContent = await TryGetPublicObject(key, ctx);
				if (publicContent is not null && !IsNumericYamlKey(key))
				{
					// Non-canonical source key — check for a scrubber-written source pointer.
					// Only entries with SourceRedirect=true are source pointers; a plain link: field
					// alone is an ordinary PR marker that must not trigger canonical deletion.
					var entry = TryDeserializeEntry(publicContent);
					if (entry?.SourceRedirect == true)
					{
						var lastSlash = key.LastIndexOf('/');
						var keyPrefix = lastSlash >= 0 ? key[..(lastSlash + 1)] : string.Empty;
						var canonicalKey = $"{keyPrefix}{entry.Link}.yaml";
						var canonicalContent = await TryGetPublicObject(canonicalKey, ctx);
						await DeleteStaleMarkersAsync(canonicalKey, canonicalContent, [], ctx);
						await DeletePublicObject(canonicalKey, ctx);
						_logger.LogInformation(
							"Source pointer {Key} traced to canonical {CanonicalKey}; deleted canonical and its markers",
							key, canonicalKey);
					}
					else
					{
						// Non-canonical key but not a pointer (e.g. a note-* file).
						await DeleteStaleMarkersAsync(key, publicContent, [], ctx);
					}
				}
				else if (publicContent is not null)
				{
					// Numeric key: canonical entry or secondary-PR marker. Either way, clean up any
					// markers the canonical may have emitted before deleting the entry itself.
					await DeleteStaleMarkersAsync(key, publicContent, [], ctx);
				}

				await DeletePublicObject(key, ctx);
				_logger.LogInformation("Private {Key} is gone; removed its public copy", key);
			}

			// Post-write source validation: sequential out-of-order events are handled by the
			// state read above, but two concurrent invocations can interleave so the older read
			// publishes last. Confirm the private object still matches the snapshot this write
			// was derived from; any change landing after this check has its own S3 event, so the
			// combination converges.
			var currentETag = await TryHeadPrivateObject(sourceBucket, key, ctx);
			var stillCurrent = snapshot is null
				? currentETag is null
				: string.Equals(currentETag, snapshot.Value.ETag, StringComparison.Ordinal);
			if (stillCurrent)
				return;

			_metrics.IncrementObjectReconcileRetries();
			_logger.LogInformation(
				"Private {Key} changed while its reconcile was in flight (attempt {Attempt}/{Max}); redoing from current state",
				key, attempt, MaxObjectAttempts);
		}

		throw new InvalidOperationException(
			$"Private {key} kept changing during {MaxObjectAttempts} reconcile attempts; failing the message for redelivery.");
	}

	private async Task<(string Content, string ETag)?> TryGetPrivateObject(string sourceBucket, string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = sourceBucket,
				Key = key
			}, ctx);

			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var content = await reader.ReadToEndAsync(ctx);
			return (content, NormalizeETag(response.ETag));
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private async Task<string?> TryHeadPrivateObject(string sourceBucket, string key, Cancel ctx)
	{
		try
		{
			var response = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
			{
				BucketName = sourceBucket,
				Key = key
			}, ctx);
			return NormalizeETag(response.ETag);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private static ChangelogEntry? TryDeserializeEntry(string? content)
	{
		if (content is null)
			return null;
		try
		{ return ReleaseNotesSerialization.DeserializeEntry(content); }
		catch { return null; }
	}

	private static bool IsNumericYamlKey(string key)
	{
		var lastSlash = key.LastIndexOf('/');
		var fileName = lastSlash >= 0 ? key[(lastSlash + 1)..] : key;
		// Only .yaml (not .yml) files are written as canonical PR keys by the pipeline.
		// A .yml source file is always non-canonical; treat it as needing pointer tracing.
		if (!fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
			return false;
		var stem = Path.GetFileNameWithoutExtension(fileName);
		return int.TryParse(stem, NumberStyles.None, CultureInfo.InvariantCulture, out _);
	}

	private async Task WriteSourcePointerAsync(string sourceKey, string canonicalKey, Cancel ctx)
	{
		var lastSlash = canonicalKey.LastIndexOf('/');
		var canonicalFileName = lastSlash >= 0 ? canonicalKey[(lastSlash + 1)..] : canonicalKey;
		var stem = Path.GetFileNameWithoutExtension(canonicalFileName);
		if (!int.TryParse(stem, NumberStyles.None, CultureInfo.InvariantCulture, out _))
			return; // Not a PR-based canonical key; nothing to point to.
		var pointerContent = ReleaseNotesSerialization.SerializeEntry(new ChangelogEntry { Link = stem, SourceRedirect = true });
		await PutPublicObject(sourceKey, pointerContent, "application/yaml", ctx);
		_logger.LogInformation("Wrote source pointer {SourceKey} → canonical {CanonicalKey}", sourceKey, canonicalKey);
	}


	private async Task<string?> TryGetPublicObject(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = publicBucketName,
				Key = key
			}, ctx);
			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			return await reader.ReadToEndAsync(ctx);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private async Task DeleteStaleMarkersAsync(
		string publicKey,
		string? oldPublicContent,
		IReadOnlyList<(string Key, string Content)> newMarkers,
		Cancel ctx)
	{
		if (oldPublicContent is null)
			return;

		IReadOnlyList<string> oldMarkerKeys;
		try
		{
			oldMarkerKeys = DeriveMarkerKeys(publicKey, oldPublicContent);
		}
		catch
		{
			// If the old content can't be parsed, we can't derive old markers — skip cleanup.
			return;
		}

		if (oldMarkerKeys.Count == 0)
			return;

		var newMarkerKeySet = newMarkers.Select(m => m.Key).ToHashSet(StringComparer.Ordinal);
		foreach (var staleKey in oldMarkerKeys)
		{
			if (newMarkerKeySet.Contains(staleKey))
				continue;
			await DeletePublicObject(staleKey, ctx);
			_logger.LogInformation("Deleted stale marker {StaleKey} (no longer referenced by {PrimaryKey})", staleKey, publicKey);
		}
	}

	private static IReadOnlyList<string> DeriveMarkerKeys(string publicKey, string content)
	{
		var entry = ReleaseNotesSerialization.DeserializeEntry(content);
		if (entry.IsMarker || entry.Prs is not { Count: > 0 })
			return [];

		var lastSlash = publicKey.LastIndexOf('/');
		if (lastSlash < 0)
			return [];
		var keyPrefix = publicKey[..(lastSlash + 1)];

		var prNumbers = entry.Prs
			.Select(pr => ChangelogTextUtilities.ExtractPrNumber(pr))
			.Where(n => n.HasValue)
			.Select(n => n!.Value)
			.Distinct()
			.OrderBy(n => n)
			.ToList();

		if (prNumbers.Count <= 1)
			return [];

		var primaryPr = prNumbers[0];
		return prNumbers
			.Skip(1)
			.Select(pr => $"{keyPrefix}{pr}.yaml")
			.Where(k => !string.Equals(k, $"{keyPrefix}{primaryPr}.yaml", StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	private async Task PutPublicObject(string key, string content, string contentType, Cancel ctx) =>
		_ = await s3Client.PutObjectAsync(new PutObjectRequest
		{
			BucketName = publicBucketName,
			Key = key,
			ContentBody = content,
			ContentType = contentType
		}, ctx);

	private async Task DeletePublicObject(string key, Cancel ctx)
	{
		try
		{
			_ = await s3Client.DeleteObjectAsync(new DeleteObjectRequest
			{
				BucketName = publicBucketName,
				Key = key
			}, ctx);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			// Already absent; converged.
		}
	}

	private static string NormalizeETag(string? etag) => etag?.Trim('"') ?? string.Empty;
}
