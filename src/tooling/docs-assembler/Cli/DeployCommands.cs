// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.Json;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Transfer;
using ConsoleAppFramework;
using Documentation.Assembler.Deploying;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Serialization;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class DeployCommands(
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ILoggerFactory logFactory,
	ICoreService githubActionsService
)
{
	private readonly ILogger<Program> _logger = logFactory.CreateLogger<Program>();

	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		ConsoleApp.Log = msg => _logger.LogInformation(msg);
		ConsoleApp.LogError = msg => _logger.LogError(msg);
	}

	/// <summary> Creates a sync plan </summary>
	/// <param name="environment"> The environment to build</param>
	/// <param name="s3BucketName">The S3 bucket name to deploy to</param>
	/// <param name="out"> The file to write the plan to</param>
	/// <param name="deleteThreshold"> The percentage of deletions allowed in the plan as percentage of total files to sync</param>
	/// <param name="ctx"></param>
	public async Task<int> Plan(
		string environment,
		string s3BucketName,
		string @out = "",
		float deleteThreshold = 0.2f,
		Cancel ctx = default
	)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);
		var fs = new FileSystem();
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fs, fs, null, null);
		var s3Client = new AmazonS3Client();
		IDocsSyncPlanStrategy planner = new AwsS3SyncPlanStrategy(logFactory, s3Client, s3BucketName, assembleContext);
		var plan = await planner.Plan(ctx);
		_logger.LogInformation("Total files to sync: {TotalFiles}", plan.TotalSyncRequests);
		_logger.LogInformation("Total files to delete: {DeleteCount}", plan.DeleteRequests.Count);
		_logger.LogInformation("Total files to add: {AddCount}", plan.AddRequests.Count);
		_logger.LogInformation("Total files to update: {UpdateCount}", plan.UpdateRequests.Count);
		_logger.LogInformation("Total files to skip: {SkipCount}", plan.SkipRequests.Count);
		_logger.LogInformation("Total local source files: {TotalSourceFiles}", plan.TotalSourceFiles);
		_logger.LogInformation("Total remote source files: {TotalSourceFiles}", plan.TotalRemoteFiles);
		var validationResult = planner.Validate(plan, deleteThreshold);
		if (!validationResult.Valid)
		{
			await githubActionsService.SetOutputAsync("plan-valid", "false");
			collector.EmitError(@out, $"Plan is invalid, delete ratio: {validationResult.DeleteRatio}, threshold: {validationResult.DeleteThreshold} over {plan.TotalSyncRequests:N0} files while plan has {plan.DeleteRequests:N0} deletions");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}

		if (!string.IsNullOrEmpty(@out))
		{
			var output = SyncPlan.Serialize(plan);
			await using var fileStream = new FileStream(@out, FileMode.Create, FileAccess.Write);
			await using var writer = new StreamWriter(fileStream);
			await writer.WriteAsync(output);
			ConsoleApp.Log("Plan written to " + @out);
		}
		await collector.StopAsync(ctx);
		await githubActionsService.SetOutputAsync("plan-valid", collector.Errors == 0 ? "true" : "false");
		return collector.Errors;
	}

	/// <summary> Applies a sync plan </summary>
	/// <param name="environment"> The environment to build</param>
	/// <param name="s3BucketName">The S3 bucket name to deploy to</param>
	/// <param name="planFile">The path to the plan file to apply</param>
	/// <param name="ctx"></param>
	public async Task<int> Apply(
		string environment,
		string s3BucketName,
		string planFile,
		Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);
		var fs = new FileSystem();
		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, fs, fs, null, null);
		var s3Client = new AmazonS3Client();
		var transferUtility = new TransferUtility(s3Client, new TransferUtilityConfig
		{
			ConcurrentServiceRequests = Environment.ProcessorCount * 2,
			MinSizeBeforePartUpload = S3EtagCalculator.PartSize
		});
		IDocsSyncApplyStrategy applier = new AwsS3SyncApplyStrategy(logFactory, s3Client, transferUtility, s3BucketName, assembleContext, collector);
		if (!File.Exists(planFile))
		{
			collector.EmitError(planFile, "Plan file does not exist.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}
		var planJson = await File.ReadAllTextAsync(planFile, ctx);
		var plan = SyncPlan.Deserialize(planJson);
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
			await collector.StopAsync(ctx);
			return collector.Errors;
		}
		await applier.Apply(plan, ctx);
		await collector.StopAsync(ctx);
		return collector.Errors;
	}

	/// <summary>Refreshes the redirects mapping in Cloudfront's KeyValueStore</summary>
	/// <param name="environment">The environment to build</param>
	/// <param name="redirectsFile">Path to the redirects mapping pre-generated by docs-assembler</param>
	/// <param name="ctx"></param>
	[Command("update-redirects")]
	public async Task<int> UpdateRedirects(
		string environment,
		string redirectsFile = ".artifacts/assembly/redirects.json",
		Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);

		if (!File.Exists(redirectsFile))
		{
			collector.EmitError(redirectsFile, "Redirects mapping does not exist.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}

		ConsoleApp.Log("Parsing redirects mapping");
		var jsonContent = await File.ReadAllTextAsync(redirectsFile, ctx);
		var sourcedRedirects = JsonSerializer.Deserialize(jsonContent, SourceGenerationContext.Default.DictionaryStringString);

		if (sourcedRedirects is null)
		{
			collector.EmitError(redirectsFile, "Redirects mapping is invalid.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}

		var kvsName = $"elastic-docs-v3-{environment}-redirects-kvs";
		var cloudFrontClient = new AwsCloudFrontKeyValueStoreProxy(collector, new FileSystem().DirectoryInfo.New(Directory.GetCurrentDirectory()));

		cloudFrontClient.UpdateRedirects(kvsName, sourcedRedirects);

		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
