// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Frozen;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.Builder;
using Elastic.Documentation.LinkIndex;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Links.CrossLinks;

/// Fetches cross-links from all the declared repositories in the docset.yml configuration see <see cref="ConfigurationFile"/>
public class DocSetConfigurationCrossLinkFetcher(
	ILoggerFactory logFactory,
	ConfigurationFile configuration,
	ILinkIndexReader? linkIndexProvider = null,
	ILinkIndexReader? codexLinkIndexReader = null)
	: CrossLinkFetcher(logFactory, linkIndexProvider ?? Aws3LinkIndexReader.CreateAnonymous())
{
	private readonly ILogger _logger = logFactory.CreateLogger(nameof(DocSetConfigurationCrossLinkFetcher));
	private readonly ILinkIndexReader? _codexReader = codexLinkIndexReader;

	public override async Task<FetchedCrossLinks> FetchCrossLinks(Cancel ctx)
	{
		Logger.LogInformation("Fetching cross-links for all repositories defined in docset.yml");
		var linkReferences = new Dictionary<string, RepositoryLinks>();
		var linkIndexEntries = new Dictionary<string, LinkRegistryEntry>();
		var registryUrlsByRepository = new Dictionary<string, string>();
		var codexRepositories = new HashSet<string>();
		var declaredRepositories = new HashSet<string>();

		var publicReader = linkIndexProvider ?? Aws3LinkIndexReader.CreateAnonymous();
		var useDualRegistry = configuration.Registry != DocSetRegistry.Public && _codexReader is not null;

		foreach (var entry in configuration.CrossLinkEntries)
		{
			_ = declaredRepositories.Add(entry.Repository);
			var isCodexEntry = useDualRegistry && entry.Registry != DocSetRegistry.Public;
			var reader = isCodexEntry ? _codexReader! : publicReader;

			if (isCodexEntry)
				_ = codexRepositories.Add(entry.Repository);

			try
			{
				var linkReference = await FetchCrossLinksFromReader(reader, entry.Repository, this, ctx);
				linkReferences.Add(entry.Repository, linkReference);
				registryUrlsByRepository[entry.Repository] = reader.RegistryUrl;

				var registry = await reader.GetRegistry(ctx);
				if (registry.Repositories.TryGetValue(entry.Repository, out var repoBranches))
				{
					var linkIndexEntry = GetNextContentSourceLinkIndexEntry(repoBranches, entry.Repository);
					linkIndexEntries.Add(entry.Repository, linkIndexEntry);
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Error fetching link data for repository '{Repository}'. Cross-links to this repository may not resolve correctly.", entry.Repository);
				_ = registryUrlsByRepository.TryAdd(entry.Repository, reader.RegistryUrl);

				if (!linkReferences.ContainsKey(entry.Repository))
				{
					linkReferences.Add(entry.Repository, new RepositoryLinks
					{
						Links = [],
						Origin = new GitCheckoutInformation
						{
							Branch = "main",
							RepositoryName = entry.Repository,
							Remote = "origin",
							Ref = "refs/heads/main"
						},
						UrlPathPrefix = "",
						CrossLinks = []
					});
				}
			}
		}

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = linkReferences.ToFrozenDictionary(),
			LinkIndexEntries = linkIndexEntries.ToFrozenDictionary(),
			RegistryUrlsByRepository = registryUrlsByRepository.ToFrozenDictionary(),
			CodexRepositories = codexRepositories.Count > 0 ? codexRepositories.ToFrozenSet() : null,
		};
	}
}
