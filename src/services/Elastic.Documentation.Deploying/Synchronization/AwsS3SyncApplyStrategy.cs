// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.ServiceDefaults.Telemetry;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying.Synchronization;

public partial class AwsS3SyncApplyStrategy(
	ILoggerFactory logFactory,
	IAmazonS3 s3Client,
	ITransferUtility transferUtility,
	string bucketName,
	IDocsSyncContext context,
	IDiagnosticsCollector collector
) : IDocsSyncApplyStrategy
{
	private static readonly ActivitySource ApplyStrategyActivitySource = new(TelemetryConstants.AssemblerSyncInstrumentationName);

	// Meter for OpenTelemetry metrics
	private static readonly Meter SyncMeter = new(TelemetryConstants.AssemblerSyncInstrumentationName);
	private const int DefaultUploadConcurrency = 32;
	private const int MaxUploadConcurrency = 128;
	private const int DefaultDeleteConcurrency = 4;
	private const int MaxDeleteConcurrency = 16;
	private const string UploadConcurrencyEnvironmentVariable = "DOCS_SYNC_UPLOAD_CONCURRENCY";
	private const string DeleteConcurrencyEnvironmentVariable = "DOCS_SYNC_DELETE_CONCURRENCY";

	// Deployment-level metrics (histograms for distribution analysis, counters for totals)
	// Note: Histograms require delta temporality to work with Elasticsearch
	// See Extensions.cs where MetricTemporalityPreference.Delta is configured
	private static readonly Histogram<double> FilesPerDeploymentHistogram = SyncMeter.CreateHistogram<double>(
		"docs.deployment.files.count",
		"files",
		"Number of files per deployment operation (added + updated + deleted + skipped)");

	private static readonly Counter<double> FilesTotalCounter = SyncMeter.CreateCounter<double>(
		"docs.deployment.files.total",
		"files",
		"Total number of files in deployment (added + updated + deleted + skipped)");

	private static readonly Counter<double> FilesAddedCounter = SyncMeter.CreateCounter<double>(
		"docs.sync.files.added.total",
		"files",
		"Total number of files added to S3");

	private static readonly Counter<double> FilesUpdatedCounter = SyncMeter.CreateCounter<double>(
		"docs.sync.files.updated.total",
		"files",
		"Total number of files updated in S3");

	private static readonly Counter<double> FilesDeletedCounter = SyncMeter.CreateCounter<double>(
		"docs.sync.files.deleted.total",
		"files",
		"Total number of files deleted from S3");

	private static readonly Counter<double> FilesSkippedCounter = SyncMeter.CreateCounter<double>(
		"docs.sync.files.skipped.total",
		"files",
		"Total number of files skipped (unchanged)");

	private static readonly Histogram<double> FileSizeHistogram = SyncMeter.CreateHistogram<double>(
		"docs.sync.file.size",
		"By",
		"Distribution of file sizes synced to S3");

	private static readonly Counter<double> FilesByExtensionCounter = SyncMeter.CreateCounter<double>(
		"docs.sync.files.by_extension",
		"files",
		"File operations grouped by extension");

	private static readonly Histogram<double> SyncDurationHistogram = SyncMeter.CreateHistogram<double>(
		"docs.sync.duration",
		"s",
		"Duration of sync operations");

	private readonly ILogger<AwsS3SyncApplyStrategy> _logger = logFactory.CreateLogger<AwsS3SyncApplyStrategy>();

	private void DisplayUploadProgress(object? sender, UploadProgressArgs args) => LogUploadProgress(_logger, args);

	[LoggerMessage(
		EventId = 4,
		Level = LogLevel.Debug,
		Message = "{Args}")]
	private static partial void LogUploadProgress(ILogger logger, UploadProgressArgs args);

	[LoggerMessage(
		EventId = 3,
		Level = LogLevel.Information,
		Message = "File operation: {Operation} | Path: {FilePath} | Size: {FileSize} bytes")]
	private static partial void LogFileOperation(ILogger logger, string operation, string filePath, long fileSize);

	public async Task<bool> Apply(SyncPlan plan, Cancel ctx = default)
	{
		var sw = Stopwatch.StartNew();
		var errorsBeforeApply = collector.Errors;

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
		var totalFiles = addCount + updateCount + deleteCount + skipCount;

		// Add aggregate metrics to span
		_ = applyActivity?.SetTag("docs.sync.files.added", addCount);
		_ = applyActivity?.SetTag("docs.sync.files.updated", updateCount);
		_ = applyActivity?.SetTag("docs.sync.files.deleted", deleteCount);
		_ = applyActivity?.SetTag("docs.sync.files.skipped", skipCount);
		_ = applyActivity?.SetTag("docs.sync.files.total", totalFiles);

		// Record deployment-level metrics (always emit, even if 0)
		// Histogram for distribution analysis (p50, p95, p99)
		FilesPerDeploymentHistogram.Record(totalFiles);

		// Record per-operation histograms (for distribution analysis by operation type)
		FilesPerDeploymentHistogram.Record(addCount, [new("operation", "add")]);
		FilesPerDeploymentHistogram.Record(updateCount, [new("operation", "update")]);
		FilesPerDeploymentHistogram.Record(deleteCount, [new("operation", "delete")]);
		FilesPerDeploymentHistogram.Record(skipCount, [new("operation", "skip")]);

		// Counter for simple totals and rates
		FilesTotalCounter.Add(totalFiles);

		// Record counter versions for easy dashboard queries (always emit, even if 0)
		FilesAddedCounter.Add(addCount);
		FilesUpdatedCounter.Add(updateCount);
		FilesDeletedCounter.Add(deleteCount);
		FilesSkippedCounter.Add(skipCount);

		_logger.LogInformation(
			"Deployment sync: {TotalFiles} files ({AddCount} added, {UpdateCount} updated, {DeleteCount} deleted, {SkipCount} skipped) in {Environment}",
			totalFiles, addCount, updateCount, deleteCount, skipCount, context.EnvironmentName);

		await Upload(plan, ctx);
		await Delete(plan, ctx);

		// Record sync duration (both histogram for distribution and counter for total)
		SyncDurationHistogram.Record(sw.Elapsed.TotalSeconds);

		return collector.Errors == errorsBeforeApply;
	}

	private async Task Upload(SyncPlan plan, Cancel ctx)
	{
		var sw = Stopwatch.StartNew();
		var uploadedCount = 0;
		var uploadRequests = plan.AddRequests.Cast<UploadRequest>().Concat(plan.UpdateRequests).ToList();
		var uploadConcurrency = GetConcurrency(UploadConcurrencyEnvironmentVariable, DefaultUploadConcurrency, MaxUploadConcurrency);

		// Always create activity span (even if 0 files) for consistent tracing
		using var uploadActivity = ApplyStrategyActivitySource.StartActivity("upload files", ActivityKind.Client);
		_ = uploadActivity?.SetTag("docs.sync.upload.count", uploadRequests.Count);
		_ = uploadActivity?.SetTag("docs.sync.upload.concurrency", uploadConcurrency);

		if (uploadRequests.Count > 0)
		{
			var addCount = plan.AddRequests.Count;
			var updateCount = plan.UpdateRequests.Count;

			_logger.LogInformation("Starting to process {AddCount} new files and {UpdateCount} updated files", addCount, updateCount);

			var addPaths = plan.AddRequests.Select(r => r.LocalPath).ToHashSet();
			_logger.LogInformation("Uploading {Count} files to S3 bucket {BucketName} with concurrency {Concurrency}", uploadRequests.Count, bucketName, uploadConcurrency);
			await Parallel.ForEachAsync(uploadRequests, new ParallelOptions
			{
				CancellationToken = ctx,
				MaxDegreeOfParallelism = uploadConcurrency
			}, async (upload, token) =>
			{
				var operation = addPaths.Contains(upload.LocalPath) ? "add" : "update";
				var fileSize = context.WriteFileSystem.FileInfo.New(upload.LocalPath).Length;
				var extension = Path.GetExtension(upload.DestinationPath).ToLowerInvariant();

				FileSizeHistogram.Record(fileSize);
				if (!string.IsNullOrEmpty(extension))
					FilesByExtensionCounter.Add(1, new("operation", operation), new("extension", extension));
				LogFileOperation(_logger, operation, upload.DestinationPath, fileSize);

				var request = new TransferUtilityUploadRequest
				{
					BucketName = bucketName,
					FilePath = upload.LocalPath,
					Key = upload.DestinationPath,
					PartSize = S3EtagCalculator.PartSize
				};
				request.UploadProgressEvent += DisplayUploadProgress;
				try
				{
					await transferUtility.UploadAsync(request, token);
					_ = Interlocked.Increment(ref uploadedCount);
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					_logger.LogError(ex, "Failed to upload {LocalPath} to s3://{BucketName}/{DestinationPath}", upload.LocalPath, bucketName, upload.DestinationPath);
					collector.EmitError(upload.LocalPath, $"Failed to upload to s3://{bucketName}/{upload.DestinationPath}", ex);
				}
			});
			_logger.LogInformation("Finished uploading {UploadedCount}/{Count} files ({AddCount} added, {UpdateCount} updated) in {ElapsedMs}ms",
				uploadedCount, uploadRequests.Count, addCount, updateCount, sw.ElapsedMilliseconds);
		}
		_ = uploadActivity?.SetTag("docs.sync.upload.duration_ms", sw.Elapsed.TotalMilliseconds);
	}

	private async Task Delete(SyncPlan plan, Cancel ctx)
	{
		var sw = Stopwatch.StartNew();
		var deleteCount = 0;
		var deleteRequests = plan.DeleteRequests;
		var deleteConcurrency = GetConcurrency(DeleteConcurrencyEnvironmentVariable, DefaultDeleteConcurrency, MaxDeleteConcurrency);

		// Always create activity span (even if 0 files) for consistent tracing
		using var deleteActivity = ApplyStrategyActivitySource.StartActivity("delete files", ActivityKind.Client);
		_ = deleteActivity?.SetTag("docs.sync.delete.count", deleteRequests.Count);
		_ = deleteActivity?.SetTag("docs.sync.delete.concurrency", deleteConcurrency);

		if (deleteRequests.Count > 0)
		{
			_logger.LogInformation("Starting to delete {Count} files from S3 bucket {BucketName} with concurrency {Concurrency}", deleteRequests.Count, bucketName, deleteConcurrency);

			// Emit file-level metrics (low cardinality) and logs for each file
			foreach (var delete in deleteRequests)
			{
				var extension = Path.GetExtension(delete.DestinationPath).ToLowerInvariant();

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
			await Parallel.ForEachAsync(deleteRequests.Chunk(1000), new ParallelOptions
			{
				CancellationToken = ctx,
				MaxDegreeOfParallelism = deleteConcurrency
			}, async (batch, token) =>
			{
				var deleteObjectsRequest = new DeleteObjectsRequest
				{
					BucketName = bucketName,
					Objects = batch.Select(d => new KeyVersion
					{
						Key = d.DestinationPath
					}).ToList()
				};
				try
				{
					var response = await s3Client.DeleteObjectsAsync(deleteObjectsRequest, token);
					if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
					{
						_logger.LogError("Delete batch failed with status code {StatusCode}", response.HttpStatusCode);
						if (response.DeleteErrors is null or { Count: 0 })
							collector.EmitGlobalError($"Delete batch failed with status code {response.HttpStatusCode}");
						foreach (var error in response.DeleteErrors ?? Enumerable.Empty<DeleteError>())
						{
							_logger.LogError("Failed to delete {Key}: {Message}", error.Key, error.Message);
							collector.EmitError(error.Key, $"Failed to delete: {error.Message}");
						}
					}
					else
					{
						var currentCount = Interlocked.Add(ref deleteCount, batch.Length);
						_logger.LogInformation("Deleted {BatchCount} files ({CurrentCount}/{TotalCount})",
							batch.Length, currentCount, deleteRequests.Count);
					}
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					_logger.LogError(ex, "Failed to delete batch from S3 bucket {BucketName}", bucketName);
					foreach (var delete in batch)
						collector.EmitError(delete.DestinationPath, "Failed to delete from S3", ex);
				}
			});

			_logger.LogInformation("Finished deleting {DeletedCount}/{Count} files in {ElapsedMs}ms", deleteCount, deleteRequests.Count, sw.ElapsedMilliseconds);
		}
		_ = deleteActivity?.SetTag("docs.sync.delete.duration_ms", sw.Elapsed.TotalMilliseconds);
	}

	private static int GetConcurrency(string environmentVariable, int defaultValue, int maxValue)
	{
		var configured = Environment.GetEnvironmentVariable(environmentVariable);
		return int.TryParse(configured, out var parsed) && parsed > 0
			? Math.Min(parsed, maxValue)
			: defaultValue;
	}
}
