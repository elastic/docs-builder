// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.Links;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.CrossLinks;

public class ConfigurationCrossLinkFetcher(ConfigurationFile configuration, ILoggerFactory logger) : CrossLinkFetcher(logger)
{
	public override async Task<FetchedCrossLinks> Fetch(Cancel ctx)
	{
		var linkReferences = new Dictionary<string, LinkReference>();
		var linkIndexEntries = new Dictionary<string, LinkRegistryEntry>();
		var declaredRepositories = new HashSet<string>();
		foreach (var repository in configuration.CrossLinkRepositories)
		{
			_ = declaredRepositories.Add(repository);
			try
			{
				var linkReference = await Fetch(repository, ["main", "master"], ctx);
				linkReferences.Add(repository, linkReference);
				var linkIndexReference = await GetLinkIndexEntry(repository, ctx);
				linkIndexEntries.Add(repository, linkIndexReference);
			}
			catch when (repository == "docs-content")
			{
				throw;
			}
			catch when (repository != "docs-content")
			{
				// TODO: ignored for now while we wait for all links.json files to populate
			}
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
