// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Documentation.Assembler.Building;
using Documentation.Assembler.Configuration;
using Documentation.Assembler.Navigation;
using Elastic.Markdown.Diagnostics;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Elastic.Markdown.Links.CrossLinks;
using Elastic.Markdown.Links.InboundLinks;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Links;

/// <summary>
/// Validates paths don't conflict with global navigation namespaces.
/// For example if the global navigation defines:
/// <code>
/// - toc: elasticsearch://reference/elasticsearch
///   path_prefix: reference/elasticsearch
///
/// - toc: docs-content://reference/elasticsearch/clients
///   path_prefix: reference/elasticsearch/clients
/// </code>
///
/// This will validate `elasticsearch://` does not create a `elasticsearch://reference/elasticsearch/clients` folder
/// since that is already claimed by `docs-content://reference/elasticsearch/clients`
///
/// </summary>
public class NavigationPrefixChecker
{
	private readonly ILogger _logger;
	private readonly PublishEnvironmentUriResolver _uriResolver;
	private readonly ILoggerFactory _loggerFactory;
	private readonly ImmutableHashSet<string> _repositories;
	private readonly ImmutableHashSet<Uri> _phantoms;

	/// <inheritdoc cref="NavigationPrefixChecker"/>
	public NavigationPrefixChecker(ILoggerFactory logger, AssembleContext context)
	{
		_phantoms = GlobalNavigationFile.GetPhantomPrefixes(context);

		_repositories = context.Configuration.ReferenceRepositories
			.Values
			.Concat([context.Configuration.Narrative])
			.Where(p => !p.Skip)
			.Select(r => r.Name)
			.ToImmutableHashSet();

		_logger = logger.CreateLogger<NavigationPrefixChecker>();
		_loggerFactory = logger;

		var tocTopLevelMappings = AssembleSources.GetConfiguredSources(context);
		_uriResolver = new PublishEnvironmentUriResolver(tocTopLevelMappings, context.Environment);
	}

	private sealed record SeenPaths
	{
		public required string Repository { get; init; }
		public required string Path { get; init; }
	}

	public async Task CheckWithLocalLinksJson(DiagnosticsCollector collector, string repository, string? localLinksJson, CancellationToken ctx)
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

	public async Task CheckAllPublishedLinks(DiagnosticsCollector collector, Cancel ctx) =>
		await FetchAndValidateCrossLinks(collector, null, null, ctx);

	private async Task FetchAndValidateCrossLinks(DiagnosticsCollector collector, string? updateRepository, LinkReference? updateReference, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(_loggerFactory);
		var resolver = new CrossLinkResolver(fetcher);
		var crossLinks = await resolver.FetchLinks(ctx);
		var dictionary = new Dictionary<string, SeenPaths>();
		if (!string.IsNullOrEmpty(updateRepository) && updateReference is not null)
			crossLinks = resolver.UpdateLinkReference(updateRepository, updateReference);
		foreach (var (repository, linkReference) in crossLinks.LinkReferences)
		{
			if (!_repositories.Contains(repository))
				continue;

			// Todo publish all relative folders as part of the link reference
			// That way we don't need to iterate over all links and find all permutations of their relative paths
			foreach (var (relativeLink, _) in linkReference.Links)
			{
				var navigationPaths = _uriResolver.ResolveToSubPaths(new Uri($"{repository}://{relativeLink}"), relativeLink);
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

	private async Task<LinkReference> ReadLocalLinksJsonAsync(string localLinksJson, Cancel ctx)
	{
		try
		{
			var json = await File.ReadAllTextAsync(localLinksJson, ctx);
			return LinkReference.Deserialize(json);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read {LocalLinksJson}", localLinksJson);
			throw;
		}
	}
}
