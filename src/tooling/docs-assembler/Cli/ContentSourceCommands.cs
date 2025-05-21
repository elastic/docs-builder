// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class ContentSourceCommands(ICoreService githubActionsService, ILoggerFactory logFactory)
{
	[SuppressMessage("Usage", "CA2254:Template should be a static expression")]
	private void AssignOutputLogger()
	{
		var log = logFactory.CreateLogger<Program>();
		ConsoleApp.Log = msg => log.LogInformation(msg);
		ConsoleApp.LogError = msg => log.LogError(msg);
	}

	/// <summary>  </summary>
	/// <param name="repository"></param>
	/// <param name="branchOrTag"></param>
	/// <param name="ctx"></param>
	[Command("match")]
	public async Task<int> Match([Argument] string? repository = null, [Argument] string? branchOrTag = null, Cancel ctx = default)
	{
		AssignOutputLogger();
		var logger = logFactory.CreateLogger<ContentSourceCommands>();

		var repo = repository ?? githubActionsService.GetInput("repository");
		var refName = branchOrTag ?? githubActionsService.GetInput("ref_name");
		logger.LogInformation(" Validating '{Repository}' '{BranchOrTag}' ", repo, refName);

		if (string.IsNullOrEmpty(repo))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(refName))
			throw new ArgumentNullException(nameof(branchOrTag));

		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		};

		_ = collector.StartAsync(ctx);

		// environment does not matter to check the configuration, defaulting to dev
		var assembleContext = new AssembleContext("dev", collector, new FileSystem(), new FileSystem(), null, null)
		{
			Force = false,
			AllowIndexing = false
		};
		var matches = assembleContext.Configuration.Match(repo, refName);
		if (matches is { Current: null, Next: null })
		{
			logger.LogInformation("'{Repository}' '{BranchOrTag}' combination not found in configuration.", repo, refName);
			await githubActionsService.SetOutputAsync("content-source-match", "false");
			await githubActionsService.SetOutputAsync("content-source-name", "");
		}
		else
		{
			if (matches.Current is { } current)
				logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, current.ToStringFast(true));
			if (matches.Next is { } next)
				logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, next.ToStringFast(true));

			await githubActionsService.SetOutputAsync("content-source-match", "true");
			await githubActionsService.SetOutputAsync("content-source-next", matches.Next is not null ? "true" : "false");
			await githubActionsService.SetOutputAsync("content-source-current", matches.Current is not null ? "true" : "false");

			//TODO remove once we've merged our changes to the github action and its workflow usage to no longer use this output
			var name = (matches.Current ?? matches.Next)!.Value.ToStringFast(true);
			await githubActionsService.SetOutputAsync("content-source-name", name);
		}

		await collector.StopAsync(ctx);
		return collector.Errors == 0 ? 0 : 1;
	}

}
