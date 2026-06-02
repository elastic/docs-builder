// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using Actions.Core.Services;
using Elastic.Codex;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Deploying;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

namespace Documentation.Builder.Commands.Codex;

/// <summary>Sync built codex output to S3 using a two-step plan/apply workflow.</summary>
internal sealed class CodexSyncCommand(
	IDiagnosticsCollector collector,
	ILoggerFactory logFactory,
	ICoreService githubActionsService
)
{
	/// <summary>Compute a diff of what would change when deploying to S3 and write it to a plan file.</summary>
	/// <remarks>
	/// Two-step deployment: <c>plan</c> computes the diff and writes a plan file; <c>apply</c> executes it.
	/// Review the plan before applying to avoid accidental mass deletions.
	/// </remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>
	/// <param name="s3BucketName">S3 bucket to deploy to.</param>
	/// <param name="environment">Named deployment target. Defaults to the value in <c>codex.yml</c> or the <c>ENVIRONMENT</c> env var.</param>
	/// <param name="out">Path to write the plan file. Defaults to <c>stdout</c>.</param>
	/// <param name="deleteThreshold">Abort if the plan would delete more than this percentage of objects (0–100).</param>
	[RequiresAuth]
	[MutationScope(MutationScope.Global)]
	[NoOptionsInjection]
	public async Task<int> Plan(
		GlobalCliOptions _,
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		string s3BucketName,
		string? environment = null,
		[ExpandUserProfile, RejectSymbolicLinks] FileInfo? @out = null,
		float? deleteThreshold = null,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var configFile = fs.FileInfo.New(config.FullName);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {config.FullName}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var resolvedEnvironment = environment
			?? codexConfig.Environment
			?? Environment.GetEnvironmentVariable("ENVIRONMENT")
			?? "internal";

		var context = new CodexContext(codexConfig, configFile, collector, fs, fs, null, null);
		var service = new IncrementalDeployService(logFactory, githubActionsService);
		serviceInvoker.AddCommand(service, (context, s3BucketName, @out, deleteThreshold, resolvedEnvironment),
			static async (s, collector, state, ctx) => await s.Plan(collector, state.context, state.s3BucketName, state.@out?.FullName ?? "", state.deleteThreshold, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Upload the changes described in a plan file to S3.</summary>
	/// <remarks>Run after <c>codex sync plan</c>. Applies the pre-computed diff to the S3 bucket.</remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>
	/// <param name="s3BucketName">S3 bucket to deploy to.</param>
	/// <param name="environment">Named deployment target. Defaults to the value in <c>codex.yml</c> or the <c>ENVIRONMENT</c> env var.</param>
	/// <param name="plan">Path to the plan file produced by <c>codex sync plan</c>.</param>
	[RequiresAuth]
	[CommandIntent(Intent.Destructive)]
	[MutationScope(MutationScope.Global)]
	[NoOptionsInjection]
	public async Task<int> Apply(
		GlobalCliOptions _,
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		string s3BucketName,
		string? environment = null,
		[Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "json,plan")] FileInfo? plan = null,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var configFile = fs.FileInfo.New(config.FullName);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {config.FullName}");
			return 1;
		}

		if (plan is null)
		{
			collector.EmitGlobalError("A plan file is required. Run 'codex sync plan' first and pass the output with --plan.");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var resolvedEnvironment = environment
			?? codexConfig.Environment
			?? Environment.GetEnvironmentVariable("ENVIRONMENT")
			?? "internal";

		var context = new CodexContext(codexConfig, configFile, collector, fs, fs, null, null);
		var service = new IncrementalDeployService(logFactory, githubActionsService);
		serviceInvoker.AddCommand(service, (context, s3BucketName, plan),
			static async (s, collector, state, ctx) => await s.Apply(collector, state.context, state.s3BucketName, state.plan.FullName, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
