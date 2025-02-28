// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Actions.Core.Services;
using Elastic.Documentation.Tooling.Diagnostics.Console;
using Elastic.Markdown.CrossLinks;
using Elastic.Markdown.IO;
using Elastic.Markdown.IO.State;
using Microsoft.Extensions.Logging;

namespace Documentation.Assembler.Links;

public class LinkIndexLinkChecker(ILoggerFactory logger, ICoreService githubActionsService)
{
	private readonly ILogger _logger = logger.CreateLogger<LinkIndexLinkChecker>();

	private sealed record RepositoryFilter
	{
		public string? LinksTo { get; set; }
		public string? LinksFrom { get; set; }

		public static RepositoryFilter None => new();
	}

	public async Task<int> CheckAll(Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logger);
		var resolver = new CrossLinkResolver(fetcher);
		//todo add ctx
		var crossLinks = await resolver.FetchLinks();

		return await ValidateCrossLinks(crossLinks, resolver, RepositoryFilter.None, ctx);
	}

	public async Task<int> CheckRepository(string? toRepository, string? fromRepository, Cancel ctx)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logger);
		var resolver = new CrossLinkResolver(fetcher);
		//todo add ctx
		var crossLinks = await resolver.FetchLinks();
		var filter = new RepositoryFilter
		{
			LinksTo = toRepository,
			LinksFrom = fromRepository
		};

		return await ValidateCrossLinks(crossLinks, resolver, filter, ctx);
	}

	public async Task<int> CheckWithLocalLinksJson(
		string repository,
		string localLinksJson,
		Cancel ctx
	)
	{
		var fetcher = new LinksIndexCrossLinkFetcher(logger);
		var resolver = new CrossLinkResolver(fetcher);
		// ReSharper disable once RedundantAssignment
		var crossLinks = await resolver.FetchLinks();

		if (string.IsNullOrEmpty(repository))
			throw new ArgumentNullException(nameof(repository));
		if (string.IsNullOrEmpty(localLinksJson))
			throw new ArgumentNullException(nameof(repository));

		_logger.LogInformation("Checking '{Repository}' with local '{LocalLinksJson}'", repository, localLinksJson);

		if (!Path.IsPathRooted(localLinksJson))
			localLinksJson = Path.Combine(Paths.Root.FullName, localLinksJson);

		try
		{
			var json = await File.ReadAllTextAsync(localLinksJson, ctx);
			var localLinkReference = LinkReference.Deserialize(json);
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

		return await ValidateCrossLinks(crossLinks, resolver, filter, ctx);
	}

	private async Task<int> ValidateCrossLinks(
		FetchedCrossLinks crossLinks,
		CrossLinkResolver resolver,
		RepositoryFilter filter,
		Cancel ctx)
	{
		var collector = new ConsoleDiagnosticsCollector(logger, githubActionsService);
		_ = collector.StartAsync(ctx);
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
				// if we are filtering we only want errors from inbound links to a certain
				// repository
				var uri = new Uri(crossLink);
				if (filter.LinksTo != null && uri.Scheme != filter.LinksTo)
					continue;

				var linksJson = $"https://elastic-docs-link-index.s3.us-east-2.amazonaws.com/elastic/{uri.Scheme}/main/links.json";
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
				}, s => collector.EmitWarning(linksJson, s), uri, out _);
			}
		}

		collector.Channel.TryComplete();
		await collector.StopAsync(ctx);
		// non-strict for now
		return collector.Errors;
		// return collector.Errors + collector.Warnings;
	}
}
