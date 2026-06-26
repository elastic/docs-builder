// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Uploading;

/// <summary>
/// Which per-product manifest a <see cref="RegistryBuilder"/> run refreshes.
/// </summary>
internal enum RegistryScope
{
	/// <summary>The bundle index at <c>bundle/{product}/registry.json</c>, listing scrubbed bundle files.</summary>
	Bundle,

	/// <summary>The changelog-entry index at <c>changelog/{repo}/registry.json</c>, listing individual entry files.</summary>
	Changelog
}

/// <summary>
/// Refreshes a <c>registry.json</c> manifest in the private bucket after an upload run.
/// Depending on <see cref="RegistryScope"/> this is either the bundle index
/// (<c>bundle/{product}/registry.json</c>) or the changelog-entry index
/// (<c>changelog/{repo}/registry.json</c>). Each grouping (product or repo) touched in the run gets
/// its manifest merged with what is already known on S3 (read back, merged by file name, written with
/// an optimistic concurrency guard so parallel uploads for the same group cannot clobber each other).
/// </summary>
internal sealed class RegistryBuilder(
	ILoggerFactory logFactory,
	IFileSystem fileSystem,
	IAmazonS3 s3Client,
	IS3EtagCalculator etagCalculator,
	string bucketName,
	TimeProvider? timeProvider = null
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<RegistryBuilder>();
	private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

	// Bounds the optimistic-concurrency retry loop. Concurrent uploads for the same product are
	// expected to be rare (releases are largely serialized), so a small ceiling is plenty.
	private const int MaxWriteAttempts = 5;

	/// <summary>Outcome counts for a manifest refresh run, used for logging only.</summary>
	internal sealed record RefreshResult(int Updated, int Unchanged, int Failed);

	/// <summary>
	/// Builds and writes per-product manifests for every product touched by <paramref name="bundleTargets"/>.
	/// Each manifest is merged with the copy already on S3 and written back with a conditional PUT
	/// (<c>If-Match</c> on update, <c>If-None-Match: *</c> on create); a precondition failure means a
	/// concurrent writer won the race, so we re-read, re-merge, and retry.
	/// </summary>
	/// <param name="collector">Diagnostics sink for non-fatal warnings.</param>
	/// <param name="uploadTargets">Upload targets produced by <c>DiscoverBundleUploadTargets</c> or <c>DiscoverUploadTargets</c>.</param>
	/// <param name="ctx">Cancellation token.</param>
	/// <param name="scope">Which per-product manifest to refresh (bundle index or changelog-entry index).</param>
	public async Task<RefreshResult> RefreshAsync(
		IDiagnosticsCollector collector,
		IReadOnlyList<UploadTarget> uploadTargets,
		Cancel ctx,
		RegistryScope scope = RegistryScope.Bundle)
	{
		// Each upload target carries an artifact-root S3 key — "bundle/{product}/{file}" or
		// "changelog/{repo}/{file}". Group by the scope's second segment (product for bundles, repo for
		// entries) so we produce one manifest per affected group.
		var byProduct = uploadTargets
			.Select(t => (Target: t, Product: ExtractGroupKey(t.S3Key, scope)))
			.Where(x => x.Product is not null)
			.GroupBy(x => x.Product!, StringComparer.Ordinal);

		var updated = 0;
		var unchanged = 0;
		var failed = 0;

		foreach (var group in byProduct)
		{
			ctx.ThrowIfCancellationRequested();

			var product = group.Key;
			var localEntries = await BuildLocalEntries(collector, product, group.Select(x => x.Target).ToList(), scope, ctx);
			if (localEntries.Count == 0)
			{
				_logger.LogDebug("No usable manifest entries derived for product {Product}; skipping", product);
				continue;
			}

			switch (await WriteManifest(collector, product, localEntries, scope, ctx))
			{
				case WriteOutcome.Updated:
					updated++;
					break;
				case WriteOutcome.Unchanged:
					unchanged++;
					break;
				default:
					failed++;
					break;
			}
		}

		return new RefreshResult(updated, unchanged, failed);
	}

	/// <summary>Extracts the grouping segment (product for <c>bundle/{product}/…</c>, repo for <c>changelog/{repo}/…</c>) from an artifact-root S3 key, or null.</summary>
	private static string? ExtractGroupKey(string s3Key, RegistryScope scope)
	{
		var prefix = scope == RegistryScope.Changelog ? "changelog/" : "bundle/";
		if (!s3Key.StartsWith(prefix, StringComparison.Ordinal))
			return null;

		var rest = s3Key.AsSpan(prefix.Length);
		var slash = rest.IndexOf('/');
		if (slash <= 0)
			return null;
		return rest[..slash].ToString();
	}

	/// <summary>The S3 key of the manifest for the given <paramref name="scope"/> and grouping segment.</summary>
	private static string RegistryKeyFor(string group, RegistryScope scope) => scope switch
	{
		RegistryScope.Changelog => $"changelog/{group}/registry.json",
		_ => $"bundle/{group}/registry.json"
	};

	/// <summary>Builds manifest entries for this run's bundles, recording the per-<paramref name="product"/> target for the bundle index.</summary>
	private async Task<List<RegistryBundle>> BuildLocalEntries(
		IDiagnosticsCollector collector,
		string product,
		IReadOnlyList<UploadTarget> targets,
		RegistryScope scope,
		Cancel ctx)
	{
		var entries = new List<RegistryBundle>(targets.Count);
		foreach (var target in targets)
		{
			ctx.ThrowIfCancellationRequested();

			// The changelog-entry index only needs to enumerate files (consumers re-read each entry
			// to filter), so target is left unset there; the bundle index records the per-product target.
			var targetVersion = scope == RegistryScope.Bundle
				? ReadTargetForProduct(collector, target.LocalPath, product)
				: null;

			string etag;
			try
			{
				etag = await etagCalculator.CalculateS3ETag(target.LocalPath, ctx);
			}
			catch (Exception ex)
			{
				collector.EmitWarning(target.LocalPath,
					$"Could not compute ETag for manifest entry: {ex.Message}");
				continue;
			}

			var fileName = fileSystem.Path.GetFileName(target.LocalPath);
			entries.Add(new RegistryBundle
			{
				File = fileName,
				Target = targetVersion,
				ETag = etag
			});
		}

		return entries;
	}

	private string? ReadTargetForProduct(IDiagnosticsCollector collector, string localPath, string product)
	{
		try
		{
			var content = fileSystem.File.ReadAllText(localPath);
			var bundle = ReleaseNotesSerialization.DeserializeBundle(content);
			if (bundle.Products.Count == 0)
				return null;

			var match = bundle.Products.FirstOrDefault(p => string.Equals(p.ProductId, product, StringComparison.Ordinal));
			return (match ?? bundle.Products[0]).Target;
		}
		catch (Exception ex)
		{
			collector.EmitWarning(localPath, $"Could not read bundle target for manifest: {ex.Message}");
			return null;
		}
	}

	private enum WriteOutcome { Updated, Unchanged, Failed }

	private async Task<WriteOutcome> WriteManifest(
		IDiagnosticsCollector collector,
		string product,
		IReadOnlyList<RegistryBundle> localEntries,
		RegistryScope scope,
		Cancel ctx)
	{
		var key = RegistryKeyFor(product, scope);

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var (existing, etag) = await TryFetchExistingManifest(product, scope, ctx);
			var merged = Merge(existing, localEntries);

			// Re-uploading the same bundles must not churn the manifest (keeps reruns idempotent).
			if (etag is not null && BundlesEqual(existing, merged))
			{
				_logger.LogDebug("registry for {Product} already up to date; skipping write", product);
				return WriteOutcome.Unchanged;
			}

			var manifest = new Registry
			{
				Product = product,
				GeneratedAt = _time.GetUtcNow(),
				Bundles = merged
			};
			var json = JsonSerializer.Serialize(manifest, RegistryJsonContext.Default.Registry);

			try
			{
				await PutManifest(key, json, etag, ctx);
				_logger.LogInformation("Wrote registry.json for {Product} with {Count} bundle(s)", product, merged.Count);
				return WriteOutcome.Updated;
			}
			catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
			{
				_logger.LogInformation(
					"registry for {Product} changed concurrently (attempt {Attempt}/{Max}); re-reading and retrying",
					product, attempt, MaxWriteAttempts);
			}
		}

		collector.EmitWarning(string.Empty,
			$"registry for {product} could not be updated after {MaxWriteAttempts} attempts due to concurrent writes; the index may be stale.");
		return WriteOutcome.Failed;
	}

	/// <summary>Reads the existing manifest and its ETag (null ETag when absent; live ETag when corrupt, so the conditional write can overwrite).</summary>
	private async Task<(IReadOnlyList<RegistryBundle> Bundles, string? ETag)> TryFetchExistingManifest(string product, RegistryScope scope, Cancel ctx)
	{
		var key = RegistryKeyFor(product, scope);
		string? etag = null;
		try
		{
			using var response = await s3Client.GetObjectAsync(new GetObjectRequest
			{
				BucketName = bucketName,
				Key = key
			}, ctx);

			etag = response.ETag;
			await using var stream = response.ResponseStream;
			var existing = await JsonSerializer.DeserializeAsync(
				stream,
				RegistryJsonContext.Default.Registry,
				ctx);

			return (existing?.Bundles ?? [], etag);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return ([], null);
		}
		catch (JsonException ex)
		{
			// Only a genuinely corrupt (unparseable) manifest is rebuilt from this run; the captured ETag
			// then lets the conditional write overwrite it safely. Transient S3/IO errors must NOT be
			// treated as corruption — otherwise the If-Match PUT would replace a valid manifest with only
			// this run's bundles and drop previously published entries. Let those bubble up to the
			// best-effort handler in ChangelogUploadService instead.
			_logger.LogWarning(ex, "Existing manifest for {Product} could not be parsed; recreating", product);
			return ([], etag);
		}
	}

	private async Task PutManifest(string key, string json, string? etag, Cancel ctx)
	{
		var request = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = key,
			ContentBody = json,
			ContentType = "application/json"
		};

		// Optimistic concurrency: update only if unchanged, create only if still absent.
		if (etag is null)
			request.IfNoneMatch = "*";
		else
			request.IfMatch = etag;

		_ = await s3Client.PutObjectAsync(request, ctx);
	}

	/// <summary>Replaces existing entries by file name and sorts newest-target-first (with a file-name tiebreak) for a stable manifest.</summary>
	private static List<RegistryBundle> Merge(
		IReadOnlyList<RegistryBundle> existing,
		IReadOnlyList<RegistryBundle> incoming)
	{
		var byFile = existing.ToDictionary(b => b.File, b => b, StringComparer.Ordinal);
		foreach (var entry in incoming)
			byFile[entry.File] = entry;

		return byFile.Values
			.OrderByDescending(b => VersionOrDate.Parse(b.Target ?? string.Empty))
			.ThenBy(b => b.File, StringComparer.Ordinal)
			.ToList();
	}

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
