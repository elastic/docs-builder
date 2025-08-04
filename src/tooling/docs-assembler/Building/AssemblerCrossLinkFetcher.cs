// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public class AssemblerCrossLinkFetcher(ILoggerFactory logFactory, AssemblyConfiguration configuration, PublishEnvironment publishEnvironment, ILinkIndexReader linkIndexProvider)
	: CrossLinkFetcher(logFactory, linkIndexProvider)
{
	public override async Task<FetchedCrossLinks> Fetch(Cancel ctx)
	{
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var linkIndexEntries = new Dictionary<string, LinkRegistryEntry>();
		var declaredRepositories = new HashSet<string>();
		// We do want to always fetch cross-link data for all repositories.
		// This is public information
		var repositories = configuration.AvailableRepositories.Values
			.Concat(configuration.PrivateRepositories.Values)
			.ToList();

		foreach (var repository in repositories)
		{
			var repositoryName = repository.Name;
			_ = declaredRepositories.Add(repositoryName);

			if (repository.Skip)
				continue;

			var branch = repository.GetBranch(publishEnvironment.ContentSource);

			var linkReference = await Fetch(repositoryName, [branch], ctx);
			linkReferences.Add(repositoryName, linkReference);
			var linkIndexReference = await GetLinkIndexEntry(repositoryName, ctx);
			linkIndexEntries.Add(repositoryName, linkIndexReference);
		}

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkIndexEntries = linkIndexEntries.ToFrozenDictionary(),
			LinkReferences = linkReferences.ToFrozenDictionary(),
			FromConfiguration = false
		};
	}
}
