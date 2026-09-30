// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Publishing;
using System.Text.RegularExpressions;
using Elastic.Documentation.Catalog;
using Elastic.Documentation.Versions;
using Microsoft.Extensions.Logging;

// Declared in the domain namespace so the behavior arrives with the Publishing records it extends; it lives here because
// it needs loader-side dependencies. The same-named assembler.yml DTOs in this folder's namespace would otherwise shadow them.
#pragma warning disable IDE0130
namespace Elastic.Documentation.Publishing;

#pragma warning restore IDE0130

/// <summary>Decides which content sources a push publishes to, logging each step of the decision.</summary>
public static class AssemblyConfigurationMatching
{
	public static AssemblyConfiguration.ContentSourceMatch Match(
		this AssemblyConfiguration assembly,
		ILoggerFactory logFactory,
		string repository,
		string branchOrTag,
		Product? product,
		bool alreadyPublishing
	)
	{
		var logger = logFactory.CreateLogger<AssemblyConfiguration.ContentSourceMatch>();
		var match = new AssemblyConfiguration.ContentSourceMatch(null, null, null, false);
		var tokens = repository.Split('/');
		var repositoryName = tokens.Last();
		var owner = tokens.First();
		var isVersionBranch = ContentSourceRegex.MatchVersionBranch().IsMatch(branchOrTag);

		if (tokens.Length < 2 || owner != "elastic")
		{
			logger.LogInformation("Repository {Repository} is not a valid elastic repository but {Owner}", repository, owner);
			return match;
		}

		// Check for new repositories
		if (!assembly.AvailableRepositories.TryGetValue(repositoryName, out var r))
		{
			logger.LogInformation("Repository {Repository} has not yet been onboarded into assembler.yml", repository);
			// this is an unknown new elastic repository
			if (isVersionBranch || branchOrTag == "main" || branchOrTag == "master")
			{
				logger.LogInformation("Speculatively building {Repository} since it looks like an integration branch", repository);
				return match with { Speculative = true };
			}
			logger.LogInformation("{Repository} on '{Branch}' does not look like it needs a speculative build", repository, branchOrTag);
			return match;
		}

		var current = r.GetBranch(ContentSource.Current);
		var next = r.GetBranch(ContentSource.Next);
		var edge = r.GetBranch(ContentSource.Edge);
		var isCdWorkflow = current == next && next == edge;
		logger.LogInformation(
			"Active content-sources for {Repository}. current: {Current}, next: {Next}, edge: {Edge} (branching strategy: {Strategy})",
			repository,
			current,
			next,
			edge,
			isCdWorkflow ? "cd/continuous-deployment" : "tagged-release"
		);
		if (current == branchOrTag)
		{
			logger.LogInformation("Content-Source current: {Current} matches: {Branch}", current, branchOrTag);
			match = match with { Current = ContentSource.Current };
		}

		if (next == branchOrTag)
		{
			logger.LogInformation("Content-Source next: {Next} matches: {Branch}", next, branchOrTag);
			match = match with { Next = ContentSource.Next };
		}

		if (edge == branchOrTag)
		{
			logger.LogInformation("Content-Source edge: {Edge} matches: {Branch}", edge, branchOrTag);
			match = match with { Edge = ContentSource.Edge };
		}

		// check version branches
		if (isVersionBranch && SemVersion.TryParse(branchOrTag + ".0", out var v))
		{
			logger.LogInformation("Branch or tag {Branch} is a versioned branch", branchOrTag);
			// if the current branch is a version, only speculatively match if branch is actually a new version
			if (SemVersion.TryParse(current + ".0", out var currentVersion))
			{
				logger.LogInformation("Current is already using versioned branches {Current}", currentVersion);
				var previousCurrentVersion = new SemVersion(currentVersion.Major, Math.Max(currentVersion.Minor - 1, 0), 0);
				if (v >= currentVersion)
				{
					logger.LogInformation("Speculative build because {Branch} is gte current {Current}", branchOrTag, currentVersion);
					match = match with { Speculative = true };
				}
				else if (v == previousCurrentVersion)
				{
					logger.LogInformation(
						"Speculative build {Branch} is the previous minor '{ProductPreviousMinor}' of current {Current}",
						branchOrTag,
						previousCurrentVersion,
						currentVersion
					);
					match = match with { Speculative = true };
				}
				else
					logger.LogInformation("NO speculative build because {Branch} is lt {Current}", branchOrTag, currentVersion);
			}
			// assume we are newly onboarding the repository to current/next
			else if (product?.VersioningSystem is { } versioningSystem)
			{
				if (!alreadyPublishing)
				{
					logger.LogInformation(
						"Current is not using versioned branches and is not publishing to the registry yet, using product information to determine speculative build is needed"
					);
					var productVersion = versioningSystem.Current;
					var anchoredProductVersion = new SemVersion(productVersion.Major, productVersion.Minor, 0);
					if (v > anchoredProductVersion)
					{
						logger.LogInformation(
							"Speculative build {Branch} is gt product current '{ProductCurrent}' anchored at {ProductAnchored}",
							branchOrTag,
							productVersion,
							anchoredProductVersion
						);
						match = match with { Speculative = true };
					}
					else
						logger.LogInformation(
							"NO speculative build {Branch} is lte product current '{ProductCurrent}'",
							branchOrTag,
							productVersion
						);
				}
				else
					logger.LogInformation(
						"NO speculative build, repository is not using version branches to publish to documentation and is already in the link registry"
					);
			}
			else
				logger.LogInformation(
					"No versioning system found for {Repository} on {Branch}, can not determine speculative build until repository is specified in docs-builder configuration",
					repository,
					branchOrTag
				);
		}

		// if we haven't matched anything yet, and the branch is 'main' or 'master' always build
		if (match is { Current: null, Next: null, Edge: null, Speculative: false } && branchOrTag is "main" or "master")
		{
			return match with { Speculative = true };
		}

		return match;
	}
}

internal static partial class ContentSourceRegex
{
	[GeneratedRegex(@"^\d+\.\d+$", RegexOptions.IgnoreCase)]
	public static partial Regex MatchVersionBranch();
}
