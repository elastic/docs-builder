// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Integrations.S3;

/// <summary>Describes a file to upload: its local path and intended S3 key.</summary>
public record UploadTarget(string LocalPath, string S3Key);

/// <summary>How the uploader treats a destination key that already holds different content.</summary>
public enum S3WritePolicy
{
	/// <summary>Live behavior: changed content replaces the remote object.</summary>
	Overwrite,

	/// <summary>
	/// Backfill behavior: never replace an existing object. Identical content is skipped; different
	/// content is a conflict. The write itself is a conditional PUT (<c>If-None-Match: *</c>) so a
	/// concurrent writer that creates the key first also surfaces as a conflict, never an overwrite.
	/// </summary>
	CreateOnly
}

/// <summary>Result of an incremental upload run.</summary>
public record UploadResult(int Uploaded, int Skipped, int Failed)
{
	/// <summary>
	/// Targets refused under <see cref="S3WritePolicy.CreateOnly"/> because the destination key already
	/// exists with different content (or was created concurrently). Always empty under
	/// <see cref="S3WritePolicy.Overwrite"/>.
	/// </summary>
	public IReadOnlyList<UploadTarget> Conflicts { get; init; } = [];
}

/// <summary>
/// Uploads files to S3, skipping those whose content has not changed (ETag comparison).
/// Reuses the same MD5-based ETag calculation that the docs assembly deploy pipeline uses.
/// </summary>
public class S3IncrementalUploader(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	IFileSystem fileSystem,
	IS3EtagCalculator etagCalculator,
	string bucketName
)
{
	private readonly ILogger _logger = logFactory.CreateLogger<S3IncrementalUploader>();

	private enum TargetOutcome { Uploaded, Skipped, Conflict }

	public async Task<UploadResult> Upload(
		IReadOnlyList<UploadTarget> targets,
		bool skipEtagCheck = false,
		S3WritePolicy writePolicy = S3WritePolicy.Overwrite,
		Cancel ctx = default)
	{
		// The content comparison is what distinguishes an idempotent re-run (skip) from a conflict, so
		// it cannot be turned off for create-only runs.
		if (writePolicy == S3WritePolicy.CreateOnly && skipEtagCheck)
			throw new ArgumentException("skipEtagCheck cannot be combined with create-only writes.", nameof(skipEtagCheck));

		var uploaded = 0;
		var skipped = 0;
		var failed = 0;
		var conflicts = new List<UploadTarget>();

		foreach (var target in targets)
		{
			ctx.ThrowIfCancellationRequested();

			try
			{
				var outcome = writePolicy == S3WritePolicy.CreateOnly
					? await CreateObject(target, ctx)
					: await OverwriteObjectIfChanged(target, skipEtagCheck, ctx);

				switch (outcome)
				{
					case TargetOutcome.Uploaded:
						uploaded++;
						break;
					case TargetOutcome.Skipped:
						skipped++;
						break;
					default:
						conflicts.Add(target);
						break;
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Failed to upload {LocalPath} → s3://{Bucket}/{S3Key}", target.LocalPath, bucketName, target.S3Key);
				failed++;
			}
		}

		return new UploadResult(uploaded, skipped, failed) { Conflicts = conflicts };
	}

	private async Task<TargetOutcome> OverwriteObjectIfChanged(UploadTarget target, bool skipEtagCheck, Cancel ctx)
	{
		if (!skipEtagCheck)
		{
			var remoteEtag = await GetRemoteEtag(target.S3Key, ctx);
			var localEtag = await etagCalculator.CalculateS3ETag(target.LocalPath, ctx);

			if (remoteEtag != null && localEtag == remoteEtag)
			{
				_logger.LogDebug("Skipping {S3Key} (ETag match)", target.S3Key);
				return TargetOutcome.Skipped;
			}
		}
		else
		{
			_logger.LogDebug("Uploading {S3Key} (--skip-etag-check)", target.S3Key);
		}

		_logger.LogInformation("Uploading {LocalPath} → s3://{Bucket}/{S3Key}", target.LocalPath, bucketName, target.S3Key);
		await PutObject(target, createOnly: false, ctx);
		return TargetOutcome.Uploaded;
	}

	private async Task<TargetOutcome> CreateObject(UploadTarget target, Cancel ctx)
	{
		// This inspection is informative only (friendly skip/conflict reporting); the conditional PUT
		// below is the actual race guard between inspection and write.
		var remoteEtag = await GetRemoteEtag(target.S3Key, ctx);
		var localEtag = await etagCalculator.CalculateS3ETag(target.LocalPath, ctx);

		if (remoteEtag != null)
		{
			if (localEtag == remoteEtag)
			{
				_logger.LogInformation("Skipping {S3Key}: already exists with identical content", target.S3Key);
				return TargetOutcome.Skipped;
			}

			_logger.LogError(
				"Conflict on s3://{Bucket}/{S3Key}: key already exists with different content (remote ETag {RemoteEtag}, local {LocalEtag}); refusing to overwrite",
				bucketName, target.S3Key, remoteEtag, localEtag);
			return TargetOutcome.Conflict;
		}

		_logger.LogInformation("Creating {LocalPath} → s3://{Bucket}/{S3Key} (If-None-Match: *)", target.LocalPath, bucketName, target.S3Key);
		try
		{
			await PutObject(target, createOnly: true, ctx);
			return TargetOutcome.Uploaded;
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.PreconditionFailed)
		{
			_logger.LogError(
				"Conflict on s3://{Bucket}/{S3Key}: object was created concurrently (precondition failed); refusing to overwrite",
				bucketName, target.S3Key);
			return TargetOutcome.Conflict;
		}
	}

	private async Task<string?> GetRemoteEtag(string key, Cancel ctx)
	{
		try
		{
			var response = await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
			{
				BucketName = bucketName,
				Key = key
			}, ctx);
			return response.ETag.Trim('"');
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private async Task PutObject(UploadTarget target, bool createOnly, Cancel ctx)
	{
		await using var stream = fileSystem.FileStream.New(target.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var request = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = target.S3Key,
			InputStream = stream,
			ChecksumAlgorithm = ChecksumAlgorithm.SHA256
		};

		if (createOnly)
			request.IfNoneMatch = "*";

		_ = await s3Client.PutObjectAsync(request, ctx);
	}
}
