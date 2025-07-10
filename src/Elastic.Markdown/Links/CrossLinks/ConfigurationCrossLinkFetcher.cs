// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.CrossLinks;

public class ConfigurationCrossLinkFetcher(ILoggerFactory logFactory, ConfigurationFile configuration, ILinkIndexReader linkIndexProvider) : CrossLinkFetcher(logFactory, linkIndexProvider)
{
	public override async Task<FetchedCrossLinks> Fetch(Cancel ctx)
	{
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var linkIndexEntries = new Dictionary<string, LinkRegistryEntry>();
		var declaredRepositories = new HashSet<string>();
		foreach (var repository in configuration.CrossLinkRepositories)
		{
			_ = declaredRepositories.Add(repository);
			var linkReference = await Fetch(repository, ["main", "master"], ctx);
			linkReferences.Add(repository, linkReference);
			var linkIndexReference = await GetLinkIndexEntry(repository, ctx);
			linkIndexEntries.Add(repository, linkIndexReference);
		}

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = linkReferences.ToFrozenDictionary(),
			LinkIndexEntries = linkIndexEntries.ToFrozenDictionary(),
			FromConfiguration = true
		};
	}


}
