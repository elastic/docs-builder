// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using Elastic.Documentation.Assembler.Links;
using Elastic.Documentation.Configuration;
using Elastic.Documentation.Configuration.DocSet;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Documentation.Links.CrossLinks;
using Elastic.Documentation.Links.InboundLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Documentation.Assembler.Navigation;

/// <summary>
/// Validates paths don't conflict with global navigation namespaces.
/// For example, if the global navigation defines:
/// <code>
/// - toc: elasticsearch://reference/elasticsearch
///   path_prefix: reference/elasticsearch
///
/// - toc: docs-content://reference/elasticsearch/clients
///   path_prefix: reference/elasticsearch/clients
/// </code>
///
/// This will validate `elasticsearch://` does not create an `elasticsearch://reference/elasticsearch/clients` folder
/// since that is already claimed by `docs-content://reference/elasticsearch/clients`
///
/// </summary>
public class NavigationPrefixChecker
{
	private readonly ILogger _logger;
	private readonly PublishEnvironmentUriResolver _uriResolver;
	private readonly ILoggerFactory _logFactoryFactory;
	private readonly ImmutableHashSet<string> _repositories;
	private readonly ImmutableHashSet<Uri> _phantoms;

	/// <inheritdoc cref="NavigationPrefixChecker"/>
	public NavigationPrefixChecker(ILoggerFactory logFactory, AssembleContext context)
	{
		var navigationFileInfo = context.ConfigurationFileProvider.NavigationFile;
		var navigationYaml = context.ReadFileSystem.File.ReadAllText(navigationFileInfo.FullName);
		var siteNavigationFile = SiteNavigationFile.Deserialize(navigationYaml);

		_phantoms = SiteNavigationFile.GetPhantomPrefixes(siteNavigationFile);

		_repositories = context.Configuration.AvailableRepositories.Values
			.Select(r => r.Name)
			.ToImmutableHashSet();

		_logger = logFactory.CreateLogger<NavigationPrefixChecker>();
		_logFactoryFactory = logFactory;

		var tocTopLevelMappings = AssembleSources.GetTocMappings(context);
		_uriResolver = new PublishEnvironmentUriResolver(tocTopLevelMappings, context.Environment);
	}

	private sealed record SeenPaths
	{
		public required string Repository { get; init; }
		public required string Path { get; init; }
	}

	public async Task CheckWithLocalLinksJson(IDiagnosticsCollector collector, string repository, string? localLinksJson, CancellationToken ctx)
	{
		if (string.IsNullOrEmpty(repository))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(localLinksJson))
			throw new ArgumentNullException(nameof(localLinksJson));

		_logger.LogInformation("Checking '{Repository}' with local '{LocalLinksJson}'", repository, localLinksJson);

		if (!Path.IsPathRooted(localLinksJson))
			localLinksJson = Path.Combine(Paths.WorkingDirectoryRoot.FullName, localLinksJson);

		var linkReference = await ReadLocalLinksJsonAsync(localLinksJson, ctx);
		await FetchAndValidateCrossLinks(collector, repository, linkReference, ctx);
	}

	public async Task CheckAllPublishedLinks(IDiagnosticsCollector collector, Cancel ctx) =>
		await FetchAndValidateCrossLinks(collector, null, null, ctx);

	private async Task FetchAndValidateCrossLinks(IDiagnosticsCollector collector, string? updateRepository, RepositoryLinks? updateReference, Cancel ctx)
	{
		var linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
		var fetcher = new LinksIndexCrossLinkFetcher(_logFactoryFactory, linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var crossLinkResolver = new CrossLinkResolver(crossLinks);
		var dictionary = new Dictionary<string, SeenPaths>();
		if (!string.IsNullOrEmpty(updateRepository) && updateReference is not null)
			crossLinks = crossLinkResolver.UpdateLinkReference(updateRepository, updateReference);

		var skippedPhantoms = 0;
		foreach (var (repository, linkReference) in crossLinks.LinkReferences)
		{
			if (!_repositories.Contains(repository))
				continue;

			_logger.LogInformation("Validating '{Repository}'", repository);
			// Todo publish all relative folders as part of the link reference
			// That way we don't need to iterate over all links and find all permutations of their relative paths
			foreach (var (relativeLink, _) in linkReference.Links)
			{
				var crossLink = new Uri($"{repository}://{relativeLink.TrimEnd('/')}");
				var navigationPaths = _uriResolver.ResolveToSubPaths(crossLink, relativeLink);
				if (navigationPaths.Length == 0)
				{
					var path = relativeLink.Split('/').SkipLast(1);
					var pathUri = new Uri($"{repository}://{string.Join('/', path)}");

					var baseOfAPhantom = _phantoms.Any(p => p == pathUri);
					if (baseOfAPhantom)
					{
						skippedPhantoms++;
						if (skippedPhantoms > _phantoms.Count * 3)
							collector.EmitError(repository, $"Too many items are being marked as part of a phantom this looks like a bug. ({skippedPhantoms})");
						continue;
					}
					collector.EmitError(repository, $"'Can not validate '{crossLink}' it's not declared in any link reference nor is it a phantom");
					continue;
				}
				foreach (var navigationPath in navigationPaths)
				{
					if (dictionary.TryGetValue(navigationPath, out var seen))
					{
						if (seen.Repository == repository)
							continue;
						if (_phantoms.Count > 0 && _phantoms.Contains(new Uri($"{repository}://{navigationPath}")))
							continue;

						var url = _uriResolver.Resolve(new Uri($"{repository}://{relativeLink}"), PublishEnvironmentUriResolver.MarkdownPathToUrlPath(relativeLink));
						collector.EmitError(repository,
							$"'{seen.Repository}' defines: '{seen.Path}' that '{repository}://{relativeLink} resolving to '{url.AbsolutePath}' conflicts with ");
					}
					else
					{
						if (_phantoms.Count > 0 && _phantoms.Contains(new Uri($"{repository}://{navigationPath}")))
							continue;

						dictionary.Add(navigationPath, new SeenPaths
						{
							Repository = repository,
							Path = navigationPath
						});
					}
				}
			}
		}
	}

	private async Task<RepositoryLinks> ReadLocalLinksJsonAsync(string localLinksJson, Cancel ctx)
	{
		try
		{
			var json = await File.ReadAllTextAsync(localLinksJson, ctx);
			return RepositoryLinks.Deserialize(json);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read {LocalLinksJson}", localLinksJson);
			throw;
		}
	}
}
