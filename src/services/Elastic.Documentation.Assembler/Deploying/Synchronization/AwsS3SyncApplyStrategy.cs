// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Elastic.Documentation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Deploying.Synchronization;

public partial class AwsS3SyncApplyStrategy(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	ITransferUtility transferUtility,
	string bucketName,
	AssembleContext context,
	IDiagnosticsCollector collector
) : IDocsSyncApplyStrategy
{
	private static readonly ActivitySource ApplyStrategyActivitySource = new("Elastic.Documentation.Assembler.Deploying.Synchronization.AwsS3SyncApplyStrategy");

	private readonly ILogger<AwsS3SyncApplyStrategy> _logger = logFactory.CreateLogger<AwsS3SyncApplyStrategy>();

	private void DisplayProgress(object? sender, UploadDirectoryProgressArgs args) => LogProgress(_logger, args);

	[LoggerMessage(
		EventId = 2,
		Level = LogLevel.Debug,
		Message = "{Args}")]
	private static partial void LogProgress(ILogger logger, UploadDirectoryProgressArgs args);

	[LoggerMessage(
		EventId = 3,
		Level = LogLevel.Information,
		Message = "File operation: {Action} | Path: {FilePath} | Size: {FileSize} bytes")]
	private static partial void LogFileOperation(ILogger logger, string action, string filePath, long fileSize);

	public async Task Apply(SyncPlan plan, Cancel ctx = default)
	{
		using var applyActivity = ApplyStrategyActivitySource.StartActivity("sync apply", ActivityKind.Client);
		if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
		{
			_ = applyActivity?.SetTag("cicd.pipeline.name", Environment.GetEnvironmentVariable("GITHUB_WORKFLOW") ?? "unknown");
			_ = applyActivity?.SetTag("cicd.pipeline.run.id", Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "unknown");
			_ = applyActivity?.SetTag("cicd.pipeline.run.attempt", Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT") ?? "unknown");
		}

		// Add aggregate metrics
		_ = applyActivity?.SetTag("sync.files.added", plan.AddRequests.Count);
		_ = applyActivity?.SetTag("sync.files.updated", plan.UpdateRequests.Count);
		_ = applyActivity?.SetTag("sync.files.deleted", plan.DeleteRequests.Count);
		_ = applyActivity?.SetTag("sync.files.total", plan.AddRequests.Count + plan.UpdateRequests.Count + plan.DeleteRequests.Count);

		await Upload(plan, ctx);
		await Delete(plan, ctx);
	}

	private async Task Upload(SyncPlan plan, Cancel ctx)
	{
		var uploadRequests = plan.AddRequests.Cast<UploadRequest>().Concat(plan.UpdateRequests).ToList();
		if (uploadRequests.Count > 0)
		{
			using var uploadActivity = ApplyStrategyActivitySource.StartActivity("upload files", ActivityKind.Client);
			_ = uploadActivity?.SetTag("sync.upload.count", uploadRequests.Count);

			var addCount = plan.AddRequests.Count;
			var updateCount = plan.UpdateRequests.Count;

			_logger.LogInformation("Starting to process {AddCount} new files and {UpdateCount} updated files", addCount, updateCount);

			// Emit individual file operations for analytics (queryable in Elastic)
			foreach (var upload in uploadRequests)
			{
				var action = plan.AddRequests.Contains(upload) ? "add" : "update";
				LogFileOperation(_logger, action, upload.DestinationPath,
					context.WriteFileSystem.FileInfo.New(upload.LocalPath).Length);
			}

			var tempDir = Path.Combine(context.WriteFileSystem.Path.GetTempPath(), context.WriteFileSystem.Path.GetRandomFileName());
			_ = context.WriteFileSystem.Directory.CreateDirectory(tempDir);
			try
			{
				_logger.LogInformation("Copying {Count} files to temp directory", uploadRequests.Count);
				foreach (var upload in uploadRequests)
				{
					var destPath = context.WriteFileSystem.Path.Combine(tempDir, upload.DestinationPath);
					var destDirPath = context.WriteFileSystem.Path.GetDirectoryName(destPath)!;
					_ = context.WriteFileSystem.Directory.CreateDirectory(destDirPath);
					context.WriteFileSystem.File.Copy(upload.LocalPath, destPath);
				}
				var directoryRequest = new TransferUtilityUploadDirectoryRequest
				{
					BucketName = bucketName,
					Directory = tempDir,
					SearchPattern = "*",
					SearchOption = SearchOption.AllDirectories,
					UploadFilesConcurrently = true
				};
				directoryRequest.UploadDirectoryProgressEvent += DisplayProgress;
				_logger.LogInformation("Uploading {Count} files to S3 bucket {BucketName}", uploadRequests.Count, bucketName);
				_logger.LogDebug("Starting directory upload from {TempDir}", tempDir);
				await transferUtility.UploadDirectoryAsync(directoryRequest, ctx);
				_logger.LogInformation("Successfully uploaded {Count} files ({AddCount} added, {UpdateCount} updated)",
					uploadRequests.Count, addCount, updateCount);
			}
			finally
			{
				// Clean up temp directory
				if (context.WriteFileSystem.Directory.Exists(tempDir))
					context.WriteFileSystem.Directory.Delete(tempDir, true);
			}
		}
	}

	private async Task Delete(SyncPlan plan, Cancel ctx)
	{
		var deleteCount = 0;
		var deleteRequests = plan.DeleteRequests.ToList();
		if (deleteRequests.Count > 0)
		{
			using var deleteActivity = ApplyStrategyActivitySource.StartActivity("delete files", ActivityKind.Client);
			_ = deleteActivity?.SetTag("sync.delete.count", deleteRequests.Count);

			_logger.LogInformation("Starting to delete {Count} files from S3 bucket {BucketName}", deleteRequests.Count, bucketName);

			// Emit individual file operations for analytics (queryable in Elastic)
			foreach (var delete in deleteRequests)
			{
				LogFileOperation(_logger, "delete", delete.DestinationPath, 0);
			}

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
					_logger.LogError("Delete batch failed with status code {StatusCode}", response.HttpStatusCode);
					foreach (var error in response.DeleteErrors)
					{
						_logger.LogError("Failed to delete {Key}: {Message}", error.Key, error.Message);
						collector.EmitError(error.Key, $"Failed to delete: {error.Message}");
					}
				}
				else
				{
					var newCount = Interlocked.Add(ref deleteCount, batch.Length);
					_logger.LogInformation("Deleted {BatchCount} files ({CurrentCount}/{TotalCount})",
						batch.Length, newCount, deleteRequests.Count);
				}
			}

			_logger.LogInformation("Successfully deleted {Count} files", deleteCount);
		}
	}
}
