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
		var registryByRepository = new Dictionary<string, DocSetRegistry>();
		var fetchFailures = new Dictionary<string, string>();
		var codexRepositories = new HashSet<string>();
		var declaredRepositories = new HashSet<string>();

		var publicReader = linkIndexProvider ?? Aws3LinkIndexReader.CreateAnonymous();
		var useDualRegistry = configuration.Registry != DocSetRegistry.Public && _codexReader is not null;

		// Fetch each registry once up front so per-repository lookups don't trigger N S3 round-trips.
		var (publicRegistry, publicRegistryFailure) = await TryGetRegistry(publicReader, ctx);
		LinkRegistry? codexRegistry = null;
		string? codexRegistryFailure = null;
		if (useDualRegistry)
			(codexRegistry, codexRegistryFailure) = await TryGetRegistry(_codexReader!, ctx);

		foreach (var entry in configuration.CrossLinkEntries)
		{
			_ = declaredRepositories.Add(entry.Repository);
			registryByRepository[entry.Repository] = entry.Registry;
			var isCodexEntry = useDualRegistry && entry.Registry != DocSetRegistry.Public;
			var reader = isCodexEntry ? _codexReader! : publicReader;
			var registry = isCodexEntry ? codexRegistry : publicRegistry;
			var registryFailure = isCodexEntry ? codexRegistryFailure : publicRegistryFailure;
			registryUrlsByRepository[entry.Repository] = reader.RegistryUrl;

			if (isCodexEntry)
				_ = codexRepositories.Add(entry.Repository);

			if (registry is null)
			{
				fetchFailures[entry.Repository] =
					registryFailure ?? $"Failed to fetch link index registry from {reader.RegistryUrl}";
			}
			else
			{
				try
				{
					if (!registry.Repositories.TryGetValue(entry.Repository, out var repoBranches))
						throw new Exception($"Repository {entry.Repository} not found in link index");

					var linkIndexEntry = GetNextContentSourceLinkIndexEntry(repoBranches, entry.Repository);
					var linkReference = await FetchLinkIndexEntryFromReader(reader, entry.Repository, linkIndexEntry, ctx);

					linkReferences.Add(entry.Repository, linkReference);
					linkIndexEntries.Add(entry.Repository, linkIndexEntry);
				}
				catch (Exception ex)
				{
					fetchFailures[entry.Repository] = ex.Message;
					_logger.LogWarning(ex, "Error fetching link data for repository '{Repository}'. Cross-links to this repository may not resolve correctly.", entry.Repository);
				}
			}

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

		return new FetchedCrossLinks
		{
			DeclaredRepositories = declaredRepositories,
			LinkReferences = linkReferences.ToFrozenDictionary(),
			LinkIndexEntries = linkIndexEntries.ToFrozenDictionary(),
			RegistryUrlsByRepository = registryUrlsByRepository.ToFrozenDictionary(),
			RegistryByRepository = registryByRepository.ToFrozenDictionary(),
			CodexRepositories = codexRepositories.Count > 0 ? codexRepositories.ToFrozenSet() : null,
			FetchFailures = fetchFailures.ToFrozenDictionary(),
		};
	}

	private async Task<(LinkRegistry? Registry, string? FailureReason)> TryGetRegistry(ILinkIndexReader reader, Cancel ctx)
	{
		try
		{
			return (await reader.GetRegistry(ctx), null);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to fetch link index registry from {RegistryUrl}", reader.RegistryUrl);
			return (null, ex.Message);
		}
	}
}
