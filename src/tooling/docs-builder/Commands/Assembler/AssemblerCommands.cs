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

internal sealed class AssembleOneShotCommand(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>Clone all repositories and build the unified documentation site in one step.</summary>
	/// <remarks>
	/// <para>
	/// The assembler clones multiple documentation repositories and builds them into a single unified site
	/// composed by a shared <c>navigation.yml</c>. This command combines <c>assembler config init</c>,
	/// <c>assembler clone</c>, and <c>assembler build</c> into a single invocation.
	/// </para>
	/// </remarks>
	/// <param name="fetchLatest">Fetch the HEAD of each branch instead of the pinned link-registry ref.</param>
	/// <param name="assumeCloned">Skip cloning; assume repositories are already on disk. Useful for iterating on the build.</param>
	/// <param name="serve">Serve the site on port 4000 after a successful build.</param>
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

/// <summary>Build a unified documentation site by composing multiple documentation sets under a shared navigation.</summary>
/// <remarks>
/// <para>
/// The assembler clones multiple documentation repositories and builds them into a single unified site.
/// A central <c>navigation.yml</c> defines the global structure, merging content from every repository
/// into one consistent navigation tree.
/// </para>
/// <para>
/// Typical workflow:
/// </para>
/// </remarks>
internal sealed class AssemblerCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>Clone all repositories listed in the assembler configuration.</summary>
	/// <remarks>
	/// Run <c>assembler config init</c> first to fetch the repository list. Clones into a local
	/// working directory; subsequent <c>assembler build</c> reads from there.
	/// </remarks>
	/// <param name="strict">Treat warnings as errors.</param>
	/// <param name="environment">Named deployment target. Determines which repositories and branches are cloned.</param>
	/// <param name="fetchLatest">Fetch the HEAD of each branch instead of the pinned link-registry ref.</param>
	/// <param name="assumeCloned">Skip cloning; assume repositories are already on disk.</param>
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

	/// <summary>Build the unified site from all previously cloned repositories.</summary>
	/// <remarks>
	/// Run after <c>assembler clone</c>. Reads every cloned repository, applies the shared <c>navigation.yml</c>,
	/// and writes the unified site to <c>.artifacts/docs/</c>.
	/// </remarks>
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

	/// <summary>Serve the output of a completed assembler build at <c>http://localhost:4000</c>.</summary>
	/// <remarks>Run after <c>assembler build</c>. Does not watch for file changes.</remarks>
	/// <param name="port">Port to listen on. Default: 4000.</param>
	/// <param name="path">Path to the built site. Defaults to <c>.artifacts/docs/</c>.</param>
	[NoOptionsInjection]
	public async Task Serve(int port = 4000, [Existing, ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? path = null, CancellationToken ct = default)
	{
		var host = new StaticWebHost(port, path?.FullName);
		await host.RunAsync(ct);
		await host.StopAsync(ct);
		await collector.StopAsync(ct);
	}
}
