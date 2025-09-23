// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation;
using Elastic.Documentation.Assembler.Building;
using Elastic.Documentation.Assembler.Configuration;
using Elastic.Documentation.Assembler.Sourcing;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.Services;
using Elastic.Documentation.Tooling.Arguments;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

// TODO This copy is scheduled for deletion soon
internal sealed class RepositoryCommands(
	ILoggerFactory logFactory,
	IDiagnosticsCollector collector,
	AssemblyConfiguration assemblyConfiguration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	private readonly ILogger<RepositoryCommands> _log = logFactory.CreateLogger<RepositoryCommands>();
	/// <summary> Clone the configuration folder </summary>
	/// <param name="gitRef">The git reference of the config, defaults to 'main'</param>
	/// <param name="ctx"></param>
	[Command("init-config")]
	public async Task<int> CloneConfigurationFolder(string? gitRef = null, Cancel ctx = default)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = new FileSystem();
		var service = new ConfigurationCloneService(logFactory, assemblyConfiguration, fs);
		serviceInvoker.AddCommand(service, gitRef, static async (s, collector, gitRef, ctx) => await s.InitConfigurationToApplicationData(collector, gitRef, ctx));
		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <summary> Clones all repositories </summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="environment"> The environment to build</param>
	/// <param name="fetchLatest"> If true, fetch the latest commit of the branch instead of the link registry entry ref</param>
	/// <param name="assumeCloned"> If true, assume the repository folder already exists on disk assume it's cloned already, primarily used for testing</param>
	/// <param name="ctx"></param>
	[Command("clone-all")]
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
	/// <param name="metadataOnly"> Only emit documentation metadata to output, ignored if 'exporters' is also set </param>
	/// <param name="exporters"> Set available exporters:
	///					html, es, config, links, state, llm, redirect, metadata, none.
	///					Defaults to (html, config, links, state, redirect) or 'default'.
	/// </param>
	/// <param name="ctx"></param>
	[Command("build-all")]
	public async Task<int> BuildAll(
		bool? strict = null,
		string? environment = null,
		bool? metadataOnly = null,
		[ExporterParser] IReadOnlySet<Exporter>? exporters = null,
		Cancel ctx = default
	)
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		var fs = new FileSystem();
		var service = new AssemblerBuildService(logFactory, assemblyConfiguration, configurationContext, githubActionsService);
		serviceInvoker.AddCommand(service, (strict, environment, metadataOnly, exporters, fs), strict ?? false,
			static async (s, collector, state, ctx) => await s.BuildAll(collector, state.strict, state.environment, state.metadataOnly, state.exporters, state.fs, ctx)
		);

		return await serviceInvoker.InvokeAsync(ctx);
	}

	/// <param name="contentSource"> The content source. "current" or "next"</param>
	/// <param name="ctx"></param>
	[Command("update-all-link-reference")]
#pragma warning disable IDE0060 // Remove unused parameter
	public async Task<int> UpdateLinkIndexAll(ContentSource contentSource, Cancel ctx = default)
#pragma warning restore IDE0060 // Remove unused parameter
	{
		await using var serviceInvoker = new ServiceInvoker(collector);

		// TODO: Implementation needed for update-all-link-reference command
		_log.LogInformation("Update link index for content source: {ContentSource}", contentSource);

		await Task.CompletedTask;
		return 0;
	}

}
