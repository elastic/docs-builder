// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Documentation.Configuration;
using Elastic.Documentation.Diagnostics;
using Elastic.Documentation.LinkIndex;
using Elastic.Documentation.Links;
using Elastic.Markdown.Links.CrossLinks;
using Microsoft.Extensions.Logging;

namespace Elastic.Markdown.Links.InboundLinks;

public class LinkIndexLinkChecker(ILoggerFactory logFactory)
{
	private readonly ILogger _logger = logFactory.CreateLogger<LinkIndexLinkChecker>();
	private readonly ILinkIndexReader _linkIndexProvider = Aws3LinkIndexReader.CreateAnonymous();
	private sealed record RepositoryFilter
	{
		public string? LinksTo { get; init; }
		public string? LinksFrom { get; init; }

		public static RepositoryFilter None => new();
	}

	public async Task CheckAll(IDiagnosticsCollector collector, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logFactory, _linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var resolver = new CrossLinkResolver(crossLinks);

		ValidateCrossLinks(collector, crossLinks, resolver, RepositoryFilter.None);
	}

	public async Task CheckRepository(IDiagnosticsCollector collector, string? toRepository, string? fromRepository, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logFactory, _linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var resolver = new CrossLinkResolver(crossLinks);
		var filter = new RepositoryFilter
		{
			LinksTo = toRepository,
			LinksFrom = fromRepository
		};

		ValidateCrossLinks(collector, crossLinks, resolver, filter);
	}

	public async Task CheckWithLocalLinksJson(IDiagnosticsCollector collector, string repository, string localLinksJson, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logFactory, _linkIndexProvider);
		var crossLinks = await fetcher.FetchCrossLinks(ctx);
		var resolver = new CrossLinkResolver(crossLinks);
		if (string.IsNullOrEmpty(repository))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(localLinksJson))
			throw new ArgumentNullException(nameof(repository));

		_logger.LogInformation("Checking '{Repository}' with local '{LocalLinksJson}'", repository, localLinksJson);

		if (!Path.IsPathRooted(localLinksJson))
			localLinksJson = Path.Combine(Paths.WorkingDirectoryRoot.FullName, localLinksJson);

		try
		{
			var json = await File.ReadAllTextAsync(localLinksJson, ctx);
			var localLinkReference = RepositoryLinks.Deserialize(json);
			crossLinks = resolver.UpdateLinkReference(repository, localLinkReference);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Failed to read {LocalLinksJson}", localLinksJson);
			throw;
		}

		_logger.LogInformation("Validating all cross links to {Repository}:// from all repositories published to link-index.json", repository);
		var filter = new RepositoryFilter
		{
			LinksTo = repository
		};

		ValidateCrossLinks(collector, crossLinks, resolver, filter);
	}

	private void ValidateCrossLinks(
		IDiagnosticsCollector collector,
		FetchedCrossLinks crossLinks,
		CrossLinkResolver resolver,
		RepositoryFilter filter
	)
	{
		foreach (var (repository, linkReference) in crossLinks.LinkReferences)
		{
			if (!string.IsNullOrEmpty(filter.LinksTo))
				_logger.LogInformation("Validating '{CurrentRepository}://' links in {TargetRepository}", filter.LinksTo, repository);
			else if (!string.IsNullOrEmpty(filter.LinksFrom))
			{
				if (repository != filter.LinksFrom)
					continue;
				_logger.LogInformation("Validating cross_links from {TargetRepository}", filter.LinksFrom);
			}
			else
				_logger.LogInformation("Validating all cross_links in {Repository}", repository);

			foreach (var crossLink in linkReference.CrossLinks)
			{
				// if we are filtering, we only want errors from inbound links to a certain repository
				var uri = new Uri(crossLink);
				if (filter.LinksTo != null && uri.Scheme != filter.LinksTo)
					continue;

				var linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{uri.Scheme}/main/links.json";
				if (crossLinks.LinkIndexEntries.TryGetValue(uri.Scheme, out var linkIndexEntry))
					linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/{linkIndexEntry.Path}";
				_ = resolver.TryResolve(s =>
				{
					if (s.Contains("is not a valid link in the"))
					{
						//
						var error = $"'elastic/{repository}' links to unknown file: " + s;
						error = error.Replace("is not a valid link in the", "in the");
						collector.EmitError(linksJson, error);
						return;
					}

					collector.EmitError(repository, s);
				}, uri, out _);
			}
		}
		// non-strict for now
	}
}
