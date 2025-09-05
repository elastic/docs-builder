// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration.Assembler;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.InboundLinks;

/// fetches cross-links for all the repositories defined in the publicized link-index.json file using the <see cref="ContentSource.Next"/> content source
public class LinksIndexCrossLinkFetcher(ILoggerFactory logFactory, ILinkIndexReader linkIndexProvider) : CrossLinkFetcher(logFactory, linkIndexProvider)
{
	public override async Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx)
	{
		Logger.LogInformation("Fetching cross-links for all repositories defined in publicized link-index.json link index registry");
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var linkEntries = new Dictionary<string, LinkRegistryEntry>();
		var declaredRepositories = new HashSet<string>();
		var linkIndex = await FetchLinkRegistry(ctx);
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
		};
	}

}
