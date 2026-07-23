// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Documentation.Configuration.ReleaseNotes;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Changelog.Reconciliation;

/// <summary>
/// Explicitly re-emits the <c>s3:ObjectCreated</c> event for objects in a <em>private</em> scope so
/// the changelog scrubber Lambda re-processes them — the recovery path for a lost or dead-lettered
/// scrub event. The re-emission is a metadata-preserving S3 self-copy (<c>CopyObject</c> with the
/// same source and destination key and <c>MetadataDirective: REPLACE</c> carrying the original
/// content type and user metadata), which leaves the object's content and ETag untouched while
/// producing an <c>ObjectCreated:Copy</c> notification, the event type the scrubber listens for.
/// </summary>
/// <remarks>
/// This never writes to the public bucket: the scrubber remains the sole public-side writer. It is
/// also never run implicitly — republishing is only ever triggered by this explicit operation.
/// </remarks>
public sealed class ChangelogRegistryRepublishService(
	ILoggerFactory logFactory,
	IAmazonS3? s3Client = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<ChangelogRegistryRepublishService>();

	public async Task<bool> Republish(IDiagnosticsCollector collector, ChangelogRegistryRepublishArguments args, Cancel ctx)
	{
		if (!args.TryResolveScope(collector, out var scope))
			return false;

		if (args.All == (args.Files.Count > 0))
		{
			collector.EmitError(string.Empty,
				"Specify exactly one selection: --files with the file name(s) to republish, or --all for every object in the scope.");
			return false;
		}

		using var defaultClient = s3Client == null ? new AmazonS3Client() : null;
		var client = s3Client ?? defaultClient!;

		var keys = args.All
			? await ResolveAllScopeKeys(client, args.S3BucketName, scope, ctx)
			: ResolveExplicitKeys(collector, scope, args.Files);
		if (keys is null)
			return false;

		if (keys.Count == 0)
		{
			_logger.LogInformation("Scope {Scope} contains no objects to republish", scope);
			return true;
		}

		var failed = 0;
		foreach (var key in keys)
		{
			ctx.ThrowIfCancellationRequested();
			if (!await RepublishObject(collector, client, args.S3BucketName, key, ctx))
				failed++;
		}

		_logger.LogInformation("Republish complete: {Succeeded} re-emitted, {Failed} failed", keys.Count - failed, failed);
		if (failed > 0)
			collector.EmitError(string.Empty, $"{failed} of {keys.Count} object(s) could not be republished.");
		return failed == 0;
	}

	/// <summary>Every single-segment object in the scope, the <c>registry.json</c> manifest included.</summary>
	private static async Task<IReadOnlyList<string>> ResolveAllScopeKeys(IAmazonS3 client, string bucketName, ChangelogScope scope, Cancel ctx)
	{
		var reader = new S3ScopeReader(client, bucketName);
		var listed = await reader.ListObjectsAsync(scope.Prefix, ctx);
		return listed
			.Where(o =>
			{
				var file = o.Key[scope.Prefix.Length..];
				return file.Length > 0 && !file.Contains('/', StringComparison.Ordinal);
			})
			.Select(o => o.Key)
			.ToList();
	}

	private static IReadOnlyList<string>? ResolveExplicitKeys(IDiagnosticsCollector collector, ChangelogScope scope, IReadOnlyList<string> files)
	{
		var keys = new List<string>(files.Count);
		foreach (var file in files)
		{
			if (!ChangelogKeys.IsSafeFileName(file))
			{
				collector.EmitError(string.Empty, $"Invalid file name \"{file}\": must be a single path segment.");
				return null;
			}
			keys.Add(scope.Prefix + file);
		}

		return keys;
	}

	private async Task<bool> RepublishObject(IDiagnosticsCollector collector, IAmazonS3 client, string bucketName, string key, Cancel ctx)
	{
		GetObjectMetadataResponse head;
		try
		{
			head = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
			{
				BucketName = bucketName,
				Key = key
			}, ctx);
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			collector.EmitError(string.Empty, $"Cannot republish {key}: the object does not exist in the private bucket.");
			return false;
		}

		// Self-copy with a replaced-but-identical metadata set: S3 only allows copying an object onto
		// itself when the metadata directive is REPLACE, and re-supplying the original values keeps the
		// rewrite content- and metadata-preserving.
		var request = new CopyObjectRequest
		{
			SourceBucket = bucketName,
			SourceKey = key,
			DestinationBucket = bucketName,
			DestinationKey = key,
			MetadataDirective = S3MetadataDirective.REPLACE,
			ContentType = head.Headers.ContentType
		};
		foreach (var metadataKey in head.Metadata.Keys)
			request.Metadata.Add(metadataKey, head.Metadata[metadataKey]);

		try
		{
			_ = await client.CopyObjectAsync(request, ctx);
			_logger.LogInformation("Re-emitted ObjectCreated for {Key}", key);
			return true;
		}
		catch (AmazonS3Exception ex)
		{
			collector.EmitError(string.Empty, $"Failed to republish {key}: {ex.Message}", ex);
			return false;
		}
	}
}
