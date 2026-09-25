// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO.Abstractions;
using Actions.Core.Services;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.FileSystems;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Services;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.ContentSources;

public class RepositoryBuildMatchingService(
	ILoggerFactory logFactory,
	AssemblyConfiguration configuration,
	IConfigurationContext configurationContext,
	ICoreService githubActionsService,
	CheckoutsFileSystem fileSystem,
	IPullRequestLookup? pullRequestLookup = null
) : IService
{
	private readonly ILogger _logger = logFactory.CreateLogger<RepositoryBuildMatchingService>();

	/// <summary>
	/// Resolves the GitHub token used to walk stacked pull requests: the <c>github_token</c> action input,
	/// else the <c>GITHUB_TOKEN</c> environment variable. Returns <c>null</c> when neither is set, in which
	/// case stacked pull requests are not resolved and the match behaves as it always has.
	/// </summary>
	private string? ResolveGitHubToken()
	{
		var input = githubActionsService.GetInput("github_token");
		if (!string.IsNullOrWhiteSpace(input))
			return input;
		var env = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
		return string.IsNullOrWhiteSpace(env) ? null : env;
	}

	private IPullRequestLookup? CreatePullRequestLookup()
	{
		if (pullRequestLookup is not null)
			return pullRequestLookup;
		var token = ResolveGitHubToken();
		if (token is null)
		{
			_logger.LogInformation("No GitHub token available; stacked pull requests are not resolved");
			return null;
		}
		return new GitHubPullRequestLookup(logFactory, token);
	}

	private async Task<LinkRegistry> GetRegistryWithRetry(Aws3LinkIndexReader provider, Cancel ctx)
	{
		const int maxAttempts = 3;
		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				return await provider.GetRegistry(ctx);
			}
			catch (Exception ex) when (attempt < maxAttempts)
			{
				var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
				_logger.LogWarning(
					"S3 link registry fetch failed (attempt {Attempt}/{Max}), retrying in {Delay}s: {Message}",
					attempt,
					maxAttempts,
					delay.TotalSeconds,
					ex.Message
				);
				await Task.Delay(delay, ctx);
			}
		}
		return await provider.GetRegistry(ctx);
	}

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

		// the link registry uses short repository names (e.g. "kibana"), not full names (e.g. "elastic/kibana")
		var repoTokens = repo.Split('/');
		var repositoryName = repoTokens.Last();

		// environment does not matter to check the configuration, defaulting to dev
		var linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
		var linkRegistry = await GetRegistryWithRetry(linkIndexProvider, ctx);
		var alreadyPublishing = linkRegistry.Repositories.ContainsKey(repositoryName);
		_logger.LogInformation(
			"'{Repository}' (registry key: '{RepositoryName}') publishing to link registry: {PublishState} ",
			repo,
			repositoryName,
			alreadyPublishing
		);
		var assembleContext = new AssembleContext(configuration, configurationContext, "dev", collector, fileSystem);
		var product = assembleContext.ProductsConfiguration.GetProductByRepositoryName(repo);
		var matches = assembleContext.Configuration.Match(logFactory, repo, refName, product, alreadyPublishing);
		var contentSourceRef = refName;
		IReadOnlyList<int> stackParents = [];

		if (IsNoMatch(matches))
		{
			_logger.LogInformation("'{Repository}' '{BranchOrTag}' combination not found in configuration.", repo, refName);

			// A stacked pull request targets the head branch of another open pull request rather than a
			// content-source branch. Walk the chain of open pull requests to find the content-source branch
			// the stack ultimately targets. The pull request's own head is still what gets built; only
			// eligibility comes from the resolved branch.
			var lookup = CreatePullRequestLookup();
			if (lookup is not null)
			{
				var resolver = new StackedPullRequestResolver(logFactory, lookup);
				var resolution = await resolver.Resolve(
					repo,
					refName,
					branch => !IsNoMatch(assembleContext.Configuration.Match(logFactory, repo, branch, product, alreadyPublishing)),
					ctx
				);
				if (resolution.Matched && resolution.ParentPullRequests.Count > 0)
				{
					contentSourceRef = resolution.ResolvedBranch;
					stackParents = resolution.ParentPullRequests;
					matches = assembleContext.Configuration.Match(logFactory, repo, contentSourceRef, product, alreadyPublishing);
				}
			}
		}

		await githubActionsService.SetOutputAsync("content-source-ref", contentSourceRef);
		await githubActionsService.SetOutputAsync("stack-parent-prs", string.Join(",", stackParents));

		if (IsNoMatch(matches))
		{
			await githubActionsService.SetOutputAsync("content-source-match", "false");
			await githubActionsService.SetOutputAsync("content-source-next", "false");
			await githubActionsService.SetOutputAsync("content-source-edge", "false");
			await githubActionsService.SetOutputAsync("content-source-current", "false");
			await githubActionsService.SetOutputAsync("content-source-speculative", "false");
			return false;
		}

		// Report the branch that actually matched so the log reads correctly for stacked pull requests.
		refName = contentSourceRef;

		if (matches.Current is { } current)
			_logger.LogInformation(
				"'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source",
				repo,
				refName,
				current.ToStringFast(true)
			);
		if (matches.Next is { } next)
			_logger.LogInformation(
				"'{Repository}' '{BranchOrTag}' is configured as '{Matches}' content-source",
				repo,
				refName,
				next.ToStringFast(true)
			);

		await githubActionsService.SetOutputAsync("content-source-match", "true");
		await githubActionsService.SetOutputAsync("content-source-next", matches.Next is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-current", matches.Current is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-edge", matches.Edge is not null ? "true" : "false");
		await githubActionsService.SetOutputAsync("content-source-speculative", matches.Speculative ? "true" : "false");
		return true;
	}

	private static bool IsNoMatch(AssemblyConfiguration.ContentSourceMatch matches) =>
		matches is { Current: null, Next: null, Edge: null, Speculative: false };
}
