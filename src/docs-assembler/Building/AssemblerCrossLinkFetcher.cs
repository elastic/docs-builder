// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Documentation.Assembler.Configuration;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Building;

public class AssemblerCrossLinkFetcher(ILoggerFactory logger, AssemblyConfiguration configuration) : CrossLinkFetcher(logger)
{
	public override async Task<FetchedCrossLinks> Fetch(Cancel ctx)
	{
		var linkReferences = new Dictionary<string, LinkReference>();
		var linkIndexEntries = new Dictionary<string, LinkIndexEntry>();
		var declaredRepositories = new HashSet<string>();
		var repositories = configuration.ReferenceRepositories.Values.Concat<Repository>([configuration.Narrative]);

		foreach (var repository in repositories)
		{
			var repositoryName = repository.Name;
			_ = declaredRepositories.Add(repositoryName);

			if (repository.Skip)
				continue;

			var linkReference = await Fetch(repositoryName, ctx);
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
