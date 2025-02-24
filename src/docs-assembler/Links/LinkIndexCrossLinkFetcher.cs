// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Links;

public class LinksIndexCrossLinkFetcher(ILoggerFactory logger) : CrossLinkFetcher(logger)
{
	public override async Task<FetchedCrossLinks> Fetch()
	{
		var dictionary = new Dictionary<string, LinkReference>();
		var declaredRepositories = new HashSet<string>();
		var linkIndex = await FetchLinkIndex();
		foreach (var (repository, value) in linkIndex.Repositories)
		{
			var linkIndexEntry = value.First().Value;
			var linkReference = await FetchLinkIndexEntry(repository, linkIndexEntry);
			dictionary.Add(repository, linkReference);
			_ = declaredRepositories.Add(repository);
		}

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = dictionary.ToFrozenDictionary()
		};
	}
}
