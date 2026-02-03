// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Links;

/// fetches all the cross-links for all repositories defined in assembler.yml configuration <see cref="AssemblyConfiguration"/>
public class AssemblerCrossLinkFetcher(ILoggerFactory logFactory, AssemblyConfiguration configuration, PublishEnvironment publishEnvironment, ILinkIndexReader linkIndexProvider)
	: CrossLinkFetcher(logFactory, linkIndexProvider)
{
	public override async Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx)
	{
		Logger.LogInformation("Fetching cross-links for all repositories defined in assembler.yml");

		// We do want to always fetch cross-link data for all repositories.
		// This is public information
		var repositories = configuration.AvailableRepositories.Values
			.Concat(configuration.PrivateRepositories.Values)
			.ToList();

		// Deduplicate and filter skipped repos
		var declaredRepositories = new HashSet<string>();
		var reposToFetch = new List<(string Name, string Branch)>();
		foreach (var repository in repositories)
		{
			if (declaredRepositories.Contains(repository.Name))
				continue;

			_ = declaredRepositories.Add(repository.Name);

			if (repository.Skip)
				continue;

			var branch = repository.GetBranch(publishEnvironment.ContentSource);
			reposToFetch.Add((repository.Name, branch));
		}

		// Fetch all cross-links in parallel
		var tasks = reposToFetch.Select(async repo =>
		{
			var linkReference = await FetchCrossLinks(repo.Name, [repo.Branch], ctx);
			var linkIndexEntry = await GetLinkIndexEntry(repo.Name, ctx);
			return (repo.Name, linkReference, linkIndexEntry);
		});

		var results = await Task.WhenAll(tasks);

		var linkReferences = results.ToDictionary(r => r.Name, r => r.linkReference);
		var linkIndexEntries = results.ToDictionary(r => r.Name, r => r.linkIndexEntry);

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkIndexEntries = linkIndexEntries.ToFrozenDictionary(),
			LinkReferences = linkReferences.ToFrozenDictionary(),
		};
	}
}
