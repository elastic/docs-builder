// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using Amazon.S3;
using Amazon.S3.Transfer;
using ConsoleAppFramework;
using Documentation.Assembler.Deploying;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class DeployCommands(ILoggerFactory logger, ICoreService githubActionsService)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	/// <summary> Creates a sync plan </summary>
	/// <param name="environment"> The environment to build</param>
	/// <param name="s3BucketName">The S3 bucket name to deploy to</param>
	/// <param name="out"> The file to write the plan to</param>
	/// <param name="ctx"></param>
	public async Task<int> Plan(
		string environment, string s3BucketName, string @out = "", Cancel ctx = default)
	{
		AssignOutputLogger();
		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);
		var assembleContext = new AssembleContext(environment, collector, new FileSystem(), new FileSystem(), null, null);
		var s3Client = new AmazonS3Client();
		IDocsSyncPlanStrategy planner = new AwsS3SyncPlanStrategy(s3Client, s3BucketName, assembleContext, logger);
		var plan = await planner.Plan(ctx);
		ConsoleApp.Log("Total files to sync: " + plan.Count);
		ConsoleApp.Log("Total files to delete: " + plan.DeleteRequests.Count);
		ConsoleApp.Log("Total files to add: " + plan.AddRequests.Count);
		ConsoleApp.Log("Total files to update: " + plan.UpdateRequests.Count);
		ConsoleApp.Log("Total files to skip: " + plan.SkipRequests.Count);
		if (!string.IsNullOrEmpty(@out))
		{
			var output = SyncPlan.Serialize(plan);
			await using var fileStream = new FileStream(@out, FileMode.Create, FileAccess.Write);
			await using var writer = new StreamWriter(fileStream);
			await writer.WriteAsync(output);
			ConsoleApp.Log("Plan written to " + @out);
		}
		await collector.StopAsync(ctx);
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
		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService)
		{
			NoHints = true
		}.StartAsync(ctx);
		var assembleContext = new AssembleContext(environment, collector, new FileSystem(), new FileSystem(), null, null);
		var s3Client = new AmazonS3Client();
		var transferUtility = new TransferUtility(s3Client, new TransferUtilityConfig
		{
			ConcurrentServiceRequests = Environment.ProcessorCount * 2,
			MinSizeBeforePartUpload = AwsS3SyncPlanStrategy.PartSize
		});
		IDocsSyncApplyStrategy applier = new AwsS3SyncApplyStrategy(s3Client, transferUtility, s3BucketName, assembleContext, logger, collector);
		if (!File.Exists(planFile))
		{
			collector.EmitError(planFile, "Plan file does not exist.");
			await collector.StopAsync(ctx);
			return collector.Errors;
		}
		var planJson = await File.ReadAllTextAsync(planFile, ctx);
		var plan = SyncPlan.Deserialize(planJson);
		await applier.Apply(plan, ctx);
		await collector.StopAsync(ctx);
		return collector.Errors;
	}
}
