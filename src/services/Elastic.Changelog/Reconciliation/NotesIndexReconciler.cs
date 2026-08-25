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
/// Rebuilds the per-target <c>notes-{target}.json</c> indexes for one repository by listing
/// all <c>note-*.yml</c> objects under <c>changelog/{org}/{repo}/</c>, reading each to extract
/// its <c>target:</c> values, and writing the affected indexes atomically.
/// </summary>
/// <remarks>
/// A note may list products at multiple targets, so one note can appear in several indexes.
/// The index stores pool-relative paths (<c>{branch}/note-{name}.yml</c>) so the same file
/// name on two branches yields two distinct entries in the same index.
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
	/// Rebuilds all <c>notes-{target}.json</c> indexes for the given repository scope.
	/// All currently published <c>note-*.yml</c> files across every branch are listed and
	/// read to derive the target grouping; every affected index is then (re)written.
	/// </summary>
	public async Task ReconcileRepoAsync(ChangelogScope notesScope, Cancel ctx)
	{
		if (notesScope.Kind != ChangelogScopeKind.Notes)
			throw new ArgumentException($"Notes reconcile requires a Notes scope; got '{notesScope}'.", nameof(notesScope));

		_logger.LogInformation("Reconciling notes indexes for repo {Repo}", notesScope.Group);

		// List every note-*.yml under changelog/{org}/{repo}/ (all branches).
		var noteObjects = await ListNoteFiles(notesScope, ctx);
		_logger.LogDebug("Found {Count} note file(s) for {Repo}", noteObjects.Count, notesScope.Group);

		// Read each note to extract its targets.
		// Using List<string> per target; duplicates are removed at write time via Distinct().
		var byTarget = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		foreach (var obj in noteObjects)
		{
			ctx.ThrowIfCancellationRequested();
			var poolRelativePath = obj.Key[notesScope.Prefix.Length..];
			var targets = await ExtractTargetsAsync(obj.Key, ctx);
			foreach (var target in targets)
			{
				if (!byTarget.TryGetValue(target, out var paths))
					byTarget[target] = paths = [];
				paths.Add(poolRelativePath);
			}
		}

		if (byTarget.Count == 0)
		{
			_logger.LogDebug("No targets found for repo {Repo}; no indexes written", notesScope.Group);
			return;
		}

		// Write one index per target.
		await Parallel.ForEachAsync(
			byTarget,
			new ParallelOptions { MaxDegreeOfParallelism = MaxParallelReads, CancellationToken = ctx },
			async (kvp, ct) =>
			{
				var (target, paths) = kvp;
				var indexKey = ChangelogKeys.NotesIndexKey(
					notesScope.Group.Split('/')[0],
					notesScope.Group.Split('/')[1],
					target);
				await WriteIndexAsync(indexKey, [.. paths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)], ct);
			});
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
				// Accept only pool-relative paths: {branch}/note-{name}.yml (no further nesting)
				var slash = relativePath.IndexOf('/');
				if (slash <= 0)
					continue;
				var fileName = relativePath[(slash + 1)..];
				if (!IsNoteFileName(fileName))
					continue;
				files.Add(obj);
				_metrics.IncrementObjectsListed();
			}
			request.ContinuationToken = response.NextContinuationToken;
		} while (response.IsTruncated == true);

		return files;
	}

	private static bool IsNoteFileName(string fileName) =>
		fileName.StartsWith("note-", StringComparison.OrdinalIgnoreCase)
		&& (fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
		&& !fileName.Contains('/', StringComparison.Ordinal);

	private async Task<IReadOnlyList<string>> ExtractTargetsAsync(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = _sourceBucketName,
				Key = key
			}, ctx);

			await using var stream = response.ResponseStream;
			using var reader = new StreamReader(stream);
			var yaml = await reader.ReadToEndAsync(ctx);

			var normalized = ReleaseNotesSerialization.NormalizeYaml(yaml);
			var dto = ReleaseNotesSerialization.GetEntryDeserializer().Deserialize<ChangelogEntryDto>(normalized);

			if (dto.Products is not { Count: > 0 })
				return [];

			return dto.Products
				.Select(p => p.Target)
				.Where(t => !string.IsNullOrWhiteSpace(t))
				.Distinct(StringComparer.Ordinal)
				.ToList()!;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			// Note was deleted between the list and the read; skip it.
			return [];
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read targets from note {Key}; skipping", key);
			return [];
		}
	}

	private async Task WriteIndexAsync(string key, IReadOnlyList<string> paths, Cancel ctx)
	{
		var index = new NotesIndex { Notes = paths };
		var json = JsonSerializer.Serialize(index, NotesIndexJsonContext.Default.NotesIndex);

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();
			try
			{
				_ = await s3Client.PutObjectAsync(new PutObjectRequest
				{
					BucketName = publicBucketName,
					Key = key,
					ContentBody = json,
					ContentType = "application/json"
				}, ctx);

				_metrics.IncrementRegistryWrites();
				_logger.LogInformation("Wrote notes index {Key} with {Count} path(s)", key, paths.Count);
				return;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				if (attempt >= MaxWriteAttempts)
					throw;

				var delay = _retryBaseDelay * attempt;
				_logger.LogDebug(ex, "Notes index write {Key} failed (attempt {A}/{Max}); retrying in {Delay}", key, attempt, MaxWriteAttempts, delay);
				await Task.Delay(delay, ctx);
			}
		}
	}
}
