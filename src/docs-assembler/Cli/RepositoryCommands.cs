// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler.Building;
using Documentation.Assembler.Navigation;
using Documentation.Assembler.Sourcing;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class RepositoryCommands(ICoreService githubActionsService, ILoggerFactory logger)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logger.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	// would love to use libgit2 so there is no git dependency but
	// libgit2 is magnitudes slower to clone repositories https://github.com/libgit2/libgit2/issues/4674
	/// <summary> Clones all repositories </summary>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="ctx"></param>
	[Command("clone-all")]
	public async Task<int> CloneAll(bool? strict = null, Cancel ctx = default)
	{
		AssignOutputLogger();

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);

		var assembleContext = new AssembleContext(collector, new FileSystem(), new FileSystem(), null, null);
		var cloner = new RepositoryCheckoutProvider(logger, assembleContext);
		_ = await cloner.AcquireAllLatest(ctx);

		if (strict ?? false)
			return collector.Errors + collector.Warnings;
		return collector.Errors;
	}

	/// <summary> Builds all repositories </summary>
	/// <param name="force"> Force a full rebuild of the destination folder</param>
	/// <param name="strict"> Treat warnings as errors and fail the build on warnings</param>
	/// <param name="allowIndexing"> Allow indexing and following of html files</param>
	/// <param name="environment"> The environment to resolve links to</param>
	/// <param name="ctx"></param>
	[Command("build-all")]
	public async Task<int> BuildAll(
		bool? force = null,
		bool? strict = null,
		bool? allowIndexing = null,
		string? environment = null,
		Cancel ctx = default)
	{
		AssignOutputLogger();
		var githubEnvironmentInput = githubActionsService.GetInput("environment");
		environment ??= !string.IsNullOrEmpty(githubEnvironmentInput) ? githubEnvironmentInput : "dev";

		await using var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		_ = collector.StartAsync(ctx);

		var assembleContext = new AssembleContext(collector, new FileSystem(), new FileSystem(), null, null)
		{
			Force = force ?? false,
			AllowIndexing = allowIndexing ?? false,
		};

		if (!assembleContext.Configuration.Environments.TryGetValue(environment, out var env))
			throw new Exception($"Could not find environment {environment}");

		var cloner = new RepositoryCheckoutProvider(logger, assembleContext);
		var checkouts = cloner.GetAll().ToArray();
		if (checkouts.Length == 0)
			throw new Exception("No checkouts found");

		var globalNavigation = GlobalNavigation.Deserialize(assembleContext, checkouts);
		var builder = new AssemblerBuilder(logger, assembleContext);
		await builder.BuildAllAsync(checkouts, env, ctx);

		if (strict ?? false)
			return collector.Errors + collector.Warnings;
		return collector.Errors;
	}
}
