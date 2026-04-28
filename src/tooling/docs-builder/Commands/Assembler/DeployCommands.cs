// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Deploying;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

/// <summary>Deploy built documentation to S3 and update CloudFront redirect rules.</summary>
internal sealed class DeployCommands(
	AssemblyConfiguration assemblyConfiguration,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ILoggerFactory logFactory,
	ICoreService githubActionsService
)
{
	/// <summary>Compute a diff of what would change when deploying to S3 and write it to a plan file.</summary>
	/// <remarks>
	/// Two-step deployment: <c>plan</c> computes the diff and writes a plan file; <c>apply</c> executes it.
	/// Review the plan before applying to avoid accidental mass deletions.
	/// </remarks>
	/// <param name="environment">Named deployment target.</param>
	/// <param name="s3BucketName">S3 bucket to deploy to.</param>
	/// <param name="out">Path to write the plan file. Defaults to <c>stdout</c>.</param>
	/// <param name="deleteThreshold">Abort if the plan would delete more than this percentage of objects (0–100).</param>
	[NoOptionsInjection]
	public async Task<int> Plan(string environment, string s3BucketName, string @out = "", float? deleteThreshold = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new IncrementalDeployService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, FileSystemFactory.RealRead, FileSystemFactory.RealWrite);
		serviceInvoker.AddCommand(service, (environment, s3BucketName, @out, deleteThreshold),
			static async (s, collector, state, ctx) => await s.Plan(collector, state.environment, state.s3BucketName, state.@out, state.deleteThreshold, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Upload the changes described in a plan file to S3.</summary>
	/// <remarks>Run after <c>assembler deploy plan</c>. Applies the pre-computed diff to the S3 bucket.</remarks>
	/// <param name="environment">Named deployment target.</param>
	/// <param name="s3BucketName">S3 bucket to deploy to.</param>
	/// <param name="planFile">Path to the plan file produced by <c>assembler deploy plan</c>.</param>
	[NoOptionsInjection]
	public async Task<int> Apply(string environment, string s3BucketName, string planFile, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new IncrementalDeployService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, FileSystemFactory.RealRead, FileSystemFactory.RealWrite);
		serviceInvoker.AddCommand(service, (environment, s3BucketName, planFile),
			static async (s, collector, state, ctx) => await s.Apply(collector, state.environment, state.s3BucketName, state.planFile, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Push the redirects mapping to CloudFront's KeyValueStore.</summary>
	/// <remarks>Run after <c>assembler build</c> produces a <c>redirects.json</c>.</remarks>
	/// <param name="environment">Named deployment target.</param>
	/// <param name="redirectsFile">Path to <c>redirects.json</c>. Defaults to <c>.artifacts/docs/redirects.json</c>.</param>
	[NoOptionsInjection]
	public async Task<int> UpdateRedirects(string environment, string? redirectsFile = null, CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = FileSystemFactory.RealRead;
		var service = new DeployUpdateRedirectsService(logFactory, fs);
		serviceInvoker.AddCommand(service, (environment, redirectsFile),
			static async (s, collector, state, ctx) => await s.UpdateRedirects(collector, state.environment, state.redirectsFile, ctx: ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}
}
