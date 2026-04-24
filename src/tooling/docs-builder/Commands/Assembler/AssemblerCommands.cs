// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Arguments;
using Documentation.Builder.Http;
using Elastic.Documentation;
using Elastic.Documentation.Assembler;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Navigation;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Configuration.Toc;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LegacyDocs;
using Elastic.Documentation.Navigation.Assembler;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.ScopedFileSystem;

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
		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		serviceInvoker.AddCommand(buildService, (strict, environment, metadataOnly, showHints, exporters, assumeBuild, readFs, writeFs), strict ?? false,
			static async (s, collector, state, ctx) =>
				await s.BuildAll(collector, state.strict, state.environment, state.metadataOnly, state.showHints, state.exporters, state.assumeBuild, state.readFs, state.writeFs, ctx)
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

		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;
		var service = new AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService, environmentVariables);
		serviceInvoker.AddCommand(service, (strict, environment, assumeBuild, metadataOnly, showHints, exporters, readFs, writeFs), strict ?? false,
			static async (s, collector, state, ctx) =>
				await s.BuildAll(collector, state.strict, state.environment, state.metadataOnly, state.showHints, state.exporters, state.assumeBuild, state.readFs, state.writeFs, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Serve assembled documentation with live reload and on-demand per-request rendering.
	/// Requires 'assembler clone' to have been run first. No prior build needed.
	/// Pages are rendered on demand; file changes invalidate the repo and trigger a live reload.
	/// </summary>
	/// <param name="port">Port to serve the documentation.</param>
	/// <param name="environment">The environment configuration to use.</param>
	/// <param name="noWatchMd">Disable watching checkout directories for markdown changes. Static asset live reload still works. Useful when doing frontend (CSS/JS) work.</param>
	/// <param name="ctx"></param>
	[Command("serve")]
	public async Task ServeAssemblerOnDemand(
		int port = 4000,
		string? environment = null,
		bool noWatchMd = false,
		Cancel ctx = default
	)
	{
		environment ??= "dev";
		var readFs = FileSystemFactory.RealRead;
		var writeFs = FileSystemFactory.RealWrite;

		var assembleContext = new AssembleContext(assemblyConfiguration, configurationContext, environment, collector, readFs, writeFs, null, null);

		var cloner = new AssemblerRepositorySourcer(logFactory, assembleContext);
		var checkoutResult = cloner.GetAll();
		var checkouts = checkoutResult.Checkouts.ToArray();

		if (checkouts.Length == 0)
			throw new Exception("No checkouts found. Run 'assembler clone' first.");

		var exporters = ExportOptions.Default
			.Except([Exporter.DocumentationState])
			.ToHashSet();

		var assembleSources = await AssembleSources.AssembleAsync(logFactory, assembleContext, checkouts, configurationContext, exporters, ctx);

		var navigationFileInfo = configurationContext.ConfigurationFileProvider.NavigationFile;
		var siteNavigationFile = SiteNavigationFile.Deserialize(await readFs.File.ReadAllTextAsync(navigationFileInfo.FullName, ctx));
		var documentationSets = assembleSources.AssembleSets.Values.Select(s => s.DocumentationSet.Navigation).ToArray();
		var navigation = new SiteNavigation(siteNavigationFile, assembleContext, documentationSets, assembleContext.Environment.PathPrefix);

		var pathProvider = new GlobalNavigationPathProvider(navigation, assembleSources, assembleContext);
		using var htmlWriter = new GlobalNavigationHtmlWriter(logFactory, navigation, collector);
		var legacyPageChecker = new LegacyPageService(logFactory);
		var historyMapper = new PageLegacyUrlMapper(legacyPageChecker, assembleContext.VersionsConfiguration, assembleSources.LegacyUrlMappings);
		var builder = new AssemblerBuilder(logFactory, assembleContext, navigation, htmlWriter, pathProvider, historyMapper);

		var host = new AssemblerServeWebHost(port, assembleSources, builder, logFactory, watchMarkdown: !noWatchMd);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);
		// since this command does not use ServiceInvoker, we stop the collector manually.
		await collector.StopAsync(ctx);
	}

	/// <summary>Serve the static output of a prior 'assembler build' run.</summary>
	/// <param name="port">Port to serve the documentation.</param>
	/// <param name="path">Optional path to serve from, defaults to .artifacts/assembly.</param>
	/// <param name="ctx"></param>
	[Command("serve-static")]
	public async Task ServeStaticAssemblerBuild(int port = 4000, string? path = null, Cancel ctx = default)
	{
		var host = new StaticWebHost(port, path);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);
		// since this command does not use ServiceInvoker, we stop the collector manually.
		await collector.StopAsync(ctx);
	}

}
