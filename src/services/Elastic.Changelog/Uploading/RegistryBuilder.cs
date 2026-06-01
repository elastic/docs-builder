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
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Uploading;

/// <summary>
/// Refreshes the per-product <c>{product}/registry.json</c> manifest in the private bundles
/// bucket after a bundle upload run. Each product touched in the run gets its manifest merged with
/// the bundles already known on S3 (read back, merged by file name, written with an optimistic
/// concurrency guard so parallel uploads for the same product cannot clobber each other).
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
	/// <param name="bundleTargets">Upload targets produced by <c>DiscoverBundleUploadTargets</c>.</param>
	/// <param name="ctx">Cancellation token.</param>
	public async Task<RefreshResult> RefreshAsync(
		IDiagnosticsCollector collector,
		IReadOnlyList<UploadTarget> bundleTargets,
		Cancel ctx)
	{
		// Each upload target carries a "{product}/bundle/{file}" S3 key. Group by product
		// so we can produce one manifest per affected product.
		var byProduct = bundleTargets
			.Select(t => (Target: t, Product: ExtractProduct(t.S3Key)))
			.Where(x => x.Product is not null)
			.GroupBy(x => x.Product!, StringComparer.Ordinal);

		var updated = 0;
		var unchanged = 0;
		var failed = 0;

		foreach (var group in byProduct)
		{
			ctx.ThrowIfCancellationRequested();

			var product = group.Key;
			var localEntries = await BuildLocalEntries(collector, product, group.Select(x => x.Target).ToList(), ctx);
			if (localEntries.Count == 0)
			{
				_logger.LogDebug("No usable manifest entries derived for product {Product}; skipping", product);
				continue;
			}

			switch (await WriteManifest(collector, product, localEntries, ctx))
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

	/// <summary>
	/// Extracts the leading <c>product</c> segment from an S3 key shaped like
	/// <c>{product}/bundle/{file}</c>. Returns null on unrecognized shapes.
	/// </summary>
	private static string? ExtractProduct(string s3Key)
	{
		var firstSlash = s3Key.IndexOf('/');
		if (firstSlash <= 0)
			return null;
		return s3Key.AsSpan(0, firstSlash).ToString();
	}

	/// <summary>
	/// Computes manifest entries for the bundles uploaded in this run by reading their YAML
	/// and computing the S3 ETag locally. The target is taken from the bundle's declaration of
	/// <paramref name="product"/> (not the first product) so multi-product bundles with differing
	/// targets are recorded correctly per product.
	/// </summary>
	private async Task<List<RegistryBundle>> BuildLocalEntries(
		IDiagnosticsCollector collector,
		string product,
		IReadOnlyList<UploadTarget> targets,
		Cancel ctx)
	{
		var entries = new List<RegistryBundle>(targets.Count);
		foreach (var target in targets)
		{
			ctx.ThrowIfCancellationRequested();

			var targetVersion = ReadTargetForProduct(collector, target.LocalPath, product);

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
		Cancel ctx)
	{
		var key = $"{product}/registry.json";

		for (var attempt = 1; attempt <= MaxWriteAttempts; attempt++)
		{
			ctx.ThrowIfCancellationRequested();

			var (existing, etag) = await TryFetchExistingManifest(product, ctx);
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

	/// <summary>
	/// Reads the existing per-product manifest from S3 together with its ETag (for the conditional
	/// write). Returns an empty list with a null ETag when the object does not exist. A corrupt object
	/// returns an empty list with the live ETag so the retry loop can conditionally overwrite it.
	/// </summary>
	private async Task<(IReadOnlyList<RegistryBundle> Bundles, string? ETag)> TryFetchExistingManifest(string product, Cancel ctx)
	{
		var key = $"{product}/registry.json";
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
		catch (Exception ex)
		{
			// Don't fail the whole upload because the existing manifest is corrupt; rebuild from this run.
			// When we captured an ETag the conditional write still overwrites the corrupt object safely.
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

	/// <summary>
	/// Replaces existing entries by file name, then appends new ones. Sort is target-desc with a
	/// deterministic tiebreak on file name to keep the on-disk JSON stable across reruns.
	/// </summary>
	private static List<RegistryBundle> Merge(
		IReadOnlyList<RegistryBundle> existing,
		IReadOnlyList<RegistryBundle> incoming)
	{
		var byFile = existing.ToDictionary(b => b.File, b => b, StringComparer.Ordinal);
		foreach (var entry in incoming)
			byFile[entry.File] = entry;

		return byFile.Values
			.OrderByDescending(b => b.Target ?? string.Empty, StringComparer.Ordinal)
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
