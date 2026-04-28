// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Documentation.Builder.Http;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Assembler;

/// <summary>
/// Full assembler pipeline in one shot: init configuration, clone all repositories, then build assembled documentation.
/// </summary>
/// <remarks>
/// Hoisted to the root scope via <c>app.Map&lt;AssembleOneShotCommand&gt;()</c> as the <c>assemble</c> command.
/// </remarks>
internal sealed class AssembleOneShotCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>
	/// Full assembler pipeline: init configuration, clone all repositories, then build assembled documentation.
	/// </summary>
	/// <remarks>
	/// <para>Equivalent to running <c>assembler config init</c>, <c>assembler clone</c>, and <c>assembler build</c> in sequence.</para>
	/// <code>
	/// docs-builder assemble
	/// docs-builder assemble --environment staging --fetch-latest --exporters Html --exporters Elasticsearch
	/// docs-builder assemble --serve
	/// </code>
	/// </remarks>
	/// <param name="fetchLatest">Fetch the latest commit of the branch instead of the link registry entry ref</param>
	/// <param name="assumeCloned">Assume the repository folder already exists on disk (skip clone); primarily used for testing</param>
	/// <param name="serve">Serve the documentation on port 4000 after a successful build</param>
	[CommandName("assemble")]
	public async Task<int> Assemble(
		GlobalCliOptions _,
		[AsParameters] AssemblerBuildOptions buildOptions,
		bool? fetchLatest = null,
		bool? assumeCloned = null,
		bool serve = false,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var cloneOptions = new AssemblerCloneOptions
		{
			Strict = buildOptions.Strict,
			Environment = buildOptions.Environment,
			FetchLatest = fetchLatest,
			AssumeCloned = assumeCloned
		};
		var cloneService = new AssemblerCloneService(logFactory, assemblyConfiguration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(cloneService, cloneOptions, buildOptions.Strict ?? false,
			static async (s, col, opts, ctx) => await s.CloneAll(col, opts, ctx)
		);

		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		var buildService = new AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, environmentVariables);
		serviceInvoker.AddCommand(buildService, (buildOptions, readFs, writeFs), buildOptions.Strict ?? false,
			static async (s, col, state, ctx) => await s.BuildAll(col, state.buildOptions, state.readFs, state.writeFs, ctx)
		);
		var result = await serviceInvoker.InvokeAsync(ct);

		if (serve && result == 0)
		{
			var host = new StaticWebHost(4000, null);
			await host.RunAsync(ct);
			await host.StopAsync(ct);
		}

		return result;
	}
}

internal sealed class AssemblerCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>Clone all repositories configured in the assembler.</summary>
	/// <param name="strict">Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment">The environment to clone for</param>
	/// <param name="fetchLatest">Fetch the latest commit of the branch instead of the link registry entry ref</param>
	/// <param name="assumeCloned">Assume repositories are already cloned; primarily used for testing</param>
	[NoOptionsInjection]
	public async Task<int> Clone(
		bool? strict = null,
		string? environment = null,
		bool? fetchLatest = null,
		bool? assumeCloned = null,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var options = new AssemblerCloneOptions
		{
			Strict = strict, Environment = environment,
			FetchLatest = fetchLatest, AssumeCloned = assumeCloned
		};
		var service = new AssemblerCloneService(logFactory, assemblyConfiguration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(service, options, strict ?? false,
			static async (s, col, opts, ctx) => await s.CloneAll(col, opts, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Build all cloned repositories into assembled documentation.</summary>
	[NoOptionsInjection]
	public async Task<int> Build(
		[AsParameters] AssemblerBuildOptions options,
		CancellationToken ct = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		var service = new AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, environmentVariables);
		serviceInvoker.AddCommand(service, (options, readFs, writeFs), options.Strict ?? false,
			static async (s, col, state, ctx) => await s.BuildAll(col, state.options, state.readFs, state.writeFs, ctx)
		);
		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Serve the output of an assembler build at <c>http://localhost:4000</c>.</summary>
	/// <param name="port">Port to serve the documentation. Default: 4000</param>
	/// <param name="path">Path to the built documentation. Defaults to the standard assembler output</param>
	[NoOptionsInjection]
	public async Task Serve(int port = 4000, string? path = null, CancellationToken ct = default)
	{
		var host = new StaticWebHost(port, path);
		await host.RunAsync(ct);
		await host.StopAsync(ct);
		await collector.StopAsync(ct);
	}
}
