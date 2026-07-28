// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Elastic.Changelog.Reconciliation;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Scrubbing;

/// <summary>One SQS message as seen by the scrubber: its receipt identity and raw body.</summary>
public sealed record ScrubberQueueMessage(string MessageId, string Body);

/// <summary>
/// The scrubber Lambda's event processor (elastic/docs-eng-team#688), extracted from
/// <c>Program.cs</c> so it is testable. Events are triggers, state decides: the handler never
/// acts on an event's <em>type</em> — an event means only "this key may have changed, look at
/// it". Every distinct key gets one object-level reconcile against the private bucket, then every
/// distinct group gets one registry reconcile against the public listing, so out-of-order and
/// at-least-once S3 notifications are harmless and each batch heals accumulated drift.
/// </summary>
public sealed class ScrubberProcessor(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	string publicBucketName,
	IChangelogContentScrubber scrubber,
	RegistryReconciler reconciler,
	ReconcileMetrics? metrics = null
)
{
	// Bounds the reread-and-redo loop of post-write source validation. Each redo only triggers
	// when the private object changed mid-flight, which itself queued another event; converging
	// here is an optimization, not a correctness requirement.
	private const int MaxObjectAttempts = 3;

	private readonly ILogger _logger = logFactory.CreateLogger<ScrubberProcessor>();
	private readonly ReconcileMetrics _metrics = metrics ?? new ReconcileMetrics();

	private sealed class ObjectWork(string sourceBucket)
	{
		public string SourceBucket { get; set; } = sourceBucket;
		public HashSet<string> MessageIds { get; } = [with(StringComparer.Ordinal)];
	}

	private sealed class GroupWork(ChangelogScope scope)
	{
		public ChangelogScope Scope { get; } = scope;
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
					Classify(message.MessageId, record.S3.Bucket.Name, key, objectWork, groupWork);
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
				await ReconcileObjectAsync(work.SourceBucket, key, ctx);
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

		_metrics.AddFailedMessages(failedIds.Count);
		return [.. failedIds];
	}

	private void Classify(
		string messageId,
		string sourceBucket,
		string key,
		Dictionary<string, ObjectWork> objectWork,
		Dictionary<string, GroupWork> groupWork)
	{
		var hasScope = ChangelogScope.TryFromKey(key, out var scope);

		// The registry pass-through is retired: registry keys are never copied or deleted. Old CLI
		// versions still write private manifests (and Phase 3's cleanup will delete them) — those
		// events only schedule a reconcile of the group, which derives the public manifest from
		// public state.
		if (ChangelogKeys.IsRegistry(key))
		{
			if (hasScope)
				AddGroup(groupWork, scope!, messageId);
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

		if (!objectWork.TryGetValue(key, out var work))
		{
			work = new ObjectWork(sourceBucket);
			objectWork[key] = work;
		}
		work.SourceBucket = sourceBucket;
		_ = work.MessageIds.Add(messageId);

		if (hasScope)
			AddGroup(groupWork, scope!, messageId);
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

	/// <summary>
	/// Order-independent object reconcile: the event type is ignored; the private bucket's current
	/// state decides between scrub-and-copy and delete. A stale <c>ObjectRemoved</c> arriving
	/// after a recreate re-copies the live object instead of deleting it.
	/// </summary>
	private async Task ReconcileObjectAsync(string sourceBucket, string key, Cancel ctx)
	{
		_metrics.IncrementObjectReconciles();

		for (var attempt = 1; attempt <= MaxObjectAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var snapshot = await TryGetPrivateObject(sourceBucket, key, ctx);
			if (snapshot is { } source)
			{
				var scrubbed = await scrubber.ScrubAsync(key, source.Content, ctx);
				await PutPublicObject(key, scrubbed, ctx);
				_logger.LogInformation("Scrubbed and wrote {Key} to public bucket", key);
			}
			else
			{
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

	private async Task PutPublicObject(string key, string content, Cancel ctx) =>
		_ = await s3Client.PutObjectAsync(new PutObjectRequest
		{
			BucketName = publicBucketName,
			Key = key,
			ContentBody = content,
			ContentType = "application/yaml"
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
