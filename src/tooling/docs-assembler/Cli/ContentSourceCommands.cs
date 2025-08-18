// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using ConsoleAppFramework;
using Documentation.Assembler.Building;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Cli;

internal sealed class ContentSourceCommands(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService
)
{
	[Command("validate")]
	public async Task<int> Validate(Cancel ctx = default)
	{
		await using var collector = new ConsoleDiagnosticsCollector(logFactory, githubActionsService)
		{
			NoHints = true
		};

		_ = collector.StartAsync(ctx);

		// environment does not matter to check the configuration, defaulting to dev
		var fs = new FileSystem();
		var context = new AssembleContext(configuration, configurationContext, "dev", collector, fs, fs, null, null);
		ILinkIndexReader linkIndexReader = Aws3LinkIndexReader.CreateAnonymous();
		var fetcher = new AssemblerCrossLinkFetcher(logFactory, context.Configuration, context.Environment, linkIndexReader, collector);
		var links = await fetcher.FetchLinkIndex(ctx);
		var repositories = context.Configuration.AvailableRepositories;

		var reportPath = context.ConfigurationFileProvider.AssemblerFile;
		foreach (var repository in repositories.Values)
		{
			if (!links.Repositories.TryGetValue(repository.Name, out var registryMapping))
			{
				collector.EmitError(reportPath, $"'{repository}' does not exist in link index");
				continue;
			}

			var current = repository.GetBranch(ContentSource.Current);
			var next = repository.GetBranch(ContentSource.Next);
			if (!registryMapping.TryGetValue(next, out _))
			{
				collector.EmitError(reportPath,
					$"'{repository.Name}' has not yet published links.json for configured 'next' content source: '{next}' see  {linkIndexReader.RegistryUrl}");
			}
			if (!registryMapping.TryGetValue(current, out _))
			{
				collector.EmitError(reportPath,
					$"'{repository.Name}' has not yet published links.json for configured 'current' content source: '{current}' see  {linkIndexReader.RegistryUrl}");
			}
		}


		await collector.StopAsync(ctx);
		return collector.Errors == 0 ? 0 : 1;
	}

	/// <summary>  </summary>
	/// <param name="repository"></param>
	/// <param name="branchOrTag"></param>
	/// <param name="ctx"></param>
	[Command("match")]
	public async Task<int> Match([Argument] string? repository = null, [Argument] string? branchOrTag = null, Cancel ctx = default)
	{
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
		var assembleContext = new AssembleContext(configuration, configurationContext, "dev", collector, new FileSystem(), new FileSystem(), null, null);
		var matches = assembleContext.Configuration.Match(repo, refName);
		if (matches is { Current: null, Next: null, Speculative: false })
		{
			logger.LogInformation("'{Repository}' '{BranchOrTag}' combination not found in configuration.", repo, refName);
			await githubActionsService.SetOutputAsync("content-source-match", "false");
			await githubActionsService.SetOutputAsync("content-source-next", "false");
			await githubActionsService.SetOutputAsync("content-source-current", "false");
			await githubActionsService.SetOutputAsync("content-source-speculative", "false");
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
			await githubActionsService.SetOutputAsync("content-source-speculative", matches.Speculative ? "true" : "false");
		}

		await collector.StopAsync(ctx);
		return collector.Errors == 0 ? 0 : 1;
	}

}
