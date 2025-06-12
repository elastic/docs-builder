// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.InboundLinks;

public class LinksIndexCrossLinkFetcher(ILinkIndexReader linkIndexProvider, ILoggerFactory logger) : CrossLinkFetcher(linkIndexProvider, logger)
{
	public override async Task<FetchedCrossLinks> Fetch(Cancel ctx)
	{
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var linkEntries = new Dictionary<string, LinkRegistryEntry>();
		var declaredRepositories = new HashSet<string>();
		var linkIndex = await FetchLinkIndex(ctx);
		foreach (var (repository, value) in linkIndex.Repositories)
		{
			var linkIndexEntry = GetNextContentSourceLinkIndexEntry(value, repository);

			linkEntries.Add(repository, linkIndexEntry);
			var linkReference = await FetchLinkIndexEntry(repository, linkIndexEntry, ctx);
			linkReferences.Add(repository, linkReference);
			_ = declaredRepositories.Add(repository);
		}

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = linkReferences.ToFrozenDictionary(),
			LinkIndexEntries = linkEntries.ToFrozenDictionary(),
			FromConfiguration = false
		};
	}

}
