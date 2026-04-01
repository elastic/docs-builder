// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Transfer;
using Elastic.Documentation.Assembler.Deploying.Synchronization;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Deploying;

public class IncrementalDeployService(
	ILoggerFactory logFactory,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	FileSystem fileSystem
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<IncrementalDeployService>();

	public async Task<bool> Plan(IDiagnosticsCollector collector, string environment, string s3BucketName, string @out, float? deleteThreshold, Cancel ctx)
	{
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fileSystem, fileSystem, null, null);
		var s3Client = new AmazonS3Client();
		var planner = new AwsS3SyncPlanStrategy(logFactory, s3Client, s3BucketName, assembleContext);
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
			await using var fileStream = new FileStream(@out, FileMode.Create, FileAccess.Write);
			await using var writer = new StreamWriter(fileStream);
			await writer.WriteAsync(output);
			_logger.LogInformation("Plan written to {OutputFile}", @out);
		}
		await githubActionsService.SetOutputAsync("plan-valid", collector.Errors == 0 ? "true" : "false");
		return collector.Errors == 0;
	}

	public async Task<bool> Apply(IDiagnosticsCollector collector, string environment, string s3BucketName, string planFile, Cancel ctx)
	{
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fileSystem, fileSystem, null, null);
		var s3Client = new AmazonS3Client();
		var transferUtility = new TransferUtility(s3Client, new TransferUtilityConfig
		{
			ConcurrentServiceRequests = Environment.ProcessorCount * 2,
			MinSizeBeforePartUpload = S3EtagCalculator.PartSize
		});
		if (!fileSystem.File.Exists(planFile))
		{
			collector.EmitError(planFile, "Plan file does not exist.");
			return false;
		}
		var planJson = await File.ReadAllTextAsync(planFile, ctx);
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
			return false;
		}
		var validator = new DocsSyncPlanValidator(logFactory);
		var validationResult = validator.Validate(plan);
		if (!validationResult.Valid)
		{
			collector.EmitError(planFile, $"Plan is invalid, {validationResult}, delete ratio: {validationResult.DeleteRatio}, remote listing completed: {plan.RemoteListingCompleted}");
			return false;
		}
		var applier = new AwsS3SyncApplyStrategy(logFactory, s3Client, transferUtility, s3BucketName, assembleContext, collector);
		await applier.Apply(plan, ctx);
		return true;
	}
}
