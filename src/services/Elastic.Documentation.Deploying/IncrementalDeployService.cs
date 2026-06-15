// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Transfer;
using Elastic.Documentation.Deploying.Synchronization;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Integrations.S3;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Deploying;

public class IncrementalDeployService(
	ILoggerFactory logFactory,
	ICoreService githubActionsService,
	IAmazonS3? s3Client = null,
	ITransferUtility? transferUtility = null,
	IS3EtagCalculator? etagCalculator = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<IncrementalDeployService>();
	private readonly IAmazonS3 _s3 = s3Client ?? new AmazonS3Client();

	public async Task<bool> Plan(IDiagnosticsCollector collector, IDocsSyncContext context, string s3BucketName, string @out, float? deleteThreshold, Cancel ctx)
	{
		var planner = new AwsS3SyncPlanStrategy(logFactory, _s3, s3BucketName, context, etagCalculator);
		var plan = await planner.Plan(deleteThreshold, ctx);
		_logger.LogInformation("Remote listing completed: {RemoteListingCompleted}", plan.RemoteListingCompleted);
		_logger.LogInformation("Total files to delete: {DeleteCount}", plan.DeleteRequests.Count);
		_logger.LogInformation("Total files to add: {AddCount}", plan.AddRequests.Count);
		_logger.LogInformation("Total files to update: {UpdateCount}", plan.UpdateRequests.Count);
		_logger.LogInformation("Total files to skip: {SkipCount}", plan.SkipRequests.Count);
		_logger.LogInformation("Total local source files: {TotalSourceFiles}", plan.TotalSourceFiles);
		_logger.LogInformation("Total remote source files: {TotalSourceFiles}", plan.TotalRemoteFiles);
		var validator = new DocsSyncPlanValidator(logFactory);
		var validationResult = validator.Validate(plan);
		if (!validationResult.Valid)
		{
			await githubActionsService.SetOutputAsync("plan-valid", "false");
			collector.EmitError(@out, $"Plan is invalid, {validationResult}, delete ratio: {validationResult.DeleteRatio}, remote listing completed: {plan.RemoteListingCompleted}");
			return false;
		}

		if (!string.IsNullOrEmpty(@out))
		{
			var output = SyncPlan.Serialize(plan);
			await using var fileStream = context.WriteFileSystem.File.Create(@out);
			await using var writer = new StreamWriter(fileStream);
			await writer.WriteAsync(output);
			_logger.LogInformation("Plan written to {OutputFile}", @out);
		}
		await githubActionsService.SetOutputAsync("plan-valid", collector.Errors == 0 ? "true" : "false");
		return collector.Errors == 0;
	}

	public async Task<bool> Apply(IDiagnosticsCollector collector, IDocsSyncContext context, string s3BucketName, string planFile, Cancel ctx)
	{
		var xfer = transferUtility ?? new TransferUtility(_s3, new TransferUtilityConfig
		{
			ConcurrentServiceRequests = Environment.ProcessorCount * 2,
			MinSizeBeforePartUpload = S3EtagCalculator.PartSize
		});
		if (!context.ReadFileSystem.File.Exists(planFile))
		{
			collector.EmitError(planFile, "Plan file does not exist.");
			return false;
		}
		var planJson = await context.ReadFileSystem.File.ReadAllTextAsync(planFile, ctx);
		var plan = SyncPlan.Deserialize(planJson);
		_logger.LogInformation("Remote listing completed: {RemoteListingCompleted}", plan.RemoteListingCompleted);
		_logger.LogInformation("Total files to sync: {TotalFiles}", plan.TotalSyncRequests);
		_logger.LogInformation("Total files to delete: {DeleteCount}", plan.DeleteRequests.Count);
		_logger.LogInformation("Total files to add: {AddCount}", plan.AddRequests.Count);
		_logger.LogInformation("Total files to update: {UpdateCount}", plan.UpdateRequests.Count);
		_logger.LogInformation("Total files to skip: {SkipCount}", plan.SkipRequests.Count);
		_logger.LogInformation("Total local source files: {TotalSourceFiles}", plan.TotalSourceFiles);
		_logger.LogInformation("Total remote source files: {TotalSourceFiles}", plan.TotalRemoteFiles);
		if (plan.TotalSyncRequests == 0)
		{
			_logger.LogInformation("Plan has no files to sync, skipping incremental synchronization");
			return true;
		}
		var validator = new DocsSyncPlanValidator(logFactory);
		var validationResult = validator.Validate(plan);
		if (!validationResult.Valid)
		{
			collector.EmitError(planFile, $"Plan is invalid, {validationResult}, delete ratio: {validationResult.DeleteRatio}, remote listing completed: {plan.RemoteListingCompleted}");
			return false;
		}
		var applier = new AwsS3SyncApplyStrategy(logFactory, _s3, xfer, s3BucketName, context, collector);
		await applier.Apply(plan, ctx);
		return true;
	}
}
