// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Integrations.S3;

/// <summary>Describes a file to upload: its local path and intended S3 key.</summary>
public record UploadTarget(string LocalPath, string S3Key);

/// <summary>Result of an incremental upload run.</summary>
public record UploadResult(int Uploaded, int Skipped, int Failed);

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

	public async Task<UploadResult> Upload(IReadOnlyList<UploadTarget> targets, Cancel ctx = default)
	{
		var uploaded = 0;
		var skipped = 0;
		var failed = 0;

		foreach (var target in targets)
		{
			ctx.ThrowIfCancellationRequested();

			try
			{
				var remoteEtag = await GetRemoteEtag(target.S3Key, ctx);
				var localEtag = await etagCalculator.CalculateS3ETag(target.LocalPath, ctx);

				if (remoteEtag != null && localEtag == remoteEtag)
				{
					_logger.LogDebug("Skipping {S3Key} (ETag match)", target.S3Key);
					skipped++;
					continue;
				}

				_logger.LogInformation("Uploading {LocalPath} → s3://{Bucket}/{S3Key}", target.LocalPath, bucketName, target.S3Key);
				await PutObject(target, ctx);
				uploaded++;
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				_logger.LogError(ex, "Failed to upload {LocalPath} → s3://{Bucket}/{S3Key}", target.LocalPath, bucketName, target.S3Key);
				failed++;
			}
		}

		return new UploadResult(uploaded, skipped, failed);
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
		catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
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
		_ = await s3Client.PutObjectAsync(request, ctx);
	}
}
