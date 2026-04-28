// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Documentation.Builder.Http;
using Elastic.Codex;
using Elastic.Codex.Building;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Codex;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Isolated;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;

namespace Documentation.Builder.Commands.Codex;

/// <summary>Build a documentation portal over multiple independent documentation sets, each with its own navigation.</summary>
/// <remarks>
/// <para>
/// A codex is a portal composed of several documentation sets. Unlike the assembler, each set retains
/// its own navigation structure — there is no merged global navigation tree. The codex configuration
/// (<c>codex.yml</c>) lists which repositories to include and how to compose the portal.
/// </para>
/// </remarks>
internal sealed class CodexCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>Clone all repositories and build the portal in one step.</summary>
	/// <remarks>
	/// </remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>
	/// <param name="strict">Treat warnings as errors.</param>
	/// <param name="fetchLatest">Fetch the HEAD of each branch instead of the pinned ref.</param>
	/// <param name="assumeCloned">Skip cloning; assume repositories are already on disk.</param>
	/// <param name="output">Output directory for the built portal. Defaults to <c>.artifacts/codex/</c>.</param>
	/// <param name="serve">Serve the portal on port 4000 after a successful build.</param>
	[DefaultCommand]
	public async Task<int> CloneAndBuild(
		GlobalCliOptions _,
		[Argument] string config,
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		string? output = null,
		bool serve = false,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealRead;

		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);

		if (string.IsNullOrWhiteSpace(codexConfig.Environment))
		{
			collector.EmitGlobalError("Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
			return 1;
		}

		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, output);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment);
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		CodexCloneResult? cloneResult = null;

		serviceInvoker.AddCommand(cloneService, (codexContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				cloneResult = await s.CloneAll(state.codexContext, state.fetchLatest, state.assumeCloned, c);
				return cloneResult.Checkouts.Count > 0;
			});

		var isolatedBuildService = new IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		var buildService = new CodexBuildService(logFactory, configurationContext, isolatedBuildService);
		serviceInvoker.AddCommand(buildService, (codexContext, cloneResult, fs), strict,
			async (s, col, state, c) =>
			{
				if (state.cloneResult == null)
					return false;
				var result = await s.BuildAll(state.codexContext, state.cloneResult, state.fs, c);
				return result.DocumentationSets.Count > 0;
			});

		var result = await serviceInvoker.InvokeAsync(ct);

		if (serve && result == 0)
		{
			var host = new StaticWebHost(4000, codexContext.OutputDirectory.FullName);
			await host.RunAsync(ct);
			await host.StopAsync(ct);
		}

		return result;
	}

	/// <summary>Clone all repositories listed in the codex configuration.</summary>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>
	/// <param name="strict">Treat warnings as errors.</param>
	/// <param name="fetchLatest">Fetch the HEAD of each branch instead of the pinned ref.</param>
	/// <param name="assumeCloned">Skip cloning; assume repositories are already on disk.</param>
	[NoOptionsInjection]
	public async Task<int> Clone(
		[Argument] string config,
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealRead;

		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);

		if (string.IsNullOrWhiteSpace(codexConfig.Environment))
		{
			collector.EmitGlobalError("Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
			return 1;
		}

		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, null);

		using var linkIndexReader = new GitLinkIndexReader(codexConfig.Environment);
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		serviceInvoker.AddCommand(cloneService, (codexContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				var result = await s.CloneAll(state.codexContext, state.fetchLatest, state.assumeCloned, c);
				return result.Checkouts.Count > 0;
			});

		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Build the portal from previously cloned repositories.</summary>
	/// <remarks>Run after <c>codex clone</c>.</remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file.</param>
	/// <param name="strict">Treat warnings as errors.</param>
	/// <param name="output">Output directory. Defaults to <c>.artifacts/codex/</c>.</param>
	[NoOptionsInjection]
	public async Task<int> Build(
		[Argument] string config,
		bool strict = false,
		string? output = null,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = FileSystemFactory.RealRead;

		var configPath = fs.Path.GetFullPath(config);
		var configFile = fs.FileInfo.New(configPath);

		if (!configFile.Exists)
		{
			collector.EmitGlobalError($"Codex configuration file not found: {configPath}");
			return 1;
		}

		var codexConfig = CodexConfiguration.Load(configFile);

		if (string.IsNullOrWhiteSpace(codexConfig.Environment))
		{
			collector.EmitGlobalError("Codex configuration must specify an 'environment' (e.g., 'internal', 'security').");
			return 1;
		}

		var codexContext = new CodexContext(codexConfig, configFile, collector, fs, fs, null, output);
		var cloneResult = await CodexCloneService.DiscoverCheckouts(codexContext, logFactory, ct);

		if (cloneResult == null || cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder codex clone' first.");
			return 1;
		}

		var isolatedBuildService = new IsolatedBuildService(logFactory, configurationContext, githubActionsService, environmentVariables);
		var buildService = new CodexBuildService(logFactory, configurationContext, isolatedBuildService);
		serviceInvoker.AddCommand(buildService, (codexContext, cloneResult, fs), strict,
			async (s, col, state, c) =>
			{
				var result = await s.BuildAll(state.codexContext, state.cloneResult, state.fs, c);
				return result.DocumentationSets.Count > 0;
			});

		return await serviceInvoker.InvokeAsync(ct);
	}

	/// <summary>Serve the built portal at <c>http://localhost:4000</c>.</summary>
	/// <remarks>Run after <c>codex build</c>. Does not rebuild on file changes.</remarks>
	/// <param name="port">Port to listen on. Default: 4000.</param>
	/// <param name="path">Path to the portal output. Defaults to <c>.artifacts/codex/docs/</c>.</param>
	[NoOptionsInjection]
	public async Task Serve(int port = 4000, string? path = null, CancellationToken ct = default)
	{
		var fs = FileSystemFactory.RealRead;
		var servePath = path ?? fs.Path.Join(Environment.CurrentDirectory, ".artifacts", "codex", "docs");

		var host = new StaticWebHost(port, servePath);
		await host.RunAsync(ct);
		await host.StopAsync(ct);
		await collector.StopAsync(ct);
	}
}
