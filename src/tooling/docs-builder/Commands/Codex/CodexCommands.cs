// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using Documentation.Builder.Http;
using Elastic.Codex;
using Elastic.Codex.Building;
using Elastic.Codex.Sourcing;
using Elastic.Documentation;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;
using Nullean.Argh;
using Nullean.Argh.Documentation;

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
	IConfigurationContext configurationContext
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
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? output = null,
		bool serve = false,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new CodexFileSystem(config.FullName, output?.FullName);
		if (!CodexConfigurationLoader.TryLoad(fs.ConfigurationFile, config.FullName, collector, out var codexConfig, out var environment))
			return 1;

		var codexContext = new CodexContext(codexConfig, fs.ConfigurationFile, collector, fs, null, output?.FullName);

		using var linkIndexReader = new GitLinkIndexReader(environment);
		var cloneService = new CodexCloneService(logFactory, linkIndexReader);
		CodexCloneResult? cloneResult = null;

		serviceInvoker.AddCommand(cloneService, (codexContext, fetchLatest, assumeCloned), strict,
			async (s, col, state, c) =>
			{
				cloneResult = await s.CloneAll(state.codexContext, state.fetchLatest, state.assumeCloned, c);
				return cloneResult.Checkouts.Count > 0;
			});

		var buildService = new CodexBuildService(logFactory, configurationContext);
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
	[CommandIntent(Intent.Idempotent)]
	[NoOptionsInjection]
	public async Task<int> Clone(
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		bool strict = false,
		bool fetchLatest = false,
		bool assumeCloned = false,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new CodexFileSystem(config.FullName);
		if (!CodexConfigurationLoader.TryLoad(fs.ConfigurationFile, config.FullName, collector, out var codexConfig, out var environment))
			return 1;

		var codexContext = new CodexContext(codexConfig, fs.ConfigurationFile, collector, fs);

		using var linkIndexReader = new GitLinkIndexReader(environment);
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
	[CommandIntent(Intent.Idempotent)]
	[NoOptionsInjection]
	public async Task<int> Build(
		[Argument, Existing, ExpandUserProfile, RejectSymbolicLinks, FileExtensions(Extensions = "yml,yaml")] FileInfo config,
		bool strict = false,
		[ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? output = null,
		CancellationToken ct = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);
		var fs = new CodexFileSystem(config.FullName, output?.FullName);
		if (!CodexConfigurationLoader.TryLoad(fs.ConfigurationFile, config.FullName, collector, out var codexConfig, out _))
			return 1;

		var codexContext = new CodexContext(codexConfig, fs.ConfigurationFile, collector, fs, null, output?.FullName);
		var cloneResult = await CodexCloneService.DiscoverCheckouts(codexContext, logFactory, ct);

		if (cloneResult == null || cloneResult.Checkouts.Count == 0)
		{
			collector.EmitGlobalError("No documentation sets found. Run 'docs-builder codex clone' first.");
			return 1;
		}

		var buildService = new CodexBuildService(logFactory, configurationContext);
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
	public async Task Serve(int port = 4000, [Existing, ExpandUserProfile, RejectSymbolicLinks] DirectoryInfo? path = null, CancellationToken ct = default)
	{
		var servePath = path?.FullName ?? Path.Join(Environment.CurrentDirectory, ".artifacts", "codex", "docs");

		var host = new StaticWebHost(port, servePath);
		await host.RunAsync(ct);
		await host.StopAsync(ct);
		await collector.StopAsync(ct);
	}
}
