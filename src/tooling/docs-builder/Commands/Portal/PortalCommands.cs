// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using ConsoleAppFramework;
using Documentation.Builder.Http;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Portal;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Portal;
using Elastic.Documentation.Portal.Building;
using Elastic.Documentation.Portal.Sourcing;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Portal;

/// <summary>
/// Commands for building documentation portals from multiple isolated documentation sets.
/// </summary>
internal sealed class PortalCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext
)
{
	/// <summary>
	/// Clone and build a documentation portal in one step.
	/// </summary>
	/// <param name="config">Path to the portal.yml configuration file.</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings.</param>
	/// <param name="fetchLatest">Fetch the latest commit even if already cloned.</param>
	/// <param name="assumeCloned">Assume repositories are already cloned.</param>
	/// <param name="output">Output directory for the built portal.</param>
	/// <param name="serve">Serve the documentation on port 4000 after build.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("")]
	public async Task<int> CloneAndBuild(
		string config = "portal.yml",
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		string? output = null,
		bool serve = false,
		Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new FileSystem();

		// Load portal configuration
		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Portal configuration file not found: {configPath}");
			return 1;
		}

		var portalConfig = PortalConfiguration.Load(configFile);
		var portalContext = new PortalContext(portalConfig, configFile, collector, fs, fs, null, output);

		// Clone service
		var cloneService = new PortalCloneService(logFactory);
		PortalCloneResult? cloneResult = null;

		serviceInvoker.AddCommand(cloneService, (portalContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				cloneResult = await s.CloneAll(state.portalContext, state.fetchLatest, state.assumeCloned, c);
				return cloneResult.Checkouts.Count > 0;
			});

		// Build service
		var buildService = new PortalBuildService(logFactory, configurationContext);
		serviceInvoker.AddCommand(buildService, (portalContext, cloneResult, fs), strict,
			async (s, col, state, c) =>
			{
				if (cloneResult == null)
					return false;
				var result = await s.BuildAll(state.portalContext, cloneResult, state.fs, c);
				return result.DocumentationSets.Count > 0;
			});

		var result = await serviceInvoker.InvokeAsync(ctx);

		if (serve && result == 0)
		{
			var host = new StaticWebHost(4000, portalContext.OutputDirectory.FullName);
			await host.RunAsync(ctx);
			await host.StopAsync(ctx);
		}

		return result;
	}

	/// <summary>
	/// Clone all repositories defined in the portal configuration.
	/// </summary>
	/// <param name="config">Path to the portal.yml configuration file.</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings.</param>
	/// <param name="fetchLatest">Fetch the latest commit even if already cloned.</param>
	/// <param name="assumeCloned">Assume repositories are already cloned.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("clone")]
	public async Task<int> Clone(
		string config = "portal.yml",
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new FileSystem();

		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Portal configuration file not found: {configPath}");
			return 1;
		}

		var portalConfig = PortalConfiguration.Load(configFile);
		var portalContext = new PortalContext(portalConfig, configFile, collector, fs, fs, null, null);

		var cloneService = new PortalCloneService(logFactory);
		serviceInvoker.AddCommand(cloneService, (portalContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				var result = await s.CloneAll(state.portalContext, state.fetchLatest, state.assumeCloned, c);
				return result.Checkouts.Count > 0;
			});

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Build all documentation sets from already cloned repositories.
	/// </summary>
	/// <param name="config">Path to the portal.yml configuration file.</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings.</param>
	/// <param name="output">Output directory for the built portal.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("build")]
	public async Task<int> Build(
		string config = "portal.yml",
		bool strict = false,
		string? output = null,
		Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new FileSystem();

		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Portal configuration file not found: {configPath}");
			return 1;
		}

		var portalConfig = PortalConfiguration.Load(configFile);
		var portalContext = new PortalContext(portalConfig, configFile, collector, fs, fs, null, output);

		// First, we need to load the checkouts that should already exist
		var cloneService = new PortalCloneService(logFactory);
		var cloneResult = await cloneService.CloneAll(portalContext, fetchLatest: false, assumeCloned: true, ctx);

		if (cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder portal clone' first.");
			return 1;
		}

		var buildService = new PortalBuildService(logFactory, configurationContext);
		serviceInvoker.AddCommand(buildService, (portalContext, cloneResult, fs), strict,
			async (s, col, state, c) =>
			{
				var result = await s.BuildAll(state.portalContext, state.cloneResult, state.fs, c);
				return result.DocumentationSets.Count > 0;
			});

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Serve the built portal documentation.
	/// </summary>
	/// <param name="port">Port to serve on.</param>
	/// <param name="path">Path to the portal output directory.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("serve")]
	public async Task Serve(
		int port = 4000,
		string? path = null,
		Cancel ctx = default)
	{
		var fs = new FileSystem();
		var servePath = path ?? fs.Path.Combine(
			Environment.CurrentDirectory, ".artifacts", "portal");

		var host = new StaticWebHost(port, servePath);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);

		// Since this command doesn't use ServiceInvoker, stop collector manually
		await collector.StopAsync(ctx);
	}
}
