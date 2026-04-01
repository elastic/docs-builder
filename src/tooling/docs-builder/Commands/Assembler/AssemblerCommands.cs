// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Arguments;
using Documentation.Builder.Http;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Assembler;

internal sealed class AssembleCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary> Do a full assembler clone and assembler build in one swoop</summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="fetchLatest"> If true, fetch the latest commit of the branch instead of the link registry entry ref</param>
	/// <param name="assumeCloned"> If true, assume the repository folder already exists on disk assume it's cloned already, primarily used for testing</param>
	/// <param name="assumeBuild"> If true, assume the build output already exists and skip building if index.html exists, primarily used for testing</param>
	/// <param name="metadataOnly"> Only emit documentation metadata to output, ignored if 'exporters' is also set </param>
	/// <param name="showHints"> Show hints from all documentation sets during assembler build</param>
	/// <param name="exporters"> Set available exporters:
	///					html, es, config, links, state, llm, redirect, metadata, none.
	///					Defaults to (html, config, links, state, redirect) or 'default'.
	/// </param>
	/// <param name="serve"> Serve the documentation on port 4000 after succesful build</param>
	/// <param name="ctx"></param>
	[Command("")]
	public async Task<int> CloneAndBuild(
		bool? strict = null,
		string? environment = null,
		bool? fetchLatest = null,
		bool? assumeCloned = null,
		bool? assumeBuild = null,
		bool? metadataOnly = null,
		bool? showHints = null,
		[ExporterParser] IReadOnlySet<Exporter>? exporters = null,
		bool serve = false,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var cloneService = new AssemblerCloneService(logFactory, assemblyConfiguration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(cloneService, (strict, environment, fetchLatest, assumeCloned, ctx), strict ?? false,
			static async (s, collector, state, ctx) => await s.CloneAll(collector, state.strict, state.environment, state.fetchLatest, state.assumeCloned, ctx)
		);

		var buildService = new AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, environmentVariables);
		var fs = new FileSystem();
		serviceInvoker.AddCommand(buildService, (strict, environment, metadataOnly, showHints, exporters, assumeBuild, fs), strict ?? false,
			static async (s, collector, state, ctx) =>
				await s.BuildAll(collector, state.strict, state.environment, state.metadataOnly, state.showHints, state.exporters, state.assumeBuild, state.fs, ctx)
		);
		var result = await serviceInvoker.InvokeAsync(ctx);

		if (serve && result == 0)
		{
			var host = new StaticWebHost(4000, null);
			await host.RunAsync(ctx);
			await host.StopAsync(ctx);
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
	/// <summary> Clones all repositories </summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="fetchLatest"> If true, fetch the latest commit of the branch instead of the link registry entry ref</param>
	/// <param name="assumeCloned"> If true, assume the repository folder already exists on disk assume it's cloned already, primarily used for testing</param>
	/// <param name="ctx"></param>
	[Command("clone")]
	public async Task<int> CloneAll(
		bool? strict = null,
		string? environment = null,
		bool? fetchLatest = null,
		bool? assumeCloned = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var service = new AssemblerCloneService(logFactory, assemblyConfiguration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(service, (strict, environment, fetchLatest, assumeCloned, ctx), strict ?? false,
			static async (s, collector, state, ctx) => await s.CloneAll(collector, state.strict, state.environment, state.fetchLatest, state.assumeCloned, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}


	/// <summary> Builds all repositories </summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="assumeBuild"> If true, assume the build output already exists and skip building if index.html exists, primarily used for testing</param>
	/// <param name="metadataOnly"> Only emit documentation metadata to output, ignored if 'exporters' is also set </param>
	/// <param name="showHints"> Show hints from all documentation sets during assembler build</param>
	/// <param name="exporters"> Set available exporters:
	///					html, es, config, links, state, llm, redirect, metadata, none.
	///					Defaults to (html, config, links, state, redirect) or 'default'.
	/// </param>
	/// <param name="ctx"></param>
	[Command("build")]
	public async Task<int> BuildAll(
		bool? strict = null,
		string? environment = null,
		bool? assumeBuild = null,
		bool? metadataOnly = null,
		bool? showHints = null,
		[ExporterParser] IReadOnlySet<Exporter>? exporters = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = new FileSystem();
		var service = new AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, environmentVariables);
		serviceInvoker.AddCommand(service, (strict, environment, assumeBuild, metadataOnly, showHints, exporters, fs), strict ?? false,
			static async (s, collector, state, ctx) =>
				await s.BuildAll(collector, state.strict, state.environment, state.metadataOnly, state.showHints, state.exporters, state.assumeBuild, state.fs, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary> Serve the output of an assembler build</summary>
	/// <param name="port">Port to serve the documentation.</param>
	/// <param name="ctx"></param>
	[Command("serve")]
	public async Task ServeAssemblerBuild(int port = 4000, string? path = null, Cancel ctx = default)
	{
		var host = new StaticWebHost(port, path);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);
		// since this command does not use ServiceInvoker, we stop the collector manually.
		// this should be an exception to the regular command pattern.
		await collector.StopAsync(ctx);
	}

}
