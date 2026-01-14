// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ContentSources;

public class RepositoryBuildMatchingService(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	FileSystem fileSystem
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<RepositoryBuildMatchingService>();

	//TODO return contentsourcematch
	/// <summary>
	/// Validates whether the <paramref name="branchOrTag"/> on <paramref name="repository"/> should be build and therefor published.
	/// <para>Will also qualify the branch as being current or next or whether we should build this speculatively</para>
	/// <para>e.g., if a new minor branch gets created, we want to build it even if it's not configured in assembler.yml yet</para>
	/// </summary>
	public async Task<bool> ShouldBuild(IDiagnosticsCollector collector, string? repository, string? branchOrTag, Cancel ctx)
	{
		var repo = repository ?? githubActionsService.GetInput("repository");
		var refName = branchOrTag ?? githubActionsService.GetInput("ref_name");
		_logger.LogInformation(" Validating '{Repository}' '{BranchOrTag}' ", repo, refName);

		if (string.IsNullOrEmpty(repo))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(refName))
			throw new ArgumentNullException(nameof(branchOrTag));

		// environment does not matter to check the configuration, defaulting to dev
		var linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
		var linkRegistry = await linkIndexProvider.GetRegistry(ctx);
		var alreadyPublishing = linkRegistry.Repositories.ContainsKey(repo);
		_logger.LogInformation("'{Repository}' publishing to link registry: {PublishState} ", repo, alreadyPublishing);
		var assembleContext = new AssembleContext(configuration, configurationContext, "dev", collector, fileSystem, fileSystem, null, null);
		var product = assembleContext.ProductsConfiguration.GetProductByRepositoryName(repo);
		var matches = assembleContext.Configuration.Match(logFactory, repo, refName, product, alreadyPublishing);
		if (matches is { Current: null, Next: null, Edge: null, Speculative: false })
		{
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' combination not found in configuration.", repo, refName);
			await githubActionsService.SetOutputAsync("content-source-match", "false");
			await githubActionsService.SetOutputAsync("content-source-next", "false");
			await githubActionsService.SetOutputAsync("content-source-edge", "false");
			await githubActionsService.SetOutputAsync("content-source-current", "false");
			await githubActionsService.SetOutputAsync("content-source-speculative", "false");
			return false;
		}

		if (matches.Current is { } current)
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, current.ToStringFast(true));
		if (matches.Next is { } next)
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source", repo, refName, next.ToStringFast(true));

		await githubActionsService.SetOutputAsync("content-source-match", "true");
		await githubActionsService.SetOutputAsync("content-source-next", matches.Next is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-current", matches.Current is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-edge", matches.Edge is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-speculative", matches.Speculative ? "true" : "false");
		return true;
	}
}
