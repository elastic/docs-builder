// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Deploying;

public class AwsS3SyncApplyStrategy(
	IAmazonS3 s3Client,
	ITransferUtility transferUtility,
	string bucketName,
	AssembleContext context,
	ILoggerFactory loggerFactory,
	DiagnosticsCollector collector) : IDocsSyncApplyStrategy
{
	private readonly ILogger<AwsS3SyncApplyStrategy> _logger = loggerFactory.CreateLogger<AwsS3SyncApplyStrategy>();

	private void DisplayProgress(object? sender, UploadDirectoryProgressArgs args) => LogProgress(_logger, args, null);

	private static readonly Action<ILogger, UploadDirectoryProgressArgs, Exception?> LogProgress = LoggerMessage.Define<UploadDirectoryProgressArgs>(
		LogLevel.Information,
		new EventId(2, nameof(LogProgress)),
		"{Args}");

	public async Task Apply(SyncPlan plan, Cancel ctx = default)
	{
		var deleteCount = 0;
		_logger.LogInformation("Processor count: {ProcessorCount}", Environment.ProcessorCount);

		// Process uploads using directory upload
		var uploadRequests = plan.AddRequests.Cast<UploadRequest>().Concat(plan.UpdateRequests).ToList();
		if (uploadRequests.Count > 0)
		{
			_logger.LogInformation("Starting to process {Count} uploads using directory upload", uploadRequests.Count);

			// Create a temporary directory for uploads
			var tempDir = Path.Combine(context.WriteFileSystem.Path.GetTempPath(), context.WriteFileSystem.Path.GetRandomFileName());
			_ = context.WriteFileSystem.Directory.CreateDirectory(tempDir);

			try
			{
				_logger.LogInformation("Copying {Count} files to temp directory", uploadRequests.Count);

				// Copy files to temp directory maintaining structure
				foreach (var upload in uploadRequests)
				{
					var destPath = context.WriteFileSystem.Path.Combine(tempDir, upload.DestinationPath);
					var destDirPath = context.WriteFileSystem.Path.GetDirectoryName(destPath)!;
					_ = context.WriteFileSystem.Directory.CreateDirectory(destDirPath);
					context.WriteFileSystem.File.Copy(upload.LocalPath, destPath);
				}

				// Configure directory upload
				var directoryRequest = new TransferUtilityUploadDirectoryRequest
				{
					BucketName = bucketName,
					Directory = tempDir,
					SearchPattern = "*",
					SearchOption = SearchOption.AllDirectories,
					UploadFilesConcurrently = true
				};

				// Track upload progress
				directoryRequest.UploadDirectoryProgressEvent += DisplayProgress;

				_logger.LogInformation("Uploading {Count} files to S3", uploadRequests.Count);
				_logger.LogDebug("Starting directory upload from {TempDir}", tempDir);

				// Start the upload
				await transferUtility.UploadDirectoryAsync(directoryRequest, ctx);

				_logger.LogDebug("Directory upload completed");
			}
			finally
			{
				// Clean up temp directory
				if (context.WriteFileSystem.Directory.Exists(tempDir))
					context.WriteFileSystem.Directory.Delete(tempDir, true);
			}
		}

		var deleteRequests = plan.DeleteRequests.ToList();
		if (deleteRequests.Count > 0)
		{
			// Process deletes in batches of 1000 (AWS S3 limit)
			foreach (var batch in deleteRequests.Chunk(1000))
			{
				var deleteObjectsRequest = new DeleteObjectsRequest
				{
					BucketName = bucketName,
					Objects = batch.Select(d => new KeyVersion
					{
						Key = d.DestinationPath
					}).ToList()
				};

				var response = await s3Client.DeleteObjectsAsync(deleteObjectsRequest, ctx);
				if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
				{
					foreach (var error in response.DeleteErrors)
						collector.EmitError(error.Key, $"Failed to delete: {error.Message}");
				}
				else
				{
					var newCount = Interlocked.Add(ref deleteCount, batch.Length);
					_logger.LogInformation("Deleted {Count} objects ({DeleteCount}/{TotalDeleteCount})",
						batch.Length, newCount, deleteRequests.Count);
				}
			}
		}
	}
}
