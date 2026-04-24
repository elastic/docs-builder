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

/// <summary>
/// Build documentation codexes from multiple isolated documentation sets.
/// </summary>
internal sealed class CodexCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	IEnvironmentVariables environmentVariables
)
{
	/// <summary>
	/// Clone and build a documentation codex in one step.
	/// </summary>
	/// <remarks>
	/// <code>
	/// docs-builder codex ./codex.yml
	/// docs-builder codex ./codex.yml --strict --fetch-latest
	/// docs-builder codex ./codex.yml --serve
	/// </code>
	/// </remarks>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings</param>
	/// <param name="fetchLatest">Fetch the latest commit even if already cloned</param>
	/// <param name="assumeCloned">Assume repositories are already cloned</param>
	/// <param name="output">Output directory for the built codex</param>
	/// <param name="serve">Serve the documentation on port 4000 after a successful build</param>
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

	/// <summary>Clone all repositories defined in the codex configuration.</summary>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings</param>
	/// <param name="fetchLatest">Fetch the latest commit even if already cloned</param>
	/// <param name="assumeCloned">Assume repositories are already cloned</param>
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

	/// <summary>Build all documentation sets from already-cloned repositories.</summary>
	/// <param name="config">Path to the <c>codex.yml</c> configuration file</param>
	/// <param name="strict">Treat warnings as errors and fail on warnings</param>
	/// <param name="output">Output directory for the built codex</param>
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

	/// <summary>Serve the built codex documentation at <c>http://localhost:4000</c>.</summary>
	/// <param name="port">Port to serve on. Default: 4000</param>
	/// <param name="path">Path to the codex output directory</param>
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
