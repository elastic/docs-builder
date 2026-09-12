// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Integrations.S3;

/// <summary>Describes a file to upload: its local path and intended S3 key.</summary>
/// <remarks>
/// When <see cref="InlineContent"/> is non-null the object is uploaded from that string
/// directly (skipping ETag comparison) and <see cref="LocalPath"/> is ignored. Used for
/// machine-generated marker objects that have no on-disk counterpart.
/// </remarks>
public record UploadTarget(string LocalPath, string S3Key, string? InlineContent = null);

/// <summary>Options for an incremental S3 upload run.</summary>
public record S3UploadOptions
{
	/// <summary>When true, Put every discovered file even when its content hash matches the remote object.</summary>
	public bool SkipEtagCheck { get; init; }

	/// <summary>
	/// When true, do not Put when the remote key already exists with different content.
	/// Unchanged (ETag match) files are still skipped. New keys are still uploaded.
	/// </summary>
	public bool NoOverwrite { get; init; }
}

/// <summary>A remote object that was not overwritten because <see cref="S3UploadOptions.NoOverwrite"/> was set.</summary>
public record UploadConflict(string S3Key, string LocalPath, string? RemoteContent, string? InlineContent = null);

/// <summary>Result of an incremental upload run.</summary>
public record UploadResult(int New, int Replaced, int Skipped, int NotOverwritten, int Failed, IReadOnlyList<UploadConflict> Conflicts)
{
	/// <summary>Files that were Put: <see cref="New"/> plus <see cref="Replaced"/>.</summary>
	public int Uploaded => New + Replaced;
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

	public Task<UploadResult> Upload(IReadOnlyList<UploadTarget> targets, bool skipEtagCheck = false, Cancel ctx = default) =>
		Upload(targets, new S3UploadOptions { SkipEtagCheck = skipEtagCheck }, ctx);

	public async Task<UploadResult> Upload(IReadOnlyList<UploadTarget> targets, S3UploadOptions options, Cancel ctx = default)
	{
		var run = new UploadRun(options, ctx);

		foreach (var target in targets)
		{
			ctx.ThrowIfCancellationRequested();

			try
			{
				if (target.InlineContent is not null)
					await ProcessInline(target, run).ConfigureAwait(false);
				else
					await ProcessFile(target, run).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Failed to upload {LocalPath} → s3://{Bucket}/{S3Key}", target.LocalPath, bucketName, target.S3Key);
				run.Failed++;
			}
		}

		return new UploadResult(run.New, run.Replaced, run.Skipped, run.NotOverwritten, run.Failed, run.Conflicts);
	}

	private async Task ProcessInline(UploadTarget target, UploadRun run)
	{
		var exists = await GetRemoteEtag(target.S3Key, run.Ctx).ConfigureAwait(false) != null;
		if (exists && run.Options.NoOverwrite)
		{
			await RefuseOverwrite(target, run).ConfigureAwait(false);
			return;
		}

		await PutClassified(target, exists, run, inline: true).ConfigureAwait(false);
	}

	private async Task ProcessFile(UploadTarget target, UploadRun run)
	{
		var remoteEtag = await GetRemoteEtag(target.S3Key, run.Ctx).ConfigureAwait(false);
		var exists = remoteEtag != null;

		if (!run.Options.SkipEtagCheck && remoteEtag != null)
		{
			var localEtag = await etagCalculator.CalculateS3ETag(target.LocalPath, run.Ctx).ConfigureAwait(false);
			if (localEtag == remoteEtag)
			{
				_logger.LogDebug("Skipping {S3Key} (ETag match)", target.S3Key);
				run.Skipped++;
				return;
			}
		}

		if (exists && run.Options.NoOverwrite)
		{
			await RefuseOverwrite(target, run).ConfigureAwait(false);
			return;
		}

		await PutClassified(target, exists, run, inline: false).ConfigureAwait(false);
	}

	private async Task PutClassified(UploadTarget target, bool exists, UploadRun run, bool inline)
	{
		var kind = exists ? "replace" : "new";
		if (inline)
		{
			_logger.LogInformation("Uploading inline marker → s3://{Bucket}/{S3Key} ({Kind})", bucketName, target.S3Key, kind);
			await PutInlineObject(target.S3Key, target.InlineContent!, run.Ctx).ConfigureAwait(false);
		}
		else
		{
			_logger.LogInformation(
				"Uploading {LocalPath} → s3://{Bucket}/{S3Key} ({Kind})",
				target.LocalPath,
				bucketName,
				target.S3Key,
				kind
			);
			await PutObject(target, run.Ctx).ConfigureAwait(false);
		}

		if (exists)
			run.Replaced++;
		else
			run.New++;
	}

	private async Task RefuseOverwrite(UploadTarget target, UploadRun run)
	{
		var content = await TryGetRemoteContent(target.S3Key, run.Ctx).ConfigureAwait(false);
		run.NotOverwritten++;
		run.Conflicts.Add(new UploadConflict(target.S3Key, target.LocalPath, content, target.InlineContent));
	}

	private async Task<string?> GetRemoteEtag(string key, Cancel ctx)
	{
		try
		{
			var response = await s3Client.GetObjectMetadataAsync(
				new GetObjectMetadataRequest { BucketName = bucketName, Key = key },
				ctx
			).ConfigureAwait(false);
			return response.ETag.Trim('"');
		}
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return null;
		}
	}

	private async Task<string?> TryGetRemoteContent(string key, Cancel ctx)
	{
		try
		{
			using var response = await s3Client.GetObjectAsync(
				new GetObjectRequest { BucketName = bucketName, Key = key },
				ctx
			).ConfigureAwait(false);
			using var reader = new StreamReader(response.ResponseStream, Encoding.UTF8);
			return await reader.ReadToEndAsync(ctx).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogWarning(ex, "Could not read existing object s3://{Bucket}/{S3Key}", bucketName, key);
			return null;
		}
	}

	private async Task PutObject(UploadTarget target, Cancel ctx)
	{
		await using var stream = fileSystem.FileStream.New(target.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		var request = new PutObjectRequest
		{
			BucketName = bucketName,
			Key = target.S3Key,
			InputStream = stream,
			ChecksumAlgorithm = ChecksumAlgorithm.SHA256
		};
		_ = await s3Client.PutObjectAsync(request, ctx).ConfigureAwait(false);
	}

	private async Task PutInlineObject(string key, string content, Cancel ctx)
	{
		var request = new PutObjectRequest { BucketName = bucketName, Key = key, ContentBody = content, ContentType = "application/yaml" };
		_ = await s3Client.PutObjectAsync(request, ctx).ConfigureAwait(false);
	}

	private sealed class UploadRun(S3UploadOptions options, Cancel ctx)
	{
		public readonly S3UploadOptions Options = options;
		public readonly Cancel Ctx = ctx;
		public readonly List<UploadConflict> Conflicts = [];
		public int New;
		public int Replaced;
		public int Skipped;
		public int NotOverwritten;
		public int Failed;
	}
}
