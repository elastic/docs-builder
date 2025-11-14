// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.ServiceDefaults.Telemetry;
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
	private static readonly ActivitySource ApplyStrategyActivitySource = new(TelemetryConstants.AssemblerSyncInstrumentationName);

	// Meter for OpenTelemetry metrics
	private static readonly Meter SyncMeter = new(TelemetryConstants.AssemblerSyncInstrumentationName);

	// Deployment-level metrics (low cardinality)
	private static readonly Histogram<long> FilesPerDeploymentHistogram = SyncMeter.CreateHistogram<long>(
		"docs.deployment.files.count",
		"files",
		"Number of files synced per deployment operation");

	private static readonly Counter<long> FilesAddedCounter = SyncMeter.CreateCounter<long>(
		"docs.sync.files.added.total",
		"files",
		"Total number of files added to S3");

	private static readonly Counter<long> FilesUpdatedCounter = SyncMeter.CreateCounter<long>(
		"docs.sync.files.updated.total",
		"files",
		"Total number of files updated in S3");

	private static readonly Counter<long> FilesDeletedCounter = SyncMeter.CreateCounter<long>(
		"docs.sync.files.deleted.total",
		"files",
		"Total number of files deleted from S3");

	private static readonly Histogram<long> FileSizeHistogram = SyncMeter.CreateHistogram<long>(
		"docs.sync.file.size",
		"By",
		"Distribution of file sizes synced to S3");

	private static readonly Counter<long> FilesByExtensionCounter = SyncMeter.CreateCounter<long>(
		"docs.sync.files.by_extension",
		"files",
		"File operations grouped by extension");

	private static readonly Histogram<double> SyncDurationHistogram = SyncMeter.CreateHistogram<double>(
		"docs.sync.duration",
		"s",
		"Duration of sync operations");

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
		Message = "File operation: {Operation} | Path: {FilePath} | Size: {FileSize} bytes")]
	private static partial void LogFileOperation(ILogger logger, string operation, string filePath, long fileSize);

	public async Task Apply(SyncPlan plan, Cancel ctx = default)
	{
		var sw = Stopwatch.StartNew();

		using var applyActivity = ApplyStrategyActivitySource.StartActivity("sync apply", ActivityKind.Client);
		if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true")
		{
			_ = applyActivity?.SetTag("cicd.pipeline.name", Environment.GetEnvironmentVariable("GITHUB_WORKFLOW") ?? "unknown");
			_ = applyActivity?.SetTag("cicd.pipeline.run.id", Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "unknown");
			_ = applyActivity?.SetTag("cicd.pipeline.run.attempt", Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT") ?? "unknown");
		}

		var addCount = plan.AddRequests.Count;
		var updateCount = plan.UpdateRequests.Count;
		var deleteCount = plan.DeleteRequests.Count;
		var skipCount = plan.SkipRequests.Count;
		var totalFiles = addCount + updateCount + deleteCount;

		// Add aggregate metrics to span
		_ = applyActivity?.SetTag("docs.sync.files.added", addCount);
		_ = applyActivity?.SetTag("docs.sync.files.updated", updateCount);
		_ = applyActivity?.SetTag("docs.sync.files.deleted", deleteCount);
		_ = applyActivity?.SetTag("docs.sync.files.skipped", skipCount);
		_ = applyActivity?.SetTag("docs.sync.files.total", totalFiles);

		// Record deployment-level metrics (always emit, even if 0)
		FilesPerDeploymentHistogram.Record(totalFiles);

		// Always record per-operation counts (even if 0) so metrics show consistent data
		FilesPerDeploymentHistogram.Record(addCount, [new("operation", "add")]);
		FilesPerDeploymentHistogram.Record(updateCount, [new("operation", "update")]);
		FilesPerDeploymentHistogram.Record(deleteCount, [new("operation", "delete")]);
		FilesPerDeploymentHistogram.Record(skipCount, [new("operation", "skip")]);

		_logger.LogInformation(
			"Deployment sync: {TotalFiles} files ({AddCount} added, {UpdateCount} updated, {DeleteCount} deleted, {SkipCount} skipped) in {Environment}",
			totalFiles, addCount, updateCount, deleteCount, skipCount, context.Environment.Name);

		await Upload(plan, ctx);
		await Delete(plan, ctx);

		// Record sync duration
		SyncDurationHistogram.Record(sw.Elapsed.TotalSeconds,
			[new("operation", "sync")]);
	}

	private async Task Upload(SyncPlan plan, Cancel ctx)
	{
		var uploadRequests = plan.AddRequests.Cast<UploadRequest>().Concat(plan.UpdateRequests).ToList();

		// Always create activity span (even if 0 files) for consistent tracing
		using var uploadActivity = ApplyStrategyActivitySource.StartActivity("upload files", ActivityKind.Client);
		_ = uploadActivity?.SetTag("docs.sync.upload.count", uploadRequests.Count);

		if (uploadRequests.Count > 0)
		{
			var addCount = plan.AddRequests.Count;
			var updateCount = plan.UpdateRequests.Count;

			_logger.LogInformation("Starting to process {AddCount} new files and {UpdateCount} updated files", addCount, updateCount);

			// Emit file-level metrics (low cardinality) and logs for each file
			foreach (var upload in uploadRequests)
			{
				var operation = plan.AddRequests.Contains(upload) ? "add" : "update";
				var fileSize = context.WriteFileSystem.FileInfo.New(upload.LocalPath).Length;
				var extension = Path.GetExtension(upload.DestinationPath).ToLowerInvariant();

				// Record counters
				if (operation == "add")
					FilesAddedCounter.Add(1);
				else
					FilesUpdatedCounter.Add(1);

				// Record file size distribution
				FileSizeHistogram.Record(fileSize, [new("operation", operation)]);

				// Record by extension (low cardinality)
				if (!string.IsNullOrEmpty(extension))
				{
					FilesByExtensionCounter.Add(1,
						new("operation", operation),
						new("extension", extension));
				}

				// Log individual file operations for detailed analysis
				LogFileOperation(_logger, operation, upload.DestinationPath, fileSize);
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

		// Always create activity span (even if 0 files) for consistent tracing
		using var deleteActivity = ApplyStrategyActivitySource.StartActivity("delete files", ActivityKind.Client);
		_ = deleteActivity?.SetTag("docs.sync.delete.count", deleteRequests.Count);

		if (deleteRequests.Count > 0)
		{
			_logger.LogInformation("Starting to delete {Count} files from S3 bucket {BucketName}", deleteRequests.Count, bucketName);

			// Emit file-level metrics (low cardinality) and logs for each file
			foreach (var delete in deleteRequests)
			{
				var extension = Path.GetExtension(delete.DestinationPath).ToLowerInvariant();

				// Record counter
				FilesDeletedCounter.Add(1);

				// Record by extension (low cardinality)
				if (!string.IsNullOrEmpty(extension))
				{
					FilesByExtensionCounter.Add(1,
						new("operation", "delete"),
						new("extension", extension));
				}

				// Log individual file operations for detailed analysis
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
