// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.CrossLinks;

/// Fetches cross-links from all the declared repositories in the docset.yml configuration see <see cref="ConfigurationFile"/>
public class DocSetConfigurationCrossLinkFetcher(ILoggerFactory logFactory, ConfigurationFile configuration, ILinkIndexReader? linkIndexProvider = null)
	: CrossLinkFetcher(logFactory, linkIndexProvider ?? Aws3LinkIndexReader.CreateAnonymous())
{
	private readonly ILogger _logger = logFactory.CreateLogger(nameof(DocSetConfigurationCrossLinkFetcher));
	private FetchedCrossLinks? _cachedLinks;

	public override async Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx)
	{
		if (_cachedLinks is not null)
			return _cachedLinks;

		Logger.LogInformation("Fetching cross-links for all repositories defined in docset.yml");
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var linkIndexEntries = new Dictionary<string, LinkRegistryEntry>();
		var declaredRepositories = new HashSet<string>();

		foreach (var repository in configuration.CrossLinkRepositories)
		{
			_ = declaredRepositories.Add(repository);
			try
			{
				var linkReference = await FetchCrossLinks(repository, ["main", "master"], ctx);
				linkReferences.Add(repository, linkReference);

				var linkIndexReference = await GetLinkIndexEntry(repository, ctx);
				linkIndexEntries.Add(repository, linkIndexReference);
			}
			catch (Exception ex)
			{
				// Log the error but continue processing other repositories
				_logger.LogWarning(ex, "Error fetching link data for repository '{Repository}'. Cross-links to this repository may not resolve correctly.", repository);

				// Add an empty entry so we at least recognize the repository exists
				if (!linkReferences.ContainsKey(repository))
				{
					linkReferences.Add(repository, new RepositoryLinks
					{
						Links = [],
						Origin = new GitCheckoutInformation
						{
							Branch = "main",
							RepositoryName = repository,
							Remote = "origin",
							Ref = "refs/heads/main"
						},
						UrlPathPrefix = "",
						CrossLinks = []
					});
				}
			}
		}

		_cachedLinks = new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = linkReferences.ToFrozenDictionary(),
			LinkIndexEntries = linkIndexEntries.ToFrozenDictionary(),
		};
		return _cachedLinks;
	}


}
