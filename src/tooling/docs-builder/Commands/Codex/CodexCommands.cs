// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Builder.Http;
using Elastic.Codex;
using Elastic.Codex.Building;
using Elastic.Codex.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Documentation.Builder.Commands.Codex;

/// <summary>
/// Commands for building documentation codexes from multiple isolated documentation sets.
/// </summary>
internal sealed class CodexCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	/// <summary>
	/// Clone and build a documentation codex in one step.
	/// </summary>
	/// <param name="config">Path to the codex.yml configuration file.</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings.</param>
	/// <param name="fetchLatest">Fetch the latest commit even if already cloned.</param>
	/// <param name="assumeCloned">Assume repositories are already cloned.</param>
	/// <param name="output">Output directory for the built codex.</param>
	/// <param name="serve">Serve the documentation on port 4000 after build.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("")]
	public async Task<int> CloneAndBuild(
		[Argument] string config,
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		string? output = null,
		bool serve = false,
		Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new FileSystem();

		// Load codex configuration
		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, output);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment ?? "dev");
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		CodexCloneResult? cloneResult = null;

		serviceInvoker.AddCommand(cloneService, (codexContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				cloneResult = await s.CloneAll(state.codexContext, state.fetchLatest, state.assumeCloned, c);
				return cloneResult.Checkouts.Count > 0;
			});

		// Build service
		var isolatedBuildService = new IsolatedBuildService(logFactory, configurationContext, githubActionsService);
		var buildService = new CodexBuildService(logFactory, configurationContext, isolatedBuildService);
		serviceInvoker.AddCommand(buildService, (codexContext, cloneResult, fs), strict,
			async (s, col, state, c) =>
			{
				if (cloneResult == null)
					return false;
				var result = await s.BuildAll(state.codexContext, cloneResult, state.fs, c);
				return result.DocumentationSets.Count > 0;
			});

		var result = await serviceInvoker.InvokeAsync(ctx);

		if (serve && result == 0)
		{
			var host = new StaticWebHost(4000, codexContext.OutputDirectory.FullName);
			await host.RunAsync(ctx);
			await host.StopAsync(ctx);
		}

		return result;
	}

	/// <summary>
	/// Clone all repositories defined in the codex configuration.
	/// </summary>
	/// <param name="config">Path to the codex.yml configuration file.</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings.</param>
	/// <param name="fetchLatest">Fetch the latest commit even if already cloned.</param>
	/// <param name="assumeCloned">Assume repositories are already cloned.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("clone")]
	public async Task<int> Clone(
		[Argument] string config,
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
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, null);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment ?? "dev");
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		serviceInvoker.AddCommand(cloneService, (codexContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				var result = await s.CloneAll(state.codexContext, state.fetchLatest, state.assumeCloned, c);
				return result.Checkouts.Count > 0;
			});

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Build all documentation sets from already cloned repositories.
	/// </summary>
	/// <param name="config">Path to the codex.yml configuration file.</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings.</param>
	/// <param name="output">Output directory for the built codex.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("build")]
	public async Task<int> Build(
		[Argument] string config,
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
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);
		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, output);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment ?? "dev");
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		var cloneResult = await cloneService.CloneAll(codexContext, fetchLatest: false, assumeCloned: true, ctx);

		if (cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder codex clone' first.");
			return 1;
		}

		var isolatedBuildService = new IsolatedBuildService(logFactory, configurationContext, githubActionsService);
		var buildService = new CodexBuildService(logFactory, configurationContext, isolatedBuildService);
		serviceInvoker.AddCommand(buildService, (codexContext, cloneResult, fs), strict,
			async (s, col, state, c) =>
			{
				var result = await s.BuildAll(state.codexContext, state.cloneResult, state.fs, c);
				return result.DocumentationSets.Count > 0;
			});

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary>
	/// Serve the built codex documentation.
	/// </summary>
	/// <param name="port">Port to serve on.</param>
	/// <param name="path">Path to the codex output directory.</param>
	/// <param name="ctx">Cancellation token.</param>
	[Command("serve")]
	public async Task Serve(
		int port = 4000,
		string? path = null,
		Cancel ctx = default)
	{
		var fs = new FileSystem();
		var servePath = path ?? fs.Path.Combine(
			Environment.CurrentDirectory, ".artifacts", "codex", "docs");

		var host = new StaticWebHost(port, servePath);
		await host.RunAsync(ctx);
		await host.StopAsync(ctx);

		// Since this command doesn't use ServiceInvoker, stop collector manually
		await collector.StopAsync(ctx);
	}
}
